import type { CostRow, RequestRow } from './api';
import { useGetCostsQuery, useGetHealthQuery, useGetRequestsQuery } from './api';
import styles from './App.module.css';

const money = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 6
});

const integer = new Intl.NumberFormat('en-US');

function formatDuration(microseconds: number): string {
  if (microseconds < 1_000) {
    return `${microseconds.toFixed(0)} us`;
  }

  return `${(microseconds / 1_000).toFixed(1)} ms`;
}

function formatTtft(microseconds: number | null): string {
  return microseconds === null ? '-' : formatDuration(microseconds);
}

function formatStatus(status: string): string {
  return status.toLowerCase();
}

function SummaryCard({
  label,
  value,
  hint
}: {
  label: string;
  value: string;
  hint: string;
}) {
  return (
    <div className={styles.card}>
      <div className={styles.cardLabel}>{label}</div>
      <div className={styles.cardValue}>{value}</div>
      <div className={styles.cardHint}>{hint}</div>
    </div>
  );
}

function CostsTable({ rows }: { rows: CostRow[] }) {
  if (rows.length === 0) {
    return (
      <div className={styles.empty}>
        No cost records yet. Run the RAG sample, then refresh this page.
      </div>
    );
  }

  return (
    <div className={styles.tableScroll}>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>Provider</th>
            <th>Model</th>
            <th className={styles.numeric}>Requests</th>
            <th className={styles.numeric}>Priced</th>
            <th className={styles.numeric}>Partial</th>
            <th className={styles.numeric}>Unknown</th>
            <th className={styles.numeric}>Total</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={`${row.provider}/${row.model}`}>
              <td className={styles.mono}>{row.provider}</td>
              <td className={styles.mono}>{row.model}</td>
              <td className={styles.numeric}>{integer.format(row.requests)}</td>
              <td className={styles.numeric}>{integer.format(row.priced)}</td>
              <td className={styles.numeric}>{integer.format(row.partial)}</td>
              <td className={styles.numeric}>{integer.format(row.unknown)}</td>
              <td className={`${styles.numeric} ${styles.cost}`}>{money.format(row.total)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function RequestsTable({ rows }: { rows: RequestRow[] }) {
  if (rows.length === 0) {
    return <div className={styles.empty}>No requests captured yet.</div>;
  }

  return (
    <div className={styles.tableScroll}>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>Started (UTC)</th>
            <th>Operation</th>
            <th>Model</th>
            <th className={styles.numeric}>Duration</th>
            <th className={styles.numeric}>TTFT</th>
            <th className={styles.numeric}>Cost</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.requestId}>
              <td className={styles.mono}>{row.startedAtUtc.replace('T', ' ').slice(0, 19)}</td>
              <td>{row.operation}</td>
              <td className={styles.mono}>{row.model}</td>
              <td className={styles.numeric}>{formatDuration(row.durationUs)}</td>
              <td className={styles.numeric}>{formatTtft(row.ttftUs)}</td>
              <td className={`${styles.numeric} ${styles.cost}`}>
                {row.cost === null ? <span className={styles.muted}>{row.costStatus.toLowerCase()}</span> : money.format(row.cost)}
              </td>
              <td>
                <span className={row.status === 'Ok' ? styles.statusOk : styles.statusBad}>
                  {formatStatus(row.status)}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function App() {
  const hasToken = Boolean(sessionStorage.getItem('runesvaner-token'));
  const health = useGetHealthQuery(undefined, { skip: !hasToken });
  const costs = useGetCostsQuery(undefined, { skip: !hasToken });
  const requests = useGetRequestsQuery(20, { skip: !hasToken });

  const rows = costs.data ?? [];
  const total = rows.reduce((sum, row) => sum + row.total, 0);
  const priced = rows.reduce((sum, row) => sum + row.priced, 0);
  const unknown = rows.reduce((sum, row) => sum + row.unknown + row.partial, 0);
  const models = rows.length;
  const isLoading = health.isLoading || costs.isLoading || requests.isLoading;
  const hasError = health.isError || costs.isError || requests.isError;

  return (
    <div className={styles.shell}>
      <header className={styles.topbar}>
        <div className={styles.brand}>
          <span className={styles.mark} aria-hidden="true" />
          <span>Runesvaner</span>
          <span className={styles.brandMuted}>Studio</span>
        </div>
        <div className={styles.meta}>
          <span>Local</span>
          <span className={styles.metaDivider} />
          <span>Read-only</span>
          {health.data ? (
            <>
              <span className={styles.metaDivider} />
              <span className={styles.path}>{health.data.dataPath}</span>
            </>
          ) : null}
        </div>
      </header>

      <main className={styles.main}>
        <div className={styles.titleRow}>
          <div>
            <div className={styles.kicker}>Cost ledger</div>
            <h1 className={styles.title}>AI spend by provider and model</h1>
          </div>
          <div className={styles.refresh}>{isLoading ? 'Loading' : 'Live snapshot'}</div>
        </div>

        {!hasToken ? (
          <div className={styles.notice}>
            <strong>Local token required.</strong> Reopen the URL printed by Runesvaner Studio.
          </div>
        ) : null}

        {hasError ? (
          <div className={styles.notice}>
            <strong>Request failed.</strong> The token may be invalid, or the Studio API is unavailable.
          </div>
        ) : null}

        <section className={styles.summaryGrid} aria-label="Cost summary">
          <SummaryCard label="Total cost" value={money.format(total)} hint="Known price records only" />
          <SummaryCard label="Priced requests" value={integer.format(priced)} hint="Matched a price table entry" />
          <SummaryCard label="Unknown / partial" value={integer.format(unknown)} hint="Never guessed" />
          <SummaryCard label="Provider / model pairs" value={integer.format(models)} hint="Attribution groups" />
        </section>

        <section className={styles.panel}>
          <div className={styles.panelHeader}>
            <div>
              <div className={styles.kicker}>Attribution</div>
              <h2 className={styles.panelTitle}>Cost by provider and model</h2>
            </div>
            <span className={styles.panelMeta}>{rows.length} groups</span>
          </div>
          <CostsTable rows={rows} />
        </section>

        <section className={styles.panel}>
          <div className={styles.panelHeader}>
            <div>
              <div className={styles.kicker}>Recent activity</div>
              <h2 className={styles.panelTitle}>Latest requests</h2>
            </div>
            <span className={styles.panelMeta}>20 newest</span>
          </div>
          <RequestsTable rows={requests.data ?? []} />
        </section>

        <footer className={styles.footer}>
          Read-only local session. Request content is not displayed; data stays on this machine.
        </footer>
      </main>
    </div>
  );
}
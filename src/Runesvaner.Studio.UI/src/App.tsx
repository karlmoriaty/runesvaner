import { useState } from 'react';
import type { CostRow, RequestDetail, RequestRow, Segment } from './api';
import {
  useGetCostsQuery,
  useGetHealthQuery,
  useGetRequestQuery,
  useGetRequestsQuery
} from './api';
import styles from './App.module.css';

const integer = new Intl.NumberFormat('en-US');

function formatMoney(value: number | null, currency = 'USD'): string {
  if (value === null) {
    return '-';
  }

  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: 6
  }).format(value);
}

function formatDuration(microseconds: number | null): string {
  if (microseconds === null) {
    return '-';
  }

  if (microseconds < 1_000) {
    return `${microseconds.toFixed(0)} us`;
  }

  return `${(microseconds / 1_000).toFixed(1)} ms`;
}

function formatSegmentLabel(kind: string): string {
  return kind.replace(/([a-z])([A-Z])/g, '$1 $2');
}

function segmentTone(kind: string): string {
  switch (kind) {
    case 'Queue':
      return styles.barQueue;
    case 'Auth':
      return styles.barAuth;
    case 'Connect':
      return styles.barConnect;
    case 'Tls':
      return styles.barTls;
    case 'Upload':
      return styles.barUpload;
    case 'ProviderQueue':
      return styles.barProviderQueue;
    case 'Ttft':
      return styles.barTtft;
    case 'Generation':
      return styles.barGeneration;
    case 'Embedding':
      return styles.barEmbedding;
    case 'Tool':
      return styles.barTool;
    case 'VectorSearch':
      return styles.barVector;
    case 'Serialize':
      return styles.barSerialize;
    case 'RetryWait':
      return styles.barRetry;
    case 'CacheLookup':
      return styles.barCache;
    case 'Unaccounted':
    case 'Overlap':
      return styles.barUnaccounted;
    default:
      return styles.barDefault;
  }
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
              <td className={`${styles.numeric} ${styles.cost}`}>{formatMoney(row.total)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function RequestsTable({
  rows,
  selectedId,
  onSelect
}: {
  rows: RequestRow[];
  selectedId: string | null;
  onSelect: (requestId: string) => void;
}) {
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
            <tr
              key={row.requestId}
              className={row.requestId === selectedId ? styles.selectedRow : undefined}
              onClick={() => onSelect(row.requestId)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  onSelect(row.requestId);
                }
              }}
              tabIndex={0}
              title="Open request detail"
            >
              <td className={styles.mono}>{row.startedAtUtc.replace('T', ' ').slice(0, 19)}</td>
              <td>{row.operation}</td>
              <td className={styles.mono}>{row.model}</td>
              <td className={styles.numeric}>{formatDuration(row.durationUs)}</td>
              <td className={styles.numeric}>{formatDuration(row.ttftUs)}</td>
              <td className={`${styles.numeric} ${styles.cost}`}>
                {row.cost === null ? (
                  <span className={styles.muted}>{row.costStatus.toLowerCase()}</span>
                ) : (
                  formatMoney(row.cost)
                )}
              </td>
              <td>
                <span className={row.status === 'Ok' ? styles.statusOk : styles.statusBad}>
                  {row.status.toLowerCase()}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.metric}>
      <div className={styles.metricLabel}>{label}</div>
      <div className={styles.metricValue}>{value}</div>
    </div>
  );
}

function Waterfall({ segments, totalUs }: { segments: Segment[]; totalUs: number }) {
  if (segments.length === 0) {
    return <div className={styles.empty}>No latency segments captured.</div>;
  }

  const scaleUs = Math.max(
    totalUs,
    ...segments.map((segment) => segment.startOffsetUs + segment.durationUs),
    1
  );

  return (
    <div className={styles.waterfall}>
      <div className={styles.waterfallScale}>
        <span>0</span>
        <span>{formatDuration(totalUs)}</span>
      </div>
      {segments.map((segment, index) => {
        const left = Math.max(0, (segment.startOffsetUs / scaleUs) * 100);
        const width = Math.max((segment.durationUs / scaleUs) * 100, 0.4);

        return (
          <div className={styles.waterfallRow} key={`${segment.kind}-${segment.startOffsetUs}-${index}`}>
            <div className={styles.waterfallLabel}>
              <span>{formatSegmentLabel(segment.kind)}</span>
              {segment.estimated ? <span className={styles.estimated}>est</span> : null}
            </div>
            <div className={styles.waterfallTrack}>
              <div
                className={`${styles.bar} ${segmentTone(segment.kind)} ${segment.estimated ? styles.barEstimated : ''}`}
                style={{ left: `${left}%`, width: `${width}%` }}
              />
            </div>
            <div className={styles.waterfallValue}>{formatDuration(segment.durationUs)}</div>
          </div>
        );
      })}
    </div>
  );
}

function CostLine({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.costLine}>
      <span>{label}</span>
      <span className={styles.mono}>{value}</span>
    </div>
  );
}

function DetailPanel({ requestId, onClose }: { requestId: string; onClose: () => void }) {
  const detail = useGetRequestQuery(requestId);

  if (detail.isLoading) {
    return <section className={styles.panel}><div className={styles.empty}>Loading request...</div></section>;
  }

  if (detail.isError || !detail.data) {
    return (
      <section className={styles.panel}>
        <div className={styles.empty}>
          Request not found in the current data directory.
          <button type="button" className={styles.closeButton} onClick={onClose}>Close</button>
        </div>
      </section>
    );
  }

  const record: RequestDetail = detail.data;
  const model = record.requestModel ?? record.responseModel ?? '-';
  const cost = record.cost;
  const stream = record.stream;
  const usage = record.usage;

  return (
    <section className={styles.panel}>
      <div className={styles.panelHeader}>
        <div>
          <div className={styles.kicker}>Request detail</div>
          <h2 className={styles.panelTitle}>{record.operation} · {model}</h2>
          <div className={styles.requestId}>{record.requestId}</div>
        </div>
        <button type="button" className={styles.closeButton} onClick={onClose}>Close</button>
      </div>

      <div className={styles.detailStats}>
        <Metric label="Duration" value={formatDuration(record.durationUs)} />
        <Metric label="TTFT" value={formatDuration(record.ttftUs)} />
        <Metric label="Chunks" value={stream ? integer.format(stream.chunkCount) : '-'} />
        <Metric label="Tokens/s" value={stream?.tokensPerSecond?.toFixed(1) ?? '-'} />
        <Metric label="Cost" value={formatMoney(cost?.total ?? null, cost?.currency ?? 'USD')} />
        <Metric label="Status" value={record.status.toLowerCase()} />
      </div>

      <div className={styles.detailSection}>
        <div className={styles.kicker}>Latency</div>
        <h3 className={styles.sectionTitle}>Request waterfall</h3>
        <Waterfall segments={record.segments} totalUs={record.durationUs} />
      </div>

      <div className={styles.detailGrid}>
        <div className={styles.detailCard}>
          <div className={styles.kicker}>Cost</div>
          <h3 className={styles.sectionTitle}>Cost breakdown</h3>
          {cost === null ? (
            <div className={styles.empty}>No price table matched this request.</div>
          ) : (
            <>
              <CostLine label="Input" value={formatMoney(cost.input, cost.currency)} />
              <CostLine label="Cached input" value={formatMoney(cost.cachedInput, cost.currency)} />
              <CostLine label="Output" value={formatMoney(cost.output, cost.currency)} />
              <CostLine label="Reasoning" value={formatMoney(cost.reasoning, cost.currency)} />
              <CostLine label="Total" value={formatMoney(cost.total, cost.currency)} />
              <div className={styles.costMeta}>
                {cost.status.toLowerCase()} · price table {cost.priceTableVersion}
              </div>
            </>
          )}
        </div>

        <div className={styles.detailCard}>
          <div className={styles.kicker}>Usage / stream</div>
          <h3 className={styles.sectionTitle}>Provider signals</h3>
          <div className={styles.kvList}>
            <span>Input tokens</span><span className={styles.mono}>{usage?.inputTokens ?? '-'}</span>
            <span>Output tokens</span><span className={styles.mono}>{usage?.outputTokens ?? '-'}</span>
            <span>Cached tokens</span><span className={styles.mono}>{usage?.cachedInputTokens ?? '-'}</span>
            <span>Reasoning tokens</span><span className={styles.mono}>{usage?.reasoningTokens ?? '-'}</span>
            <span>ITL P50</span><span className={styles.mono}>{formatDuration(stream?.itlP50Us ?? null)}</span>
            <span>ITL P95</span><span className={styles.mono}>{formatDuration(stream?.itlP95Us ?? null)}</span>
            <span>Finish reason</span><span className={styles.mono}>{stream?.finishReason ?? '-'}</span>
            <span>Stream flags</span>
            <span className={styles.mono}>
              {stream?.cancelled ? 'cancelled ' : ''}
              {stream?.interrupted ? 'interrupted' : ''}
              {!stream?.cancelled && !stream?.interrupted ? 'none' : ''}
            </span>
          </div>
        </div>
      </div>

      {record.tags && Object.keys(record.tags).length > 0 ? (
        <div className={styles.detailSection}>
          <div className={styles.kicker}>Attribution</div>
          <h3 className={styles.sectionTitle}>Tags</h3>
          <div className={styles.tags}>
            {Object.entries(record.tags).map(([key, value]) => (
              <span className={styles.tag} key={key}>{key}={value}</span>
            ))}
          </div>
        </div>
      ) : null}
    </section>
  );
}

export function App() {
  const hasToken = Boolean(sessionStorage.getItem('runesvaner-token'));
  const health = useGetHealthQuery(undefined, { skip: !hasToken });
  const costs = useGetCostsQuery(undefined, { skip: !hasToken });
  const requests = useGetRequestsQuery(20, { skip: !hasToken });
  const [selectedId, setSelectedId] = useState<string | null>(null);

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
            <h1 className={styles.title}>AI spend and latency, request by request</h1>
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
          <SummaryCard label="Total cost" value={formatMoney(total)} hint="Known price records only" />
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
            <span className={styles.panelMeta}>Click a row for the waterfall</span>
          </div>
          <RequestsTable rows={requests.data ?? []} selectedId={selectedId} onSelect={setSelectedId} />
        </section>

        {selectedId ? (
          <DetailPanel requestId={selectedId} onClose={() => setSelectedId(null)} />
        ) : null}

        <footer className={styles.footer}>
          Read-only local session. Request content is not displayed; data stays on this machine.
        </footer>
      </main>
    </div>
  );
}
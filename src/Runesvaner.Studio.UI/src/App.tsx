import { useState } from 'react';
import type { CostRow, RequestDetail, RequestRow, Segment, SessionRow } from './api';
import {
  useGetCostsQuery,
  useGetHealthQuery,
  useGetRequestQuery,
  useGetRequestsQuery,
  useGetSessionsQuery
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

function SessionsTable({
  rows,
  selectedSession,
  onSelect
}: {
  rows: SessionRow[];
  selectedSession: string | null;
  onSelect: (sessionId: string) => void;
}) {
  if (rows.length === 0) {
    return <div className={styles.empty}>No diagnostic sessions captured yet.</div>;
  }

  return (
    <div className={styles.tableScroll}>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>Started (UTC)</th>
            <th>Session</th>
            <th className={styles.numeric}>Requests</th>
            <th>Models</th>
            <th className={styles.numeric}>Total</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={row.sessionId}
              className={row.sessionId === selectedSession ? styles.selectedRow : undefined}
              onClick={() => onSelect(row.sessionId)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  onSelect(row.sessionId);
                }
              }}
              tabIndex={0}
              title="Filter requests by session"
            >
              <td className={styles.mono}>{row.startedAtUtc.replace('T', ' ').slice(0, 19)}</td>
              <td className={styles.mono} title={row.sessionId}>{row.sessionId.slice(0, 12)}</td>
              <td className={styles.numeric}>{integer.format(row.requests)}</td>
              <td className={styles.sessionModels} title={row.models.join(', ')}>
                {row.models.length === 0 ? '-' : row.models.join(', ')}
              </td>
              <td className={`${styles.numeric} ${styles.cost}`}>{formatMoney(row.totalCost)}</td>
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


function signedDuration(base: number | null, target: number | null): string {
  if (base === null || target === null) {
    return '-';
  }

  const diff = target - base;
  const percent = base > 0 ? ` (${((diff / base) * 100).toFixed(1)}%)` : '';
  return `${diff >= 0 ? '+' : '-'}${formatDuration(Math.abs(diff))}${percent}`;
}

function signedInteger(base: number | null, target: number | null): string {
  if (base === null || target === null) {
    return '-';
  }

  const diff = target - base;
  const percent = base > 0 ? ` (${((diff / base) * 100).toFixed(1)}%)` : '';
  return `${diff >= 0 ? '+' : '-'}${integer.format(Math.abs(diff))}${percent}`;
}

function signedMoney(base: number | null, target: number | null): string {
  if (base === null || target === null) {
    return '-';
  }

  const diff = target - base;
  const percent = base > 0 ? ` (${((diff / base) * 100).toFixed(1)}%)` : '';
  return `${diff >= 0 ? '+' : '-'}${formatMoney(Math.abs(diff))}${percent}`;
}

function signedDecimal(base: number | null, target: number | null): string {
  if (base === null || target === null) {
    return '-';
  }

  const diff = target - base;
  const percent = base > 0 ? ` (${((diff / base) * 100).toFixed(1)}%)` : '';
  return `${diff >= 0 ? '+' : '-'}${Math.abs(diff).toFixed(1)}${percent}`;
}

function ComparePanel({ requests }: { requests: RequestRow[] }) {
  const [baseId, setBaseId] = useState('');
  const [targetId, setTargetId] = useState('');
  const base = useGetRequestQuery(baseId, { skip: !baseId });
  const target = useGetRequestQuery(targetId, { skip: !targetId });
  const baseData = base.data;
  const targetData = target.data;

  const options = requests.map((row) => (
    <option key={row.requestId} value={row.requestId}>
      {row.startedAtUtc.replace('T', ' ').slice(0, 19)} · {row.operation} · {row.model} · {formatDuration(row.durationUs)}
    </option>
  ));

  const rows = baseData && targetData
    ? [
        {
          label: 'Duration',
          base: formatDuration(baseData.durationUs),
          target: formatDuration(targetData.durationUs),
          delta: signedDuration(baseData.durationUs, targetData.durationUs)
        },
        {
          label: 'TTFT',
          base: formatDuration(baseData.ttftUs),
          target: formatDuration(targetData.ttftUs),
          delta: signedDuration(baseData.ttftUs, targetData.ttftUs)
        },
        {
          label: 'Cost',
          base: formatMoney(baseData.cost?.total ?? null),
          target: formatMoney(targetData.cost?.total ?? null),
          delta: signedMoney(baseData.cost?.total ?? null, targetData.cost?.total ?? null)
        },
        {
          label: 'Input tokens',
          base: integer.format(baseData.usage?.inputTokens ?? 0),
          target: integer.format(targetData.usage?.inputTokens ?? 0),
          delta: signedInteger(baseData.usage?.inputTokens ?? null, targetData.usage?.inputTokens ?? null)
        },
        {
          label: 'Output tokens',
          base: integer.format(baseData.usage?.outputTokens ?? 0),
          target: integer.format(targetData.usage?.outputTokens ?? 0),
          delta: signedInteger(baseData.usage?.outputTokens ?? null, targetData.usage?.outputTokens ?? null)
        },
        {
          label: 'ITL P95',
          base: formatDuration(baseData.stream?.itlP95Us ?? null),
          target: formatDuration(targetData.stream?.itlP95Us ?? null),
          delta: signedDuration(baseData.stream?.itlP95Us ?? null, targetData.stream?.itlP95Us ?? null)
        },
        {
          label: 'Tokens/s',
          base: baseData.stream?.tokensPerSecond?.toFixed(1) ?? '-',
          target: targetData.stream?.tokensPerSecond?.toFixed(1) ?? '-',
          delta: signedDecimal(baseData.stream?.tokensPerSecond ?? null, targetData.stream?.tokensPerSecond ?? null)
        }
      ]
    : [];

  return (
    <section className={styles.panel}>
      <div className={styles.panelHeader}>
        <div>
          <div className={styles.kicker}>Comparison</div>
          <h2 className={styles.panelTitle}>Compare two requests</h2>
        </div>
        <span className={styles.panelMeta}>Latency and cost deltas</span>
      </div>

      <div className={styles.compareSelectors}>
        <label>
          Base
          <select className={styles.select} value={baseId} onChange={(event) => setBaseId(event.target.value)}>
            <option value="">Select base request</option>
            {options}
          </select>
        </label>
        <label>
          Target
          <select className={styles.select} value={targetId} onChange={(event) => setTargetId(event.target.value)}>
            <option value="">Select target request</option>
            {options}
          </select>
        </label>
      </div>

      {!baseData || !targetData ? (
        <div className={styles.empty}>
          {base.isLoading || target.isLoading ? 'Loading requests...' : 'Select two requests to compare.'}
        </div>
      ) : (
        <div className={styles.compareBody}>
          <div className={styles.compareGrid}>
            <div className={styles.compareCard}>
              <div className={styles.kicker}>Base</div>
              <div className={styles.compareTitle}>{baseData.requestId}</div>
              <Waterfall segments={baseData.segments} totalUs={baseData.durationUs} />
            </div>
            <div className={styles.compareCard}>
              <div className={styles.kicker}>Target</div>
              <div className={styles.compareTitle}>{targetData.requestId}</div>
              <Waterfall segments={targetData.segments} totalUs={targetData.durationUs} />
            </div>
          </div>

          <table className={styles.table}>
            <thead>
              <tr>
                <th>Metric</th>
                <th>Base</th>
                <th>Target</th>
                <th className={styles.numeric}>Delta</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.label}>
                  <td>{row.label}</td>
                  <td className={styles.mono}>{row.base}</td>
                  <td className={styles.mono}>{row.target}</td>
                  <td className={`${styles.numeric} ${styles.delta}`}>{row.delta}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export function App() {
  const hasToken = Boolean(sessionStorage.getItem('runesvaner-token'));
  const health = useGetHealthQuery(undefined, { skip: !hasToken });
  const costs = useGetCostsQuery(undefined, { skip: !hasToken });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedSession, setSelectedSession] = useState<string | null>(null);
  const sessions = useGetSessionsQuery(undefined, { skip: !hasToken });
  const requests = useGetRequestsQuery(
    { limit: 20, session: selectedSession },
    { skip: !hasToken }
  );

  const rows = costs.data ?? [];
  const total = rows.reduce((sum, row) => sum + row.total, 0);
  const priced = rows.reduce((sum, row) => sum + row.priced, 0);
  const unknown = rows.reduce((sum, row) => sum + row.unknown + row.partial, 0);
  const models = rows.length;
  const isLoading = health.isLoading || costs.isLoading || requests.isLoading || sessions.isLoading;
  const hasError = health.isError || costs.isError || requests.isError || sessions.isError;

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
              <div className={styles.kicker}>Sessions</div>
              <h2 className={styles.panelTitle}>Diagnostic sessions</h2>
            </div>
            <span className={styles.panelMeta}>
              {selectedSession ? 'Filtered' : `${sessions.data?.length ?? 0} sessions`}
            </span>
          </div>
          <SessionsTable
            rows={sessions.data ?? []}
            selectedSession={selectedSession}
            onSelect={(sessionId) => {
              setSelectedSession((current) => (current === sessionId ? null : sessionId));
              setSelectedId(null);
            }}
          />
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
            <div className={styles.panelActions}>
              {selectedSession ? (
                <button
                  type="button"
                  className={styles.linkButton}
                  onClick={() => setSelectedSession(null)}
                >
                  Clear session filter
                </button>
              ) : (
                <span>Click a row for the waterfall</span>
              )}
            </div>
          </div>
          <RequestsTable rows={requests.data ?? []} selectedId={selectedId} onSelect={setSelectedId} />
        </section>

        <ComparePanel requests={requests.data ?? []} />

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
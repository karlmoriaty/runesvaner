import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

export type Health = {
  status: string;
  version: string;
  dataPath: string;
};

export type CostRow = {
  provider: string;
  model: string;
  requests: number;
  priced: number;
  partial: number;
  unknown: number;
  total: number;
};

export type RequestRow = {
  requestId: string;
  sessionId: string | null;
  operation: string;
  provider: string | null;
  model: string;
  startedAtUtc: string;
  durationUs: number;
  ttftUs: number | null;
  status: string;
  cost: number | null;
  costStatus: string;
};

export type SessionRow = {
  sessionId: string;
  startedAtUtc: string;
  endedAtUtc: string;
  requests: number;
  priced: number;
  partial: number;
  unknown: number;
  totalCost: number;
  models: string[];
  providers: string[];
};

export type RequestQuery = {
  limit?: number;
  session?: string | null;
};

export type Usage = {
  inputTokens: number | null;
  outputTokens: number | null;
  cachedInputTokens: number | null;
  reasoningTokens: number | null;
  source: string;
};

export type Segment = {
  kind: string;
  startOffsetUs: number;
  durationUs: number;
  estimated: boolean;
  measurement: string;
  lane: number;
};

export type CostBreakdown = {
  currency: string;
  input: number | null;
  cachedInput: number | null;
  output: number | null;
  reasoning: number | null;
  total: number | null;
  status: string;
  priceTableVersion: string;
  priceEntryId: string | null;
};

export type StreamStats = {
  ttftUs: number | null;
  ttfbUs: number | null;
  chunkCount: number;
  itlP50Us: number | null;
  itlP95Us: number | null;
  itlEstimated: boolean;
  tokensPerSecond: number | null;
  cancelled: boolean;
  interrupted: boolean;
  finishReason: string | null;
};

export type RequestDetail = {
  requestId: string;
  operation: string;
  provider: string | null;
  requestModel: string | null;
  responseModel: string | null;
  startedAtUtc: string;
  durationUs: number;
  ttftUs: number | null;
  status: string;
  usage: Usage | null;
  segments: Segment[];
  tags: Record<string, string> | null;
  cost: CostBreakdown | null;
  stream: StreamStats | null;
};

export const studioApi = createApi({
  reducerPath: 'studioApi',
  baseQuery: fetchBaseQuery({
    baseUrl: '/',
    prepareHeaders: (headers) => {
      const token = sessionStorage.getItem('runesvaner-token');
      if (token) {
        headers.set('authorization', `Bearer ${token}`);
      }
      return headers;
    }
  }),
  endpoints: (builder) => ({
    getHealth: builder.query<Health, void>({
      query: () => 'api/health'
    }),
    getCosts: builder.query<CostRow[], void>({
      query: () => 'api/costs'
    }),
    getRequests: builder.query<RequestRow[], RequestQuery | void>({
      query: (args) => {
        const limit = args?.limit ?? 20;
        const session = args?.session;
        const suffix = session ? `&session=${encodeURIComponent(session)}` : '';
        return `api/requests?limit=${limit}${suffix}`;
      }
    }),
    getSessions: builder.query<SessionRow[], void>({
      query: () => 'api/sessions'
    }),
    getRequest: builder.query<RequestDetail, string>({
      query: (requestId) => `api/requests/${encodeURIComponent(requestId)}`
    })
  })
});

export const { useGetHealthQuery, useGetCostsQuery, useGetRequestsQuery, useGetRequestQuery, useGetSessionsQuery } = studioApi;
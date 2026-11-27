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
    getRequests: builder.query<RequestRow[], number | void>({
      query: (limit = 20) => `api/requests?limit=${limit}`
    })
  })
});

export const { useGetHealthQuery, useGetCostsQuery, useGetRequestsQuery } = studioApi;
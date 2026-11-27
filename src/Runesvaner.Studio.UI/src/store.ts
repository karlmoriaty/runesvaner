import { configureStore } from '@reduxjs/toolkit';
import { studioApi } from './api';

export const store = configureStore({
  reducer: {
    [studioApi.reducerPath]: studioApi.reducer
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(studioApi.middleware)
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
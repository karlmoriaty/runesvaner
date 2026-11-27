import React from 'react';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { App } from './App';
import { store } from './store';
import './global.css';

const params = new URLSearchParams(window.location.search);
const token = params.get('token');
if (token) {
  sessionStorage.setItem('runesvaner-token', token);
  window.history.replaceState({}, '', window.location.pathname);
}

const root = document.getElementById('root');
if (!root) {
  throw new Error('Missing #root element.');
}

createRoot(root).render(
  <React.StrictMode>
    <Provider store={store}>
      <App />
    </Provider>
  </React.StrictMode>
);
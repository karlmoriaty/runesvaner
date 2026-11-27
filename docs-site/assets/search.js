const input = document.getElementById('search-input');
const results = document.getElementById('search-results');

let pages = [];
let activeIndex = -1;
let currentMatches = [];

fetch('./search-index.json')
  .then((response) => response.json())
  .then((data) => {
    pages = data;
  })
  .catch(() => {
    pages = [];
  });

input?.addEventListener('input', () => {
  const query = input.value.trim().toLowerCase();

  if (!query) {
    hideResults();
    return;
  }

  const tokens = query.split(/\s+/).filter(Boolean);
  currentMatches = pages
    .map((page) => {
      const title = page.title.toLowerCase();
      const text = page.text.toLowerCase();
      let score = 0;

      for (const token of tokens) {
        if (!title.includes(token) && !text.includes(token)) {
          return null;
        }

        score += title.includes(token) ? 10 : 1;
      }

      return { page, score };
    })
    .filter(Boolean)
    .sort((left, right) => right.score - left.score)
    .slice(0, 8)
    .map((entry) => entry.page);

  activeIndex = currentMatches.length > 0 ? 0 : -1;
  renderResults(query);
});

input?.addEventListener('keydown', (event) => {
  if (event.key === 'Escape') {
    input.value = '';
    hideResults();
    return;
  }

  if (currentMatches.length === 0) {
    return;
  }

  if (event.key === 'ArrowDown') {
    event.preventDefault();
    activeIndex = (activeIndex + 1) % currentMatches.length;
    renderResults(input.value.trim());
  } else if (event.key === 'ArrowUp') {
    event.preventDefault();
    activeIndex = (activeIndex - 1 + currentMatches.length) % currentMatches.length;
    renderResults(input.value.trim());
  } else if (event.key === 'Enter' && activeIndex >= 0) {
    event.preventDefault();
    window.location.href = `./${currentMatches[activeIndex].slug}.html`;
  }
});

document.addEventListener('click', (event) => {
  if (!results.contains(event.target) && event.target !== input) {
    hideResults();
  }
});

function renderResults(query) {
  if (!results) {
    return;
  }

  if (currentMatches.length === 0) {
    results.innerHTML = `<div class="search-empty">No matches for “${escapeHtml(query)}”.</div>`;
    results.hidden = false;
    return;
  }

  results.innerHTML = currentMatches
    .map((page, index) => {
      const active = index === activeIndex ? ' active' : '';
      return `<a class="search-result${active}" href="./${page.slug}.html">${escapeHtml(page.title)}</a>`;
    })
    .join('');

  results.hidden = false;
}

function hideResults() {
  currentMatches = [];
  activeIndex = -1;

  if (results) {
    results.hidden = true;
    results.innerHTML = '';
  }
}

function escapeHtml(value) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
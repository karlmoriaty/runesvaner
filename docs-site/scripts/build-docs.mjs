import fs from 'node:fs';
import path from 'node:path';
import { marked } from 'marked';

const projectRoot = process.cwd();
const docsDirectory = path.resolve(projectRoot, '..', 'docs');
const outputDirectory = path.join(projectRoot, 'dist');
const assetsDirectory = path.join(projectRoot, 'assets');

const sourceFiles = fs
  .readdirSync(docsDirectory)
  .filter((file) => file.endsWith('.md'))
  .filter((file) => fs.statSync(path.join(docsDirectory, file)).isFile())
  .sort();

const slugByFile = new Map(
  sourceFiles.map((file) => [file, file === 'README.md' ? 'index' : file.replace(/\.md$/, '')])
);

const pages = sourceFiles.map((file) => {
  const slug = slugByFile.get(file) ?? file.replace(/\.md$/, '');
  const raw = fs.readFileSync(path.join(docsDirectory, file), 'utf8');
  const heading = raw.match(/^#\s+(.+)$/m);
  const title = (heading?.[1] ?? file.replace(/\.md$/, '')).replace(/`/g, '').trim();
  const body = rewriteMarkdown(heading ? raw.replace(/^#\s+.+\r?\n+/, '') : raw);
  return { file, slug, title, body };
});

fs.rmSync(outputDirectory, { recursive: true, force: true });
fs.mkdirSync(path.join(outputDirectory, 'assets'), { recursive: true });
fs.copyFileSync(path.join(assetsDirectory, 'site.css'), path.join(outputDirectory, 'assets', 'site.css'));
fs.copyFileSync(path.join(assetsDirectory, 'logo.svg'), path.join(outputDirectory, 'assets', 'logo.svg'));
fs.copyFileSync(path.join(assetsDirectory, 'search.js'), path.join(outputDirectory, 'assets', 'search.js'));

const searchIndex = pages.map((page) => ({
  slug: page.slug,
  title: page.title,
  text: toPlainText(page.body)
}));

fs.writeFileSync(
  path.join(outputDirectory, 'search-index.json'),
  JSON.stringify(searchIndex),
  'utf8'
);

for (const page of pages) {
  const html = await marked.parse(page.body, { gfm: true });
  fs.writeFileSync(
    path.join(outputDirectory, `${page.slug}.html`),
    renderPage(page, html),
    'utf8'
  );
}

fs.writeFileSync(
  path.join(outputDirectory, '404.html'),
  renderPage(
    { slug: '404', title: 'Page not found' },
    '<p>The requested document does not exist. Return to the <a href="./index.html">documentation overview</a>.</p>'
  ),
  'utf8'
);

console.log(`Built ${pages.length} documentation pages into ${outputDirectory}.`);

function rewriteMarkdown(markdown) {
  return markdown.replace(/\]\(([^)]+)\)/g, (full, target) => {
    if (target.startsWith('http://') || target.startsWith('https://') || target.startsWith('#') || target.startsWith('mailto:')) {
      return full;
    }

    if (target.startsWith('zh-Hans/')) {
      return '](https://github.com/karlmoriaty/runesvaner/tree/main/docs/zh-Hans)';
    }

    if (target.startsWith('../pricing/')) {
      return '](https://github.com/karlmoriaty/runesvaner/blob/main/pricing/README.md)';
    }

    if (target.startsWith('../README.md')) {
      return '](https://github.com/karlmoriaty/runesvaner)';
    }

    const [targetPath, anchor] = target.split('#');
    const basename = targetPath.split('/').pop() ?? targetPath;
    const slug = slugByFile.get(basename);
    if (slug) {
      return `](./${slug}.html${anchor ? `#${anchor}` : ''})`;
    }

    return full;
  });
}

function renderPage(page, html) {
  const nav = renderSidebar(page.slug);
  const title = escapeHtml(page.title);

  return `<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="color-scheme" content="dark" />
    <meta name="description" content="Runesvaner design documentation." />
    <title>${title} · Runesvaner</title>
    <link rel="icon" href="./assets/logo.svg" type="image/svg+xml" />
    <link rel="stylesheet" href="./assets/site.css" />
    <script src="./assets/search.js" defer></script>
  </head>
  <body>
    <header class="topbar">
      <a class="brand" href="./index.html">
        <img src="./assets/logo.svg" alt="" />
        <span>Runesvaner</span>
        <span class="brand-muted">Docs</span>
      </a>
      <div class="top-meta">
        <span>English source of truth</span>
        <a href="https://github.com/karlmoriaty/runesvaner" rel="noreferrer">GitHub ↗</a>
      </div>
    </header>
    <div class="layout">
      <aside class="sidebar">
        <div class="search">
          <input id="search-input" type="search" placeholder="Search docs" autocomplete="off" aria-label="Search documentation" />
          <div id="search-results" class="search-results" hidden></div>
        </div>
        ${nav}
      </aside>
      <main class="content">
        <header class="page-header">
          <div class="kicker">Design documentation</div>
          <h1>${title}</h1>
        </header>
        <article>${html}</article>
        <footer class="site-footer">
          English is the source of truth. Chinese legacy: docs/zh-Hans.
        </footer>
      </main>
    </div>
  </body>
</html>`;
}

function renderSidebar(activeSlug) {
  const isExample = (slug) => /^2[0-4]-/.test(slug);
  const isReference = (slug) => slug === 'benchmarks-baseline' || slug.startsWith('25-');
  const examples = pages.filter((page) => isExample(page.slug));
  const reference = pages.filter((page) => isReference(page.slug));
  const design = pages.filter(
    (page) => page.slug !== 'index' && !isExample(page.slug) && !isReference(page.slug)
  );
  const groups = [
    { label: 'Overview', pages: pages.filter((page) => page.slug === 'index') },
    { label: 'Examples', pages: examples },
    { label: 'Design', pages: design },
    { label: 'Reference', pages: reference }
  ];

  const groupsHtml = groups
    .map((group) => {
      const links = group.pages
        .map((page) => {
          const active = page.slug === activeSlug ? ' active' : '';
          return `<a class="nav-link${active}" href="./${page.slug}.html">${escapeHtml(page.title)}</a>`;
        })
        .join('\n');

      return `<div class="nav-group"><div class="nav-group-label">${group.label}</div>${links}</div>`;
    })
    .join('\n');

  return `${groupsHtml}
<div class="nav-group">
  <div class="nav-group-label">Legacy</div>
  <a class="nav-link" href="https://github.com/karlmoriaty/runesvaner/tree/main/docs/zh-Hans">
    中文 legacy <span class="ext">↗</span>
  </a>
</div>`;
}

function toPlainText(markdown) {
  return markdown
    .replace(/```[\s\S]*?```/g, ' ')
    .replace(/`[^`]*`/g, ' ')
    .replace(/!\[[^\]]*\]\([^)]*\)/g, ' ')
    .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/^#{1,6}\s+/gm, '')
    .replace(/[*_>|-]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 4000);
}

function escapeHtml(value) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
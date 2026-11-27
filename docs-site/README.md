# Runesvaner Documentation Site

Static English documentation site generated from `docs/*.md`.

## Build

    npm ci
    npm run build

Output: `dist/`.

## Preview

    npm run build
    npm run preview

Preview binds to `127.0.0.1:4321` by default.

## Deployment

`.github/workflows/pages.yml` builds this directory and deploys `dist/` to GitHub Pages at:

    https://karlmoriaty.github.io/runesvaner/

The build uses a dependency-light static generator. Astro Starlight remains the long-term target (D-21); the local npm dependency tree for Astro blocked installation, so the Pages site is currently generated without a framework. The content pipeline still reads the English source of truth from `docs/`.
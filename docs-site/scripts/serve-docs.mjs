import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(process.cwd(), 'dist');
const port = Number(process.env.PORT ?? 4321);

if (!fs.existsSync(root)) {
  console.error('dist does not exist. Run "npm run build" first.');
  process.exit(1);
}

const contentTypes = new Map([
  ['.html', 'text/html; charset=utf-8'],
  ['.css', 'text/css; charset=utf-8'],
  ['.svg', 'image/svg+xml'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8']
]);

const server = http.createServer((request, response) => {
  const requestUrl = new URL(request.url ?? '/', `http://${request.headers.host ?? '127.0.0.1'}`);
  let pathname = decodeURIComponent(requestUrl.pathname);

  if (pathname === '/') {
    pathname = '/index.html';
  }

  const filePath = path.resolve(root, `.${pathname}`);
  if (!filePath.startsWith(root + path.sep)) {
    response.writeHead(403);
    response.end('Forbidden');
    return;
  }

  fs.readFile(filePath, (error, content) => {
    if (error) {
      fs.readFile(path.join(root, '404.html'), (notFoundError, notFound) => {
        response.writeHead(404, { 'content-type': 'text/html; charset=utf-8' });
        response.end(notFoundError ? 'Not found' : notFound);
      });
      return;
    }

    response.writeHead(200, {
      'content-type': contentTypes.get(path.extname(filePath)) ?? 'application/octet-stream'
    });
    response.end(content);
  });
});

server.listen(port, '127.0.0.1', () => {
  console.log(`Runesvaner docs: http://127.0.0.1:${port}/`);
});
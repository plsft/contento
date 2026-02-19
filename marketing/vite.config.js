import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import { resolve } from 'path';
import { readFileSync } from 'fs';

function includePartials() {
  return {
    name: 'include-partials',
    transformIndexHtml: {
      order: 'pre',
      handler(html) {
        return html.replace(/<%\s*([\w-]+)\s*%>/g, (_, name) => {
          return readFileSync(resolve(__dirname, 'partials', `${name}.html`), 'utf-8');
        });
      },
    },
  };
}

export default defineConfig({
  plugins: [
    tailwindcss(),
    includePartials(),
  ],
  build: {
    rollupOptions: {
      input: {
        main:            resolve(__dirname, 'index.html'),
        features:        resolve(__dirname, 'features.html'),
        'docs-index':    resolve(__dirname, 'docs/index.html'),
        'docs-admin':    resolve(__dirname, 'docs/admin.html'),
        'docs-api':      resolve(__dirname, 'docs/api.html'),
        'docs-headless': resolve(__dirname, 'docs/headless.html'),
        'docs-plugins':  resolve(__dirname, 'docs/plugins.html'),
        'docs-themes':   resolve(__dirname, 'docs/themes.html'),
        'legal-privacy': resolve(__dirname, 'legal/privacy.html'),
        'legal-terms':   resolve(__dirname, 'legal/terms.html'),
        'legal-cookies': resolve(__dirname, 'legal/cookies.html'),
        'branding':      resolve(__dirname, 'branding.html'),
      },
    },
  },
});

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
        main:                  resolve(__dirname, 'index.html'),
        features:              resolve(__dirname, 'features.html'),
        'how-it-works':        resolve(__dirname, 'how-it-works.html'),
        niches:                resolve(__dirname, 'niches.html'),
        pricing:               resolve(__dirname, 'pricing.html'),
        branding:              resolve(__dirname, 'branding.html'),
        'docs-index':          resolve(__dirname, 'docs/index.html'),
        'docs-installation':   resolve(__dirname, 'docs/installation.html'),
        'docs-first-project':  resolve(__dirname, 'docs/first-project.html'),
        'docs-projects':       resolve(__dirname, 'docs/projects.html'),
        'docs-niches':         resolve(__dirname, 'docs/niches.html'),
        'docs-schemas':        resolve(__dirname, 'docs/schemas.html'),
        'docs-collections':    resolve(__dirname, 'docs/collections.html'),
        'docs-generation':     resolve(__dirname, 'docs/generation.html'),
        'docs-publishing':     resolve(__dirname, 'docs/publishing.html'),
        'docs-chrome':         resolve(__dirname, 'docs/chrome.html'),
        'docs-renderers':      resolve(__dirname, 'docs/renderers.html'),
        'docs-internal-linking': resolve(__dirname, 'docs/internal-linking.html'),
        'docs-api':            resolve(__dirname, 'docs/api.html'),
        'docs-pseo-api':       resolve(__dirname, 'docs/pseo-api.html'),
        'docs-dns-domains':    resolve(__dirname, 'docs/dns-domains.html'),
        'docs-deployment':     resolve(__dirname, 'docs/deployment.html'),
        'docs-analytics':      resolve(__dirname, 'docs/analytics.html'),
        'docs-cli':            resolve(__dirname, 'docs/cli.html'),
        'docs-agents':         resolve(__dirname, 'docs/agents.html'),
        'docs-pseo-guide':     resolve(__dirname, 'docs/pseo-guide.html'),
        'docs-admin':          resolve(__dirname, 'docs/admin.html'),
        'docs-headless':       resolve(__dirname, 'docs/headless.html'),
        'docs-plugins':        resolve(__dirname, 'docs/plugins.html'),
        'docs-themes':         resolve(__dirname, 'docs/themes.html'),
        'legal-privacy':       resolve(__dirname, 'legal/privacy.html'),
        'legal-terms':         resolve(__dirname, 'legal/terms.html'),
        'legal-cookies':       resolve(__dirname, 'legal/cookies.html'),
      },
    },
  },
});

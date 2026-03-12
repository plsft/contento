import { defineConfig } from 'tsup';

export default defineConfig([
  {
    entry: { 'bin/contento': 'bin/contento.ts' },
    format: ['esm'],
    target: 'node18',
    clean: true,
    sourcemap: true,
    splitting: false,
    dts: false,
    banner: {
      js: '#!/usr/bin/env node',
    },
  },
  {
    entry: { 'src/index': 'src/index.ts' },
    format: ['esm'],
    target: 'node18',
    clean: false,
    sourcemap: true,
    splitting: false,
    dts: false,
  },
]);

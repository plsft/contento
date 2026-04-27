import { Command } from 'commander';
import chalk from 'chalk';

import { output, isJsonMode } from '../output.js';
import { findSchemaBySlug, getAllSchemas } from '../lib/schema-store.js';

export function registerSchemasCommand(program: Command): void {
  const schemas = program
    .command('schemas')
    .description('Browse the bundled content schemas');

  // ── list ─────────────────────────────────────────────────────────────
  schemas
    .command('list')
    .description('List all bundled content schemas')
    .action(() => {
      const all = getAllSchemas();

      if (isJsonMode()) {
        output.json(all);
        return;
      }

      if (all.length === 0) {
        output.info('No schemas available.');
        return;
      }

      output.table(
        ['Slug', 'Name', 'Title Pattern', 'Renderer'],
        all.map((s) => [s.slug, s.name, s.titlePattern, s.rendererSlug]),
      );
    });

  // ── view ─────────────────────────────────────────────────────────────
  schemas
    .command('view')
    .description('View detailed information about a schema')
    .argument('<slug>', 'Schema slug')
    .action((slug: string) => {
      const s = findSchemaBySlug(slug);
      if (!s) {
        output.error(`No schema found with slug "${slug}".`);
        process.exit(1);
      }

      if (isJsonMode()) {
        output.json(s);
        return;
      }

      output.blank();
      output.text(chalk.bold(s.name) + chalk.dim(` (${s.slug})`));
      if (s.description) {
        output.blank();
        output.text(s.description);
      }
      output.blank();
      output.text(chalk.cyan.bold('Title pattern: ') + s.titlePattern);
      if (s.metaDescPattern) {
        output.text(chalk.cyan.bold('Meta description pattern: ') + s.metaDescPattern);
      }
      output.text(chalk.cyan.bold('Renderer: ') + s.rendererSlug);
      output.blank();
      output.text(chalk.cyan.bold('Schema (JSON):'));
      output.text(JSON.stringify(s.schemaJson, null, 2));
      output.blank();
    });
}

import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output } from '../output.js';
import { createSpinner } from '../ui/spinner.js';

interface Schema {
  id: string;
  name: string;
  slug: string;
  description?: string;
  fieldsCount?: number;
}

interface SchemasResponse {
  schemas: Schema[];
}

export function registerSchemasCommand(program: Command): void {
  const schemas = program
    .command('schemas')
    .description('View available content schemas');

  // List schemas
  schemas
    .command('list')
    .description('List all available schemas')
    .action(async () => {
      const spinner = createSpinner('Fetching schemas...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<SchemasResponse>('/pseo/schemas');
        spinner.stop();

        const schemaList = data.schemas ?? [];

        if (schemaList.length === 0) {
          output.info('No schemas found.');
          return;
        }

        output.table(
          ['ID', 'Name', 'Slug', 'Description', 'Fields'],
          schemaList.map((s) => [
            s.id,
            s.name,
            s.slug,
            s.description ?? '',
            s.fieldsCount ?? 0,
          ]),
        );
      } catch (err) {
        spinner.fail('Failed to fetch schemas');
        handleError(err);
      }
    });
}

function handleError(err: unknown): void {
  if (err instanceof ApiError) {
    output.error(`${err.message} (${err.code})`);
  } else if (err instanceof Error) {
    output.error(err.message);
  } else {
    output.error(String(err));
  }
  process.exit(1);
}

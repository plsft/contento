import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output } from '../output.js';
import { createSpinner } from '../ui/spinner.js';

interface Niche {
  id: string;
  name: string;
  slug: string;
  category?: string;
  keywords?: string[];
  description?: string;
}

interface NichesResponse {
  niches: Niche[];
}

interface NicheResponse {
  niche: Niche;
}

export function registerNichesCommand(program: Command): void {
  const niches = program
    .command('niches')
    .description('Browse and fork niche templates');

  // List niches
  niches
    .command('list')
    .description('List available niches')
    .option('--category <category>', 'Filter by category')
    .option('--search <query>', 'Search niches by keyword')
    .action(async (opts: { category?: string; search?: string }) => {
      const spinner = createSpinner('Fetching niches...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });

        const params = new URLSearchParams();
        if (opts.category) params.set('category', opts.category);
        if (opts.search) params.set('search', opts.search);

        const query = params.toString();
        const path = `/pseo/niches${query ? `?${query}` : ''}`;

        const data = await client.get<NichesResponse>(path);
        spinner.stop();

        const nicheList = data.niches ?? [];

        if (nicheList.length === 0) {
          output.info('No niches found matching your criteria.');
          return;
        }

        output.table(
          ['ID', 'Name', 'Category', 'Keywords'],
          nicheList.map((n) => [
            n.id,
            n.name,
            n.category ?? '',
            (n.keywords ?? []).join(', '),
          ]),
        );
      } catch (err) {
        spinner.fail('Failed to fetch niches');
        handleError(err);
      }
    });

  // Fork a niche
  niches
    .command('fork')
    .description('Fork a niche template into your project')
    .argument('<id>', 'Niche ID to fork')
    .requiredOption('--name <name>', 'Name for the forked niche')
    .action(async (id: string, opts: { name: string }) => {
      const spinner = createSpinner('Forking niche...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.post<NicheResponse>(`/pseo/niches/${id}/fork`, {
          name: opts.name,
        });
        spinner.succeed('Niche forked');

        const n = data.niche;
        output.success(`Niche "${n.name}" forked with ID: ${n.id}`);
      } catch (err) {
        spinner.fail('Failed to fork niche');
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

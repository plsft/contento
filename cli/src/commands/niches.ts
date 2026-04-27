import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output, isJsonMode } from '../output.js';
import { createSpinner } from '../ui/spinner.js';
import { checkbox, select, input } from '@inquirer/prompts';
import chalk from 'chalk';

interface Niche {
  id: string;
  name: string;
  slug: string;
  category?: string;
  keywords?: string[];
  subtopics?: string[];
  description?: string;
  context?: {
    audience?: string;
    painPoints?: string[];
    monetization?: string[];
  };
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
    .description('Fork a niche template and optionally customize it')
    .argument('<id>', 'Niche ID to fork')
    .requiredOption('--name <name>', 'Name for the forked niche')
    .option('--add-subtopics <list>', 'Comma-separated subtopics to add to the fork')
    .option('--remove-subtopics <list>', 'Comma-separated subtopics to remove from the fork')
    .option('--keywords <list>', 'Replace keywords (comma-separated)')
    .option('--audience <audience>', 'Override target audience')
    .action(
      async (
        id: string,
        opts: {
          name: string;
          addSubtopics?: string;
          removeSubtopics?: string;
          keywords?: string;
          audience?: string;
        },
      ) => {
        const spinner = createSpinner('Forking niche...').start();
        try {
          const client = createClient({ baseUrl: program.opts().apiUrl });
          const body: Record<string, unknown> = { name: opts.name };
          if (opts.addSubtopics) {
            body.addSubtopics = opts.addSubtopics.split(',').map((s) => s.trim());
          }
          if (opts.removeSubtopics) {
            body.removeSubtopics = opts.removeSubtopics.split(',').map((s) => s.trim());
          }
          if (opts.keywords) {
            body.keywords = opts.keywords.split(',').map((s) => s.trim());
          }
          if (opts.audience) {
            body.context = { audience: opts.audience };
          }

          const data = await client.post<NicheResponse>(
            `/pseo/niches/${id}/fork`,
            body,
          );
          spinner.succeed('Niche forked');

          const n = data.niche;
          output.success(`Niche "${n.name}" forked with ID: ${n.id}`);

          output.table(
            ['Field', 'Value'],
            [
              ['ID', n.id],
              ['Name', n.name],
              ['Category', n.category ?? '—'],
              ['Subtopics', `${n.subtopics?.length ?? '—'}`],
              ['Keywords', (n.keywords ?? []).join(', ') || '—'],
            ],
          );
        } catch (err) {
          spinner.fail('Failed to fork niche');
          handleError(err);
        }
      },
    );

  // Create a custom niche from scratch
  niches
    .command('create')
    .description('Create a custom niche (interactive or flags)')
    .requiredOption('--project <id>', 'Project ID')
    .option('--name <name>', 'Niche name')
    .option('--category <category>', 'Category')
    .option('--subtopics <list>', 'Comma-separated subtopics')
    .option('--keywords <list>', 'Comma-separated keywords')
    .option('--audience <audience>', 'Target audience')
    .option('--pain-points <list>', 'Comma-separated pain points')
    .action(
      async (opts: {
        project: string;
        name?: string;
        category?: string;
        subtopics?: string;
        keywords?: string;
        audience?: string;
        painPoints?: string;
      }) => {
        try {
          const client = createClient({ baseUrl: program.opts().apiUrl });
          const isTTY = process.stdout.isTTY && !isJsonMode();

          let name = opts.name;
          let category = opts.category;
          let subtopics: string[] = opts.subtopics
            ? opts.subtopics.split(',').map((s) => s.trim())
            : [];
          let keywords: string[] = opts.keywords
            ? opts.keywords.split(',').map((s) => s.trim())
            : [];
          let audience = opts.audience;
          let painPoints: string[] = opts.painPoints
            ? opts.painPoints.split(',').map((s) => s.trim())
            : [];

          if (isTTY) {
            if (!name) {
              name = await input({ message: 'Niche name', required: true });
            }
            if (!category) {
              category = await input({
                message: 'Category (e.g., Technology, Business, Health)',
                default: 'Custom',
              });
            }
            if (subtopics.length === 0) {
              const raw = await input({
                message: 'Subtopics (comma-separated — these become your pages)',
                required: true,
              });
              subtopics = raw.split(',').map((s) => s.trim()).filter(Boolean);
            }
            if (keywords.length === 0) {
              const raw = await input({
                message: 'Keywords (comma-separated, for SEO targeting)',
                default: '',
              });
              keywords = raw
                .split(',')
                .map((s) => s.trim())
                .filter(Boolean);
            }
            if (!audience) {
              audience = await input({
                message: 'Target audience (e.g., "frontend developers")',
                default: '',
              });
            }
            if (painPoints.length === 0) {
              const raw = await input({
                message: 'Pain points (comma-separated)',
                default: '',
              });
              painPoints = raw
                .split(',')
                .map((s) => s.trim())
                .filter(Boolean);
            }
          } else {
            if (!name) {
              output.error('--name is required in non-interactive mode');
              process.exit(1);
            }
            if (subtopics.length === 0) {
              output.error(
                '--subtopics is required in non-interactive mode',
              );
              process.exit(1);
            }
          }

          const spinner = createSpinner('Creating niche...').start();
          const data = await client.post<NicheResponse>(
            `/pseo/projects/${opts.project}/niches`,
            {
              name,
              category: category ?? 'Custom',
              subtopics,
              keywords,
              context: {
                audience: audience || undefined,
                painPoints: painPoints.length ? painPoints : undefined,
              },
            },
          );
          spinner.succeed('Niche created');

          const n = data.niche;
          output.success(`Niche "${n.name}" created with ID: ${n.id}`);

          output.table(
            ['Field', 'Value'],
            [
              ['ID', n.id],
              ['Name', n.name],
              ['Category', n.category ?? 'Custom'],
              ['Subtopics', `${(n.subtopics ?? subtopics).length} topics`],
              ['Keywords', (n.keywords ?? keywords).join(', ') || '—'],
            ],
          );
        } catch (err) {
          if (err instanceof Error && err.message.includes('ExitPrompt')) {
            output.info('Cancelled.');
            process.exit(0);
          }
          handleError(err);
        }
      },
    );

  // Edit a niche (update subtopics, keywords, context)
  niches
    .command('edit')
    .description('Edit a niche — add/remove subtopics, update context')
    .argument('<id>', 'Niche ID')
    .option('--name <name>', 'Update name')
    .option('--add-subtopics <list>', 'Comma-separated subtopics to add')
    .option('--remove-subtopics <list>', 'Comma-separated subtopics to remove')
    .option('--keywords <list>', 'Replace keywords (comma-separated)')
    .option('--audience <audience>', 'Update target audience')
    .option('--pain-points <list>', 'Replace pain points (comma-separated)')
    .action(
      async (
        id: string,
        opts: {
          name?: string;
          addSubtopics?: string;
          removeSubtopics?: string;
          keywords?: string;
          audience?: string;
          painPoints?: string;
        },
      ) => {
        try {
          const client = createClient({ baseUrl: program.opts().apiUrl });

          const patch: Record<string, unknown> = {};
          if (opts.name) patch.name = opts.name;
          if (opts.addSubtopics) {
            patch.addSubtopics = opts.addSubtopics.split(',').map((s) => s.trim());
          }
          if (opts.removeSubtopics) {
            patch.removeSubtopics = opts.removeSubtopics.split(',').map((s) => s.trim());
          }
          if (opts.keywords) {
            patch.keywords = opts.keywords.split(',').map((s) => s.trim());
          }
          if (opts.audience || opts.painPoints) {
            patch.context = {
              ...(opts.audience ? { audience: opts.audience } : {}),
              ...(opts.painPoints
                ? {
                    painPoints: opts.painPoints
                      .split(',')
                      .map((s) => s.trim()),
                  }
                : {}),
            };
          }

          if (Object.keys(patch).length === 0) {
            output.error(
              'Nothing to update. Use --name, --add-subtopics, --remove-subtopics, --keywords, --audience, or --pain-points.',
            );
            process.exit(1);
          }

          const spinner = createSpinner('Updating niche...').start();
          const data = await client.put<NicheResponse>(
            `/pseo/niches/${id}`,
            patch,
          );
          spinner.succeed('Niche updated');

          const n = data.niche;
          output.success(`Niche "${n.name}" updated`);

          if (n.subtopics?.length) {
            output.text(
              chalk.dim(`  Subtopics: ${n.subtopics.length} total`),
            );
          }
        } catch (err) {
          handleError(err);
        }
      },
    );

  // View niche details
  niches
    .command('view')
    .description('View detailed information about a niche')
    .argument('<id>', 'Niche ID')
    .action(async (id: string) => {
      const spinner = createSpinner('Fetching niche...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<NicheResponse>(`/pseo/niches/${id}`);
        spinner.stop();

        const n = data.niche;

        if (isJsonMode()) {
          output.json(n);
          return;
        }

        output.blank();
        output.text(chalk.bold(n.name) + chalk.dim(` (${n.slug})`));
        if (n.category) output.text(chalk.dim(`Category: `) + n.category);
        if (n.description) {
          output.blank();
          output.text(n.description);
        }
        if (n.context?.audience) {
          output.blank();
          output.text(chalk.cyan.bold('Audience'));
          output.text(n.context.audience);
        }
        if (n.context?.painPoints?.length) {
          output.blank();
          output.text(chalk.cyan.bold('Pain Points'));
          for (const p of n.context.painPoints) {
            output.text(chalk.dim('  • ') + p);
          }
        }
        if (n.context?.monetization?.length) {
          output.blank();
          output.text(chalk.cyan.bold('Monetization'));
          for (const m of n.context.monetization) {
            output.text(chalk.dim('  • ') + m);
          }
        }
        if (n.subtopics?.length) {
          output.blank();
          output.text(chalk.cyan.bold(`Subtopics (${n.subtopics.length})`));
          for (const s of n.subtopics) {
            output.text(chalk.dim('  • ') + s);
          }
        }
        if (n.keywords?.length) {
          output.blank();
          output.text(chalk.cyan.bold('Keywords'));
          output.text(chalk.dim('  ') + n.keywords.join(', '));
        }
        output.blank();
      } catch (err) {
        spinner.fail('Failed to fetch niche');
        handleError(err);
      }
    });

  // Interactive niche selection
  niches
    .command('select')
    .description('Interactively browse and select niches')
    .option('--category <category>', 'Start with a specific category')
    .action(async (opts: { category?: string }) => {
      if (isJsonMode()) {
        output.error('Interactive selection is not available in --json mode. Use `niches list --json` instead.');
        process.exit(1);
      }

      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });

        // Fetch all niches
        const spinner = createSpinner('Loading niches...').start();
        const data = await client.get<NichesResponse>('/pseo/niches');
        spinner.stop();

        const allNiches = data.niches ?? [];
        if (allNiches.length === 0) {
          output.info('No niches available.');
          return;
        }

        // Group by category
        const categories = new Map<string, Niche[]>();
        for (const n of allNiches) {
          const cat = n.category ?? 'Uncategorized';
          if (!categories.has(cat)) categories.set(cat, []);
          categories.get(cat)!.push(n);
        }

        // Step 1: Pick a category (or use provided one)
        let selectedCategory = opts.category;
        if (!selectedCategory) {
          selectedCategory = await select({
            message: 'Select a category',
            choices: [...categories.keys()].sort().map((cat) => ({
              name: `${cat} (${categories.get(cat)!.length} niches)`,
              value: cat,
            })),
          });
        }

        const categoryNiches = categories.get(selectedCategory) ?? allNiches;

        // Step 2: Select niches from that category (checkbox)
        const selectedIds = await checkbox({
          message: `Select niches from ${selectedCategory}`,
          choices: categoryNiches.map((n) => ({
            name: `${n.name}${n.keywords?.length ? chalk.dim(` — ${n.keywords.slice(0, 3).join(', ')}`) : ''}`,
            value: n.id,
          })),
          required: true,
        });

        // Step 3: Ask if they want to add from another category
        let addMore = true;
        const allSelectedIds = [...selectedIds];

        while (addMore) {
          addMore = await select({
            message: `${allSelectedIds.length} niche(s) selected. Add from another category?`,
            choices: [
              { name: 'No, done selecting', value: false },
              { name: 'Yes, browse another category', value: true },
            ],
          });

          if (addMore) {
            const anotherCat = await select({
              message: 'Select another category',
              choices: [...categories.keys()]
                .sort()
                .filter((cat) => cat !== selectedCategory)
                .map((cat) => ({
                  name: `${cat} (${categories.get(cat)!.length} niches)`,
                  value: cat,
                })),
            });

            const moreIds = await checkbox({
              message: `Select niches from ${anotherCat}`,
              choices: categories.get(anotherCat)!.map((n) => ({
                name: `${n.name}${n.keywords?.length ? chalk.dim(` — ${n.keywords.slice(0, 3).join(', ')}`) : ''}`,
                value: n.id,
                checked: allSelectedIds.includes(n.id),
              })),
            });

            for (const id of moreIds) {
              if (!allSelectedIds.includes(id)) allSelectedIds.push(id);
            }
          }
        }

        // Output the selected IDs
        const selectedNiches = allNiches.filter((n) => allSelectedIds.includes(n.id));

        output.blank();
        output.success(`Selected ${selectedNiches.length} niche(s):`);
        for (const n of selectedNiches) {
          output.text(chalk.dim('  • ') + n.name + chalk.dim(` (${n.id})`));
        }

        output.blank();
        output.text(chalk.bold('Use with collections create:'));
        output.text(chalk.dim('  contento collections create \\'));
        output.text(chalk.dim('    --niches ') + allSelectedIds.join(','));
        output.blank();
      } catch (err) {
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

import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output, isJsonMode } from '../output.js';
import { createSpinner } from '../ui/spinner.js';
import { createProgressBar } from '../ui/progress.js';
import { connectSSE } from '../sse-client.js';
import { checkbox, select, input } from '@inquirer/prompts';
import chalk from 'chalk';
import type { SSEProgress } from '../sse-client.js';

interface Collection {
  id: string;
  projectId: string;
  schemaSlug: string;
  name?: string;
  urlPattern: string;
  titleTemplate: string;
  status: string;
  pagesGenerated?: number;
  pagesTotal?: number;
  createdAt?: string;
}

interface CollectionsResponse {
  collections: Collection[];
}

interface CollectionResponse {
  collection: Collection;
}

interface GenerateResponse {
  jobId: string;
  streamUrl?: string;
  status: string;
}

export function registerCollectionsCommand(program: Command): void {
  const collections = program
    .command('collections')
    .description('Manage page collections');

  // List collections
  collections
    .command('list')
    .description('List collections for a project')
    .requiredOption('--project <id>', 'Project ID')
    .action(async (opts: { project: string }) => {
      const spinner = createSpinner('Fetching collections...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<CollectionsResponse>(
          `/pseo/projects/${opts.project}/collections`,
        );
        spinner.stop();

        const list = data.collections ?? [];

        if (list.length === 0) {
          output.info('No collections found. Create one with `contento collections create`.');
          return;
        }

        output.table(
          ['ID', 'Schema', 'URL Pattern', 'Status', 'Generated', 'Total'],
          list.map((c) => [
            c.id,
            c.schemaSlug,
            c.urlPattern,
            c.status,
            c.pagesGenerated ?? 0,
            c.pagesTotal ?? 0,
          ]),
        );
      } catch (err) {
        spinner.fail('Failed to fetch collections');
        handleError(err);
      }
    });

  // Create collection
  collections
    .command('create')
    .description('Create a new collection (interactive niche selection if --niches omitted)')
    .requiredOption('--project <id>', 'Project ID')
    .option('--schema <slug>', 'Schema slug')
    .option('--niches <ids>', 'Comma-separated niche IDs (interactive if omitted)')
    .option('--url-pattern <pattern>', 'URL pattern (e.g., /{slug})')
    .option('--title-template <template>', 'Title template (e.g., "{keyword} Guide")')
    .action(
      async (opts: {
        project: string;
        schema?: string;
        niches?: string;
        urlPattern?: string;
        titleTemplate?: string;
      }) => {
        try {
          const client = createClient({ baseUrl: program.opts().apiUrl });
          const isTTY = process.stdout.isTTY && !isJsonMode();

          // --- Resolve schema ---
          let schemaSlug = opts.schema;
          if (!schemaSlug) {
            if (!isTTY) {
              output.error('--schema is required in non-interactive mode');
              process.exit(1);
            }
            const spinner = createSpinner('Loading schemas...').start();
            const schemasData = await client.get<{ schemas: { slug: string; name: string; titlePattern: string }[] }>('/pseo/schemas');
            spinner.stop();

            schemaSlug = await select({
              message: 'Select a content schema',
              choices: schemasData.schemas.map((s) => ({
                name: `${s.name} ${chalk.dim(`— ${s.titlePattern}`)}`,
                value: s.slug,
              })),
            });
          }

          // --- Resolve niches ---
          let nicheIds: string[];
          if (opts.niches) {
            nicheIds = opts.niches.split(',').map((s) => s.trim());
          } else {
            if (!isTTY) {
              output.error('--niches is required in non-interactive mode');
              process.exit(1);
            }

            // Fetch all niches and let user select interactively
            const spinner = createSpinner('Loading niches...').start();
            const nichesData = await client.get<{ niches: { id: string; name: string; slug: string; category?: string; keywords?: string[] }[] }>('/pseo/niches');
            spinner.stop();

            const niches = nichesData.niches ?? [];
            const categories = new Map<string, typeof niches>();
            for (const n of niches) {
              const cat = n.category ?? 'Uncategorized';
              if (!categories.has(cat)) categories.set(cat, []);
              categories.get(cat)!.push(n);
            }

            // Pick category first
            const selectedCat = await select({
              message: 'Select a niche category',
              choices: [...categories.keys()].sort().map((cat) => ({
                name: `${cat} ${chalk.dim(`(${categories.get(cat)!.length} niches)`)}`,
                value: cat,
              })),
            });

            // Pick niches from category
            nicheIds = await checkbox({
              message: `Select niches from ${selectedCat}`,
              choices: categories.get(selectedCat)!.map((n) => ({
                name: `${n.name}${n.keywords?.length ? chalk.dim(` — ${n.keywords.slice(0, 3).join(', ')}`) : ''}`,
                value: n.id,
              })),
              required: true,
            });

            // Offer to add from other categories
            let addMore = true;
            while (addMore) {
              addMore = await select({
                message: `${nicheIds.length} niche(s) selected. Add from another category?`,
                choices: [
                  { name: 'No, continue', value: false },
                  { name: 'Yes, browse another category', value: true },
                ],
              });

              if (addMore) {
                const anotherCat = await select({
                  message: 'Select another category',
                  choices: [...categories.keys()]
                    .sort()
                    .filter((c) => c !== selectedCat)
                    .map((cat) => ({
                      name: `${cat} ${chalk.dim(`(${categories.get(cat)!.length})`)}`,
                      value: cat,
                    })),
                });

                const moreIds = await checkbox({
                  message: `Select niches from ${anotherCat}`,
                  choices: categories.get(anotherCat)!.map((n) => ({
                    name: `${n.name}${n.keywords?.length ? chalk.dim(` — ${n.keywords.slice(0, 3).join(', ')}`) : ''}`,
                    value: n.id,
                    checked: nicheIds.includes(n.id),
                  })),
                });

                for (const id of moreIds) {
                  if (!nicheIds.includes(id)) nicheIds.push(id);
                }
              }
            }
          }

          // --- Resolve URL pattern ---
          let urlPattern = opts.urlPattern;
          if (!urlPattern && isTTY) {
            urlPattern = await input({
              message: 'URL pattern (e.g., /{slug})',
              default: '/{slug}',
            });
          }
          urlPattern = urlPattern ?? '/{slug}';

          // --- Resolve title template ---
          let titleTemplate = opts.titleTemplate;
          if (!titleTemplate && isTTY) {
            titleTemplate = await input({
              message: 'Title template (use {subtopic}, {niche} tokens)',
              default: '{subtopic} — Complete Guide',
            });
          }
          titleTemplate = titleTemplate ?? '{subtopic} — Complete Guide';

          // --- Create collection ---
          const spinner = createSpinner('Creating collection...').start();
          const data = await client.post<CollectionResponse>(
            `/pseo/projects/${opts.project}/collections`,
            {
              schemaSlug,
              nicheIds,
              urlPattern,
              titleTemplate,
            },
          );
          spinner.succeed('Collection created');

          const c = data.collection;
          output.success(`Collection created with ID: ${c.id}`);

          output.table(
            ['Field', 'Value'],
            [
              ['ID', c.id],
              ['Schema', c.schemaSlug],
              ['Niches', `${nicheIds.length} selected`],
              ['URL Pattern', c.urlPattern],
              ['Title Template', c.titleTemplate],
              ['Status', c.status],
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

  // Generate content for a collection
  collections
    .command('generate')
    .description('Generate content for a collection (streams progress by default)')
    .argument('<id>', 'Collection ID')
    .option('--no-wait', 'Return job ID immediately without streaming progress')
    .action(async (id: string, opts: { wait: boolean }) => {
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });

        // Start generation
        const spinner = createSpinner('Starting generation...').start();
        const data = await client.post<GenerateResponse>(
          `/pseo/collections/${id}/generate`,
        );
        spinner.succeed(`Generation started (Job: ${data.jobId})`);

        if (!opts.wait) {
          // --no-wait: just return the job ID
          output.success(`Job started: ${data.jobId}`, { jobId: data.jobId });
          return;
        }

        // Stream progress via SSE
        const streamUrl =
          data.streamUrl ?? client.streamUrl(`/pseo/jobs/${data.jobId}/stream`);

        output.blank();
        const progress = createProgressBar('Generating');

        let started = false;

        await connectSSE({
          url: streamUrl,
          onProgress: (p: SSEProgress) => {
            if (!started && p.total > 0) {
              progress.start(p.total, p.current);
              started = true;
            } else if (started) {
              progress.update(p.current, { stage: p.message ?? p.stage });
            }
          },
          onComplete: (result) => {
            progress.stop();
            output.blank();
            output.success('Generation complete!', result);
          },
          onError: (err) => {
            progress.stop();
            output.blank();
            output.error(`Generation failed: ${err.message}`);
            process.exit(1);
          },
        });
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

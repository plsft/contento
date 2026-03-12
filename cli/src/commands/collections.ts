import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output, isJsonMode } from '../output.js';
import { createSpinner } from '../ui/spinner.js';
import { createProgressBar } from '../ui/progress.js';
import { connectSSE } from '../sse-client.js';
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
    .description('Create a new collection')
    .requiredOption('--project <id>', 'Project ID')
    .requiredOption('--schema <slug>', 'Schema slug')
    .requiredOption('--niches <ids>', 'Comma-separated niche IDs')
    .requiredOption('--url-pattern <pattern>', 'URL pattern (e.g., /blog/{slug})')
    .requiredOption('--title-template <template>', 'Title template (e.g., "{keyword} Guide")')
    .action(
      async (opts: {
        project: string;
        schema: string;
        niches: string;
        urlPattern: string;
        titleTemplate: string;
      }) => {
        const spinner = createSpinner('Creating collection...').start();
        try {
          const client = createClient({ baseUrl: program.opts().apiUrl });
          const nicheIds = opts.niches.split(',').map((s) => s.trim());

          const data = await client.post<CollectionResponse>(
            `/pseo/projects/${opts.project}/collections`,
            {
              schemaSlug: opts.schema,
              nicheIds,
              urlPattern: opts.urlPattern,
              titleTemplate: opts.titleTemplate,
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
              ['URL Pattern', c.urlPattern],
              ['Title Template', c.titleTemplate],
              ['Status', c.status],
            ],
          );
        } catch (err) {
          spinner.fail('Failed to create collection');
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

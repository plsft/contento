import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output } from '../output.js';
import { createSpinner } from '../ui/spinner.js';

interface PublishResponse {
  status: string;
  message?: string;
  publishedCount?: number;
  scheduledCount?: number;
  jobId?: string;
}

type PublishMode = 'immediate' | 'batched' | 'scheduled' | 'manual';

export function registerPublishCommand(program: Command): void {
  program
    .command('publish')
    .description('Publish a collection')
    .argument('<collection-id>', 'Collection ID to publish')
    .option('--mode <mode>', 'Publish mode: immediate, batched, scheduled, manual', 'immediate')
    .option('--batch-size <n>', 'Number of pages per batch (for batched mode)', '50')
    .action(async (collectionId: string, opts: { mode: string; batchSize: string }) => {
      const mode = opts.mode as PublishMode;
      const validModes: PublishMode[] = ['immediate', 'batched', 'scheduled', 'manual'];

      if (!validModes.includes(mode)) {
        output.error(`Invalid mode "${mode}". Valid modes: ${validModes.join(', ')}`);
        process.exit(1);
      }

      const batchSize = parseInt(opts.batchSize, 10);
      if (isNaN(batchSize) || batchSize < 1) {
        output.error('Batch size must be a positive integer');
        process.exit(1);
      }

      const spinner = createSpinner(`Publishing collection (${mode})...`).start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.post<PublishResponse>(
          `/pseo/collections/${collectionId}/publish`,
          {
            mode,
            batchSize: mode === 'batched' ? batchSize : undefined,
          },
        );
        spinner.succeed('Publish initiated');

        switch (mode) {
          case 'immediate':
            output.success(
              `Published ${data.publishedCount ?? 'all'} pages immediately.`,
              data,
            );
            break;
          case 'batched':
            output.success(
              `Batched publishing started. ${data.scheduledCount ?? 0} pages queued in batches of ${batchSize}.`,
              data,
            );
            break;
          case 'scheduled':
            output.success(
              `${data.scheduledCount ?? 0} pages scheduled for publishing.`,
              data,
            );
            break;
          case 'manual':
            output.success(
              'Pages marked for manual review. Publish individually from the dashboard.',
              data,
            );
            break;
        }
      } catch (err) {
        spinner.fail('Publish failed');
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

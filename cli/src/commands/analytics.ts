import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output, isJsonMode } from '../output.js';
import { createSpinner } from '../ui/spinner.js';

interface AnalyticsSummary {
  totalPages: number;
  totalImpressions: number;
  totalClicks: number;
  averageCtr: number;
  averagePosition: number;
  indexedPages: number;
  period: string;
}

interface AnalyticsResponse {
  summary: AnalyticsSummary;
}

interface PageMetric {
  url: string;
  impressions: number;
  clicks: number;
  ctr: number;
  position: number;
}

interface TopPagesResponse {
  pages: PageMetric[];
}

interface ZeroTrafficResponse {
  pages: { url: string; status: string; createdAt: string }[];
}

interface ExportResponse {
  csv?: string;
  downloadUrl?: string;
}

export function registerAnalyticsCommand(program: Command): void {
  const analytics = program
    .command('analytics')
    .description('View project analytics');

  // Summary
  analytics
    .command('summary')
    .description('Show analytics summary for a project')
    .argument('<project-id>', 'Project ID')
    .option('--days <n>', 'Number of days to look back', '30')
    .action(async (projectId: string, opts: { days: string }) => {
      const spinner = createSpinner('Fetching analytics...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<AnalyticsResponse>(
          `/pseo/projects/${projectId}/analytics?days=${opts.days}`,
        );
        spinner.stop();

        const s = data.summary;

        if (isJsonMode()) {
          output.json(s);
        } else {
          output.table(
            ['Metric', 'Value'],
            [
              ['Period', s.period],
              ['Total Pages', s.totalPages],
              ['Indexed Pages', s.indexedPages],
              ['Impressions', s.totalImpressions],
              ['Clicks', s.totalClicks],
              ['Avg CTR', `${(s.averageCtr * 100).toFixed(2)}%`],
              ['Avg Position', s.averagePosition.toFixed(1)],
            ],
          );
        }
      } catch (err) {
        spinner.fail('Failed to fetch analytics');
        handleError(err);
      }
    });

  // Top pages
  analytics
    .command('top-pages')
    .description('Show top performing pages')
    .argument('<project-id>', 'Project ID')
    .option('--days <n>', 'Number of days to look back', '30')
    .option('--limit <n>', 'Number of pages to show', '20')
    .action(async (projectId: string, opts: { days: string; limit: string }) => {
      const spinner = createSpinner('Fetching top pages...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<TopPagesResponse>(
          `/pseo/projects/${projectId}/analytics/top-pages?days=${opts.days}&limit=${opts.limit}`,
        );
        spinner.stop();

        const pages = data.pages ?? [];

        if (pages.length === 0) {
          output.info('No page data available yet.');
          return;
        }

        output.table(
          ['URL', 'Impressions', 'Clicks', 'CTR', 'Position'],
          pages.map((p) => [
            p.url,
            p.impressions,
            p.clicks,
            `${(p.ctr * 100).toFixed(2)}%`,
            p.position.toFixed(1),
          ]),
        );
      } catch (err) {
        spinner.fail('Failed to fetch top pages');
        handleError(err);
      }
    });

  // Zero-traffic pages
  analytics
    .command('zero-traffic')
    .description('Show pages with zero traffic')
    .argument('<project-id>', 'Project ID')
    .option('--days <n>', 'Minimum age in days before flagging', '14')
    .action(async (projectId: string, opts: { days: string }) => {
      const spinner = createSpinner('Fetching zero-traffic pages...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<ZeroTrafficResponse>(
          `/pseo/projects/${projectId}/analytics/zero-traffic?days=${opts.days}`,
        );
        spinner.stop();

        const pages = data.pages ?? [];

        if (pages.length === 0) {
          output.success('No zero-traffic pages found. All pages are getting traffic!');
          return;
        }

        output.warn(`${pages.length} pages with zero traffic:`);
        output.table(
          ['URL', 'Status', 'Created'],
          pages.map((p) => [p.url, p.status, p.createdAt]),
        );
      } catch (err) {
        spinner.fail('Failed to fetch zero-traffic pages');
        handleError(err);
      }
    });

  // Export analytics
  analytics
    .command('export')
    .description('Export analytics data')
    .argument('<project-id>', 'Project ID')
    .option('--format <format>', 'Export format: csv', 'csv')
    .option('--days <n>', 'Number of days to look back', '30')
    .action(async (projectId: string, opts: { format: string; days: string }) => {
      if (opts.format !== 'csv') {
        output.error(`Unsupported format "${opts.format}". Currently supported: csv`);
        process.exit(1);
      }

      const spinner = createSpinner('Exporting analytics...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<ExportResponse>(
          `/pseo/projects/${projectId}/analytics/export?format=${opts.format}&days=${opts.days}`,
        );
        spinner.stop();

        if (data.csv) {
          // Write CSV directly to stdout for piping
          process.stdout.write(data.csv);
        } else if (data.downloadUrl) {
          output.success(`Export ready: ${data.downloadUrl}`);
        } else {
          output.success('Export complete', data);
        }
      } catch (err) {
        spinner.fail('Export failed');
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

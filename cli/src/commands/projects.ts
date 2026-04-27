import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output } from '../output.js';
import { createSpinner } from '../ui/spinner.js';

interface Project {
  id: string;
  name: string;
  domain: string;
  subdomain: string;
  status: string;
  pagesCount?: number;
  createdAt?: string;
}

interface ProjectsResponse {
  projects: Project[];
}

interface ProjectResponse {
  project: Project;
}

export function registerProjectsCommand(program: Command): void {
  const projects = program
    .command('projects')
    .description('Manage pSEO projects');

  // List projects
  projects
    .command('list')
    .description('List all projects')
    .action(async () => {
      const spinner = createSpinner('Fetching projects...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<ProjectsResponse>('/pseo/projects');
        spinner.stop();

        const projectList = data.projects ?? [];

        if (projectList.length === 0) {
          output.info('No projects found. Create one with `contento projects create`.');
          return;
        }

        output.table(
          ['ID', 'Name', 'Domain', 'Subdomain', 'Status', 'Pages'],
          projectList.map((p) => [
            p.id,
            p.name,
            p.domain ?? '',
            p.subdomain ?? '',
            p.status ?? '',
            p.pagesCount ?? 0,
          ]),
        );
      } catch (err) {
        spinner.fail('Failed to fetch projects');
        handleError(err);
      }
    });

  // Create project
  projects
    .command('create')
    .description('Create a new project')
    .requiredOption('--name <name>', 'Project name')
    .requiredOption('--domain <domain>', 'Root domain (e.g., example.com)')
    .requiredOption('--subdomain <subdomain>', 'Subdomain prefix')
    .action(async (opts: { name: string; domain: string; subdomain: string }) => {
      const spinner = createSpinner('Creating project...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.post<ProjectResponse>('/pseo/projects', {
          name: opts.name,
          domain: opts.domain,
          subdomain: opts.subdomain,
        });
        spinner.succeed('Project created');

        const p = data.project;
        output.success(`Project "${p.name}" created with ID: ${p.id}`);

        if (!output) return;
        output.table(
          ['Field', 'Value'],
          [
            ['ID', p.id],
            ['Name', p.name],
            ['Domain', p.domain],
            ['Subdomain', p.subdomain],
            ['Status', p.status],
          ],
        );
      } catch (err) {
        spinner.fail('Failed to create project');
        handleError(err);
      }
    });

  // Project status
  projects
    .command('status')
    .description('Get project status and details')
    .argument('<id>', 'Project ID')
    .action(async (id: string) => {
      const spinner = createSpinner('Fetching project status...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<ProjectResponse>(`/pseo/projects/${id}`);
        spinner.stop();

        const p = data.project;
        output.table(
          ['Field', 'Value'],
          [
            ['ID', p.id],
            ['Name', p.name],
            ['Domain', p.domain],
            ['Subdomain', p.subdomain],
            ['Status', p.status],
            ['Pages', p.pagesCount ?? 0],
            ['Created', p.createdAt ?? ''],
          ],
        );
      } catch (err) {
        spinner.fail('Failed to fetch project status');
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

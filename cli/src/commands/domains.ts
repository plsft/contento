import { Command } from 'commander';
import { createClient, ApiError } from '../api-client.js';
import { output, isJsonMode } from '../output.js';
import { createSpinner } from '../ui/spinner.js';

interface Domain {
  subdomain: string;
  domain: string;
  fullDomain: string;
  verified: boolean;
  dnsRecords?: DnsRecord[];
  sslStatus?: string;
}

interface DnsRecord {
  type: string;
  name: string;
  value: string;
  status: string;
}

interface DomainResponse {
  domain: Domain;
}

interface DomainStatusResponse {
  domain: Domain;
  verified: boolean;
  ssl: string;
}

export function registerDomainsCommand(program: Command): void {
  const domains = program
    .command('domains')
    .description('Manage custom domains');

  // Add domain
  domains
    .command('add')
    .description('Add a custom domain to a project')
    .requiredOption('--project <id>', 'Project ID')
    .requiredOption('--subdomain <subdomain>', 'Subdomain (e.g., blog)')
    .requiredOption('--domain <domain>', 'Root domain (e.g., example.com)')
    .action(async (opts: { project: string; subdomain: string; domain: string }) => {
      const spinner = createSpinner('Adding domain...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.post<DomainResponse>(
          `/pseo/projects/${opts.project}/domains`,
          {
            subdomain: opts.subdomain,
            domain: opts.domain,
          },
        );
        spinner.succeed('Domain added');

        const d = data.domain;
        output.success(`Domain ${d.fullDomain} added to project.`);

        if (d.dnsRecords && d.dnsRecords.length > 0) {
          output.blank();
          output.info('Configure the following DNS records:');
          output.table(
            ['Type', 'Name', 'Value', 'Status'],
            d.dnsRecords.map((r) => [r.type, r.name, r.value, r.status]),
          );
        }
      } catch (err) {
        spinner.fail('Failed to add domain');
        handleError(err);
      }
    });

  // Verify domain
  domains
    .command('verify')
    .description('Verify DNS configuration for a project domain')
    .argument('<project-id>', 'Project ID')
    .action(async (projectId: string) => {
      const spinner = createSpinner('Verifying DNS configuration...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.post<DomainStatusResponse>(
          `/pseo/projects/${projectId}/domains/verify`,
        );

        if (data.verified) {
          spinner.succeed('Domain verified');
          output.success(`Domain is verified and ${data.ssl === 'active' ? 'SSL is active' : 'SSL is provisioning'}.`);
        } else {
          spinner.fail('Domain not yet verified');
          output.warn('DNS records have not propagated yet. This can take up to 48 hours.');

          if (data.domain.dnsRecords) {
            output.blank();
            output.info('Required DNS records:');
            output.table(
              ['Type', 'Name', 'Value', 'Status'],
              data.domain.dnsRecords.map((r) => [r.type, r.name, r.value, r.status]),
            );
          }
        }
      } catch (err) {
        spinner.fail('Verification failed');
        handleError(err);
      }
    });

  // Domain status
  domains
    .command('status')
    .description('Check domain and SSL status')
    .argument('<project-id>', 'Project ID')
    .action(async (projectId: string) => {
      const spinner = createSpinner('Checking domain status...').start();
      try {
        const client = createClient({ baseUrl: program.opts().apiUrl });
        const data = await client.get<DomainStatusResponse>(
          `/pseo/projects/${projectId}/domains/status`,
        );
        spinner.stop();

        const d = data.domain;

        if (isJsonMode()) {
          output.json(data);
        } else {
          output.table(
            ['Field', 'Value'],
            [
              ['Domain', d.fullDomain],
              ['Verified', d.verified ? 'Yes' : 'No'],
              ['SSL', data.ssl ?? 'unknown'],
            ],
          );

          if (d.dnsRecords && d.dnsRecords.length > 0) {
            output.blank();
            output.info('DNS Records:');
            output.table(
              ['Type', 'Name', 'Value', 'Status'],
              d.dnsRecords.map((r) => [r.type, r.name, r.value, r.status]),
            );
          }
        }
      } catch (err) {
        spinner.fail('Failed to check domain status');
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

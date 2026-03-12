import { Command } from 'commander';
import { input, password } from '@inquirer/prompts';
import { saveConfig, getConfig, clearConfig, getConfigPath } from '../config.js';
import { validateApiKey } from '../api-client.js';
import { output, isJsonMode } from '../output.js';
import { createSpinner } from '../ui/spinner.js';

export function registerLoginCommand(program: Command): void {
  program
    .command('login')
    .description('Authenticate with the Contento API')
    .option('--api-key <key>', 'API key (non-interactive)')
    .action(async (opts: { apiKey?: string }) => {
      try {
        let apiKey: string;

        if (opts.apiKey) {
          // Non-interactive mode (for agents)
          apiKey = opts.apiKey;
        } else if (process.stdin.isTTY) {
          // Interactive mode
          output.blank();
          output.info('Enter your Contento API key. You can find it at https://app.contentocms.com/settings/api');
          output.blank();

          apiKey = await password({
            message: 'API Key:',
            mask: '*',
          });
        } else {
          // Piped/non-TTY without --api-key flag
          output.error('Non-interactive mode requires --api-key flag');
          process.exit(1);
        }

        if (!apiKey || apiKey.trim() === '') {
          output.error('API key cannot be empty');
          process.exit(1);
        }

        apiKey = apiKey.trim();

        // Get current base URL override if one exists
        const baseUrl = program.opts().apiUrl as string | undefined;

        // Validate the key
        const spinner = createSpinner('Validating API key...').start();

        const valid = await validateApiKey(apiKey, baseUrl);

        if (!valid) {
          spinner.fail('Invalid API key');
          output.error('The API key could not be validated. Please check your key and try again.');
          process.exit(1);
        }

        spinner.succeed('API key validated');

        // Save config
        saveConfig({ apiKey, baseUrl });

        output.success(`Logged in successfully. Config saved to ${getConfigPath()}`);
      } catch (err) {
        output.error('Login failed', err instanceof Error ? err.message : String(err));
        process.exit(1);
      }
    });

  program
    .command('logout')
    .description('Remove stored credentials')
    .action(() => {
      try {
        clearConfig();
        output.success('Logged out. Credentials removed.');
      } catch (err) {
        output.error('Logout failed', err instanceof Error ? err.message : String(err));
        process.exit(1);
      }
    });

  program
    .command('whoami')
    .description('Show current authentication status')
    .action(() => {
      try {
        const config = getConfig();
        if (!config?.apiKey) {
          if (isJsonMode()) {
            output.json({ authenticated: false });
          } else {
            output.warn('Not logged in. Run `contento login` to authenticate.');
          }
          return;
        }

        const maskedKey = config.apiKey.slice(0, 8) + '...' + config.apiKey.slice(-4);
        if (isJsonMode()) {
          output.json({
            authenticated: true,
            apiKey: maskedKey,
            baseUrl: config.baseUrl ?? 'https://api.contentocms.com/v1',
            configPath: getConfigPath(),
          });
        } else {
          output.success('Authenticated');
          output.text(`  API Key:  ${maskedKey}`);
          output.text(`  Base URL: ${config.baseUrl ?? 'https://api.contentocms.com/v1'}`);
          output.text(`  Config:   ${getConfigPath()}`);
        }
      } catch (err) {
        output.error('Failed to read config', err instanceof Error ? err.message : String(err));
        process.exit(1);
      }
    });
}

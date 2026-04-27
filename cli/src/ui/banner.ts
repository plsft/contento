import chalk from 'chalk';
import { isJsonMode } from '../output.js';

const BANNER = `
   ${chalk.cyan('\u250C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510')}
   ${chalk.cyan('\u2502')}  ${chalk.white.bold('contento')} ${chalk.dim('\u2014 pSEO engine')}             ${chalk.cyan('\u2502')}
   ${chalk.cyan('\u2502')}  ${chalk.dim('cloud-hosted \u00B7 cli-first \u00B7 agentic')} ${chalk.cyan('\u2502')}
   ${chalk.cyan('\u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518')}
`;

/**
 * Show the ASCII banner. No-op in --json mode.
 */
export function showBanner(): void {
  if (!isJsonMode()) {
    console.log(BANNER);
  }
}

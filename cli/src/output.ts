import chalk from 'chalk';
import Table from 'cli-table3';

let jsonMode = false;
let verboseMode = false;

export function setJsonMode(enabled: boolean): void {
  jsonMode = enabled;
}

export function setVerboseMode(enabled: boolean): void {
  verboseMode = enabled;
}

export function isJsonMode(): boolean {
  return jsonMode;
}

export function isVerboseMode(): boolean {
  return verboseMode;
}

export const output = {
  /**
   * Output structured JSON to stdout.
   */
  json(data: unknown): void {
    process.stdout.write(JSON.stringify(data, null, 2) + '\n');
  },

  /**
   * Output NDJSON (single line).
   */
  ndjson(data: unknown): void {
    process.stdout.write(JSON.stringify(data) + '\n');
  },

  /**
   * Success message — green checkmark in pretty mode, included in JSON.
   */
  success(message: string, data?: unknown): void {
    if (jsonMode) {
      this.json({ success: true, message, ...(data !== undefined ? { data } : {}) });
    } else {
      console.log(chalk.green('\u2714') + ' ' + message);
    }
  },

  /**
   * Error message — red cross in pretty mode, JSON to stderr in --json mode.
   */
  error(message: string, details?: unknown): void {
    if (jsonMode) {
      process.stderr.write(
        JSON.stringify({ error: true, message, ...(details !== undefined ? { details } : {}) }) + '\n',
      );
    } else {
      console.error(chalk.red('\u2718') + ' ' + message);
      if (details && verboseMode) {
        console.error(chalk.dim(typeof details === 'string' ? details : JSON.stringify(details, null, 2)));
      }
    }
  },

  /**
   * Warning message.
   */
  warn(message: string): void {
    if (jsonMode) {
      process.stderr.write(JSON.stringify({ warning: true, message }) + '\n');
    } else {
      console.warn(chalk.yellow('\u26A0') + ' ' + message);
    }
  },

  /**
   * Info message — only in pretty mode.
   */
  info(message: string): void {
    if (!jsonMode) {
      console.log(chalk.blue('\u2139') + ' ' + message);
    }
  },

  /**
   * Verbose/debug message — only in verbose mode.
   */
  verbose(message: string): void {
    if (verboseMode && !jsonMode) {
      console.log(chalk.dim('[verbose] ' + message));
    }
  },

  /**
   * Table output — formatted table in pretty mode, JSON array in --json mode.
   */
  table(headers: string[], rows: (string | number)[][]): void {
    if (jsonMode) {
      const objects = rows.map((row) => {
        const obj: Record<string, string | number> = {};
        headers.forEach((h, i) => {
          obj[h] = row[i] ?? '';
        });
        return obj;
      });
      this.json(objects);
    } else {
      const table = new Table({
        head: headers.map((h) => chalk.cyan.bold(h)),
        style: { head: [], border: [] },
      });
      for (const row of rows) {
        table.push(row.map(String));
      }
      console.log(table.toString());
    }
  },

  /**
   * Plain text output — only in pretty mode.
   */
  text(message: string): void {
    if (!jsonMode) {
      console.log(message);
    }
  },

  /**
   * Blank line — only in pretty mode.
   */
  blank(): void {
    if (!jsonMode) {
      console.log();
    }
  },
};

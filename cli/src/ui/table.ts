import chalk from 'chalk';
import Table from 'cli-table3';
import { isJsonMode, output } from '../output.js';

export interface TableOptions {
  /** Column headers */
  headers: string[];
  /** Row data — each row is an array of cell values */
  rows: (string | number)[][];
  /** Optional: compact style (no borders) */
  compact?: boolean;
}

/**
 * Render a table. In --json mode, outputs a JSON array of objects keyed by header names.
 */
export function renderTable(options: TableOptions): void {
  const { headers, rows, compact } = options;

  if (isJsonMode()) {
    const objects = rows.map((row) => {
      const obj: Record<string, string | number> = {};
      headers.forEach((h, i) => {
        obj[h] = row[i] ?? '';
      });
      return obj;
    });
    output.json(objects);
    return;
  }

  const tableOpts: Table.TableConstructorOptions = {
    head: headers.map((h) => chalk.cyan.bold(h)),
    style: { head: [], border: [] },
  };

  if (compact) {
    tableOpts.chars = {
      top: '',
      'top-mid': '',
      'top-left': '',
      'top-right': '',
      bottom: '',
      'bottom-mid': '',
      'bottom-left': '',
      'bottom-right': '',
      left: '',
      'left-mid': '',
      mid: '',
      'mid-mid': '',
      right: '',
      'right-mid': '',
      middle: '  ',
    };
    tableOpts.style = { head: [], border: [], 'padding-left': 0, 'padding-right': 1 };
  }

  const table = new Table(tableOpts);
  for (const row of rows) {
    table.push(row.map(String));
  }
  console.log(table.toString());
}

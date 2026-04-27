import cliProgress from 'cli-progress';
import chalk from 'chalk';
import { isJsonMode, output } from '../output.js';

export interface ProgressBar {
  start(total: number, initial?: number): void;
  update(current: number, payload?: Record<string, unknown>): void;
  increment(delta?: number, payload?: Record<string, unknown>): void;
  stop(): void;
}

class CliProgressBar implements ProgressBar {
  private bar: cliProgress.SingleBar;

  constructor(label: string) {
    this.bar = new cliProgress.SingleBar(
      {
        format: `${chalk.cyan(label)} ${chalk.white('{bar}')} {percentage}% | {value}/{total} | {stage}`,
        hideCursor: true,
        clearOnComplete: false,
        stopOnComplete: true,
      },
      cliProgress.Presets.shades_classic,
    );
  }

  start(total: number, initial = 0): void {
    this.bar.start(total, initial, { stage: '' });
  }

  update(current: number, payload?: Record<string, unknown>): void {
    this.bar.update(current, payload);
  }

  increment(delta = 1, payload?: Record<string, unknown>): void {
    this.bar.increment(delta, payload);
  }

  stop(): void {
    this.bar.stop();
  }
}

class NdjsonProgressBar implements ProgressBar {
  private total = 0;
  private current = 0;
  private readonly label: string;

  constructor(label: string) {
    this.label = label;
  }

  start(total: number, initial = 0): void {
    this.total = total;
    this.current = initial;
    output.ndjson({ type: 'progress', label: this.label, current: this.current, total: this.total });
  }

  update(current: number, payload?: Record<string, unknown>): void {
    this.current = current;
    output.ndjson({
      type: 'progress',
      label: this.label,
      current: this.current,
      total: this.total,
      ...payload,
    });
  }

  increment(delta = 1, payload?: Record<string, unknown>): void {
    this.current += delta;
    output.ndjson({
      type: 'progress',
      label: this.label,
      current: this.current,
      total: this.total,
      ...payload,
    });
  }

  stop(): void {
    output.ndjson({
      type: 'progress',
      label: this.label,
      current: this.current,
      total: this.total,
      done: true,
    });
  }
}

/**
 * Create a progress bar. Writes NDJSON in --json mode.
 */
export function createProgressBar(label: string): ProgressBar {
  if (isJsonMode()) {
    return new NdjsonProgressBar(label);
  }
  return new CliProgressBar(label);
}

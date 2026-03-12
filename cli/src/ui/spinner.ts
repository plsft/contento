import ora from 'ora';
import type { Ora } from 'ora';
import { isJsonMode } from '../output.js';

export interface Spinner {
  start(text?: string): Spinner;
  stop(): Spinner;
  succeed(text?: string): Spinner;
  fail(text?: string): Spinner;
  text: string;
}

class NoopSpinner implements Spinner {
  text = '';

  start(_text?: string): Spinner {
    return this;
  }

  stop(): Spinner {
    return this;
  }

  succeed(_text?: string): Spinner {
    return this;
  }

  fail(_text?: string): Spinner {
    return this;
  }
}

class OraSpinner implements Spinner {
  private readonly spinner: Ora;

  constructor(text: string) {
    this.spinner = ora({ text, spinner: 'dots' });
  }

  get text(): string {
    return this.spinner.text;
  }

  set text(value: string) {
    this.spinner.text = value;
  }

  start(text?: string): Spinner {
    this.spinner.start(text);
    return this;
  }

  stop(): Spinner {
    this.spinner.stop();
    return this;
  }

  succeed(text?: string): Spinner {
    this.spinner.succeed(text);
    return this;
  }

  fail(text?: string): Spinner {
    this.spinner.fail(text);
    return this;
  }
}

/**
 * Create a spinner. Returns a noop spinner in --json mode.
 */
export function createSpinner(text: string): Spinner {
  if (isJsonMode()) {
    return new NoopSpinner();
  }
  return new OraSpinner(text);
}

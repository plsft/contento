import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Command } from 'commander';
import { registerSchemasCommand } from './schemas.js';

const { mockOutput, jsonModeRef } = vi.hoisted(() => ({
  mockOutput: {
    success: vi.fn(),
    error: vi.fn(),
    warn: vi.fn(),
    info: vi.fn(),
    table: vi.fn(),
    json: vi.fn(),
    text: vi.fn(),
    blank: vi.fn(),
  },
  jsonModeRef: { value: false },
}));

vi.mock('../output.js', () => ({
  output: mockOutput,
  isJsonMode: () => jsonModeRef.value,
}));

function buildProgram(): Command {
  const program = new Command();
  program.exitOverride();
  registerSchemasCommand(program);
  return program;
}

async function run(...args: string[]): Promise<void> {
  const program = buildProgram();
  await program.parseAsync(['node', 'contento', 'schemas', ...args]);
}

beforeEach(() => {
  jsonModeRef.value = false;
  vi.clearAllMocks();
});

describe('schemas list', () => {
  it('lists all bundled schemas', async () => {
    await run('list');
    expect(mockOutput.table).toHaveBeenCalled();
    const [, rows] = mockOutput.table.mock.calls[0];
    expect(rows.length).toBe(10);
  });

  it('emits JSON in --json mode with full schema records', async () => {
    jsonModeRef.value = true;
    await run('list');
    const arr = mockOutput.json.mock.calls[0][0] as Array<{ slug: string }>;
    expect(arr).toHaveLength(10);
    expect(arr.map((s) => s.slug).sort()).toEqual(
      [
        'alternatives',
        'checklist',
        'comparison',
        'faq',
        'glossary',
        'how-to',
        'idea-list',
        'resource-list',
        'template-pack',
        'tool-page',
      ].sort(),
    );
  });
});

describe('schemas view', () => {
  it('shows details for a bundled schema', async () => {
    await run('view', 'idea-list');
    const textCalls = mockOutput.text.mock.calls.map((c) => String(c[0]));
    expect(textCalls.some((c) => c.includes('Idea List'))).toBe(true);
    expect(textCalls.some((c) => c.includes('Title pattern'))).toBe(true);
  });

  it('errors on unknown slug', async () => {
    const exit = vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
      throw new Error(`exit ${code}`);
    }) as never);
    await expect(run('view', 'no-such-schema')).rejects.toThrow();
    expect(mockOutput.error).toHaveBeenCalledWith(expect.stringContaining('no-such-schema'));
    exit.mockRestore();
  });
});

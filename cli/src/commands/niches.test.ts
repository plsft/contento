import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { Command } from 'commander';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

// ── Hoisted mocks ────────────────────────────────────────────────────────────

const { mockOutput, jsonModeRef, mockSelect, mockCheckbox, mockInput, mockConfirm } =
  vi.hoisted(() => ({
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
    mockSelect: vi.fn(),
    mockCheckbox: vi.fn(),
    mockInput: vi.fn(),
    mockConfirm: vi.fn(),
  }));

vi.mock('../output.js', () => ({
  output: mockOutput,
  isJsonMode: () => jsonModeRef.value,
}));

vi.mock('@inquirer/prompts', () => ({
  select: mockSelect,
  checkbox: mockCheckbox,
  input: mockInput,
  confirm: mockConfirm,
}));

// Use a temp HOME so custom-niche files don't pollute the real ~/.contento/
const tempHome = mkdtempSync(join(tmpdir(), 'contento-test-'));
process.env.HOME = tempHome;
process.env.USERPROFILE = tempHome;

// Import AFTER env override so os.homedir() resolves to tempHome
const { registerNichesCommand } = await import('./niches.js');

afterEach(() => {
  // Reset between tests by wiping the custom dir
  try {
    rmSync(join(tempHome, '.contento', 'niches'), { recursive: true, force: true });
  } catch {
    // ignore
  }
});

function buildProgram(): Command {
  const program = new Command();
  program.exitOverride();
  registerNichesCommand(program);
  return program;
}

async function run(...args: string[]): Promise<void> {
  const program = buildProgram();
  await program.parseAsync(['node', 'contento', 'niches', ...args]);
}

beforeEach(() => {
  jsonModeRef.value = false;
  vi.clearAllMocks();
});

describe('niches list', () => {
  it('lists all bundled niches in a table', async () => {
    await run('list');
    expect(mockOutput.table).toHaveBeenCalled();
    const [, rows] = mockOutput.table.mock.calls[0];
    expect(rows.length).toBeGreaterThanOrEqual(150);
  });

  it('filters by category', async () => {
    await run('list', '--category', 'Software / SaaS');
    const [, rows] = mockOutput.table.mock.calls[0];
    expect(rows.length).toBeGreaterThan(0);
    expect(rows.every((row: unknown[]) => row[2] === 'Software / SaaS')).toBe(true);
  });

  it('filters by search query against name and subtopics', async () => {
    await run('list', '--search', 'CLI tools');
    const [, rows] = mockOutput.table.mock.calls[0];
    expect(rows.length).toBeGreaterThan(0);
  });

  it('emits JSON in --json mode', async () => {
    jsonModeRef.value = true;
    await run('list');
    expect(mockOutput.json).toHaveBeenCalledOnce();
    const arr = mockOutput.json.mock.calls[0][0] as unknown[];
    expect(arr.length).toBeGreaterThanOrEqual(150);
  });
});

describe('niches view', () => {
  it('shows details for a bundled niche', async () => {
    await run('view', 'developer-tools');
    expect(mockOutput.text).toHaveBeenCalled();
    const calls = mockOutput.text.mock.calls.map((c) => String(c[0]));
    expect(calls.some((c) => c.includes('Developer Tools'))).toBe(true);
    expect(calls.some((c) => c.includes('Audience'))).toBe(true);
  });

  it('errors on unknown slug', async () => {
    const exit = vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
      throw new Error(`exit ${code}`);
    }) as never);
    await expect(run('view', 'no-such-niche')).rejects.toThrow();
    expect(mockOutput.error).toHaveBeenCalledWith(expect.stringContaining('no-such-niche'));
    exit.mockRestore();
  });
});

describe('niches fork / edit / delete', () => {
  it('forks a bundled niche to the custom store', async () => {
    await run('fork', 'developer-tools', '--name', 'My Dev Tools', '--new-slug', 'my-dev-tools');
    expect(mockOutput.success).toHaveBeenCalledWith(expect.stringContaining('Forked'));

    // Now visible in list
    vi.clearAllMocks();
    await run('list', '--search', 'my-dev-tools');
    const [, rows] = mockOutput.table.mock.calls[0];
    const forkRow = rows.find((r: unknown[]) => r[0] === 'my-dev-tools');
    expect(forkRow).toBeDefined();
    expect(forkRow![4]).toBe('custom');
  });

  it('rejects fork when slug collides with an existing custom niche', async () => {
    await run('fork', 'developer-tools', '--name', 'Dupe', '--new-slug', 'dupe-niche');
    const exit = vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
      throw new Error(`exit ${code}`);
    }) as never);
    await expect(
      run('fork', 'developer-tools', '--name', 'Dupe Again', '--new-slug', 'dupe-niche'),
    ).rejects.toThrow();
    expect(mockOutput.error).toHaveBeenCalledWith(expect.stringContaining('already exists'));
    exit.mockRestore();
  });

  it('refuses to edit a bundled niche', async () => {
    const exit = vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
      throw new Error(`exit ${code}`);
    }) as never);
    await expect(run('edit', 'developer-tools', '--name', 'Hacked')).rejects.toThrow();
    expect(mockOutput.error).toHaveBeenCalledWith(expect.stringContaining('bundled niche'));
    exit.mockRestore();
  });

  it('edits a custom niche and persists changes', async () => {
    await run('fork', 'developer-tools', '--name', 'Edited', '--new-slug', 'edited');
    vi.clearAllMocks();

    await run('edit', 'edited', '--name', 'Edited v2', '--add-subtopics', 'WASM');
    expect(mockOutput.success).toHaveBeenCalledWith(expect.stringContaining('updated'));

    vi.clearAllMocks();
    jsonModeRef.value = true;
    await run('view', 'edited');
    const niche = mockOutput.json.mock.calls[0][0] as { name: string; context: { subtopics: string[] } };
    expect(niche.name).toBe('Edited v2');
    expect(niche.context.subtopics).toContain('WASM');
  });

  it('deletes a custom niche with --yes', async () => {
    await run('fork', 'developer-tools', '--name', 'Doomed', '--new-slug', 'doomed');
    vi.clearAllMocks();
    await run('delete', 'doomed', '--yes');
    expect(mockOutput.success).toHaveBeenCalledWith(expect.stringContaining('deleted'));
  });

  it('refuses to delete a bundled niche', async () => {
    const exit = vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
      throw new Error(`exit ${code}`);
    }) as never);
    await expect(run('delete', 'developer-tools', '--yes')).rejects.toThrow();
    expect(mockOutput.error).toHaveBeenCalledWith(expect.stringContaining('cannot be deleted'));
    exit.mockRestore();
  });
});

describe('niches create', () => {
  it('creates a custom niche from flags', async () => {
    await run(
      'create',
      '--name', 'My Custom',
      '--slug', 'my-custom',
      '--category', 'Custom',
      '--subtopics', 'a, b, c',
      '--audience', 'devs',
      '--pain-points', 'time',
      '--monetization', 'SaaS',
      '--content-that-works', 'guides',
    );
    expect(mockOutput.success).toHaveBeenCalledWith(expect.stringContaining('created'));

    vi.clearAllMocks();
    jsonModeRef.value = true;
    await run('view', 'my-custom');
    const niche = mockOutput.json.mock.calls[0][0] as { context: { subtopics: string[] } };
    expect(niche.context.subtopics).toEqual(['a', 'b', 'c']);
  });

  it('errors when --name is missing in non-interactive mode', async () => {
    const wasTTY = process.stdout.isTTY;
    Object.defineProperty(process.stdout, 'isTTY', { value: false, configurable: true });
    const exit = vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
      throw new Error(`exit ${code}`);
    }) as never);
    await expect(run('create', '--subtopics', 'a,b')).rejects.toThrow();
    expect(mockOutput.error).toHaveBeenCalledWith(expect.stringContaining('--name is required'));
    exit.mockRestore();
    Object.defineProperty(process.stdout, 'isTTY', { value: wasTTY, configurable: true });
  });
});

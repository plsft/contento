import { existsSync, mkdirSync, readFileSync, writeFileSync, unlinkSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

export interface ContentoConfig {
  apiKey: string;
  baseUrl?: string;
}

const CONFIG_DIR = join(homedir(), '.contento');
const CONFIG_FILE = join(CONFIG_DIR, 'config.json');

function ensureConfigDir(): void {
  if (!existsSync(CONFIG_DIR)) {
    mkdirSync(CONFIG_DIR, { recursive: true });
  }
}

export function getConfig(): ContentoConfig | null {
  try {
    if (!existsSync(CONFIG_FILE)) {
      return null;
    }
    const raw = readFileSync(CONFIG_FILE, 'utf-8');
    return JSON.parse(raw) as ContentoConfig;
  } catch {
    return null;
  }
}

export function saveConfig(config: ContentoConfig): void {
  ensureConfigDir();
  writeFileSync(CONFIG_FILE, JSON.stringify(config, null, 2), 'utf-8');
}

export function clearConfig(): void {
  try {
    if (existsSync(CONFIG_FILE)) {
      unlinkSync(CONFIG_FILE);
    }
  } catch {
    // ignore
  }
}

export function getConfigPath(): string {
  return CONFIG_FILE;
}

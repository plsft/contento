import {
  existsSync,
  mkdirSync,
  readFileSync,
  readdirSync,
  unlinkSync,
  writeFileSync,
} from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

import bundledNiches from '../data/niches.json';
import type { Niche } from '../data/types.js';

const CUSTOM_DIR = join(homedir(), '.contento', 'niches');

function ensureCustomDir(): void {
  if (!existsSync(CUSTOM_DIR)) {
    mkdirSync(CUSTOM_DIR, { recursive: true });
  }
}

/**
 * Read-only access to the niches bundled in the npm package.
 * Marks each as `isCustom: false`.
 */
export function getBundledNiches(): Niche[] {
  return (bundledNiches as Niche[]).map((n) => ({ ...n, isCustom: false }));
}

/**
 * Read all custom/forked niches from `~/.contento/niches/`.
 * Files that fail to parse are silently skipped.
 */
export function getCustomNiches(): Niche[] {
  if (!existsSync(CUSTOM_DIR)) return [];
  const niches: Niche[] = [];
  for (const file of readdirSync(CUSTOM_DIR)) {
    if (!file.endsWith('.json')) continue;
    try {
      const raw = readFileSync(join(CUSTOM_DIR, file), 'utf-8');
      const niche = JSON.parse(raw) as Niche;
      niches.push({ ...niche, isCustom: true });
    } catch {
      // skip malformed file
    }
  }
  return niches;
}

/**
 * Merge custom + bundled niches. A custom niche with the same slug as a
 * bundled one shadows the bundled version.
 */
export function getAllNiches(): Niche[] {
  const custom = getCustomNiches();
  const customSlugs = new Set(custom.map((n) => n.slug));
  const bundled = getBundledNiches().filter((n) => !customSlugs.has(n.slug));
  return [...custom, ...bundled].sort((a, b) => a.name.localeCompare(b.name));
}

export function findNicheBySlug(slug: string): Niche | null {
  return getAllNiches().find((n) => n.slug === slug) ?? null;
}

export function getCategories(): string[] {
  const cats = new Set<string>();
  for (const n of getAllNiches()) cats.add(n.category);
  return [...cats].sort();
}

/**
 * Persist a custom niche to `~/.contento/niches/<slug>.json`.
 * Returns the absolute path written.
 */
export function saveCustomNiche(niche: Niche): string {
  ensureCustomDir();
  const path = join(CUSTOM_DIR, `${niche.slug}.json`);
  const { isCustom: _ignore, ...data } = niche;
  writeFileSync(path, JSON.stringify(data, null, 2), 'utf-8');
  return path;
}

/**
 * Delete a custom niche file. Returns true if the file existed and was
 * removed; false if it did not exist (e.g., trying to delete a bundled
 * niche).
 */
export function deleteCustomNiche(slug: string): boolean {
  const path = join(CUSTOM_DIR, `${slug}.json`);
  if (!existsSync(path)) return false;
  unlinkSync(path);
  return true;
}

export function getCustomDir(): string {
  return CUSTOM_DIR;
}

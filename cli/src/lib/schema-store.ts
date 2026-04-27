import bundledSchemas from '../data/schemas.json';
import type { ContentSchema } from '../data/types.js';

/**
 * Schemas ship bundled with the CLI. Custom schemas are not yet supported
 * — this store is read-only.
 */
export function getAllSchemas(): ContentSchema[] {
  return [...(bundledSchemas as ContentSchema[])];
}

export function findSchemaBySlug(slug: string): ContentSchema | null {
  return getAllSchemas().find((s) => s.slug === slug) ?? null;
}

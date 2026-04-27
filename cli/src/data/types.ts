/**
 * Shared types for the bundled niche taxonomy and content schemas.
 *
 * Niches and schemas ship with the CLI as JSON files under `cli/src/data/`.
 * Custom user niches are persisted to `~/.contento/niches/<slug>.json` and
 * merged at read time by `lib/niche-store.ts`.
 */

export interface NicheContext {
  audience: string;
  pain_points: string;
  monetization: string;
  content_that_works: string;
  subtopics: string[];
}

export interface Niche {
  slug: string;
  name: string;
  category: string;
  context: NicheContext;
  /** True for niches loaded from `~/.contento/niches/`; false (or undefined) for bundled. */
  isCustom?: boolean;
}

export interface ContentSchemaSettings {
  [key: string]: unknown;
}

export interface ContentSchema {
  slug: string;
  name: string;
  description: string;
  schemaJson: Record<string, unknown>;
  promptTemplate: string;
  userPromptTemplate: string;
  rendererSlug: string;
  titlePattern: string;
  metaDescPattern: string;
  settings: ContentSchemaSettings;
}

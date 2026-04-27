import { Command } from 'commander';
import { checkbox, select, input, confirm } from '@inquirer/prompts';
import chalk from 'chalk';

import { output, isJsonMode } from '../output.js';
import {
  deleteCustomNiche,
  findNicheBySlug,
  getAllNiches,
  getCustomDir,
  saveCustomNiche,
} from '../lib/niche-store.js';
import type { Niche } from '../data/types.js';

function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function splitList(value: string | undefined): string[] {
  if (!value) return [];
  return value
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean);
}

export function registerNichesCommand(program: Command): void {
  const niches = program
    .command('niches')
    .description('Browse, fork, and customize niches');

  // ── list ─────────────────────────────────────────────────────────────
  niches
    .command('list')
    .description('List bundled and custom niches')
    .option('--category <category>', 'Filter by category')
    .option('--search <query>', 'Search by name, slug, or subtopic')
    .action((opts: { category?: string; search?: string }) => {
      let all = getAllNiches();
      if (opts.category) {
        const needle = opts.category.toLowerCase();
        all = all.filter((n) => n.category.toLowerCase() === needle);
      }
      if (opts.search) {
        const q = opts.search.toLowerCase();
        all = all.filter(
          (n) =>
            n.slug.toLowerCase().includes(q) ||
            n.name.toLowerCase().includes(q) ||
            n.context.subtopics.some((s) => s.toLowerCase().includes(q)),
        );
      }

      if (isJsonMode()) {
        output.json(all);
        return;
      }

      if (all.length === 0) {
        output.info('No niches found matching your criteria.');
        return;
      }

      output.table(
        ['Slug', 'Name', 'Category', 'Subtopics', 'Source'],
        all.map((n) => [
          n.slug,
          n.name,
          n.category,
          n.context.subtopics.length,
          n.isCustom ? 'custom' : 'bundled',
        ]),
      );
    });

  // ── view ─────────────────────────────────────────────────────────────
  niches
    .command('view')
    .description('View detailed information about a niche')
    .argument('<slug>', 'Niche slug')
    .action((slug: string) => {
      const n = findNicheBySlug(slug);
      if (!n) {
        output.error(`No niche found with slug "${slug}".`);
        process.exit(1);
      }

      if (isJsonMode()) {
        output.json(n);
        return;
      }

      output.blank();
      output.text(
        chalk.bold(n.name) +
          chalk.dim(` (${n.slug})`) +
          (n.isCustom ? chalk.yellow.dim(' [custom]') : ''),
      );
      output.text(chalk.dim('Category: ') + n.category);

      output.blank();
      output.text(chalk.cyan.bold('Audience'));
      output.text(n.context.audience);

      output.blank();
      output.text(chalk.cyan.bold('Pain Points'));
      output.text(n.context.pain_points);

      output.blank();
      output.text(chalk.cyan.bold('Monetization'));
      output.text(n.context.monetization);

      output.blank();
      output.text(chalk.cyan.bold('Content That Works'));
      output.text(n.context.content_that_works);

      output.blank();
      output.text(chalk.cyan.bold(`Subtopics (${n.context.subtopics.length})`));
      for (const s of n.context.subtopics) {
        output.text(chalk.dim('  • ') + s);
      }
      output.blank();
    });

  // ── fork ─────────────────────────────────────────────────────────────
  niches
    .command('fork')
    .description('Fork a bundled niche to your local custom store and optionally edit it')
    .argument('<slug>', 'Slug of the bundled niche to fork')
    .option('--name <name>', 'Name for the fork (defaults to "<original> (custom)")')
    .option('--new-slug <slug>', 'Slug for the fork (defaults to a slugified --name)')
    .option('--add-subtopics <list>', 'Comma-separated subtopics to append')
    .option('--remove-subtopics <list>', 'Comma-separated subtopics to remove')
    .option('--audience <text>', 'Override audience')
    .option('--pain-points <text>', 'Override pain points')
    .option('--monetization <text>', 'Override monetization')
    .option('--content-that-works <text>', 'Override content-that-works')
    .action((sourceSlug: string, opts: {
      name?: string;
      newSlug?: string;
      addSubtopics?: string;
      removeSubtopics?: string;
      audience?: string;
      painPoints?: string;
      monetization?: string;
      contentThatWorks?: string;
    }) => {
      const source = findNicheBySlug(sourceSlug);
      if (!source) {
        output.error(`No niche found with slug "${sourceSlug}".`);
        process.exit(1);
      }

      const name = opts.name ?? `${source.name} (custom)`;
      const newSlug = opts.newSlug ?? slugify(name);
      if (newSlug === sourceSlug && !source.isCustom) {
        output.error(
          `--new-slug must differ from the bundled slug. Try: --new-slug ${slugify(name)}`,
        );
        process.exit(1);
      }
      if (findNicheBySlug(newSlug)?.isCustom) {
        output.error(
          `A custom niche with slug "${newSlug}" already exists. Use --new-slug to choose a different slug.`,
        );
        process.exit(1);
      }

      const remove = new Set(splitList(opts.removeSubtopics));
      const additions = splitList(opts.addSubtopics);
      const subtopics = source.context.subtopics
        .filter((s) => !remove.has(s))
        .concat(additions);

      const fork: Niche = {
        slug: newSlug,
        name,
        category: source.category,
        context: {
          audience: opts.audience ?? source.context.audience,
          pain_points: opts.painPoints ?? source.context.pain_points,
          monetization: opts.monetization ?? source.context.monetization,
          content_that_works: opts.contentThatWorks ?? source.context.content_that_works,
          subtopics,
        },
        isCustom: true,
      };

      const path = saveCustomNiche(fork);

      if (isJsonMode()) {
        output.json({ ...fork, path });
        return;
      }

      output.success(`Forked "${source.name}" → "${fork.name}" (${fork.slug})`);
      output.text(chalk.dim(`Saved to ${path}`));
      output.text(chalk.dim(`Subtopics: ${subtopics.length}`));
    });

  // ── create ───────────────────────────────────────────────────────────
  niches
    .command('create')
    .description('Create a custom niche from scratch (interactive or via flags)')
    .option('--slug <slug>', 'Slug (defaults to slugified --name)')
    .option('--name <name>', 'Niche name')
    .option('--category <category>', 'Category', 'Custom')
    .option('--subtopics <list>', 'Comma-separated subtopics (becomes the page list)')
    .option('--audience <text>', 'Target audience')
    .option('--pain-points <text>', 'Pain points')
    .option('--monetization <text>', 'Monetization angle')
    .option('--content-that-works <text>', 'Content formats that work in this niche')
    .action(async (opts: {
      slug?: string;
      name?: string;
      category?: string;
      subtopics?: string;
      audience?: string;
      painPoints?: string;
      monetization?: string;
      contentThatWorks?: string;
    }) => {
      const isTTY = process.stdout.isTTY && !isJsonMode();

      let name = opts.name;
      let category = opts.category ?? 'Custom';
      let subtopics = splitList(opts.subtopics);
      let audience = opts.audience;
      let painPoints = opts.painPoints;
      let monetization = opts.monetization;
      let contentThatWorks = opts.contentThatWorks;

      if (isTTY) {
        if (!name) name = await input({ message: 'Niche name', required: true });
        if (!opts.category) {
          category = await input({
            message: 'Category (e.g., Technology, Business, Health)',
            default: 'Custom',
          });
        }
        if (subtopics.length === 0) {
          const raw = await input({
            message: 'Subtopics (comma-separated — these become your pages)',
            required: true,
          });
          subtopics = splitList(raw);
        }
        if (!audience) {
          audience = await input({
            message: 'Audience',
            default: '',
          });
        }
        if (!painPoints) {
          painPoints = await input({
            message: 'Pain points',
            default: '',
          });
        }
        if (!monetization) {
          monetization = await input({
            message: 'Monetization angle',
            default: '',
          });
        }
        if (!contentThatWorks) {
          contentThatWorks = await input({
            message: 'Content formats that work',
            default: '',
          });
        }
      } else {
        if (!name) {
          output.error('--name is required in non-interactive mode');
          process.exit(1);
        }
        if (subtopics.length === 0) {
          output.error('--subtopics is required in non-interactive mode');
          process.exit(1);
        }
      }

      const slug = opts.slug ?? slugify(name!);
      if (findNicheBySlug(slug)?.isCustom) {
        output.error(`A custom niche with slug "${slug}" already exists.`);
        process.exit(1);
      }

      const niche: Niche = {
        slug,
        name: name!,
        category,
        context: {
          audience: audience ?? '',
          pain_points: painPoints ?? '',
          monetization: monetization ?? '',
          content_that_works: contentThatWorks ?? '',
          subtopics,
        },
        isCustom: true,
      };

      const path = saveCustomNiche(niche);

      if (isJsonMode()) {
        output.json({ ...niche, path });
        return;
      }

      output.success(`Niche "${niche.name}" created (${niche.slug})`);
      output.text(chalk.dim(`Saved to ${path}`));
      output.text(chalk.dim(`Subtopics: ${subtopics.length}`));
    });

  // ── edit ─────────────────────────────────────────────────────────────
  niches
    .command('edit')
    .description('Edit a custom niche — bundled niches cannot be edited (use `fork` first)')
    .argument('<slug>', 'Niche slug')
    .option('--name <name>', 'Update name')
    .option('--category <category>', 'Update category')
    .option('--add-subtopics <list>', 'Comma-separated subtopics to append')
    .option('--remove-subtopics <list>', 'Comma-separated subtopics to remove')
    .option('--replace-subtopics <list>', 'Comma-separated subtopics to use as the full list')
    .option('--audience <text>', 'Update audience')
    .option('--pain-points <text>', 'Update pain points')
    .option('--monetization <text>', 'Update monetization')
    .option('--content-that-works <text>', 'Update content-that-works')
    .action((slug: string, opts: {
      name?: string;
      category?: string;
      addSubtopics?: string;
      removeSubtopics?: string;
      replaceSubtopics?: string;
      audience?: string;
      painPoints?: string;
      monetization?: string;
      contentThatWorks?: string;
    }) => {
      const existing = findNicheBySlug(slug);
      if (!existing) {
        output.error(`No niche found with slug "${slug}".`);
        process.exit(1);
      }
      if (!existing.isCustom) {
        output.error(
          `"${slug}" is a bundled niche and cannot be edited directly. Run \`contento niches fork ${slug}\` first.`,
        );
        process.exit(1);
      }

      let subtopics = existing.context.subtopics.slice();
      if (opts.replaceSubtopics) {
        subtopics = splitList(opts.replaceSubtopics);
      } else {
        if (opts.removeSubtopics) {
          const remove = new Set(splitList(opts.removeSubtopics));
          subtopics = subtopics.filter((s) => !remove.has(s));
        }
        if (opts.addSubtopics) {
          for (const s of splitList(opts.addSubtopics)) {
            if (!subtopics.includes(s)) subtopics.push(s);
          }
        }
      }

      const updated: Niche = {
        ...existing,
        name: opts.name ?? existing.name,
        category: opts.category ?? existing.category,
        context: {
          audience: opts.audience ?? existing.context.audience,
          pain_points: opts.painPoints ?? existing.context.pain_points,
          monetization: opts.monetization ?? existing.context.monetization,
          content_that_works: opts.contentThatWorks ?? existing.context.content_that_works,
          subtopics,
        },
      };

      const path = saveCustomNiche(updated);

      if (isJsonMode()) {
        output.json({ ...updated, path });
        return;
      }

      output.success(`Niche "${updated.name}" updated`);
      output.text(chalk.dim(`Subtopics: ${subtopics.length} total`));
    });

  // ── delete ───────────────────────────────────────────────────────────
  niches
    .command('delete')
    .description('Delete a custom niche (bundled niches cannot be deleted)')
    .argument('<slug>', 'Niche slug')
    .option('--yes', 'Skip the confirmation prompt')
    .action(async (slug: string, opts: { yes?: boolean }) => {
      const existing = findNicheBySlug(slug);
      if (!existing) {
        output.error(`No niche found with slug "${slug}".`);
        process.exit(1);
      }
      if (!existing.isCustom) {
        output.error(`"${slug}" is a bundled niche and cannot be deleted.`);
        process.exit(1);
      }

      if (!opts.yes && process.stdout.isTTY && !isJsonMode()) {
        const ok = await confirm({
          message: `Delete custom niche "${existing.name}" (${slug})?`,
          default: false,
        });
        if (!ok) {
          output.info('Cancelled.');
          return;
        }
      }

      const removed = deleteCustomNiche(slug);
      if (isJsonMode()) {
        output.json({ slug, removed });
        return;
      }
      if (removed) {
        output.success(`Custom niche "${slug}" deleted.`);
      } else {
        output.warn(`Custom niche "${slug}" was not found on disk.`);
      }
    });

  // ── select (interactive) ─────────────────────────────────────────────
  niches
    .command('select')
    .description('Interactively browse and select niches')
    .option('--category <category>', 'Start with a specific category')
    .action(async (opts: { category?: string }) => {
      if (isJsonMode()) {
        output.error(
          'Interactive selection is not available in --json mode. Use `niches list --json` instead.',
        );
        process.exit(1);
      }

      const all = getAllNiches();
      if (all.length === 0) {
        output.info(`No niches available. Custom niches live in ${getCustomDir()}.`);
        return;
      }

      const categories = new Map<string, Niche[]>();
      for (const n of all) {
        const arr = categories.get(n.category) ?? [];
        arr.push(n);
        categories.set(n.category, arr);
      }

      let selectedCategory = opts.category;
      if (selectedCategory && !categories.has(selectedCategory)) {
        output.warn(`No niches found in category "${selectedCategory}". Listing all categories.`);
        selectedCategory = undefined;
      }
      if (!selectedCategory) {
        selectedCategory = await select({
          message: 'Select a category',
          choices: [...categories.keys()].sort().map((cat) => ({
            name: `${cat} (${categories.get(cat)!.length} niches)`,
            value: cat,
          })),
        });
      }

      const categoryNiches = categories.get(selectedCategory) ?? all;

      const selectedSlugs = await checkbox({
        message: `Select niches from ${selectedCategory}`,
        choices: categoryNiches.map((n) => ({
          name: `${n.name}${chalk.dim(` — ${n.context.subtopics.length} subtopics`)}`,
          value: n.slug,
        })),
        required: true,
      });

      const allSelectedSlugs = [...selectedSlugs];
      let addMore = true;
      while (addMore) {
        addMore = await select({
          message: `${allSelectedSlugs.length} niche(s) selected. Add from another category?`,
          choices: [
            { name: 'No, done selecting', value: false },
            { name: 'Yes, browse another category', value: true },
          ],
        });

        if (addMore) {
          const anotherCat = await select({
            message: 'Select another category',
            choices: [...categories.keys()]
              .sort()
              .filter((cat) => cat !== selectedCategory)
              .map((cat) => ({
                name: `${cat} (${categories.get(cat)!.length} niches)`,
                value: cat,
              })),
          });

          const moreSlugs = await checkbox({
            message: `Select niches from ${anotherCat}`,
            choices: categories.get(anotherCat)!.map((n) => ({
              name: `${n.name}${chalk.dim(` — ${n.context.subtopics.length} subtopics`)}`,
              value: n.slug,
              checked: allSelectedSlugs.includes(n.slug),
            })),
          });
          for (const slug of moreSlugs) {
            if (!allSelectedSlugs.includes(slug)) allSelectedSlugs.push(slug);
          }
        }
      }

      const selectedNiches = all.filter((n) => allSelectedSlugs.includes(n.slug));
      output.blank();
      output.success(`Selected ${selectedNiches.length} niche(s):`);
      for (const n of selectedNiches) {
        output.text(chalk.dim('  • ') + n.name + chalk.dim(` (${n.slug})`));
      }
      output.blank();
      output.text(chalk.bold('Use with collections create:'));
      output.text(chalk.dim('  contento collections create \\'));
      output.text(chalk.dim('    --niches ') + allSelectedSlugs.join(','));
      output.blank();
    });
}

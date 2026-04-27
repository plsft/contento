using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for constructing AI prompts from schemas, niches, and subtopics for pSEO content generation.
/// </summary>
public interface IPromptBuilderService
{
    /// <summary>
    /// Builds the system prompt from a content schema, instructing the AI on output structure and constraints.
    /// </summary>
    /// <param name="schema">The content schema defining the expected output format.</param>
    /// <returns>The system prompt string.</returns>
    Task<string> BuildSystemPromptAsync(ContentSchema schema);

    /// <summary>
    /// Builds the user prompt with title, niche context, and subtopic details for a specific page.
    /// </summary>
    /// <param name="title">The page title.</param>
    /// <param name="niche">The niche taxonomy providing domain context.</param>
    /// <param name="subtopic">The specific subtopic for this page.</param>
    /// <returns>The user prompt string.</returns>
    Task<string> BuildUserPromptAsync(string title, NicheTaxonomy niche, string subtopic);
}

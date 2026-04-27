using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Join between a pSEO collection and a niche taxonomy, with optional subtopic overrides
/// </summary>
[Table("pseo_collection_niches")]
[TableOrder(32)]
public class PseoCollectionNiche
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("collection_id")]
    [ForeignKey("pseo_collections", ReferencedColumn = "id")]
    [Index("ix_pseo_collection_niches_collection_id")]
    [Index("ux_pseo_collection_niches_col_niche", IsUnique = true)]
    public Guid CollectionId { get; set; }

    [Column("niche_id")]
    [ForeignKey("niche_taxonomies", ReferencedColumn = "id")]
    [Index("ux_pseo_collection_niches_col_niche", IsUnique = true)]
    public Guid NicheId { get; set; }

    [Column("subtopics", TypeName = "jsonb")]
    [DefaultValue("'[]'", IsRawSql = true)]
    public string Subtopics { get; set; } = "[]";

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

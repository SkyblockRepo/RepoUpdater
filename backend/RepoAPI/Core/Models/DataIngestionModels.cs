using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Util;

namespace RepoAPI.Core.Models;

public class DataIngestionBatch
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    [MaxLength(128)]
    public required string Source { get; set; }
    
    public required IngestionStatus Status { get; set; }
    
    public List<PendingEntityChange> PendingChanges { get; set; } = [];
    public List<PendingDeprecation> PendingDeprecations { get; set; } = [];
}

[JsonStringEnum]
public enum IngestionStatus
{
    PendingApproval,
    InProgress,
    Approved,
    Applied,
    Failed,
}

public class PendingEntityChange
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required int BatchId { get; set; }
    
    [MaxLength(128)]
    public required string EntityType { get; set; }
    
    [MaxLength(512)]
    public required string InternalId { get; set; }
    
    [Column(TypeName = "jsonb")]
    public required JsonDocument EntityData { get; set; }
}


public class PendingDeprecation
{
    public required int BatchId { get; set; }
    
    /// <summary>
    /// The primary key of the entity version to mark as not 'Latest'.
    /// </summary>
    public required int EntityIdToDeprecate { get; set; }
    
    /// <summary>
    /// The type of entity being deprecated (e.g., "SkyblockItem", "SkyblockPet").
    /// </summary>
    [MaxLength(128)]
    public required string EntityType { get; set; }
}

public class IngestionConfiguration : IEntityTypeConfiguration<DataIngestionBatch>,
    IEntityTypeConfiguration<PendingEntityChange>,
    IEntityTypeConfiguration<PendingDeprecation>
{
    public void Configure(EntityTypeBuilder<DataIngestionBatch> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Status);
    }
    
    public void Configure(EntityTypeBuilder<PendingEntityChange> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne<DataIngestionBatch>()
            .WithMany(b => b.PendingChanges)
            .HasForeignKey(p => p.BatchId);
    }

    public void Configure(EntityTypeBuilder<PendingDeprecation> builder)
    {
        builder.HasKey(x => new { x.BatchId, x.EntityIdToDeprecate, x.EntityType });
        
        builder.HasOne<DataIngestionBatch>()
            .WithMany(b => b.PendingDeprecations)
            .HasForeignKey(p => p.BatchId);
    }
}
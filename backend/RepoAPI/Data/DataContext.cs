using RepoAPI.Features.Items.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RepoAPI.Core.Models;
using RepoAPI.Features.Enchantments.Models;
using RepoAPI.Features.NPCs.Models;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Recipes.Models;
using RepoAPI.Features.Shops.Models;
using RepoAPI.Features.Zones.Models;

namespace RepoAPI.Data;

public class DataContext(DbContextOptions<DataContext> options): DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        
        // This automatically applies all IEntityTypeConfiguration implementations in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
    
    public DbSet<SkyblockItem> SkyblockItems => Set<SkyblockItem>();
    public DbSet<SkyblockPet> SkyblockPets => Set<SkyblockPet>();
    public DbSet<SkyblockRecipe> SkyblockRecipes => Set<SkyblockRecipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<SkyblockItemRecipeLink> SkyblockItemRecipeLinks => Set<SkyblockItemRecipeLink>();
    public DbSet<SkyblockEnchantment> SkyblockEnchantments => Set<SkyblockEnchantment>();
    public DbSet<SkyblockNpc> SkyblockNpcs => Set<SkyblockNpc>();
    public DbSet<SkyblockZone> SkyblockZones => Set<SkyblockZone>();
    public DbSet<SkyblockShop> SkyblockShops => Set<SkyblockShop>();
    
    public DbSet<PendingEntityChange> PendingEntityChanges => Set<PendingEntityChange>();
    public DbSet<PendingDeprecation> PendingDeprecations => Set<PendingDeprecation>();
    public DbSet<DataIngestionBatch> DataIngestionBatches => Set<DataIngestionBatch>();
}
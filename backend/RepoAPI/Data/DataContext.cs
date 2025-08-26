using RepoAPI.Features.Items.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Util;

namespace RepoAPI.Data;

public class DataContext(DbContextOptions<DataContext> options, IConfiguration config, IWebHostEnvironment environment): DbContext(options)
{
	private static NpgsqlDataSource? Source { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        var connection = config.GetConnectionString("Postgres");

        if (string.IsNullOrEmpty(connection)) {
            throw new InvalidOperationException("No database connection string found.");
        }
        
        if (Source is null) {
            var builder = new NpgsqlDataSourceBuilder(connection);
            builder.EnableDynamicJson();
            Source = builder.Build();
        }
        
        if (environment.IsTesting()) return;
        
        optionsBuilder.EnableSensitiveDataLogging(environment.IsDevelopment());
        optionsBuilder.UseNpgsql(Source, opt => {
            opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        
        // This automatically applies all IEntityTypeConfiguration implementations in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
    
    public DbSet<SkyblockItem> SkyblockItems => Set<SkyblockItem>();
    public DbSet<SkyblockPet> SkyblockPets => Set<SkyblockPet>();
}
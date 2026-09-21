using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarmachineAPI.Models;

namespace WarmachineAPI.Data;

public class WarmachineDbContext : DbContext
{
    public WarmachineDbContext(DbContextOptions<WarmachineDbContext> options) : base(options)
    {
    }

    public DbSet<Faction> Factions => Set<Faction>();
    public DbSet<UnitDefinition> UnitDefinitions => Set<UnitDefinition>();
    public DbSet<Spell> Spells => Set<Spell>();
    public DbSet<Ability> Abilities => Set<Ability>();
    public DbSet<Map> Maps => Set<Map>();
    public DbSet<TerrainFeature> TerrainFeatures => Set<TerrainFeature>();
    public DbSet<DeploymentZone> DeploymentZones => Set<DeploymentZone>();
    public DbSet<Army> Armies => Set<Army>();
    public DbSet<ArmyEntry> ArmyEntries => Set<ArmyEntry>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameParticipant> GameParticipants => Set<GameParticipant>();
    public DbSet<ModelInstance> ModelInstances => Set<ModelInstance>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ControlZone> ControlZones => Set<ControlZone>();
    public DbSet<CombatLogEntry> CombatLogEntries => Set<CombatLogEntry>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureJsonList<ModelInstance, StatusEffect>(modelBuilder.Entity<ModelInstance>(), m => m.StatusEffects);
        ConfigureJsonList<ModelInstance, DamageColumnState>(modelBuilder.Entity<ModelInstance>(), m => m.DamageGrid);
        ConfigureJsonList<UnitDefinition, DamageColumnTemplate>(modelBuilder.Entity<UnitDefinition>(), u => u.DamageColumns);
    }

    private static void ConfigureJsonList<TEntity, TItem>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, List<TItem>>> propertyExpression)
        where TEntity : class
    {
        builder.Property(propertyExpression)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<TItem>>(v, (JsonSerializerOptions?)null) ?? new List<TItem>())
            .Metadata.SetValueComparer(new ValueComparer<List<TItem>>(
                (a, b) => (a ?? new List<TItem>()).SequenceEqual(b ?? new List<TItem>()),
                v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                v => v.ToList()));
    }
}

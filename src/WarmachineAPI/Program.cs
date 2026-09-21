using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WarmachineAPI.Data;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<WarmachineDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IRepository<Faction>, EfRepository<Faction>>();
builder.Services.AddScoped<IRepository<UnitDefinition>, EfRepository<UnitDefinition>>();
builder.Services.AddScoped<IRepository<Map>, EfRepository<Map>>();
builder.Services.AddScoped<IRepository<TerrainFeature>, EfRepository<TerrainFeature>>();
builder.Services.AddScoped<IRepository<Army>, EfRepository<Army>>();
builder.Services.AddScoped<IRepository<ArmyEntry>, EfRepository<ArmyEntry>>();
builder.Services.AddScoped<IRepository<GameSession>, EfRepository<GameSession>>();
builder.Services.AddScoped<IRepository<GameParticipant>, EfRepository<GameParticipant>>();
builder.Services.AddScoped<IRepository<ModelInstance>, EfRepository<ModelInstance>>();
builder.Services.AddScoped<IRepository<Spell>, EfRepository<Spell>>();
builder.Services.AddScoped<IRepository<Ability>, EfRepository<Ability>>();
builder.Services.AddScoped<IRepository<DeploymentZone>, EfRepository<DeploymentZone>>();
builder.Services.AddScoped<IRepository<Scenario>, EfRepository<Scenario>>();
builder.Services.AddScoped<IRepository<ControlZone>, EfRepository<ControlZone>>();
builder.Services.AddScoped<IRepository<CombatLogEntry>, EfRepository<CombatLogEntry>>();
builder.Services.AddScoped<IRepository<Account>, EfRepository<Account>>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WarmachineDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

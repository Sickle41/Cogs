using System.Text.Json.Serialization;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IRepository<Faction>, InMemoryRepository<Faction>>();
builder.Services.AddSingleton<IRepository<UnitDefinition>, InMemoryRepository<UnitDefinition>>();
builder.Services.AddSingleton<IRepository<Map>, InMemoryRepository<Map>>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

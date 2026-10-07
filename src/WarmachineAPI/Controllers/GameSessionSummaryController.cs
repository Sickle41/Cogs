using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

public class GameSessionParticipantSummary
{
    public Guid ArmyId { get; set; }
    public string ArmyName { get; set; } = string.Empty;
    public int ModelCount { get; set; }
    public int ModelsDestroyed { get; set; }
    public int TotalDamageTaken { get; set; }
}

public class GameSessionSummary
{
    public SessionStatus Status { get; set; }
    public int CurrentRound { get; set; }
    public string? ScenarioName { get; set; }
    public List<GameSessionParticipantSummary> Participants { get; set; } = new();
    public Dictionary<CombatLogEventType, int> LogEntryCountsByType { get; set; } = new();
}

[ApiController]
public class GameSessionSummaryController : ControllerBase
{
    private readonly IRepository<GameSession> _sessions;
    private readonly IRepository<Scenario> _scenarios;
    private readonly IRepository<GameParticipant> _participants;
    private readonly IRepository<Army> _armies;
    private readonly IRepository<ArmyEntry> _armyEntries;
    private readonly IRepository<ModelInstance> _models;
    private readonly IRepository<CombatLogEntry> _logEntries;

    public GameSessionSummaryController(
        IRepository<GameSession> sessions,
        IRepository<Scenario> scenarios,
        IRepository<GameParticipant> participants,
        IRepository<Army> armies,
        IRepository<ArmyEntry> armyEntries,
        IRepository<ModelInstance> models,
        IRepository<CombatLogEntry> logEntries)
    {
        _sessions = sessions;
        _scenarios = scenarios;
        _participants = participants;
        _armies = armies;
        _armyEntries = armyEntries;
        _models = models;
        _logEntries = logEntries;
    }

    [HttpGet("api/sessions/{id:guid}/summary")]
    public ActionResult<GameSessionSummary> GetSummary(Guid id)
    {
        var session = _sessions.GetById(id);
        if (session is null)
        {
            return this.ProblemNotFound($"Session '{id}' does not exist.");
        }

        var scenarioName = session.ScenarioId.HasValue
            ? _scenarios.GetById(session.ScenarioId.Value)?.Name
            : null;

        var sessionModels = _models.GetAll().Where(m => m.GameSessionId == id).ToList();
        var allArmyEntries = _armyEntries.GetAll().ToList();

        var participantSummaries = _participants.GetAll()
            .Where(p => p.GameSessionId == id)
            .Select(p =>
            {
                var army = _armies.GetById(p.ArmyId);
                var armyEntryIds = allArmyEntries
                    .Where(e => e.ArmyId == p.ArmyId)
                    .Select(e => e.Id)
                    .ToHashSet();
                var modelsForArmy = sessionModels.Where(m => armyEntryIds.Contains(m.ArmyEntryId)).ToList();

                return new GameSessionParticipantSummary
                {
                    ArmyId = p.ArmyId,
                    ArmyName = army?.Name ?? "Unknown",
                    ModelCount = modelsForArmy.Count,
                    ModelsDestroyed = modelsForArmy.Count(m => m.IsDestroyed),
                    TotalDamageTaken = modelsForArmy.Sum(m => m.DamageTaken)
                };
            })
            .ToList();

        var logCounts = _logEntries.GetAll()
            .Where(e => e.GameSessionId == id)
            .GroupBy(e => e.EventType)
            .ToDictionary(g => g.Key, g => g.Count());

        return Ok(new GameSessionSummary
        {
            Status = session.Status,
            CurrentRound = session.CurrentRound,
            ScenarioName = scenarioName,
            Participants = participantSummaries,
            LogEntryCountsByType = logCounts
        });
    }
}

namespace WarmachineAPI.Models;

public class CombatLogEntry : IEntity
{
    public Guid Id { get; set; }
    public Guid GameSessionId { get; set; }
    public int Round { get; set; }
    public CombatLogEventType EventType { get; set; }
    public Guid? ActorModelInstanceId { get; set; }
    public Guid? TargetModelInstanceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

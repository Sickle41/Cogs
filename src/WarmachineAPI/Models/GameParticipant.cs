namespace WarmachineAPI.Models;

public class GameParticipant : IEntity
{
    public Guid Id { get; set; }
    public Guid GameSessionId { get; set; }
    public Guid ArmyId { get; set; }
    public int TurnOrder { get; set; }
}

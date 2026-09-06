namespace WarmachineAPI.Models;

public class GameSession : IEntity
{
    public Guid Id { get; set; }
    public Guid MapId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Setup;
    public int CurrentRound { get; set; } = 1;
    public Guid? ActiveParticipantId { get; set; }
}

namespace WarmachineAPI.Models;

public class Army : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid FactionId { get; set; }
    public int PointLimit { get; set; }
}

namespace WarmachineAPI.Models;

public class Map : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Width { get; set; } = 1219.2;
    public double Height { get; set; } = 1219.2;
}

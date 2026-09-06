namespace WarmachineAPI.Models;

public class Player
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Health { get; set; } = 100;
    public int Score { get; set; } = 0;
}

namespace WarmachineAPI.Models;

public interface IConcurrent
{
    Guid ConcurrencyToken { get; set; }
}

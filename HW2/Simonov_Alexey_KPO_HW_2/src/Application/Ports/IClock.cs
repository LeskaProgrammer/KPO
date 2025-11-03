namespace Application.Ports;

public interface IClock { System.DateTime Now { get; } }

public sealed class SystemClock : IClock
{
    public System.DateTime Now => System.DateTime.UtcNow;
}
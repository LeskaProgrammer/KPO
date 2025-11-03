namespace Application.Ports;

public interface IIdGenerator { string NewId(); }

public sealed class GuidIdGenerator : IIdGenerator
{
    public string NewId() => System.Guid.NewGuid().ToString("N");
}
using Application.Ports;

namespace Infrastructure.Persistence.InMemory;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public void Commit() { /* no-op for in-memory */ }
}
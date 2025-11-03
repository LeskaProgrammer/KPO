using Domain.Entities;

namespace Application.Ports;

public interface IOperationRepository
{
    Operation? Get(string id);
    IEnumerable<Operation> GetAll();
    IEnumerable<Operation> GetByAccount(string accountId);
    IEnumerable<Operation> GetByAccountAndPeriod(string accountId, System.DateTime from, System.DateTime to);
    void Add(Operation op);
    void Update(Operation op);
    void Remove(string id);
}
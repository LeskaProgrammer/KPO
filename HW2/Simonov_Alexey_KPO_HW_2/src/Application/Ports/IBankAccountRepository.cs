using Domain.Entities;

namespace Application.Ports;

public interface IBankAccountRepository
{
    BankAccount? Get(string id);
    IEnumerable<BankAccount> GetAll();
    void Add(BankAccount account);
    void Update(BankAccount account);
    void Remove(string id);
}
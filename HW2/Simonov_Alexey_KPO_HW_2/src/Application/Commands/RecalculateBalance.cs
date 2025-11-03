using Application.Commands;
using Application.Ports;
using Domain.Services;

namespace Application.Commands.Maintenance;

/// <summary>
/// Команда пересчёта баланса счёта по операциям.
/// </summary>
/// <remarks>
/// Паттерны: Command, Domain Service, Unit of Work.
/// </remarks>
public sealed class RecalculateBalance : ICommand
{
    private readonly IBankAccountRepository _accounts;
    private readonly IOperationRepository _operations;
    private readonly IUnitOfWork _uow;
    private readonly string _accountId;

    /// <summary>
    /// Создать команду пересчёта баланса.
    /// </summary>
    /// <param name="accounts">Репозиторий счетов.</param>
    /// <param name="operations">Репозиторий операций.</param>
    /// <param name="uow">Единица работы (коммит).</param>
    /// <param name="accountId">Счёт, для которого пересчитываем баланс.</param>
    public RecalculateBalance(IBankAccountRepository accounts, IOperationRepository operations, IUnitOfWork uow, string accountId)
    { _accounts = accounts; _operations = operations; _uow = uow; _accountId = accountId; }

    /// <summary>
    /// Выполняет пересчёт баланса и сохраняет изменения.
    /// </summary>
    public void Execute()
    {
        var acc = _accounts.Get(_accountId) ?? throw new InvalidOperationException("account not found");
        var ops = _operations.GetByAccount(_accountId);
        BalanceRecalculationService.Recalculate(acc, ops);
        _accounts.Update(acc);
        _uow.Commit();
    }
}
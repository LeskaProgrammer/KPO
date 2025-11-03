namespace Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Банковский счёт».
    /// </summary>
    /// <remarks>
    /// Инварианты:
    /// <list type="bullet">
    /// <item><description><see cref="Id"/> — непустой идентификатор.</description></item>
    /// <item><description><see cref="Name"/> — непустое человекочитаемое имя.</description></item>
    /// </list>
    /// Поле <see cref="Balance"/> отражает текущий баланс счёта и изменяется только через методы-мутаторы.
    /// </remarks>
    public class BankAccount
    {
        /// <summary>
        /// Уникальный идентификатор счёта.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Название счёта (редактируемое свойство).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Текущий баланс счёта.
        /// Положительные значения — излишек средств, отрицательные — задолженность.
        /// </summary>
        public decimal Balance { get; private set; }

        /// <summary>
        /// Создаёт банковский счёт.
        /// </summary>
        /// <param name="id">Идентификатор (обязан быть непустым).</param>
        /// <param name="name">Человекочитаемое имя (обязательное, непустое).</param>
        /// <param name="balance">Начальный баланс (по умолчанию 0).</param>
        /// <exception cref="ArgumentException">
        /// Бросается, если <paramref name="id"/> или <paramref name="name"/> пустые или состоят из пробелов.
        /// </exception>
        public BankAccount(string id, string name, decimal balance = 0m)
        {
            // Базовая валидация аргументов
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required");

            Id = id;
            Name = name.Trim();    // нормализуем пробелы по краям
            Balance = balance;     // стартовое значение баланса
        }

        /// <summary>
        /// Переименовывает счёт.
        /// </summary>
        /// <param name="newName">Новое имя (непустое).</param>
        /// <exception cref="ArgumentException">
        /// Бросается, если <paramref name="newName"/> пустое или состоит из пробелов.
        /// </exception>
        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("name is required");
            Name = newName.Trim();
        }

        /// <summary>
        /// Применяет входящую сумму (доход) к балансу: <c>Balance += amount</c>.
        /// </summary>
        /// <param name="amount">Сумма дохода (предполагается неотрицательной, проверка выше по слою).</param>
        public void ApplyIncome(decimal amount) => Balance += amount;

        /// <summary>
        /// Применяет исходящую сумму (расход) к балансу: <c>Balance -= amount</c>.
        /// </summary>
        /// <param name="amount">Сумма расхода (предполагается неотрицательной, проверка выше по слою).</param>
        public void ApplyExpense(decimal amount) => Balance -= amount;

        /// <summary>
        /// Жёстко устанавливает баланс (используется для операций пересчёта).
        /// </summary>
        /// <param name="newBalance">Новое значение баланса.</param>
        public void SetBalance(decimal newBalance) => Balance = newBalance;
    }
}

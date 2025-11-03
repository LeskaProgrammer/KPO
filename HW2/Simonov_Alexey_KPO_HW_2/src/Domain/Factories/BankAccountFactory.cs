namespace Domain.Factories
{
    using System;
    using Domain.Entities;

    /// <summary>
    /// Фабрика доменных счетов <see cref="BankAccount"/>.
    /// </summary>
    /// <remarks>
    /// Паттерн <b>Factory</b>: инкапсулирует процесс создания сущности и базовую
    /// валидацию входных аргументов (идентификатор и человекочитаемое имя).
    /// Сама фабрика не хранит состояния и является чистой.
    /// </remarks>
    public sealed class BankAccountFactory : IBankAccountFactory
    {
        /// <summary>
        /// Создаёт новый банковский счёт.
        /// </summary>
        /// <param name="id">Идентификатор счёта (обязан быть непустым).</param>
        /// <param name="name">Название счёта (обязательное, непустое).</param>
        /// <returns>Экземпляр <see cref="BankAccount"/> в валидном состоянии.</returns>
        /// <exception cref="ArgumentException">
        /// Выбрасывается, если <paramref name="id"/> или <paramref name="name"/> пусты
        /// либо содержат только пробелы.
        /// </exception>
        /// <remarks>
        /// Здесь выполняется лишь «механическая» проверка аргументов; любые дополнительные
        /// доменные инварианты (например, уникальность имени для пользователя) должны
        /// проверяться на уровне сервисов/репозиториев.
        /// </remarks>
        public BankAccount Create(string id, string name)
        {
            // Базовая валидация входных аргументов
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("BankAccount id must be non-empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("BankAccount name must be non-empty.", nameof(name));

            // Непосредственное создание доменной сущности (сигнатура конструктора соответствует модели)
            return new BankAccount(id, name); // сигнатура у тебя именно такая
        }
    }
}
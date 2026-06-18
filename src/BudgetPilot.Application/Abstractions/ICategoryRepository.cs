using BudgetPilot.Domain.Entities;

namespace BudgetPilot.Application.Abstractions;

/// <summary>Persistenz-Zugriff auf <see cref="Category"/>. Implementierung in der Infrastructure-Schicht.</summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>True, wenn der Kategorie mindestens ein BudgetItem zugeordnet ist (verhindert Hard-Delete).</summary>
    Task<bool> HasItemsAsync(Guid categoryId, CancellationToken ct = default);

    Task AddAsync(Category category, CancellationToken ct = default);

    void Update(Category category);
}

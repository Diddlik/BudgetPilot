using BudgetPilot.Application.Abstractions;
using BudgetPilot.Domain.Entities;

namespace BudgetPilot.Application.Tests.Fakes;

/// <summary>
/// In-memory Datastore + Repository/UnitOfWork-Fakes. Erlauben es, die echten Application-Services
/// ohne Infrastructure/EF zu testen (Track D). Bewusst simpel: keine echte Change-Tracking-Semantik,
/// die Entities werden direkt referenziert.
/// </summary>
public sealed class InMemoryStore
{
    public List<Category> Categories { get; } = new();
    public List<BudgetItem> Items { get; } = new();

    public int SaveCount { get; set; }

    public void Link()
    {
        // Navigation Category auf den Items setzen (wie es ein echtes Repository per Include täte).
        foreach (var item in Items)
        {
            var category = Categories.FirstOrDefault(c => c.Id == item.CategoryId);
            if (category is not null)
                item.Category = category;
        }
    }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly InMemoryStore _store;
    public FakeUnitOfWork(InMemoryStore store) => _store = store;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        _store.SaveCount++;
        return Task.FromResult(1);
    }
}

public sealed class FakeBudgetItemRepository : IBudgetItemRepository
{
    private readonly InMemoryStore _store;
    public FakeBudgetItemRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<BudgetItem>> GetAllWithVersionsAsync(CancellationToken ct = default)
    {
        _store.Link();
        return Task.FromResult<IReadOnlyList<BudgetItem>>(_store.Items.ToList());
    }

    public Task<BudgetItem?> GetByIdWithVersionsAsync(Guid id, CancellationToken ct = default)
    {
        _store.Link();
        return Task.FromResult(_store.Items.FirstOrDefault(i => i.Id == id));
    }

    public Task<IReadOnlyList<BudgetItem>> GetByCategoryWithVersionsAsync(Guid categoryId, CancellationToken ct = default)
    {
        _store.Link();
        return Task.FromResult<IReadOnlyList<BudgetItem>>(
            _store.Items.Where(i => i.CategoryId == categoryId).ToList());
    }

    public Task AddAsync(BudgetItem item, CancellationToken ct = default)
    {
        _store.Items.Add(item);
        return Task.CompletedTask;
    }

    public void AddVersion(BudgetItemVersion version)
    {
        // Im Fake hängt der Service die Version bereits an item.Versions an; nichts weiter zu tun.
    }

    public void Update(BudgetItem item)
    {
        // Entities werden direkt referenziert; nichts weiter zu tun.
    }

    public void Remove(BudgetItem item) => _store.Items.Remove(item);
}

public sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly InMemoryStore _store;
    public FakeCategoryRepository(InMemoryStore store) => _store = store;

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Category>>(_store.Categories.ToList());

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.Categories.FirstOrDefault(c => c.Id == id));

    public Task<bool> HasItemsAsync(Guid categoryId, CancellationToken ct = default)
        => Task.FromResult(_store.Items.Any(i => i.CategoryId == categoryId));

    public Task AddAsync(Category category, CancellationToken ct = default)
    {
        _store.Categories.Add(category);
        return Task.CompletedTask;
    }

    public void Update(Category category)
    {
        // Entities werden direkt referenziert; nichts weiter zu tun.
    }
}

using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Web.Services;

// TEMP: Web-only render data until Track A/B provide real Application/Infrastructure implementations.
public sealed class TemporaryBudgetStore
{
    private readonly object _gate = new();
    private readonly List<CategoryDto> _categories = new();
    private readonly List<BudgetItemDto> _items = new();

    public TemporaryBudgetStore()
    {
        var income = AddCategory("Einkommen");
        var housing = AddCategory("Wohnen");
        var energy = AddCategory("Energie");
        var subscriptions = AddCategory("Abos");
        var insurance = AddCategory("Versicherungen");
        var household = AddCategory("Haushalt");

        AddItem("Gehalt", BudgetItemType.Income, income.Id, 3500m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1));
        AddItem("Miete", BudgetItemType.Expense, housing.Id, 1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1));
        var power = AddItem("Strom", BudgetItemType.Expense, energy.Id, 120m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1), note: "Ab März neuer Abschlag");
        power.Versions[0].ValidTo = new DateOnly(2026, 2, 28);
        power.Versions.Insert(0, NewVersion(145m, BudgetFrequency.Monthly, new DateOnly(2026, 3, 1), null, null, "Tarifanpassung"));
        AddItem("Netflix", BudgetItemType.Expense, subscriptions.Id, 15.99m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1));
        AddItem("Amazon Prime", BudgetItemType.Expense, subscriptions.Id, 89.90m, BudgetFrequency.Yearly, new DateOnly(2026, 2, 1), null, 2);
        AddItem("Kfz-Versicherung", BudgetItemType.Expense, insurance.Id, 720m, BudgetFrequency.Yearly, new DateOnly(2026, 1, 1), null, 3);
        AddItem("Haftpflicht", BudgetItemType.Expense, insurance.Id, 90m, BudgetFrequency.Yearly, new DateOnly(2026, 1, 1), null, 1);
        AddItem("Waschmaschine", BudgetItemType.Expense, household.Id, 600m, BudgetFrequency.Once, new DateOnly(2026, 5, 15), note: "Einmaliger Ersatz");
    }

    public IReadOnlyList<CategoryDto> Categories
    {
        get
        {
            lock (_gate)
            {
                return _categories.Select(CloneCategoryWithCount).ToList();
            }
        }
    }

    public IReadOnlyList<BudgetItemDto> Items
    {
        get
        {
            lock (_gate)
            {
                return _items.Select(CloneItem).ToList();
            }
        }
    }

    public CategoryDto CreateCategory(string name)
    {
        lock (_gate)
        {
            return CloneCategoryWithCount(AddCategory(name));
        }
    }

    public void RenameCategory(Guid id, string name)
    {
        lock (_gate)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            if (category is null)
            {
                return;
            }

            category.Name = name;
            category.UpdatedAt = DateTime.UtcNow;
        }
    }

    public void DeactivateCategory(Guid id)
    {
        lock (_gate)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            if (category is not null)
            {
                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    public BudgetItemDto CreateItem(CreateBudgetItemRequest request)
    {
        lock (_gate)
        {
            var item = AddItem(request.Name, request.Type, request.CategoryId, request.Amount, request.Frequency,
                request.ValidFrom, request.PaymentDay, request.PaymentMonth, request.Description, request.Note);
            return CloneItem(item);
        }
    }

    public void UpdateMetadata(Guid id, UpdateBudgetItemMetadataRequest request)
    {
        lock (_gate)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item is null)
            {
                return;
            }

            item.Name = request.Name;
            item.Description = request.Description;
            item.Type = request.Type;
            item.CategoryId = request.CategoryId;
            item.CategoryName = CategoryName(request.CategoryId);
            item.UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateCurrentVersion(Guid id, UpdateVersionRequest request)
    {
        lock (_gate)
        {
            var version = _items.FirstOrDefault(i => i.Id == id)?.Versions.FirstOrDefault(v => v.IsCurrent);
            if (version is null)
            {
                return;
            }

            version.Amount = request.Amount;
            version.Frequency = request.Frequency;
            version.ValidFrom = request.ValidFrom;
            version.PaymentDay = request.PaymentDay;
            version.PaymentMonth = request.PaymentMonth;
            version.Note = request.Note;
        }
    }

    public BudgetItemVersionDto AddVersion(Guid id, CreateBudgetItemVersionRequest request)
    {
        lock (_gate)
        {
            var item = _items.First(i => i.Id == id);
            foreach (var open in item.Versions.Where(v => v.ValidTo is null))
            {
                open.ValidTo = request.ValidFrom.AddDays(-1);
                open.IsCurrent = false;
            }

            var version = NewVersion(request.Amount, request.Frequency, request.ValidFrom, request.PaymentDay, request.PaymentMonth, request.Note);
            item.Versions.Insert(0, version);
            return CloneVersion(version);
        }
    }

    public void SetActive(Guid id, bool active)
    {
        lock (_gate)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item is not null)
            {
                item.IsActive = active;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    public void DeleteItem(Guid id)
    {
        lock (_gate)
        {
            _items.RemoveAll(i => i.Id == id);
        }
    }

    private CategoryDto AddCategory(string name)
    {
        var category = new CategoryDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _categories.Add(category);
        return category;
    }

    private BudgetItemDto AddItem(
        string name,
        BudgetItemType type,
        Guid categoryId,
        decimal amount,
        BudgetFrequency frequency,
        DateOnly validFrom,
        int? paymentDay = null,
        int? paymentMonth = null,
        string? description = null,
        string? note = null)
    {
        var item = new BudgetItemDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Type = type,
            CategoryId = categoryId,
            CategoryName = CategoryName(categoryId),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Versions = [NewVersion(amount, frequency, validFrom, paymentDay, paymentMonth, note)]
        };
        _items.Add(item);
        return item;
    }

    private static BudgetItemVersionDto NewVersion(
        decimal amount,
        BudgetFrequency frequency,
        DateOnly validFrom,
        int? paymentDay,
        int? paymentMonth,
        string? note) =>
        new()
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Frequency = frequency,
            ValidFrom = validFrom,
            PaymentDay = paymentDay,
            PaymentMonth = paymentMonth,
            Note = note,
            IsCurrent = true
        };

    private string CategoryName(Guid id) => _categories.FirstOrDefault(c => c.Id == id)?.Name ?? "Ohne Kategorie";

    private CategoryDto CloneCategoryWithCount(CategoryDto category) =>
        new()
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            ItemCount = _items.Count(i => i.CategoryId == category.Id)
        };

    private static BudgetItemDto CloneItem(BudgetItemDto item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Type = item.Type,
            CategoryId = item.CategoryId,
            CategoryName = item.CategoryName,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            Versions = item.Versions.Select(CloneVersion).ToList()
        };

    private static BudgetItemVersionDto CloneVersion(BudgetItemVersionDto version) =>
        new()
        {
            Id = version.Id,
            Amount = version.Amount,
            Frequency = version.Frequency,
            ValidFrom = version.ValidFrom,
            ValidTo = version.ValidTo,
            PaymentDay = version.PaymentDay,
            PaymentMonth = version.PaymentMonth,
            Note = version.Note,
            IsCurrent = version.ValidTo is null
        };
}

public sealed class TemporaryCategoryService(TemporaryBudgetStore store) : ICategoryService
{
    public Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default) =>
        Task.FromResult(store.CreateCategory(request.Name));

    public Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(store.Categories);

    public Task<IReadOnlyList<BudgetItemDto>> GetItemsByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        Task.FromResult((IReadOnlyList<BudgetItemDto>)store.Items.Where(i => i.CategoryId == categoryId).ToList());

    public Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        store.RenameCategory(id, newName);
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        store.DeactivateCategory(id);
        return Task.CompletedTask;
    }
}

public sealed class TemporaryBudgetItemService(TemporaryBudgetStore store) : IBudgetItemService
{
    public Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request, CancellationToken ct = default) =>
        Task.FromResult(store.CreateItem(request));

    public Task<BudgetItemDto> UpdateMetadataAsync(Guid id, UpdateBudgetItemMetadataRequest request, CancellationToken ct = default)
    {
        store.UpdateMetadata(id, request);
        return Task.FromResult(store.Items.First(i => i.Id == id));
    }

    public Task<BudgetItemVersionDto> AddVersionAsync(Guid budgetItemId, CreateBudgetItemVersionRequest request, CancellationToken ct = default) =>
        Task.FromResult(store.AddVersion(budgetItemId, request));

    public Task UpdateCurrentVersionAsync(Guid budgetItemId, UpdateVersionRequest request, CancellationToken ct = default)
    {
        store.UpdateCurrentVersion(budgetItemId, request);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BudgetItemDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(store.Items);

    public Task<BudgetItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(store.Items.FirstOrDefault(i => i.Id == id));

    public Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        store.SetActive(id, false);
        return Task.CompletedTask;
    }

    public Task ReactivateAsync(Guid id, CancellationToken ct = default)
    {
        store.SetActive(id, true);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        store.DeleteItem(id);
        return Task.CompletedTask;
    }
}

public sealed class TemporaryBudgetProjectionService(TemporaryBudgetStore store) : IBudgetProjectionService
{
    public Task<MonthlyBudgetProjectionDto> GetMonthlyProjectionAsync(
        int year,
        int month,
        BudgetViewMode viewMode,
        CancellationToken ct = default) =>
        Task.FromResult(ProjectMonth(year, month, viewMode));

    public Task<YearlyBudgetProjectionDto> GetYearlyProjectionAsync(
        int year,
        BudgetViewMode viewMode,
        CancellationToken ct = default)
    {
        var months = Enumerable.Range(1, 12).Select(month => ProjectMonth(year, month, viewMode)).ToList();
        return Task.FromResult(new YearlyBudgetProjectionDto
        {
            Year = year,
            ViewMode = viewMode,
            Months = months,
            TotalIncome = months.Sum(m => m.TotalIncome),
            TotalExpense = months.Sum(m => m.TotalExpense),
            Balance = months.Sum(m => m.Balance)
        });
    }

    private MonthlyBudgetProjectionDto ProjectMonth(int year, int month, BudgetViewMode viewMode)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var lines = new List<BudgetProjectionLine>();

        foreach (var item in store.Items.Where(i => i.IsActive))
        {
            var version = item.Versions
                .Where(v => v.ValidFrom <= last && (v.ValidTo is null || v.ValidTo >= first))
                .OrderByDescending(v => v.ValidFrom)
                .FirstOrDefault();

            if (version is null)
            {
                continue;
            }

            var (amount, due) = ProjectVersion(version, year, month, viewMode);
            lines.Add(new BudgetProjectionLine
            {
                BudgetItemId = item.Id,
                BudgetItemName = item.Name,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                Type = item.Type,
                Frequency = version.Frequency,
                Amount = version.Amount,
                ProjectedMonthlyAmount = amount,
                IsDue = due,
                Note = version.Note
            });
        }

        var income = lines.Where(l => l.Type == BudgetItemType.Income).Sum(l => l.ProjectedMonthlyAmount);
        var expense = lines.Where(l => l.Type == BudgetItemType.Expense).Sum(l => l.ProjectedMonthlyAmount);

        return new MonthlyBudgetProjectionDto
        {
            Year = year,
            Month = month,
            ViewMode = viewMode,
            TotalIncome = income,
            TotalExpense = expense,
            Balance = income - expense,
            Lines = lines,
            Categories = lines
                .Where(l => l.Type == BudgetItemType.Expense)
                .GroupBy(l => new { l.CategoryId, l.CategoryName })
                .Select(g => new CategoryProjectionSummary
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    TotalAmount = g.Sum(l => l.ProjectedMonthlyAmount)
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList()
        };
    }

    private static (decimal Amount, bool IsDue) ProjectVersion(
        BudgetItemVersionDto version,
        int year,
        int month,
        BudgetViewMode viewMode)
    {
        if (viewMode == BudgetViewMode.Budget)
        {
            return version.Frequency switch
            {
                BudgetFrequency.Monthly => (version.Amount, true),
                BudgetFrequency.Quarterly => (version.Amount / 3m, true),
                BudgetFrequency.Yearly => (version.Amount / 12m, true),
                BudgetFrequency.Once => (version.ValidFrom.Year == year && version.ValidFrom.Month == month ? version.Amount : 0m, true),
                _ => (0m, false)
            };
        }

        var monthDistance = (year - version.ValidFrom.Year) * 12 + (month - version.ValidFrom.Month);
        return version.Frequency switch
        {
            BudgetFrequency.Monthly => (version.Amount, true),
            BudgetFrequency.Quarterly => monthDistance >= 0 && monthDistance % 3 == 0 ? (version.Amount, true) : (0m, false),
            BudgetFrequency.Yearly => month == (version.PaymentMonth ?? version.ValidFrom.Month) ? (version.Amount, true) : (0m, false),
            BudgetFrequency.Once => version.ValidFrom.Year == year && version.ValidFrom.Month == month ? (version.Amount, true) : (0m, false),
            _ => (0m, false)
        };
    }
}

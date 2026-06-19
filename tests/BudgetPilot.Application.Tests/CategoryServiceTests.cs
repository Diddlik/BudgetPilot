using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services;
using BudgetPilot.Application.Tests.Fakes;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

public sealed class CategoryServiceTests
{
    private static (CategoryService svc, InMemoryStore store) Build()
    {
        var store = new InMemoryStore();
        var svc = new CategoryService(
            new FakeCategoryRepository(store),
            new FakeBudgetItemRepository(store),
            new FakeUnitOfWork(store),
            new NoopAuditLog());
        return (svc, store);
    }

    [Fact]
    public async Task Create_AddsActiveCategory()
    {
        var (svc, store) = Build();

        var dto = await svc.CreateAsync(new CreateCategoryRequest("Wohnen"));

        dto.Name.Should().Be("Wohnen");
        dto.IsActive.Should().BeTrue();
        dto.ItemCount.Should().Be(0);
        store.Categories.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_EmptyName_Throws()
    {
        var (svc, _) = Build();
        var act = () => svc.CreateAsync(new CreateCategoryRequest("  "));
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task GetAll_ReportsItemCount()
    {
        var (svc, store) = Build();
        var cat = SeedData.Category("Energie");
        store.Categories.Add(cat);
        store.Items.Add(SeedData.Item("Strom", BudgetItemType.Expense, cat,
            SeedData.Version(120m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));
        store.Items.Add(SeedData.Item("Gas", BudgetItemType.Expense, cat,
            SeedData.Version(80m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));

        var all = await svc.GetAllAsync();

        all.Single(c => c.Name == "Energie").ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task GetItemsByCategory_ReturnsOnlyThatCategorysItems()
    {
        var (svc, store) = Build();
        var energie = SeedData.Category("Energie");
        var wohnen = SeedData.Category("Wohnen");
        store.Categories.AddRange(new[] { energie, wohnen });
        store.Items.Add(SeedData.Item("Strom", BudgetItemType.Expense, energie,
            SeedData.Version(120m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));
        store.Items.Add(SeedData.Item("Miete", BudgetItemType.Expense, wohnen,
            SeedData.Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));

        var items = await svc.GetItemsByCategoryAsync(energie.Id);

        items.Should().ContainSingle();
        items[0].Name.Should().Be("Strom");
    }

    [Fact]
    public async Task Rename_ChangesName()
    {
        var (svc, store) = Build();
        var cat = SeedData.Category("Energie");
        store.Categories.Add(cat);

        await svc.RenameAsync(cat.Id, "Energiekosten");

        cat.Name.Should().Be("Energiekosten");
    }

    [Fact]
    public async Task Rename_EmptyName_Throws()
    {
        var (svc, store) = Build();
        var cat = SeedData.Category("Energie");
        store.Categories.Add(cat);
        var act = () => svc.RenameAsync(cat.Id, "");
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Deactivate_SetsInactive_AndKeepsCategory()
    {
        var (svc, store) = Build();
        var cat = SeedData.Category("Energie");
        store.Categories.Add(cat);
        store.Items.Add(SeedData.Item("Strom", BudgetItemType.Expense, cat,
            SeedData.Version(120m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1))));

        await svc.DeactivateAsync(cat.Id);

        cat.IsActive.Should().BeFalse();
        // Kategorie mit Items bleibt erhalten (nur deaktiviert, kein Hard-Delete).
        store.Categories.Should().Contain(cat);
    }

    [Fact]
    public async Task Rename_UnknownId_Throws()
    {
        var (svc, _) = Build();
        var act = () => svc.RenameAsync(Guid.NewGuid(), "X");
        await act.Should().ThrowAsync<DomainException>();
    }
}

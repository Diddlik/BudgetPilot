using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services;
using BudgetPilot.Application.Tests.Fakes;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

public sealed class BudgetItemServiceTests
{
    private static (BudgetItemService svc, InMemoryStore store, Category cat) Build()
    {
        var store = new InMemoryStore();
        var cat = SeedData.Category("Energie");
        store.Categories.Add(cat);
        var svc = new BudgetItemService(
            new FakeBudgetItemRepository(store),
            new FakeCategoryRepository(store),
            new FakeUnitOfWork(store));
        return (svc, store, cat);
    }

    private static CreateBudgetItemRequest CreateReq(Guid catId, decimal amount = 120m,
        BudgetFrequency freq = BudgetFrequency.Monthly, int? day = null, int? month = null)
        => new("Strom", null, BudgetItemType.Expense, catId, amount, freq, new DateOnly(2026, 1, 1), day, month, null);

    [Fact]
    public async Task Create_AddsItemWithFirstOpenVersion_AndSaves()
    {
        var (svc, store, cat) = Build();

        var dto = await svc.CreateAsync(CreateReq(cat.Id));

        dto.Name.Should().Be("Strom");
        dto.CategoryName.Should().Be("Energie");
        dto.Versions.Should().ContainSingle();
        dto.Versions[0].IsCurrent.Should().BeTrue();
        dto.Versions[0].ValidTo.Should().BeNull();
        store.SaveCount.Should().Be(1);
        store.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_NegativeAmount_ThrowsAndDoesNotPersist()
    {
        var (svc, store, cat) = Build();

        var act = () => svc.CreateAsync(CreateReq(cat.Id, amount: -1m));

        await act.Should().ThrowAsync<DomainException>();
        store.Items.Should().BeEmpty();
        store.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_InvalidPaymentMonth_Throws()
    {
        var (svc, _, cat) = Build();
        var act = () => svc.CreateAsync(CreateReq(cat.Id, freq: BudgetFrequency.Yearly, month: 13));
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Create_InvalidPaymentDay_Throws()
    {
        var (svc, _, cat) = Build();
        var act = () => svc.CreateAsync(CreateReq(cat.Id, day: 0));
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Create_InactiveCategory_Throws()
    {
        var (svc, store, cat) = Build();
        cat.IsActive = false;
        var act = () => svc.CreateAsync(CreateReq(cat.Id));
        await act.Should().ThrowAsync<DomainException>();
    }

    // §8.8 / §4.3 constructive: AddVersion ends the previous version gaplessly & without overlap.
    [Fact]
    public async Task AddVersion_EndsPreviousVersionTheDayBefore_AndAppendsOpenVersion()
    {
        var (svc, store, cat) = Build();
        var created = await svc.CreateAsync(CreateReq(cat.Id)); // 120 ab 01.01.2026, offen

        await svc.AddVersionAsync(created.Id,
            new CreateBudgetItemVersionRequest(145m, BudgetFrequency.Monthly, new DateOnly(2026, 3, 1), null, null, null));

        var item = store.Items.Single();
        item.Versions.Should().HaveCount(2);

        var prev = item.Versions.Single(v => v.Amount == 120m);
        var next = item.Versions.Single(v => v.Amount == 145m);
        prev.ValidTo.Should().Be(new DateOnly(2026, 2, 28)); // D - 1 Tag
        next.ValidFrom.Should().Be(new DateOnly(2026, 3, 1));
        next.ValidTo.Should().BeNull();

        // Lückenlos: prev.ValidTo + 1 == next.ValidFrom
        prev.ValidTo!.Value.AddDays(1).Should().Be(next.ValidFrom);
    }

    [Fact]
    public async Task AddVersion_DtoMarksNewVersionAsCurrent()
    {
        var (svc, store, cat) = Build();
        var created = await svc.CreateAsync(CreateReq(cat.Id));

        var versionDto = await svc.AddVersionAsync(created.Id,
            new CreateBudgetItemVersionRequest(145m, BudgetFrequency.Monthly, new DateOnly(2026, 3, 1), null, null, null));

        versionDto.IsCurrent.Should().BeTrue();

        var refreshed = await svc.GetByIdAsync(created.Id);
        refreshed!.Versions.Should().HaveCount(2);
        refreshed.Versions.Single(v => v.IsCurrent).Amount.Should().Be(145m);
        // Versionen neueste zuerst.
        refreshed.Versions[0].ValidFrom.Should().Be(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public async Task UpdateCurrentVersion_ChangesLatestOpenVersionInPlace()
    {
        var (svc, store, cat) = Build();
        var created = await svc.CreateAsync(CreateReq(cat.Id));
        await svc.AddVersionAsync(created.Id,
            new CreateBudgetItemVersionRequest(145m, BudgetFrequency.Monthly, new DateOnly(2026, 3, 1), null, null, null));

        await svc.UpdateCurrentVersionAsync(created.Id,
            new UpdateVersionRequest(150m, BudgetFrequency.Monthly, new DateOnly(2026, 3, 1), null, null, "korrigiert"));

        var item = store.Items.Single();
        item.Versions.Should().HaveCount(2); // keine neue Version
        item.Versions.Single(v => v.ValidTo is null).Amount.Should().Be(150m);
    }

    [Fact]
    public async Task UpdateMetadata_ChangesStammdaten()
    {
        var (svc, store, cat) = Build();
        var created = await svc.CreateAsync(CreateReq(cat.Id));

        var dto = await svc.UpdateMetadataAsync(created.Id,
            new UpdateBudgetItemMetadataRequest("Stromkosten", "neu", BudgetItemType.Expense, cat.Id));

        dto.Name.Should().Be("Stromkosten");
        dto.Description.Should().Be("neu");
        store.Items.Single().Versions.Should().ContainSingle(); // Versionen unberührt
    }

    [Fact]
    public async Task DeactivateReactivate_TogglesIsActive()
    {
        var (svc, store, cat) = Build();
        var created = await svc.CreateAsync(CreateReq(cat.Id));

        await svc.DeactivateAsync(created.Id);
        store.Items.Single().IsActive.Should().BeFalse();

        await svc.ReactivateAsync(created.Id);
        store.Items.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_RemovesItemWithAllVersions()
    {
        var (svc, store, cat) = Build();
        var created = await svc.CreateAsync(CreateReq(cat.Id));
        await svc.AddVersionAsync(created.Id,
            new CreateBudgetItemVersionRequest(145m, BudgetFrequency.Monthly, new DateOnly(2026, 3, 1), null, null, null));

        await svc.DeleteAsync(created.Id);

        store.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var (svc, _, _) = Build();
        (await svc.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetAll_OrdersVersionsNewestFirst()
    {
        var (svc, store, cat) = Build();
        var created = await svc.CreateAsync(CreateReq(cat.Id));
        await svc.AddVersionAsync(created.Id,
            new CreateBudgetItemVersionRequest(145m, BudgetFrequency.Monthly, new DateOnly(2026, 3, 1), null, null, null));

        var all = await svc.GetAllAsync();

        var dto = all.Single();
        dto.Versions[0].ValidFrom.Should().BeAfter(dto.Versions[1].ValidFrom);
        dto.Versions[0].IsCurrent.Should().BeTrue();
    }
}

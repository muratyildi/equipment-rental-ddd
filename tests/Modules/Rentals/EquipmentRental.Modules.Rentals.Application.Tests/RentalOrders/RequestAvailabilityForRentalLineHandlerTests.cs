using EquipmentRental.Modules.Rentals.Application.Availability;
using EquipmentRental.Modules.Rentals.Application.RentalOrders;
using EquipmentRental.Modules.Rentals.Domain.Abstractions;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;

namespace EquipmentRental.Modules.Rentals.Application.Tests.RentalOrders;

public sealed class RequestAvailabilityForRentalLineHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WhenFleetAccepts_RecordsTranslatedCommitment()
    {
        var (order, line) = CreateQuotedOrder();
        var repository = new StubRentalOrderRepository(order);
        var commitmentId = AvailabilityCommitmentId.From(Guid.NewGuid());
        var gateway = new StubAvailabilityGateway(
            new EquipmentAvailabilityOutcome(
                Accepted: true,
                commitmentId,
                AvailableQuantity: 2,
                WasAlreadyCommitted: false,
                RejectionCode: null));
        var handler = new RequestAvailabilityForRentalLineHandler(
            repository,
            gateway,
            new FixedTimeProvider(Now.AddMinutes(5)));

        var result = await handler.Handle(
            new RequestAvailabilityForRentalLine(order.Id.Value, line.Id.Value),
            CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Equal(commitmentId, line.AvailabilityCommitmentId);
        Assert.Equal(1, repository.SaveCount);
        Assert.NotNull(gateway.LastRequest);
        Assert.Equal(order.FulfilmentLocationId, gateway.LastRequest.FulfilmentLocationId);
        Assert.Equal(order.Period, gateway.LastRequest.Period);
        Assert.Equal(line.EquipmentCategoryId, gateway.LastRequest.EquipmentCategoryId);
    }

    [Fact]
    public async Task Handle_WhenFleetRejects_LeavesRentalOrderUnchanged()
    {
        var (order, line) = CreateQuotedOrder();
        var repository = new StubRentalOrderRepository(order);
        var gateway = new StubAvailabilityGateway(
            new EquipmentAvailabilityOutcome(
                Accepted: false,
                CommitmentId: null,
                AvailableQuantity: 0,
                WasAlreadyCommitted: false,
                RejectionCode: "fleet_availability.insufficient_capacity"));
        var handler = new RequestAvailabilityForRentalLineHandler(
            repository,
            gateway,
            new FixedTimeProvider(Now.AddMinutes(5)));

        var result = await handler.Handle(
            new RequestAvailabilityForRentalLine(order.Id.Value, line.Id.Value),
            CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.Equal("fleet_availability.insufficient_capacity", result.RejectionCode);
        Assert.Null(line.AvailabilityCommitmentId);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenRentalAlreadyRecordedCommitment_DoesNotCallFleetAgain()
    {
        var (order, line) = CreateQuotedOrder();
        order.RecordAvailabilityCommitment(
            line.Id,
            AvailabilityCommitmentId.From(Guid.NewGuid()),
            order.Period,
            Now.AddMinutes(1));
        var repository = new StubRentalOrderRepository(order);
        var gateway = new StubAvailabilityGateway(
            new EquipmentAvailabilityOutcome(
                Accepted: false,
                CommitmentId: null,
                AvailableQuantity: 0,
                WasAlreadyCommitted: false,
                RejectionCode: "must_not_be_used"));
        var handler = new RequestAvailabilityForRentalLineHandler(
            repository,
            gateway,
            new FixedTimeProvider(Now.AddMinutes(5)));

        var result = await handler.Handle(
            new RequestAvailabilityForRentalLine(order.Id.Value, line.Id.Value),
            CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.True(result.WasAlreadyCommitted);
        Assert.Equal(0, gateway.CallCount);
    }

    [Fact]
    public async Task Handle_WhenRentalIsNotQuoted_DoesNotCreateOrphanFleetCommitment()
    {
        var order = RentalOrder.Draft(
            RentalOrderId.New(),
            CustomerId.From(Guid.NewGuid()),
            FulfilmentLocationId.From(Guid.NewGuid()),
            RentalPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 13)),
            Now);
        var lineId = order.AddLine(
            EquipmentCategoryId.From(Guid.NewGuid()),
            quantity: 1,
            Money.Of(100, "TRY"),
            Now);
        var repository = new StubRentalOrderRepository(order);
        var gateway = new StubAvailabilityGateway(
            new EquipmentAvailabilityOutcome(
                Accepted: true,
                AvailabilityCommitmentId.From(Guid.NewGuid()),
                AvailableQuantity: 1,
                WasAlreadyCommitted: false,
                RejectionCode: null));
        var handler = new RequestAvailabilityForRentalLineHandler(
            repository,
            gateway,
            new FixedTimeProvider(Now.AddMinutes(5)));

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => handler.Handle(
                new RequestAvailabilityForRentalLine(order.Id.Value, lineId.Value),
                CancellationToken.None));

        Assert.Equal("rentals.order.status_transition_invalid", exception.Code);
        Assert.Equal(0, gateway.CallCount);
    }

    private static (RentalOrder Order, RentalLine Line) CreateQuotedOrder()
    {
        var order = RentalOrder.Draft(
            RentalOrderId.New(),
            CustomerId.From(Guid.NewGuid()),
            FulfilmentLocationId.From(Guid.NewGuid()),
            RentalPeriod.From(
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 8, 13)),
            Now);
        order.AddLine(
            EquipmentCategoryId.From(Guid.NewGuid()),
            quantity: 1,
            Money.Of(100, "TRY"),
            Now);
        order.Quote(Now.AddDays(1), Now);

        return (order, Assert.Single(order.Lines));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubAvailabilityGateway(
        EquipmentAvailabilityOutcome outcome)
        : IEquipmentAvailabilityGateway
    {
        public EquipmentAvailabilityRequest? LastRequest { get; private set; }

        public int CallCount { get; private set; }

        public Task<EquipmentAvailabilityOutcome> CommitAsync(
            EquipmentAvailabilityRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequest = request;
            CallCount++;
            return Task.FromResult(outcome);
        }

        public Task<EquipmentAvailabilityReleaseOutcome> ReleaseAsync(
            EquipmentAvailabilityReleaseRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubRentalOrderRepository(RentalOrder order)
        : IRentalOrderRepository
    {
        public int SaveCount { get; private set; }

        public Task AddAsync(
            RentalOrder rentalOrder,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RentalOrder?> GetAsync(
            RentalOrderId id,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<RentalOrder?>(id == order.Id ? order : null);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}

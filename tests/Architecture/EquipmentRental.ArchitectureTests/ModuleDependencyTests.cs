using EquipmentRental.BuildingBlocks.Eventing;
using EquipmentRental.Modules.FleetAvailability.Application.Availability;
using EquipmentRental.Modules.FleetAvailability.Contracts.Availability;
using EquipmentRental.Modules.FleetAvailability.Domain.Schedules;
using EquipmentRental.Modules.FleetAvailability.ReadModel.Queries;
using EquipmentRental.Modules.Notifications.Application.RentalConfirmations;
using EquipmentRental.Modules.Notifications.Infrastructure.Eventing;
using EquipmentRental.Modules.Rentals.Application.RentalOrders;
using EquipmentRental.Modules.Rentals.Contracts.IntegrationEvents;
using EquipmentRental.Modules.Rentals.Domain.RentalOrders;
using EquipmentRental.Modules.Rentals.ProcessManagers;

using FleetAvailabilityRepository =
    EquipmentRental.Modules.FleetAvailability.Infrastructure.Persistence.InMemoryAvailabilityScheduleRepository;
using RentalOrderRepository =
    EquipmentRental.Modules.Rentals.Infrastructure.Persistence.InMemoryRentalOrderRepository;

namespace EquipmentRental.ArchitectureTests;

public sealed class ModuleDependencyTests
{
    [Fact]
    public void Domain_HasNoDependencyOnOtherProjectAssemblies()
    {
        var references = typeof(RentalOrder).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name!.StartsWith("EquipmentRental.", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructure()
    {
        var references = typeof(CreateRentalOrderHandler).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain(
            "EquipmentRental.Modules.Rentals.Infrastructure",
            references);
        Assert.DoesNotContain("EquipmentRental.Api", references);
    }

    [Fact]
    public void Infrastructure_DependsOnDomainButNotApi()
    {
        var references = typeof(RentalOrderRepository).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains(
            "EquipmentRental.Modules.Rentals.Domain",
            references);
        Assert.DoesNotContain("EquipmentRental.Api", references);
    }

    [Fact]
    public void FleetAvailabilityDomain_HasNoDependencyOnOtherProjectAssemblies()
    {
        var references = typeof(AvailabilitySchedule).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name!.StartsWith("EquipmentRental.", StringComparison.Ordinal));
    }

    [Fact]
    public void FleetAvailabilityContracts_AreIndependentOfInternalLayers()
    {
        var references = typeof(IAvailabilityCommitmentService).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name!.StartsWith("EquipmentRental.", StringComparison.Ordinal));
    }

    [Fact]
    public void RentalsContracts_AreIndependentOfInternalLayers()
    {
        var references = typeof(RentalOrderConfirmedV1).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name!.StartsWith("EquipmentRental.", StringComparison.Ordinal));
    }

    [Fact]
    public void EventingBuildingBlock_DoesNotDependOnBusinessModules()
    {
        var references = typeof(IntegrationEventEnvelope).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name!.StartsWith(
                "EquipmentRental.Modules.",
                StringComparison.Ordinal));
    }

    [Fact]
    public void NotificationsApplication_IsIndependentOfOtherModules()
    {
        var references = typeof(
                RequestRentalConfirmationNotificationHandler)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name!.StartsWith("EquipmentRental.", StringComparison.Ordinal));
    }

    [Fact]
    public void NotificationsInfrastructure_ConsumesOnlyRentalsPublishedContract()
    {
        var references = typeof(RentalOrderConfirmedIntegrationEventConsumer)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains(
            "EquipmentRental.Modules.Rentals.Contracts",
            references);
        Assert.DoesNotContain(
            references,
            name => name?.StartsWith(
                "EquipmentRental.Modules.Rentals.Domain",
                StringComparison.Ordinal) is true);
        Assert.DoesNotContain(
            references,
            name => name?.StartsWith(
                "EquipmentRental.Modules.Rentals.Application",
                StringComparison.Ordinal) is true);
        Assert.DoesNotContain(
            references,
            name => name?.StartsWith(
                "EquipmentRental.Modules.Rentals.Infrastructure",
                StringComparison.Ordinal) is true);
    }

    [Fact]
    public void FleetAvailabilityApplication_DoesNotDependOnInfrastructureOrRentals()
    {
        var references = typeof(AvailabilityCommitmentService).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain(
            "EquipmentRental.Modules.FleetAvailability.Infrastructure",
            references);
        Assert.DoesNotContain(
            references,
            name => name?.StartsWith(
                "EquipmentRental.Modules.Rentals",
                StringComparison.Ordinal) is true);
    }

    [Fact]
    public void RentalsApplication_OwnsPortAndDoesNotDependOnFleetAvailability()
    {
        var references = typeof(RequestAvailabilityForRentalLineHandler).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name?.StartsWith(
                "EquipmentRental.Modules.FleetAvailability",
                StringComparison.Ordinal) is true);
    }

    [Fact]
    public void RentalsInfrastructure_UsesOnlyFleetAvailabilityPublishedContract()
    {
        var references = typeof(RentalOrderRepository).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains(
            "EquipmentRental.Modules.FleetAvailability.Contracts",
            references);
        Assert.DoesNotContain(
            "EquipmentRental.Modules.FleetAvailability.Domain",
            references);
        Assert.DoesNotContain(
            "EquipmentRental.Modules.FleetAvailability.Application",
            references);
        Assert.DoesNotContain(
            "EquipmentRental.Modules.FleetAvailability.Infrastructure",
            references);
    }

    [Fact]
    public void FleetAvailabilityInfrastructure_DoesNotDependOnRentals()
    {
        var references = typeof(FleetAvailabilityRepository).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name?.StartsWith(
                "EquipmentRental.Modules.Rentals",
                StringComparison.Ordinal) is true);
    }

    [Fact]
    public void AvailabilityCalendarReadModel_DependsOnlyOnPublishedContracts()
    {
        var references = typeof(GetAvailabilityCalendarHandler).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains(
            "EquipmentRental.Modules.FleetAvailability.Contracts",
            references);
        Assert.Contains("EquipmentRental.BuildingBlocks.Eventing", references);
        Assert.DoesNotContain(
            "EquipmentRental.Modules.FleetAvailability.Domain",
            references);
        Assert.DoesNotContain(
            "EquipmentRental.Modules.FleetAvailability.Application",
            references);
        Assert.DoesNotContain(
            "EquipmentRental.Modules.FleetAvailability.Infrastructure",
            references);
    }

    [Fact]
    public void RentalProcessManager_UsesRentalsPortsWithoutFleetInternals()
    {
        var references = typeof(RentalConfirmationProcessProcessor).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains(
            "EquipmentRental.Modules.Rentals.Application",
            references);
        Assert.Contains(
            "EquipmentRental.Modules.Rentals.Domain",
            references);
        Assert.DoesNotContain(
            references,
            name => name?.StartsWith(
                "EquipmentRental.Modules.FleetAvailability",
                StringComparison.Ordinal) is true);
        Assert.DoesNotContain(
            "EquipmentRental.Modules.Rentals.Infrastructure",
            references);
        Assert.DoesNotContain("EquipmentRental.Api", references);
    }
}

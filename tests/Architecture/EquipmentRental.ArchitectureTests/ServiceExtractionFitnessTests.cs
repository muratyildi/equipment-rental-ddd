using System.Reflection;

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

public sealed class ServiceExtractionFitnessTests
{
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(RentalOrder).Assembly,
        typeof(CreateRentalOrderHandler).Assembly,
        typeof(RentalOrderRepository).Assembly,
        typeof(RentalConfirmationProcessProcessor).Assembly,
        typeof(RentalOrderConfirmedV1).Assembly,
        typeof(AvailabilitySchedule).Assembly,
        typeof(AvailabilityCommitmentService).Assembly,
        typeof(FleetAvailabilityRepository).Assembly,
        typeof(IAvailabilityCommitmentService).Assembly,
        typeof(GetAvailabilityCalendarHandler).Assembly,
        typeof(RequestRentalConfirmationNotificationHandler).Assembly,
        typeof(RentalOrderConfirmedIntegrationEventConsumer).Assembly,
    ];

    [Fact]
    public void CrossModuleDependencies_TargetOnlyPublishedContracts()
    {
        var violations = ModuleAssemblies
            .SelectMany(source => source
                .GetReferencedAssemblies()
                .Where(reference => IsAnotherBusinessModule(
                    source.GetName(),
                    reference))
                .Where(reference => !IsPublishedContract(reference))
                .Select(reference =>
                    $"{source.GetName().Name} -> {reference.Name}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void BusinessModules_DoNotDependOnApiCompositionRoot()
    {
        var violations = ModuleAssemblies
            .Where(assembly => assembly
                .GetReferencedAssemblies()
                .Any(reference =>
                    reference.Name == "EquipmentRental.Api"))
            .Select(assembly => assembly.GetName().Name)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void PublishedContracts_AreTransportAndPersistenceIndependent()
    {
        Assembly[] contractAssemblies =
        [
            typeof(RentalOrderConfirmedV1).Assembly,
            typeof(IAvailabilityCommitmentService).Assembly,
        ];
        string[] forbiddenPrefixes =
        [
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
        ];

        var violations = contractAssemblies
            .SelectMany(contract => contract
                .GetReferencedAssemblies()
                .Where(reference => forbiddenPrefixes.Any(prefix =>
                    reference.Name?.StartsWith(
                        prefix,
                        StringComparison.Ordinal) is true))
                .Select(reference =>
                    $"{contract.GetName().Name} -> {reference.Name}"))
            .ToArray();

        Assert.Empty(violations);
    }

    private static bool IsAnotherBusinessModule(
        AssemblyName source,
        AssemblyName reference)
    {
        var sourceModule = GetModuleName(source.Name);
        var referencedModule = GetModuleName(reference.Name);

        return sourceModule is not null
            && referencedModule is not null
            && sourceModule != referencedModule;
    }

    private static string? GetModuleName(string? assemblyName)
    {
        const string prefix = "EquipmentRental.Modules.";

        if (assemblyName?.StartsWith(
                prefix,
                StringComparison.Ordinal) is not true)
        {
            return null;
        }

        return assemblyName[prefix.Length..]
            .Split('.', 2)[0];
    }

    private static bool IsPublishedContract(AssemblyName reference) =>
        reference.Name?.EndsWith(
            ".Contracts",
            StringComparison.Ordinal) is true;
}

#!/usr/bin/env sh
set -eu

dotnet tool run dotnet-ef database update \
  --project src/Modules/Rentals/EquipmentRental.Modules.Rentals.Infrastructure \
  --context RentalsDbContext \
  --configuration Release \
  --no-build

dotnet tool run dotnet-ef database update \
  --project src/Modules/FleetAvailability/EquipmentRental.Modules.FleetAvailability.Infrastructure \
  --context FleetAvailabilityDbContext \
  --configuration Release \
  --no-build

dotnet tool run dotnet-ef database update \
  --project src/Modules/Notifications/EquipmentRental.Modules.Notifications.Infrastructure \
  --context NotificationsDbContext \
  --configuration Release \
  --no-build

dotnet tool run dotnet-ef database update \
  --project src/Modules/FleetAvailability/EquipmentRental.Modules.FleetAvailability.ReadModel \
  --context AvailabilityCalendarDbContext \
  --configuration Release \
  --no-build

dotnet tool run dotnet-ef database update \
  --project src/Modules/Rentals/EquipmentRental.Modules.Rentals.ProcessManagers \
  --context RentalConfirmationProcessDbContext \
  --configuration Release \
  --no-build

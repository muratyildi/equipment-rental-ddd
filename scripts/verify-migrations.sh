#!/usr/bin/env bash

set -euo pipefail

configuration="${1:-Release}"

verify_context() {
  local context="$1"
  local project="$2"

  echo "Verifying ${context}..."
  dotnet tool run dotnet-ef migrations has-pending-model-changes \
    --project "${project}" \
    --context "${context}" \
    --configuration "${configuration}" \
    --no-build
}

verify_context \
  "RentalsDbContext" \
  "src/Modules/Rentals/EquipmentRental.Modules.Rentals.Infrastructure/EquipmentRental.Modules.Rentals.Infrastructure.csproj"
verify_context \
  "FleetAvailabilityDbContext" \
  "src/Modules/FleetAvailability/EquipmentRental.Modules.FleetAvailability.Infrastructure/EquipmentRental.Modules.FleetAvailability.Infrastructure.csproj"
verify_context \
  "AvailabilityCalendarDbContext" \
  "src/Modules/FleetAvailability/EquipmentRental.Modules.FleetAvailability.ReadModel/EquipmentRental.Modules.FleetAvailability.ReadModel.csproj"
verify_context \
  "NotificationsDbContext" \
  "src/Modules/Notifications/EquipmentRental.Modules.Notifications.Infrastructure/EquipmentRental.Modules.Notifications.Infrastructure.csproj"
verify_context \
  "RentalConfirmationProcessDbContext" \
  "src/Modules/Rentals/EquipmentRental.Modules.Rentals.ProcessManagers/EquipmentRental.Modules.Rentals.ProcessManagers.csproj"

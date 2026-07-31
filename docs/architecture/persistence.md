# Persistence ve optimistic concurrency

Bu milestone'un amacı “EF Core kullanmak” değil, DDD'nin consistency ve veri
sahipliği kararlarını veritabanında dürüstçe uygulamaktır.

## 1. Repository neyi saklar?

Repository bir tablo koleksiyonu değildir. Domain açısından
`IRentalOrderRepository`, `RentalOrder` aggregate yaşam döngüsünü; Fleet
repository'si ise `AvailabilitySchedule` yaşam döngüsünü temsil eder.

Bu nedenle repository:

- yalnızca aggregate root üzerinden erişilir;
- child entity için ayrı repository sunmaz;
- yüklerken karar vermek için gereken aggregate'in tamamını getirir;
- transaction'ı `SaveChangesAsync` ile tamamlar.

`RentalLine` ve `AvailabilityCommitment` ayrı tablolardır ama bağımsız
aggregate değildir. Tablo sınırı ile aggregate sınırı aynı kavram değildir.

## 2. Domain neden EF Core referansı içermiyor?

Mapping'ler Infrastructure katmanındaki fluent configuration sınıflarındadır.
Domain nesnelerinde `[Key]`, `[Column]`, `[Timestamp]` gibi persistence
attribute'ları yoktur.

EF Core'un nesneyi veritabanından yeniden oluşturabilmesi için private
parameterless constructor ve private setter kullanılır. Bunlar dışarıya
mutasyon yetkisi vermez; uygulama hâlâ yalnızca `Draft`, `AddLine`, `Quote`,
`Commit` gibi domain davranışlarını çağırabilir.

## 3. Strongly typed ID ve Value Object mapping

`RentalOrderId`, `CustomerId`, `AvailabilityScheduleId` gibi tipler domain'de
yanlış kimliklerin birbirine karıştırılmasını engeller. PostgreSQL'de `uuid`
olarak saklanırlar; dönüşüm Infrastructure'daki `HasConversion` ile yapılır.

`Money`, `RentalPeriod` ve `AvailabilityPeriod` sahip olunan değerler olarak
aynı tabloya açılır. Domain anlamı tek nesne olarak kalırken ilişkisel model
kolonlara ayrılır.

## 4. Schema neden context başına ayrıldı?

```text
equipment_rental database
├── rentals
│   ├── rental_orders
│   ├── rental_lines
│   ├── outbox_messages
│   └── __ef_migrations_history
├── fleet_availability
│   ├── availability_schedules
│   ├── availability_commitments
│   ├── outbox_messages
│   └── __ef_migrations_history
├── fleet_availability_read
│   ├── availability_schedules
│   ├── availability_days
│   ├── inbox_messages
│   └── __ef_migrations_history
├── rentals_process_manager
│   ├── rental_confirmation_processes
│   ├── rental_confirmation_steps
│   └── __ef_migrations_history
└── notifications
    ├── inbox_messages
    ├── notification_work_items
    └── __ef_migrations_history
```

Bu ayrım, aynı process ve database kullanılsa bile veri sahipliğini görünür
kılar. Rentals, Fleet tablolarına foreign key veya doğrudan sorgu ile
bağlanmaz; Fleet'in published contract'ını kullanır.

## 5. Optimistic concurrency hangi problemi çözüyor?

Kapasite 2 iken iki istek aynı anda schedule'ı okuyabilir. İkisi de yerel
kopyasında 2 adet müsait görür. Kontrol yalnızca domain metodunda olursa iki
işlem de kabul kararı verebilir.

PostgreSQL her satırda son değiştiren transaction kimliğini taşıyan gizli
`xmin` kolonuna sahiptir. Npgsql mapping'i bunu `IsRowVersion()` concurrency
token'ı olarak kullanır. EF UPDATE sırasında okuduğu eski `xmin` değerini
koşula ekler:

```sql
UPDATE fleet_availability.availability_schedules
SET updated_at_utc = ...
WHERE id = ... AND xmin = <okunan sürüm>;
```

İlk işlem kökü günceller ve `xmin` değişir. İkinci işlem sıfır satır
güncellediğinde EF Core `DbUpdateConcurrencyException` üretir.

API bu hatayı `409 Conflict` ve
`persistence.optimistic_concurrency_conflict` problem koduna çevirir. İstemci
güncel state'i yükleyip niyetini yeniden göndermelidir. Eski işlemi körlemesine
otomatik retry etmek, domain kararını güncel veri üzerinde tekrar vermeden
uygulamak anlamına gelebilir.

Alt entity eklemek normalde yalnızca child tabloyu değiştirebilirdi. Bu yüzden
DbContext her save işleminde izlenen aggregate root'un `updated_at_utc`
alanına dokunur. Böylece child değişimi de kökün `xmin` kontrolünden geçer.

## 6. Neden otomatik startup migration yok?

API başlangıcında otomatik migration, birden fazla instance aynı anda açılırken
yarış ve production yetki sorunları yaratabilir. Migration açık bir deployment
adımıdır:

```bash
dotnet tool restore
docker compose up -d

dotnet tool run dotnet-ef database update \
  --project src/Modules/Rentals/EquipmentRental.Modules.Rentals.Infrastructure \
  --context RentalsDbContext

dotnet tool run dotnet-ef database update \
  --project src/Modules/FleetAvailability/EquipmentRental.Modules.FleetAvailability.Infrastructure \
  --context FleetAvailabilityDbContext

dotnet tool run dotnet-ef database update \
  --project src/Modules/FleetAvailability/EquipmentRental.Modules.FleetAvailability.ReadModel \
  --context AvailabilityCalendarDbContext

dotnet tool run dotnet-ef database update \
  --project src/Modules/Rentals/EquipmentRental.Modules.Rentals.ProcessManagers \
  --context RentalConfirmationProcessDbContext
```

Connection string varsayılan olarak yalnızca local demo içindir. Başka bir
ortamda `ConnectionStrings__Database` ile override edilmelidir.

## 7. Test stratejisi

EF Core InMemory provider kullanılmaz; çünkü PostgreSQL veri tiplerini,
migration'ları ve `xmin` davranışını taklit etmez. Testcontainers geçici gerçek
PostgreSQL 18 container'ı açar ve şunları kanıtlar:

1. İki modülün migration'ları uygulanabilir.
2. Aggregate, child entity ve Value Object'leriyle round-trip olur.
3. Rehydration yeni Domain Event üretmez.
4. Stale Rentals yazısı concurrency exception alır.
5. Stale Fleet yazısı overbooking yapamaz.

Unit testler domain kararlarının hızlı kanıtıdır; integration testleri ise
teknik adapter'ın bu kararları bozmadığının kanıtıdır. İkisi birbirinin yerine
geçmez.

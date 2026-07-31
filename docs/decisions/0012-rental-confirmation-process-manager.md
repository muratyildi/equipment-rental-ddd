# ADR-0012: Rental confirmation'ı kalıcı Process Manager ile yürütmek

- Durum: Accepted
- Tarih: 2026-07-31
- Supersedes: ADR-0007

## Bağlam

Çok satırlı RentalOrder farklı Fleet Availability aggregate'lerinde
commitment gerektirir. Kısmi başarı kapasiteyi gereksiz tutabilir; process veya
instance restart'ı yalnızca bellekte tutulan ilerlemeyi kaybettirir.

## Seçenekler

1. Aggregate sınırlarını aşan tek database/distributed transaction
2. Stateless application service ile sıralı çağrı
3. Event koreografisi
4. Kalıcı state ve explicit compensation içeren Process Manager

## Karar

Dördüncü seçenek seçildi. Rentals'ın consumer-owned Fleet portunu kullanan ayrı
bir ProcessManagers projesi oluşturulacaktır. Her RentalLine idempotent commit
edilecek; ret veya timeout'ta başarılı commitment'lar ters sırada release
edilecek; hepsi başarılı olduğunda RentalOrder confirm edilecektir.

Process Manager Fleet internallerine referans vermeyecek, kendi PostgreSQL
schema/migration yaşam döngüsüne sahip olacak ve `xmin` tabanlı worker claim
kullanacaktır.

## Sonuçlar

Olumlu:

- Restart sonrasında süreç kaldığı yerden devam eder.
- Partial success görünür ve telafi edilir.
- Duplicate start, commit ve release güvenlidir.
- HTTP isteğinin ömrü business process ömründen ayrılır.
- Aggregate ve bounded-context transaction sınırları korunur.

Olumsuz:

- Strong global atomicity yoktur; eventual completion vardır.
- Ek state machine, worker, schema ve operasyonel izleme gerekir.
- Compensation da başarısız olabilir ve retry/operatör müdahalesi ister.
- Yeni süreç sürümleri için in-flight state migration politikası gerekecektir.

## Kanıtlar

- `RentalConfirmationProcessManagerTests`
- Fleet ve Rentals release domain testleri
- `RentalProcessManager_UsesRentalsPortsWithoutFleetInternals`
- [Process Manager rehberi](../architecture/rental-confirmation-process-manager.md)

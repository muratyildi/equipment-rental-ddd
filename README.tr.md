# Equipment Rental Platform

[![CI](https://github.com/muratyildi/equipment-rental-ddd/actions/workflows/ci.yml/badge.svg)](https://github.com/muratyildi/equipment-rental-ddd/actions/workflows/ci.yml)

> .NET 10 ile geliştirilen, öğrenme odaklı fakat üretim kalitesini hedefleyen
> Domain-Driven Design referans projesi.

**Durum:** `v1.0.0-rc.1` release candidate · 81 test · sıfır uyarılı Release
build · ölçülmüş kanıta dayalı modüler monolit kararı

[English](README.md) · [Mimari](docs/architecture/README.md) ·
[Domain keşfi](docs/discovery/01-product-vision.md) ·
[Karar kayıtları](docs/decisions/README.md)

## Projenin amacı

Bu depo DDD'yi yalnızca `Entity`, `Repository` ve `Service` klasörleriyle
göstermeyi amaçlamaz. İzlediğimiz sıra:

1. İş problemi ve ürün vizyonu
2. Domain uzmanları
3. Big Picture Event Storming
4. Ubiquitous Language
5. Subdomain sınıflandırması
6. Bounded Context ve Context Map
7. Taktiksel model
8. Uygulama ve altyapı

Her önemli kararda şu sorular cevaplanır:

- Hangi iş problemi var?
- Seçenekler neler?
- Neyi, neden seçtik?
- Kararın maliyeti nedir?
- Hangi koşulda yeniden değerlendirilecek?

## Örnek iş alanı

Endüstriyel ekipman kiralama şirketinin teklif, rezervasyon, teslimat, kiralama,
iade, bakım ve faturalama süreçlerini modelliyoruz.

İlk çalışan dikey dilim `Rentals` ve `Fleet Availability` bounded
context'lerini entegre eder:

```text
Taslak oluştur
    → ticari kalemleri ekle
    → teklif ver
    → her kalem için Fleet Availability'den taahhüt iste
    → kiralamayı onayla
```

`RentalOrder` aggregate'i şu invariants'ı korur:

- Boş sipariş teklif hâline gelemez.
- Teklif süresi gelecekte olmalıdır.
- Tekliften sonra ticari kalemler sessizce değiştirilemez.
- Bütün kalemler aynı para biriminde olmalıdır.
- Availability commitment kabul edilen dönemin tamamını kapsamalıdır.
- Her kalemin commitment'ı olmadan kiralama onaylanamaz.
- Süresi dolmuş teklif onaylanamaz.

## Neden modüler monolit?

Bounded Context bir model sınırıdır; zorunlu olarak microservice değildir.
Sınırlar henüz öğrenilirken dağıtık sistem maliyetini yüklenmek yerine:

- context'leri ayrı modüller olarak koruyoruz;
- bağımlılık yönlerini mimari testlerle denetliyoruz;
- context'ler arasında entity paylaşmıyoruz;
- gelecekte servis ayırma hakkını koruyoruz.

Bağımsız ölçekleme, takım sahipliği, güvenlik veya ayrı yayın ritmi ölçülürse
deployment sınırı yeniden değerlendirilecek.

Mevcut değerlendirmede hiçbir context servis yapılmamıştır. Notifications en
düşük riskli teknik pilot, Fleet Availability ise farklı scale/contention
profili kanıtlanırsa en güçlü operasyonel adaydır. Bu hipotezler ölçülmüş
production kanıtı olmadığı için extraction kararı sayılmaz.

## Çalıştırma

Gereksinimler:

- `.NET SDK 10.0.103` veya uyumlu daha yeni bir .NET 10 feature band;
- PostgreSQL ve integration testleri için Docker.

```bash
dotnet restore EquipmentRental.slnx
dotnet tool restore
docker compose up -d postgres
dotnet tool run dotnet-ef database update \
  --project src/Modules/Rentals/EquipmentRental.Modules.Rentals.Infrastructure \
  --context RentalsDbContext
dotnet tool run dotnet-ef database update \
  --project src/Modules/FleetAvailability/EquipmentRental.Modules.FleetAvailability.Infrastructure \
  --context FleetAvailabilityDbContext
dotnet tool run dotnet-ef database update \
  --project src/Modules/Notifications/EquipmentRental.Modules.Notifications.Infrastructure \
  --context NotificationsDbContext
dotnet tool run dotnet-ef database update \
  --project src/Modules/FleetAvailability/EquipmentRental.Modules.FleetAvailability.ReadModel \
  --context AvailabilityCalendarDbContext
dotnet tool run dotnet-ef database update \
  --project src/Modules/Rentals/EquipmentRental.Modules.Rentals.ProcessManagers \
  --context RentalConfirmationProcessDbContext
dotnet build EquipmentRental.slnx --configuration Release
dotnet test EquipmentRental.slnx --configuration Release
dotnet run --project src/Api/EquipmentRental.Api
```

Tam container ortamı alternatifi:

```bash
cp .env.example .env
# .env içindeki iki örnek anahtarı değiştirin.
docker compose up --build
```

Örnek HTTP çağrıları:
[`EquipmentRental.Api.http`](src/Api/EquipmentRental.Api/EquipmentRental.Api.http)

## Mevcut durum

- Ürün vizyonu ve domain uzmanları tanımlandı.
- Event Storming zaman çizgisi ve hotspot'lar çıkarıldı.
- Ubiquitous Language context'lere göre ayrıldı.
- Subdomain ve Bounded Context analizi yapıldı.
- Context Map hazırlandı.
- İlk `RentalOrder` aggregate'i ve Value Object'ler uygulandı.
- Fleet Availability context'i, kapasite aggregate'i ve published contract
  uygulandı.
- Rentals consumer-owned port ve anticorruption adapter üzerinden Fleet'e
  bağlandı.
- Her bounded context kendi PostgreSQL schema'sına ve DbContext'ine sahip.
- Aggregate'ler PostgreSQL `xmin` ile optimistic concurrency korumasına sahip.
- Migration, Docker Compose ve gerçek PostgreSQL kullanan Testcontainers
  testleri eklendi.
- Seçilen Domain Event'ler sürümlü Integration Event sözleşmelerine çevriliyor
  ve business değişikliğiyle aynı transaction'da Outbox'a yazılıyor.
- Background worker optimistic claim, retry ve at-least-once publish davranışı
  sağlıyor.
- Notifications supporting context'i `rental-order-confirmed.v1` event'ini
  transaction script ile tüketiyor.
- Inbox kaydı ve notification work item aynı transaction'da oluşturularak
  duplicate teslimatlar idempotent hâle getiriliyor.
- Fleet capacity ve commitment event'leri, gün bazında sorguya göre tasarlanmış
  ayrı bir CQRS Availability Calendar projection'ına aktarılıyor.
- Projection kendi schema, DbContext ve Transactional Inbox'ına sahip;
  duplicate ve sırası değişen event teslimatları güvenli.
- Çok satırlı confirmation kalıcı Process Manager tarafından yürütülüyor;
  ret veya timeout'ta önceki Fleet commitment'ları ters sırada release ediliyor.
- Worker lease, idempotent start/commit/release ve crash-safe retry uygulanıyor.
- Business endpoint'leri read/write scope'lu API key ve rate limiting ile
  korunuyor.
- JSON log, correlation id, liveness/readiness, runtime metrics, Outbox
  dead-letter ve Process Manager intervention state'leri eklendi.
- Multi-stage non-root API image ve ayrı migration job içeren tam Docker
  Compose ortamı hazırlandı.
- 22 domain, 4 application ve 18 mimari/servis ayrılabilirliği testi geçiyor.
- 24 PostgreSQL integration ve 13 API boundary/security/observability testi
  geçiyor.
- Release build sıfır uyarıyla tamamlanıyor.
- HTTP create → quote → commitment → confirm akışı doğrulandı.

Projenin tamamlanmış gibi gösterilmemesi bilinçlidir. Gerçek message broker ve
telemetry exporter adapter'ları, kontrollü replay/runbook ve diğer context'ler
ölçülmüş ihtiyaçlarla birlikte sonraki milestone'larda eklenecek.

## Belgeler

- [Product Vision](docs/discovery/01-product-vision.md)
- [Big Picture Event Storming](docs/discovery/02-big-picture-event-storming.md)
- [Ubiquitous Language](docs/discovery/03-ubiquitous-language.md)
- [Subdomain ve Bounded Context analizi](docs/discovery/04-subdomains-and-bounded-contexts.md)
- [Context Map](docs/discovery/05-context-map.md)
- [Mimari rehber](docs/architecture/README.md)
- [Persistence ve concurrency rehberi](docs/architecture/persistence.md)
- [Domain Event ve Transactional Outbox rehberi](docs/architecture/domain-events-and-outbox.md)
- [Idempotent Consumer ve Inbox rehberi](docs/architecture/idempotent-consumers-and-inbox.md)
- [CQRS Availability Calendar rehberi](docs/architecture/cqrs-availability-calendar.md)
- [Rental Confirmation Process Manager rehberi](docs/architecture/rental-confirmation-process-manager.md)
- [Operational Readiness rehberi](docs/architecture/operational-readiness.md)
- [Service Extraction değerlendirmesi](docs/architecture/service-extraction-assessment.md)
- [Service Extraction playbook](docs/architecture/service-extraction-playbook.md)
- [v1.0.0-rc.1 yayın kontrol listesi](docs/releases/v1.0.0-rc.1.md)
- [Rentals model açıklaması](docs/modules/rentals.md)
- [Fleet Availability model açıklaması](docs/modules/fleet-availability.md)
- [Notifications model açıklaması](docs/modules/notifications.md)
- [Karar günlüğü](docs/decisions/README.md)
- [DDD öğrenme notları](docs/learning/00-kaynak-incelemesi.md)

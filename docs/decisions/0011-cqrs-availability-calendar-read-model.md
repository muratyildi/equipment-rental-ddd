# ADR-0011: Availability Calendar için CQRS read model

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

`AvailabilitySchedule` aggregate'i overbooking'i önleyen bir karar modelidir.
Takvim ekranı ise bir tarih aralığındaki her gün için toplam, committed ve
müsait kapasiteyi istemektedir. Aggregate'i doğrudan sorgu modeli yapmak,
yazma invariant'ı için seçilen yapıyı okuma biçimine bağımlı kılacaktır.

## Karar sürücüleri

- Aggregate sınırını ve strong consistency kararını korumak
- Gün bazında sabit ve öngörülebilir sorgu maliyeti
- At-least-once event teslimatında doğru sonuç
- Bounded context sahipliğini bozmadan eventual consistency öğretmek
- Gereksiz microservice, mediator ve Event Sourcing maliyetinden kaçınmak

## Değerlendirilen seçenekler

1. Aggregate'i yükleyip takvimi her sorguda hesaplamak
2. Write tablolarına özel SQL ile doğrudan sorgu yapmak
3. Event'lerle güncellenen ayrı, denormalize read model

İlk seçenek sorguyu aggregate büyüklüğüne bağlar. İkinci seçenek daha hızlı
olabilir fakat query'yi write schema'ya ve EF mapping ayrıntılarına bağlar.
Üçüncü seçenek ek operasyonel maliyet karşılığında açık bir sorgu modeli ve
bağımsız optimizasyon sağlar.

## Karar

Fleet Availability bounded context'i içinde ayrı
`EquipmentRental.Modules.FleetAvailability.ReadModel` projesi ve
`fleet_availability_read` PostgreSQL schema'sı kullanılacaktır.

Projection, Fleet'in sürümlü `capacity-defined.v1` ve
`availability-committed.v1` event'lerini tüketir. Transactional Inbox duplicate
teslimatı etkisiz kılar. Takvim sorgusu yalnızca read-model DbContext'ini okur.

## Olumlu sonuçlar

- Write model invariant odaklı kalır.
- Takvim sorgusu gün sayısıyla orantılı ve API ihtiyacına uygun olur.
- Query tarafı Domain/Application/Infrastructure iç katmanlarından ayrıdır.
- Eventual consistency, ordering ve duplicate delivery entegrasyon testleriyle
  görünürdür.
- Aynı bounded context içinde kalarak dağıtık sistem maliyeti ertelenir.

## Olumsuz sonuçlar ve riskler

- Komut ile sorgu arasında kısa süreli gecikme vardır.
- Dördüncü DbContext, schema ve migration yaşam döngüsü oluşur.
- Projection hataları için replay, gözlemlenebilirlik ve operasyonel onarım
  gerekir.
- Yeni domain davranışları yeni event sözleşmeleri ve projector değişiklikleri
  gerektirir.

## Yeniden değerlendirme koşulları

- Takvim ayrı ölçekleme veya deployment ritmi gerektirirse
- Projection lag için daha güçlü SLA oluşursa
- Broker replay/partition ordering seçimi yapılırsa
- Capacity change, release ve cancellation davranışları eklenirse
- Birden fazla read model aynı event akışını tüketmeye başlarsa

## İlgili kanıtlar

- `AvailabilityCalendarProjectionTests`
- `AvailabilityCalendarReadModel_DependsOnlyOnPublishedContracts`
- [CQRS Availability Calendar rehberi](../architecture/cqrs-availability-calendar.md)

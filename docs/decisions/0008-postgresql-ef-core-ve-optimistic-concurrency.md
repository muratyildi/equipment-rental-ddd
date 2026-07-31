# ADR-0008 — PostgreSQL, EF Core ve aggregate optimistic concurrency

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

In-memory adapter ilk dikey akışı doğruladı; fakat process yeniden başladığında
veriyi kaybediyor, gerçek rehydration davranışını göstermiyor ve iki eşzamanlı
yazmanın aynı invariant'ı bozmasını engellemiyordu. Özellikle Fleet
Availability için iki eski aggregate kopyasının aynı kapasiteyi ayrı ayrı
satması overbooking yaratabilir.

## Karar sürücüleri

- Domain modelini EF Core'a bağımlı kılmamak
- Bounded Context veri sahipliğini fiziksel olarak görünür yapmak
- Aggregate'i tek consistency boundary olarak korumak
- Gerçek PostgreSQL davranışını otomatik test etmek
- Migration geçmişini modül bazında yönetmek

## Değerlendirilen seçenekler

1. EF Core InMemory provider: ilişkisel ve concurrency davranışını kanıtlamaz.
2. SQLite: hızlıdır fakat PostgreSQL'e özgü `xmin` davranışını göstermez.
3. Elle yazılmış SQL/Dapper: mümkündür; bu milestone'da mapping ve change
   tracking maliyetini gereksiz artırır.
4. EF Core + PostgreSQL + Testcontainers: seçildi.

## Karar

- Tek PostgreSQL database kullanılacak.
- Rentals `rentals`, Fleet Availability `fleet_availability` schema'sına sahip
  olacak.
- Her modül kendi DbContext, mapping, migration ve repository adapter'ını
  barındıracak.
- Context'ler arasında foreign key olmayacak.
- Aggregate root satırındaki PostgreSQL `xmin` alanı `IsRowVersion()` olarak
  eşlenecek.
- Alt entity değişiminde de kök satır `updated_at_utc` üzerinden dokunulacak;
  böylece `xmin` tüm aggregate değişimini koruyacak.
- Testcontainers testleri migration, rehydration ve stale-write çatışmasını
  gerçek PostgreSQL üzerinde doğrulayacak.

## Olumlu sonuçlar

- Domain katmanı EF Core referansı içermez.
- Strongly typed ID ve Value Object'ler fluent mapping ile korunur.
- Repository aggregate'i davranış kararı verecek bütünlükte yükler.
- Eşzamanlı kapasite taahhütleri sessizce overbooking yapamaz.
- İki modül bağımsız migration geçmişine sahiptir.

## Olumsuz sonuçlar ve riskler

- Yerel geliştirme ve integration testleri Docker gerektirir.
- `Include` unutulursa aggregate eksik rehydrate edilebilir; round-trip testleri
  bunu korur.
- Optimistic concurrency çatışması API'de `409 Conflict` olur; otomatik retry
  bilinçli olarak yapılmaz çünkü domain kararı güncel state üzerinde yeniden
  verilmelidir.
- Tek database operasyonel olarak context'leri birlikte taşır; bu bilinçli
  modüler monolit tercihidir.

## Yeniden değerlendirme koşulları

- Bir context ayrı deployment veya database gerektirirse
- Yüksek contention nedeniyle optimistic retry oranı kabul edilemez olursa
- Write modeli için EF Core ölçülmüş performans sorunu yaratırsa
- Aggregate sınırının gereğinden büyük olduğu gözlemlenirse

## Kanıtlayan testler

- Rentals aggregate round-trip testi
- Fleet Availability aggregate round-trip testi
- İki stale Rentals kopyasının concurrency çatışması
- Aynı kapasiteyi tüketen iki stale Fleet kopyasının overbooking'i engellemesi

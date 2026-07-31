# Domain Event, Integration Event ve Transactional Outbox

Bu üç kavram aynı şey değildir:

| Kavram | Sahibi | Amacı | Kalıcılık |
|---|---|---|---|
| Domain Event | Bounded Context domain modeli | Aggregate içinde gerçekleşen iş gerçeğini ifade etmek | Commit'e kadar aggregate üzerinde |
| Integration Event | Yayımlayan Bounded Context | Başka context'lere kararlı sözleşme sunmak | Outbox payload'ı |
| Outbox Message | Infrastructure | Integration Event'i güvenilir biçimde publish etme niyetini saklamak | Modül schema'sında tablo |

## 1. Akış

```text
HTTP command
  → Application handler
  → Aggregate davranışı
  → Domain Event oluşur
  → SaveChanges
      ├─ aggregate değişikliği
      └─ sürümlü Integration Event Outbox INSERT'i
         [aynı PostgreSQL transaction]
  → commit
  → background worker mesajı claim eder
  → transport adapter publish eder
  → processed_at_utc yazılır
```

Aggregate broker'a mesaj göndermez, JSON üretmez ve Outbox tablosunu bilmez.
Yalnızca kendi dilinde bir iş gerçeği oluşturur.

## 2. Domain Event kimliği

Her event oluşturulduğunda bir `EventId` alır. Bu kimlik:

- Outbox primary key'i olur;
- retry sırasında değişmez;
- consumer inbox/deduplication kaydının anahtarı olabilir;
- log ve trace korelasyonunu kolaylaştırır.

`EventId` teslimatı exactly-once yapmaz. Duplicate'i tanınabilir yapar.

## 3. Seçici mapping

Rentals içindeki `RentalOrderDrafted` veya `RentalLineAdded` gibi her ara gerçek
dışarı yayımlanmaz. Şu anda dış sözleşmeye çevrilen olaylar:

```text
RentalOrderConfirmed
  → rentals.rental-order-confirmed.v1

EquipmentAvailabilityCommitted
  → fleet-availability.equipment-availability-committed.v1
```

Integration contract assembly'leri Domain assembly'lerine referans vermez.
Sözleşmeler `Guid`, `DateOnly`, `decimal`, `string` ve benzeri taşınabilir
değerlerden oluşur.

## 4. Atomicity

DbContext, tracked aggregate'lerin Domain Event'lerini `SaveChangesAsync`
başında toplar. Seçilen event'leri Outbox entity'sine map eder. EF Core
aggregate UPDATE/INSERT ile Outbox INSERT'i aynı transaction'da yürütür.

- Commit başarılıysa Domain Event listesi temizlenir.
- Commit başarısızsa hem aggregate hem Outbox rollback olur.
- Event listesi temizlenmediği için aynı context üzerinde güvenli retry
  mümkündür.
- Aynı `EventId` için ikinci tracked Outbox entity oluşturulmaz.

Bu davranış gerçek PostgreSQL üzerinde duplicate aggregate key ile zorlanan
rollback testiyle doğrulanır.

## 5. Claim ve çoklu worker

Worker yalnızca:

- henüz işlenmemiş;
- retry zamanı gelmiş;
- claim edilmemiş veya claim süresi dolmuş

mesajları seçer. Mesaj `claimed_by` ve `claimed_until_utc` ile işaretlenirken
PostgreSQL `xmin` concurrency kontrolü kullanılır. İki worker aynı satırı okursa
yalnızca biri claim UPDATE'ini tamamlayabilir.

Claim kalıcı kilit değildir. Worker ölürse süre dolduğunda başka worker mesajı
devralabilir.

## 6. Retry

Publish hatası business transaction'ını geri almaz. Outbox kaydında:

- `attempts` artırılır;
- `last_error` kaydedilir;
- claim temizlenir;
- `next_attempt_at_utc` exponential backoff ile ileri alınır.

Bir mesaj beş başarısız publish denemesinden sonra `DeadLetteredAtUtc` ile
terminal hâle gelir ve normal claim sorgusundan çıkar. Böylece poison message
diğer mesajların işlenmesini sonsuza kadar engellemez. Readiness bu durumu
`Degraded` olarak raporlar. Kontrollü replay ve retention politikası hâlâ
operasyon runbook'u gerektirir.

## 7. At-least-once neden duplicate üretebilir?

Şu sıra kaçınılmaz bir failure penceresi taşır:

```text
broker publish başarılı
  → process çöker
  → processed_at_utc yazılamaz
  → mesaj yeniden publish edilir
```

Bu nedenle Notifications consumer'ı, `EventId` değerini Inbox tablosunda
notification work item ile atomik olarak kaydeder. Aynı mesaj yeniden gelirse
ikinci kez iş etkisi oluşturmaz.

## 8. Mevcut transport adapter'ı

API şu anda local geliştirme için matching consumer'lara dispatch eden
in-process transport adapter'ı kullanır. Adapter envelope'u structured log'a da
yazar. Outbox, Inbox ve consumer transporttan bağımsızdır; RabbitMQ, Azure
Service Bus veya Kafka kararı verildiğinde yalnızca publisher/receiver
adapter'ları değişir.

Bu bilinçli sınır, broker seçimini DDD'nin parçasıymış gibi göstermeden
reliability modelini tamamlamamızı sağlar.

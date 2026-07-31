# Service Extraction Playbook

Bu belge bugünkü deployment planı değildir. Ölçülmüş bir sürücü
[değerlendirme eşiklerini](service-extraction-assessment.md) aşarsa izlenecek
güvenli geçiş yoludur.

## Temel ilkeler

1. Bounded context sınırı deployment değişmeden önce zaten mevcut olmalıdır.
2. Veri tek bir context tarafından yazılmalıdır.
3. Database dual-write yapılmamalıdır.
4. İç domain modeli wire contract olarak yayımlanmamalıdır.
5. Network çağrısı local method call gibi davranıyormuş kabul edilmemelidir.
6. Cutover küçük, gözlemlenebilir ve geri alınabilir olmalıdır.
7. Eski yol, yeni yol kanıtlanmadan kaldırılmamalıdır.

## Ortak geçiş aşamaları

### 0. Kararı doğrula

- Hard driver ve baseline kaydedilir.
- “Daha ucuz çözüm neden yeterli değil?” sorusu cevaplanır.
- Context sahibi, SLO, hata bütçesi ve on-call sorumluluğu belirlenir.
- Extraction ADR'si `Proposed` olarak açılır.

### 1. Sözleşmeyi sabitle

- Command/request ve Integration Event şemaları versionlanır.
- Consumer-driven contract testleri eklenir.
- Idempotency anahtarları ve retry semantiği belgelenir.
- Breaking change için parallel version ve sunset politikası tanımlanır.

Monolit içindeki `.Contracts` assembly'si tasarım sınırını gösterir. Ayrı
deployment sonrasında bu assembly'yi iki servisin birlikte release edilmesini
gerektiren ortak domain paketi hâline getirmeyiz. Wire schema; OpenAPI,
AsyncAPI, Protobuf veya açık JSON schema olarak bağımsız versionlanır.

### 2. Transport adapter'ını ekle

Consumer-owned port korunur. Local adapter'ın yanına remote adapter gelir:

```text
Rentals Application
        |
IEquipmentAvailabilityGateway
        |
        +-- InProcess adapter
        +-- Remote adapter
```

Feature flag composition root'ta hangi adapter'ın kullanılacağını seçer.
Domain ve Application transport bilgisini öğrenmez.

### 3. Veriyi ayır

- Context schema'sı bağımsız database'e kopyalanır.
- Başka context'ten foreign key veya doğrudan SQL olmadığı doğrulanır.
- Snapshot + change stream/backfill yöntemi seçilir.
- Reconciliation raporu hazırlanır.
- Cutover'dan sonra yalnızca yeni servis yazma sahibi olur.

Aynı business değişikliğini iki database'e uygulama koduyla yazmak yasaktır.
İkinci tarafta gerekli model Integration Event ile türetilir.

### 4. Shadow ve canary doğrulama

- Read-only veya karar karşılaştırması yapılabiliyorsa remote sonuç shadow
  olarak alınır; kullanıcı sonucunu değiştirmez.
- Sonuç farkları primitive kimlik ve correlation ID ile kaydedilir.
- Küçük trafik yüzdesi yeni servise yönlendirilir.
- Latency, error, saturation ve business outcome karşılaştırılır.

State-changing command'lerde aynı komutu iki sisteme kontrolsüz uygulamayız.
Idempotent command ve açık reconciliation olmadan shadow write yapılmaz.

### 5. Cutover

- Producer/consumer sırası backward-compatible contract'a göre planlanır.
- Traffic kademeli artırılır.
- Outbox lag, Inbox duplicate, timeout ve business rejection ayrı ölçülür.
- Eski adapter rollback süresi boyunca kullanılabilir kalır.

### 6. Temizlik

- Stabilizasyon penceresi tamamlanınca local adapter kaldırılır.
- Eski schema read-only tutulur, sonra retention politikasına göre arşivlenir.
- Mimari testler yeni sınırı yansıtacak şekilde sıkılaştırılır.
- ADR `Accepted` yapılır ve gerçek sonuçlar kaydedilir.

## Fleet Availability çıkarma planı

### Neden özel dikkat gerekir?

Rentals, commitment sonucunu confirmation'a devam etmek için bilmek zorundadır.
Bu nedenle commit/release bir request-response konuşmasıdır. Ayrı process
sonrasında üç sonuç vardır:

1. Açık kabul veya ret alındı.
2. Çağrı Fleet'e hiç ulaşmadı.
3. Fleet commit etti fakat cevap kayboldu.

Üçüncü durumda “timeout = başarısız” denemez. Aynı `DemandId` ile tekrar çağrı,
Fleet'in idempotent davranışı sayesinde önceki sonucu döndürmelidir.

### Hedef iletişim

- Commit/release: timeout'lu senkron HTTP veya gRPC adapter.
- Capacity/commitment facts: durable broker üzerinden Integration Event.
- Her request: correlation ID ve stable `DemandId`.
- Retry: yalnızca idempotent operation, bounded exponential backoff.
- Circuit breaker: hızlı hata için; business rejection ile karıştırılmaz.
- Process Manager: belirsiz teknik sonucu retry eder, bütçe bitince
  `RequiresIntervention` durumuna geçer.

### Cutover sırası

1. Fleet'i aynı contract semantiğiyle bağımsız host'ta çalıştır.
2. Ayrı Fleet database'ine migration ve veri doğrulaması yap.
3. Remote gateway'i Rentals composition root'una ekle.
4. Read-only availability sorgularını canary olarak taşı.
5. İdempotent commit/release çağrılarını düşük trafikle taşı.
6. Fleet Integration Event'lerini broker'a geçir.
7. Reconciliation sonrası in-process gateway'i kaldır.

### Rollback

- Remote adapter feature flag ile kapatılır.
- Yazma sahipliği cutover edilmişse eski database'e körlemesine dönülmez.
- Fleet servisi ayakta, deployment geri alınır veya traffic eski API
  versiyonuna yönlendirilir.
- Veri reconciliation tamamlanmadan ownership geri taşınmaz.

## Notifications çıkarma planı

### Neden daha düşük riskli?

Rentals notification sonucunu beklemez. `RentalOrderConfirmedV1` zaten
versionlanmış, Outbox mesajı stable ID taşır ve Notifications Inbox duplicate
etkiyi engeller.

### Hedef iletişim

```text
Rentals transaction
  -> Rentals Outbox
  -> durable broker
  -> Notifications consumer
  -> Notifications Inbox + work item transaction
```

Broker acknowledgement yalnızca Inbox ve work item commit edildikten sonra
verilir. Poison message retry bütçesi sonunda dead-letter'a gider. Event
redelivery normal kabul edilir.

### Cutover sırası

1. Broker publisher adapter'ını `IIntegrationEventPublisher` arkasına ekle.
2. Notifications consumer'ını bağımsız worker olarak deploy et.
3. Kendi database/schema migration'ını çalıştır.
4. Aynı event'in local ve remote etkisini kontrollü test ortamında karşılaştır.
5. Production publisher'ı broker'a geçir.
6. Inbox count, duplicate count, oldest-message age ve dead-letter metriğini
   izle.
7. Stabilizasyon sonrası local consumer registration'ını kaldır.

### Rollback

- Broker'daki event'ler kaybedilmez; consumer deployment geri alınabilir.
- Eski local consumer yalnızca aynı event stream tek owner tarafından
  tüketilecek şekilde tekrar açılır.
- Inbox anahtarı aynı kaldığı için replay duplicate work item üretmez.

## Availability Calendar hakkında

Projection'ı ayrı process'te çalıştırmak, onu otomatik olarak ayrı bounded
context yapmaz. Yüksek read trafiği oluşursa önce projection worker veya query
host'u ayrı deploy edilebilir. Model sahipliği yine Fleet Availability'de
kalabilir.

Bu ayrım önemlidir:

```text
ayrı process ≠ yeni bounded context
ayrı bounded context ≠ zorunlu ayrı process
```

## Extraction tamamlanma ölçütleri

- Eski code path kaldırılmıştır.
- Tek write owner doğrulanmıştır.
- Contract compatibility testleri geçmektedir.
- SLO ve error budget dashboard'ları aktiftir.
- Outbox/Inbox lag ve dead-letter runbook'u vardır.
- Failure injection ile timeout, duplicate ve broker kesintisi denenmiştir.
- Rollback provası kaydedilmiştir.
- Operasyon maliyetinin kararda beklenen faydayı sağladığı gözlemlenmiştir.

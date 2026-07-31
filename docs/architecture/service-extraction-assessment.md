# Service Extraction Değerlendirmesi

## Sonuç

**31 Temmuz 2026 itibarıyla hiçbir bounded context bağımsız servise
çıkarılmayacaktır.** Sistem modüler monolit olarak kalacaktır.

Bu, servis çıkaramayacağımız anlamına gelmez. Aksine mevcut kod sınırları,
gerektiğinde seçili bir modülün ayrılabilmesi için korunmaktadır. Fakat henüz
ayrı process, ağ, broker, bağımsız deployment ve dağıtık operasyon maliyetini
haklı çıkaran üretim kanıtımız yoktur.

İki farklı adaylık türünü ayırıyoruz:

- **Notifications**, teknik olarak en düşük riskli ilk ayrıştırma pilotudur.
  Akışı asenkrondur, kendi verisine sahiptir ve Rentals'ın yalnızca yayımlanmış
  event sözleşmesini tüketir.
- **Fleet Availability**, bağımsız ölçekleme veya yoğun write contention
  oluşursa en güçlü operasyonel servis adayıdır. Fakat Rentals ile arasındaki
  commit/release konuşması bugün process içi ve senkrondur; ayırmak gerçek bir
  network failure modeli getirir.

Bu iki gözlem bir extraction kararı değildir. Yalnızca hangi sinyaller
oluştuğunda önce nereye bakacağımızı söyler.

## Bounded Context, modül ve servis aynı şey değildir

```text
Bounded Context = modelin ve Ubiquitous Language'ın anlam sınırı
Module          = bu sınırın aynı codebase/process içindeki fiziksel koruması
Service         = bağımsız process ve deployment sınırı
```

Bir bounded context modüler monolitte eksiksiz biçimde uygulanabilir. Bir
servisin de kötü tasarlanmış olması ve birden fazla context'i birbirine
karıştırması mümkündür. DDD, her bounded context için otomatik olarak ayrı
servis istemez.

Mevcut topoloji:

```mermaid
flowchart TB
    HOST["EquipmentRental.Api<br/>tek process / tek deployment"]
    HOST --> R["Rentals module<br/>rentals schema"]
    HOST --> FA["Fleet Availability module<br/>fleet_availability schema"]
    HOST --> N["Notifications module<br/>notifications schema"]
    HOST --> CAL["Availability Calendar projection<br/>fleet_availability_read schema"]
    HOST --> PM["Rental Confirmation Process Manager<br/>rentals_process_manager schema"]
```

Availability Calendar ayrı bir bounded context değildir. Fleet Availability
tarafından yayımlanan gerçeklerden türetilen CQRS read model'dir. Process
Manager da yeni bir bounded context değil, Rentals'ın context'ler arası uzun
süren confirmation sürecini yöneten orchestration bileşenidir.

## Kanıt sınıfları

Değerlendirmede üç farklı bilgi türü kullanılır:

| Sınıf | Anlamı | Bu değerlendirmedeki örnek |
|---|---|---|
| Doğrulanmış yapısal kanıt | Kod ve testlerden doğrudan görülebilir | Ayrı schema, Contracts assembly, yasak bağımlılık testleri |
| Domain hipotezi | İş modelinden beklenen fakat üretimde ölçülmemiş davranış | Fleet write contention riski |
| Operasyonel kanıt | Canlı trafik, incident ve deployment verisi | Henüz yok |

Bir hipotezi ölçülmüş sonuç gibi sunmuyoruz. Repository'nin henüz gerçek
production trafiği olmadığı için latency yüzdeleri, CPU oranları, incident
sayıları veya takım teslimat ritimleri hakkında skor uydurulmamıştır.

## Değerlendirme ölçütleri

Bir context aşağıdaki sürücülerden biri veya birkaçı için bağımsız deployment
gerektiriyorsa servis adayı olabilir:

1. **Bağımsız ölçekleme:** Diğer modüllerden belirgin şekilde farklı CPU,
   memory, throughput veya concurrency profili.
2. **Hata izolasyonu:** Bir modülün arızasının başka bir kritik capability'nin
   SLO'sunu bozması.
3. **Bağımsız teslimat ritmi:** Aynı deployment'ın farklı takımların güvenli
   teslimatını sürekli engellemesi.
4. **Takım sahipliği:** Context'i uçtan uca sahiplenen kalıcı ve özerk takım.
5. **Güvenlik/uyumluluk:** Process, network veya veri düzeyinde zorunlu
   izolasyon.
6. **Teknoloji ihtiyacı:** İş yükünün mevcut runtime veya storage modelinden
   ölçülmüş biçimde farklı bir çözüm gerektirmesi.

Şunlar tek başına yeterli gerekçe değildir:

- context'in ayrı bir adı olması;
- klasör veya schema sayısının artması;
- mikroservislerin popüler olması;
- “ileride büyüyebilir” tahmini;
- deployment sayısını mimari olgunluk göstergesi sanmak.

## Mevcut context değerlendirmesi

| Context / model | Yapısal ayrılabilirlik | Operasyonel sürücü | Dağıtık sistem maliyeti | Bugünkü karar |
|---|---|---|---|---|
| Rentals | Orta | Ölçülmedi | Yüksek; core workflow ve Process Manager merkezi | Modüler monolitte tut |
| Fleet Availability | Yüksek | Contention ve farklı ölçek profili yalnızca hipotez | Orta/Yüksek; senkron commit/release ve network failure | Ayrılabilir tut, ölç |
| Notifications | Yüksek | Bağımsız backlog/kanal ölçeği ölçülmedi | Orta; broker ve teslimat operasyonu gerekir | Düşük riskli pilot adayı, henüz ayırma |
| Availability Calendar | Teknik olarak yüksek | Read trafiği ölçülmedi | Orta; event transport ve freshness SLO gerekir | Fleet içindeki read model olarak tut |
| Confirmation Process Manager | Düşük | Ayrı worker ölçeği ölçülmedi | Yüksek; Rentals state transition'larıyla yakın | Rentals sınırında tut |

### Rentals

Doğrulanmış kanıtlar:

- Core subdomain davranışını ve ticari lifecycle'ı sahiplenir.
- Kendi aggregate, application portları, schema ve Outbox'ı vardır.
- Fleet internallerini referans etmez.
- Çok satırlı confirmation ilerlemesini kendi Process Manager'ı yönetir.

Neden şimdi ayrılmıyor?

- API'nin ana iş akışı Rentals merkezlidir.
- Ayrı deployment bugün bağımsız ölçekleme faydasından çok network ve
  koordinasyon maliyeti getirir.
- Farklı takım, güvenlik sınırı veya teslimat darboğazı kanıtı yoktur.

### Fleet Availability

Doğrulanmış kanıtlar:

- Kendi domain modeli, aggregate'i, application katmanı ve schema'sı vardır.
- Primitive tiplerden oluşan Published Contract sağlar.
- Rentals, consumer-owned port ve ACL üzerinden bu contract'ı kullanır.
- Event'leri Outbox üzerinden yayımlar; read projection idempotent consumer'dır.
- Mimari testler Rentals'ın Fleet internallerine erişmesini engeller.

Neden güçlü aday?

- Capacity commitment yazmaları, Rentals'ın ticari okuma/yazmalarından farklı
  concurrency ve contention profili geliştirebilir.
- Bir schedule aggregate'i büyürse bağımsız ölçekleme veya serialized command
  processing gerekebilir.
- Domain ve veri sahipliği sınırı nettir.

Neden şimdi ayrılmıyor?

- Farklı yük profili ve contention henüz ölçülmedi.
- Commit/release çağrıları bugün senkron in-process'tir. Network'e taşınınca
  timeout, belirsiz sonuç ve circuit breaking zorunlu olur.
- Ayrı deployment overbooking invariant'ını iyileştirmez; invariant yine Fleet
  aggregate transaction'ında korunmalıdır.

### Notifications

Doğrulanmış kanıtlar:

- Supporting subdomain'dir ve Rentals'ın yalnızca
  `rental-order-confirmed.v1` event'ini tüketir.
- Application katmanı başka business module referans etmez.
- Inbox ve notification work item aynı local transaction'da yazılır.
- Kendi schema'sı vardır.

Neden düşük riskli pilot?

- Producer cevap beklemez; eventual consistency zaten açıkça kabul edilmiştir.
- Event ID ve Inbox, at-least-once teslimata hazırdır.
- Ayrıştırma Rentals domain modelini değiştirmez.

Neden şimdi ayrılmıyor?

- Mevcut publisher process içidir; gerçek broker ve broker operasyonu yoktur.
- Notification hacmi, backlog yaşı veya farklı availability hedefi ölçülmedi.
- Tek basit transaction script için ayrı servis şimdilik daha fazla operasyon
  yüzeyi üretir.

## Karar eşikleri

Extraction tek bir puana bağlanmaz. Aşağıdaki eşiklerden en az bir **hard
driver** gözlenmeli ve sonuç en az 30 günlük temsilî trafik veya üç ardışık
release boyunca doğrulanmalıdır.

| Sürücü | Toplanacak sinyal | Değerlendirme eşiği |
|---|---|---|
| Bağımsız ölçekleme | Context bazlı CPU, memory, throughput, saturation | Bir context host kaynağının sürekli `%40+` bölümünü tüketiyor ve diğerlerinden farklı scale-out gerektiriyor |
| Fleet contention | Optimistic concurrency conflict ve retry exhaustion | Conflict oranı anlamlı trafikte `%1+` veya retry sonrası başarısızlık SLO'yu ihlal ediyor |
| Hata izolasyonu | Context kaynaklı incident ve etkilenen endpoint'ler | Aynı context 30 günde iki kez başka capability'nin SLO'sunu bozuyor |
| Deployment bağımsızlığı | Birleşik release yüzünden bekleyen/cancel edilen değişiklik | Üç ardışık release döneminde context'ler birbirini bloke ediyor |
| Notifications backlog | En eski pending work item yaşı ve queue depth | Hedef teslimat süresi iki ardışık ölçüm penceresinde ihlal ediliyor ve host ölçeklemek sorunu ekonomik çözmüyor |
| Güvenlik/uyumluluk | Onaylı risk veya regülasyon gereksinimi | Ayrı process/network/data isolation zorunlu hâle geliyor |
| Takım özerkliği | Ownership ve on-call sınırı | Kalıcı ayrı takım, bağımsız SLO ve release sorumluluğunu üstleniyor |

Sayılar ilk karar politikasıdır; üretim baselining sonrasında ADR ile
güncellenebilir. Eşiğin aşılması otomatik extraction emri değildir. Önce daha
ucuz seçenekler denenir:

- sorgu veya index optimizasyonu;
- worker concurrency ayarı;
- process içi bulkhead;
- read replica veya cache;
- aggregate partitioning;
- ayrı worker process fakat aynı bounded context;
- deployment pipeline iyileştirmesi.

## Extraction readiness kontrolü

Bir servis çıkarma önerisi ancak aşağıdaki kanıtları içerirse görüşmeye alınır:

- [ ] Problem ölçülmüş ve dashboard/incident/release verisine bağlanmış.
- [ ] Context'in sahibi ve on-call sorumlusu belli.
- [ ] Bağımsız SLO ve capacity planı tanımlı.
- [ ] Senkron ve asenkron sözleşmeler sürümlenmiş.
- [ ] Idempotency, timeout ve belirsiz sonuç semantiği tanımlı.
- [ ] Verinin tek yazma sahibi belli; dual-write yok.
- [ ] Migration, cutover ve rollback adımları prova edilmiş.
- [ ] Dağıtık tracing ve correlation iki process boyunca çalışıyor.
- [ ] Güvenlik, secret rotation ve service identity çözülmüş.
- [ ] Ek operasyon maliyeti beklenen iş faydasından düşük.

## Fitness functions

Servis çıkarmasak da ayrılabilirlik iddiası executable kurallarla korunur.
`ServiceExtractionFitnessTests` şunları doğrular:

- bir business module başka module'ün yalnızca `.Contracts` assembly'sine
  bağımlı olabilir;
- business module'ler API composition root'una bağımlı olamaz;
- Published Contract assembly'leri ASP.NET Core, EF Core veya Npgsql'a bağımlı
  olamaz.

Bu testler “sistem mikroservistir” demez. Yalnızca modüler monolit içindeki
tasarım erozyonunun gelecekteki seçenekleri kapatmasını önler.

## İlgili belgeler

- [Service Extraction Playbook](service-extraction-playbook.md)
- [Context Map](../discovery/05-context-map.md)
- [ADR-0001 — Modüler monolit ile başlama](../decisions/0001-moduler-monolit-ile-baslama.md)
- [ADR-0014 — Ölçülmüş sürücü oluşana kadar modüler monoliti koruma](../decisions/0014-olculmus-surucu-olmadan-servis-cikarmama.md)

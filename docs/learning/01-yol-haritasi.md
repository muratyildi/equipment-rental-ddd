# 01 — Uygulamalı öğrenme ve geliştirme yol haritası

Amaç, “tam donanımlı” görünmek için tüm desenleri aynı yere yığmak değil; farklı
iş problemlerinin neden farklı çözümler gerektirdiğini tek bir tutarlı sistemde
göstermektir.

## Aşama 1 — Problem keşfi

Üreteceklerimiz:

- ürün vizyonu ve başarı ölçütleri;
- domain uzmanı/persona listesi;
- ana kullanıcı yolculukları;
- ilk Event Storming çıktısı;
- ubiquitous language sözlüğü;
- varsayımlar ve açık sorular.

Öğreneceğimiz soru: **Hangi problemi çözüyoruz ve neden?**

Kod yazılmayacak. Çünkü henüz keşfedilmemiş bir problem için güzel kod yazmak
yalnızca pahalı tahmindir.

## Aşama 2 — Stratejik tasarım

Üreteceklerimiz:

- subdomain haritası ve core/generic/supporting sınıflandırması;
- bounded context adayları;
- context map;
- context'ler arası ilişki ve sözleşmeler;
- takım sahipliği varsayımları;
- ADR'ler.

Öğreneceğimiz soru: **Modelin anlamı nerede başlar ve nerede biter?**

## Aşama 3 — Yürüyen iskelet

- .NET 10 solution ve merkezi build ayarları;
- modüler monolit yapısı;
- tek bir ince uçtan uca kullanım senaryosu;
- CI, kod biçimlendirme ve temel test altyapısı;
- yerel geliştirme deneyimi;
- ilk mimari testler.

Öğreneceğimiz soru: **Mimari sınırları derleme ve testlerle nasıl görünür
kılarız?**

## Aşama 4 — Farklı iş mantığı modelleri

Aynı çözüm içinde, gerekçelerine uygun context'lerde:

- transaction script;
- active record veya eşdeğer basit veri-merkezli model;
- zengin domain model;
- gerektiği kanıtlanırsa event-sourced domain model

uygulanacaktır.

Öğreneceğimiz soru: **Karmaşıklık seviyesine uygun en basit model hangisi?**

## Aşama 5 — Taktiksel DDD

- value object ve güçlü tipler;
- entity yaşam döngüsü;
- küçük ve tutarlı aggregate sınırları;
- domain service;
- domain event;
- repository portları;
- application service ve transaction sınırı;
- validation ile invariant ayrımı;
- optimistic concurrency.

Her kavram için “yanlış örnek → sorun → refactoring → doğru gerekçe” akışı
kullanılacaktır.

## Aşama 6 — Mimari ve okuma modelleri

- ports and adapters;
- use case odaklı application katmanı;
- EF Core eşlemelerinin domain'den ayrılması;
- CQRS'nin basit biçiminden başlayarak read model projection;
- API sözleşmeleri ve hata modeli.

CQRS başlangıçta MediatR veya message bus zorunluluğu olarak ele alınmayacaktır.
Önce kavramsal ayrım, sonra gerçekten yarar sağlayan araç seçilecektir.

## Aşama 7 — Context entegrasyonu

- in-process entegrasyondan açık sözleşmelere geçiş;
- domain event → integration event dönüşümü;
- transactional outbox;
- idempotent consumer;
- eventual consistency;
- saga ve process manager karşılaştırması;
- anticorruption layer;
- contract ve entegrasyon testleri.

## Aşama 8 — Operasyonel kalite

- structured logging, metrics ve distributed tracing;
- health/readiness kontrolleri;
- hata senaryoları ve retry politikaları;
- güvenlik ve yetkilendirme sınırları;
- veri göçleri;
- container tabanlı yerel ortam;
- performans ve dayanıklılık testleri.

## Aşama 9 — Evrim ve mikroservis kararı

Modüler monolit ölçülecek. Yalnızca bağımsız ölçekleme, güvenlik/uyumluluk,
ayrı teslimat ritmi veya net takım sahipliği gibi bir ihtiyaç kanıtlanırsa bir
bounded context servis olarak ayrılacak.

Böylece microservice'i “son hedef” değil, belirli bir değişim maliyetini düşüren
olası deployment seçeneği olarak öğreneceğiz.

İlk değerlendirme tamamlandı: production ölçümü olmadığı için hiçbir context
servise çıkarılmadı. Notifications düşük riskli teknik pilot, Fleet
Availability ise farklı scale veya contention profili ölçülürse operasyonel
aday olarak kaydedildi. Karar eşikleri ve geri alınabilir geçiş yolu
[Service Extraction değerlendirmesinde](../architecture/service-extraction-assessment.md)
ve [playbook'ta](../architecture/service-extraction-playbook.md) bulunur.

## Her aşamada kullanacağımız açıklama şablonu

Her önemli değişiklikte şu sorular cevaplanacak:

1. İş problemi nedir?
2. Domain dilindeki karşılığı nedir?
3. Hangi seçenekleri değerlendirdik?
4. Neyi seçtik?
5. Neden seçtik?
6. Neyi özellikle seçmedik ve neden?
7. Bedeli ve riskleri nedir?
8. Kararı hangi koşulda yeniden ele alırız?
9. Test bunu nasıl kanıtlar?

Bu cevaplar README, ilgili context dokümanı, test isimleri ve ADR'lerde
birbirini tamamlayacaktır.

## İlk sonraki adım

Kodlamadan önce örnek domain'i seçip bir ürün vizyonu ve ilk Event Storming
senaryosu oluşturacağız. Domain şu özellikleri taşımalıdır:

- basit ve karmaşık subdomain'leri birlikte barındırması;
- para, zaman, kapasite veya durum geçişleri gibi gerçek invariants içermesi;
- birden fazla bounded context gerektirmesi;
- context'ler arası uzun süren bir iş akışına sahip olması;
- CQRS/outbox/process manager gibi desenleri doğal olarak gerekçelendirmesi;
- yazılım geliştiricinin domain uzmanı olmadan anlayabileceği kadar erişilebilir
  olması.

Blue Book'un birleşik örneği kargo taşımacılığı olduğu için, bilgi transferini
gerçekten sınamak amacıyla aynı alanı kullanmayacağız. Güncel domain önerisi ve
gerekçesi
[tarihsel ve bütünsel okuma rehberinde](03-tarihsel-ve-butunsel-okuma-rehberi.md)
açıklanmıştır.

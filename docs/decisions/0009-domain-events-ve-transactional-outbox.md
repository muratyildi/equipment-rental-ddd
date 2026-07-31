# ADR-0009 — Domain Event'leri Transactional Outbox ile yayımlamak

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

Aggregate'ler iş sırasında Domain Event üretiyor; fakat bu gerçeklerin başka
context'lere güvenilir şekilde bildirilmesi çözülmemişti. Önce database'e
commit edip sonra broker'a publish etmek process çökmesinde mesaj kaybeder.
Önce publish edip sonra commit etmek ise gerçekte gerçekleşmeyen bir değişikliği
dışarı duyurabilir. Bu, iki bağımsız kaynağa atomik yazamama anlamındaki
dual-write problemidir.

## Karar sürücüleri

- Domain modelinde broker, JSON veya EF Core bağımlılığı oluşturmamak
- İş değişikliği ile publish niyetini atomik kaydetmek
- Bounded Context'in kendi integration sözleşmesine sahip olması
- Çoklu API instance'ında aynı mesajı mümkün olduğunca tek worker'a vermek
- Geçici transport hatalarını business transaction'dan ayırmak
- Teslim garantisini dürüstçe adlandırmak

## Karar

- Her Domain Event benzersiz ve sabit bir `EventId` taşır.
- Her Domain Event dışarı açılmaz. Yalnızca dış tüketicisi anlamlı olan gerçekler
  sürümlü Integration Event'e map edilir.
- Integration Event primitive değerlerden oluşur; Domain Value Object veya
  aggregate dışarı sızdırmaz.
- Her modül kendi schema'sında `outbox_messages` tablosuna sahiptir.
- Aggregate değişikliği ve Outbox INSERT aynı EF Core `SaveChanges`
  transaction'ında yapılır.
- Domain Event'ler yalnızca başarılı commit sonrasında aggregate'den temizlenir.
- Worker mesajı `xmin` optimistic concurrency ile claim eder.
- Başarılı publish sonrasında `processed_at_utc` yazılır.
- Hata durumunda claim bırakılır, hata ve attempt sayısı kaydedilir, exponential
  backoff ile sonraki deneme planlanır.
- Teslim semantiği **at-least-once**'dır; exactly-once iddia edilmez.

## Neden bütün Domain Event'ler Integration Event değil?

Domain Event context içindeki modelin gerçeğidir ve daha zengin domain tipleri
taşıyabilir. Integration Event ise context'ler arası uyumluluk sözleşmesidir.
İkisini aynı sınıf yapmak iç modeli public API'ye dönüştürür ve her domain
refactoring'ini dış tüketiciler için breaking change hâline getirir.

İlk yayımlanan sözleşmeler:

- `rentals.rental-order-confirmed.v1`
- `fleet-availability.equipment-availability-committed.v1`

## Neden exactly-once değil?

Publisher broker'a mesajı gönderdikten sonra `processed_at_utc` commit edilmeden
çökerse mesaj yeniden gönderilir. Database ve broker arasında dağıtık
transaction kullanmadan bu pencere kapatılamaz. Sabit `EventId`, consumer'ın
aynı mesajı idempotent işlemesi için anahtardır.

## Olumlu sonuçlar

- Database commit ile publish niyeti arasında mesaj kaybı olmaz.
- Transport geçici hataları domain transaction'ını geri almaz.
- Mesaj sözleşmeleri açıkça sürümlüdür.
- Bounded Context veri ve event sahipliği korunur.
- Gerçek PostgreSQL testleri rollback, publish ve retry davranışını kanıtlar.

## Olumsuz sonuçlar ve riskler

- Outbox tablosu retention/temizlik politikası gerektirir.
- Claim süresinden uzun publish işlemleri duplicate olasılığını artırır.
- Consumer idempotency uygulanmadan duplicate mesajlar yan etkiyi tekrarlayabilir.
- Mevcut API composition root'u local geliştirme için logging transport adapter'ı
  kullanır; gerçek broker adapter'ı henüz eklenmemiştir.

## Yeniden değerlendirme koşulları

- Broker seçildiğinde publisher adapter'ı değiştirilecek.
- Mesaj hacmi polling maliyetini artırırsa notification/CDC yaklaşımı ölçülecek.
- Claim contention artarsa PostgreSQL `SKIP LOCKED` tabanlı toplu claim
  değerlendirilecek.
- Outbox büyümesi için arşivleme ve retention politikası eklenecek.

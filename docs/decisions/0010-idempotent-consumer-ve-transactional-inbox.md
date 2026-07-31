# ADR-0010 — Idempotent consumer ve Transactional Inbox

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

Transactional Outbox mesaj kaybını önler fakat at-least-once teslimat duplicate
üretebilir. Publisher mesajı ilettikten sonra `processed_at_utc` yazamadan
çökerse aynı `EventId` tekrar teslim edilir. Notification work item'ı her
teslimatta oluşturmak müşteriye aynı bildirimin birden fazla gönderilmesine
yol açar.

## Karar sürücüleri

- Duplicate teslimatı normal çalışma koşulu olarak kabul etmek
- Deduplication kaydı ile iş etkisini atomik yapmak
- Consumer'ın yayınlayan Domain modeline bağımlı olmaması
- Eşzamanlı duplicate teslimat yarışını database constraint ile çözmek
- Basit supporting subdomain'e gereksiz rich domain model yüklememek

## Karar

- Notifications supporting bounded context'i eklenecek.
- Notifications Infrastructure, Rentals'ın
  `rentals.rental-order-confirmed.v1` Published Contract'ını tüketecek.
- Adapter sözleşmeyi Notifications Application komutuna çevirecek.
- Application basit transaction script kullanacak ve notification work item
  oluşturacak.
- Inbox primary key'i `(consumer, message_id)` olacak.
- Inbox INSERT, notification work item INSERT ve `processed_at_utc` aynı
  Notifications transaction'ında commit edilecek.
- Aynı mesaj daha önce işlendiğinde consumer başarılı no-op dönecek.
- İki ilk teslimat eşzamanlı yarışırsa unique constraint kazananı belirleyecek;
  kaybeden transaction rollback edip Inbox kaydını doğrulayarak no-op olacak.
- Business handler başarısızsa Inbox da rollback olacak; transport mesajı tekrar
  deneyebilecek.

## Neden sadece `message_id` primary key değil?

Aynı Integration Event bir bounded context içinde birden fazla bağımsız
consumer tarafından işlenebilir. `(consumer, message_id)` anahtarı her consumer
için ayrı idempotency alanı oluşturur.

## Neden Notifications rich domain model değil?

Şimdiki iş mantığı “onaylanan kiralama için pending notification work item
oluştur” işlemidir. Karmaşık invariant, entity lifecycle veya model içi karar
yoktur. Transaction script daha açık ve düşük maliyetlidir. İş kuralları
karmaşıklaşırsa model yeniden değerlendirilecektir.

## Olumlu sonuçlar

- Sequential ve concurrent duplicate mesajlar tek iş etkisi üretir.
- Başarısız iş işlemi “işlenmiş” olarak kaydedilmez.
- Notifications Application, Rentals assembly'sine bağımlı değildir.
- Idempotency transport veya process içi belleğe değil kalıcı veriye dayanır.

## Olumsuz sonuçlar ve riskler

- Inbox tablosu retention/temizlik politikası gerektirir.
- Çok yüksek duplicate yarışında unique violation normal control flow olur.
- Notification work item henüz gerçek e-posta/SMS gönderimi değildir.
- Local transport in-process'tir; gerçek broker receiver adapter'ı sonraki
  operasyonel karardır.

## Kanıtlayan testler

- Aynı mesajın iki ardışık teslimatı tek work item üretir.
- İki eşzamanlı teslimat tek work item üretir.
- Business validation hatası Inbox ve work item'ı birlikte rollback eder.
- Rentals Outbox → publisher → Notifications Inbox uçtan uca akışı aynı mesajı
  yeniden teslim ettiğinde duplicate etki üretmez.

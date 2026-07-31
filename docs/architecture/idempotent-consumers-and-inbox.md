# Idempotent Consumer ve Transactional Inbox

## Problem

Outbox teslimatı at-least-once'tır. Aynı Integration Event'in birden fazla kez
gelmesi istisna değil, beklenen dağıtık sistem davranışıdır.

```text
publish başarılı
  → publisher process çöker
  → Outbox processed işareti yazılamaz
  → aynı EventId yeniden publish edilir
```

Consumer duplicate mesajı tanımazsa notification, ödeme veya stok düşümü gibi
yan etkiler tekrarlanabilir.

## Notifications örneği

`rentals.rental-order-confirmed.v1`, Notifications Infrastructure tarafından
tüketilir:

```text
IntegrationEventEnvelope
  → Rentals published contract deserialize edilir
  → Notifications consumer-owned adapter
  → RequestRentalConfirmationNotification
  → transaction script
  → notification_work_items INSERT
```

Notifications Application, Rentals'a referans vermez. Dış sözleşmeyi kendi
komutuna çeviren adapter Infrastructure'dadır.

## Transaction sınırı

Consumer şu üç değişikliği aynı PostgreSQL transaction'ında yapar:

1. `(consumer, message_id)` Inbox kaydı;
2. notification work item;
3. Inbox `processed_at_utc` işareti.

```text
BEGIN
  INSERT inbox_messages
  INSERT notification_work_items
  UPDATE inbox processed
COMMIT
```

Business handler hata verirse transaction rollback olur. Inbox kaydı
olmadığından mesaj yeniden denendiğinde tekrar işlenebilir.

## Sequential duplicate

Consumer önce kendi adı ve mesaj kimliğiyle Inbox'ı sorgular. Kayıt varsa iş
etkisini tekrar çalıştırmadan başarıyla döner.

```text
ilk teslimat   → Inbox yok → work item oluştur → commit
ikinci teslimat → Inbox var → no-op
```

## Concurrent duplicate

“Önce sorgula” tek başına yeterli değildir. İki transaction aynı anda Inbox
yok sonucunu görebilir. Asıl doğruluk garantisi composite primary key'dir:

```text
PRIMARY KEY (consumer, message_id)
```

Bir transaction kazanır. Diğeri unique violation alır, kendi transaction'ını
rollback eder ve kazanan Inbox kaydını doğruladıktan sonra başarılı no-op olur.

## Neden consumer adı anahtarın parçası?

Aynı mesajı iki farklı consumer farklı amaçlarla işleyebilir. Yalnızca
`message_id` kullanmak ilk consumer'ın diğerini yanlışlıkla engellemesine neden
olur. İdempotency scope'u mesaj değil, **consumer + mesaj** çiftidir.

## Exactly-once etkisi

Transport exactly-once değildir. Fakat consumer'ın database içindeki etkisi,
aynı `EventId` için bir kez uygulanır. Buna bazen “effectively once” denir.
Garanti yalnızca Inbox ile aynı transaction'a katılan yerel database etkileri
için geçerlidir.

Harici e-posta sağlayıcısını transaction içinde çağırmak bu garantiyi bozar.
Gerçek gönderim için notification work item ayrı bir güvenilir teslimat
mekanizmasıyla işlenmelidir.

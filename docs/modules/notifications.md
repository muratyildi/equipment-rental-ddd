# Notifications Bounded Context

## Amaç

Notifications, başka context'lerde gerçekleşen iş olaylarını müşteri iletişim
işlerine dönüştüren supporting subdomain'dir. Kiralama kurallarını, kapasiteyi
veya müşteri uygunluğunu sahiplenmez.

İlk kullanım senaryosu:

```text
RentalOrderConfirmedV1
  → rental-order-confirmed template'i için pending work item
```

## Neden rich domain model yok?

Mevcut davranış basit bir transaction script'tir:

1. contract alanlarını doğrula;
2. notification work item oluştur;
3. Inbox ile birlikte commit et.

Aggregate veya Value Object eklemek şu aşamada gerçek bir invariant çözmez.
Template seçimi, kanal tercihleri, quiet hours, localization veya retry
politikaları karmaşıklaştığında daha zengin model değerlendirilecektir.

## Context sınırı

- Application, Rentals'ı referans etmez.
- Infrastructure yalnızca Rentals Contracts assembly'sini tüketir.
- Rental domain tipleri Notifications'a girmez.
- Customer ve Rental Order kimlikleri Notifications açısından harici
  referanslardır.

## Veri sahipliği

Notifications kendi PostgreSQL schema'sına sahiptir:

- `inbox_messages`
- `notification_work_items`
- `__ef_migrations_history`

Başka bir context bu tablolara yazamaz.

## İdempotency

Inbox anahtarı `(consumer, message_id)` çiftidir. Sequential ve concurrent
duplicate teslimatlar tek notification work item üretir. Business işlemi
başarısızsa Inbox da rollback olur.

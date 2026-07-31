# ADR-0005 — Walking skeleton için geçici in-memory repository

- Durum: Superseded by [ADR-0008](0008-postgresql-ef-core-ve-optimistic-concurrency.md)
- Tarih: 2026-07-31

## Bağlam

İlk hedef domain modelini ve HTTP'den aggregate'e uzanan dikey akışı
doğrulamaktır. Kalıcı veritabanı şeması henüz context sınırlarından önce
tasarlanmamalıdır.

## Karar

`IRentalOrderRepository` ve `IAvailabilityScheduleRepository` portları için
process içi, geçici in-memory adapter'lar kullanılacaktır.

## Bu kararın söylemediği şey

Bu adapter:

- üretim kalıcılığı sağlamaz;
- transaction veya concurrency kanıtlamaz;
- uygulama yeniden başladığında veriyi korumaz;
- PostgreSQL tasarımının yerine geçmez.
- eşzamanlı yazmalarda optimistic concurrency sağlamaz.

## Olumlu sonuçlar

- Domain ve application katmanı veritabanı ayrıntısından bağımsız doğrulanır.
- İlk uçtan uca akış düşük maliyetle çalışır.
- Repository portunun gerçek kullanım biçimi görünür olur.

## Riskler

- Uzun süre kalırsa sistem olduğundan daha güvenilir görünebilir.
- Referans sakladığı için gerçek persistence rehydration davranışını test etmez.

## Değiştirme koşulu

Bir sonraki altyapı milestone'unda PostgreSQL ve EF Core adapter'ı, optimistic
concurrency ve integration testleriyle birlikte eklenecektir. In-memory adapter
yalnızca hızlı demo/test seçeneği olarak kalabilir.

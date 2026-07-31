# ADR-0007 — Çok satırlı availability sürecini şimdilik satır bazında yürütmek

- Durum: Superseded by ADR-0012
- Tarih: 2026-07-31
- Niteliği: Geçici ve bilinçli sınırlama

## Bağlam

Rental Order birden fazla ekipman kategorisi içerebilir. Her kategori farklı
AvailabilitySchedule aggregate'ine aittir. Bütün satırları tek transaction'da
commit etmek aggregate sınırlarını aşar.

## Değerlendirilen seçenekler

1. Birden fazla schedule'ı tek veritabanı transaction'ına almak
2. Dağıtık transaction varsaymak
3. Atomic batch contract oluşturmak
4. Satırları idempotent biçimde ayrı commit etmek ve süreci sonra Process
   Manager ile tamamlamak

## Karar

Dördüncü seçenek seçildi.

İlk milestone'da:

- her satır ayrı talep edilir;
- kabul edilen commitment korunur;
- ret alan satır Rental'ı onaysız bırakır;
- retry güvenlidir;
- otomatik release/compensation yoktur.

Batch contract, farklı schedule aggregate'leri arasındaki atomicity problemini
ortadan kaldırmaz; yalnızca API arkasına gizler.

## Risk

Kısmi commitment kapasiteyi gereğinden uzun süre tutabilir.

## Sonraki karar

Process Manager milestone'u; tamamlanan satırları, timeout'u, başarısızlıkta
release komutlarını, retry/idempotency durumunu ve Rental confirmation
tetiklemesini açıkça modelleyecektir.

Bu geçici sınırlama
[ADR-0012](0012-rental-confirmation-process-manager.md) ile kaldırılmıştır.

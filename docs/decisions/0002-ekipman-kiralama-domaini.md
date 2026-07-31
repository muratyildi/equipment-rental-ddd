# ADR-0002 — Ekipman kiralama domain'ini seçmek

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

Öğretici sistem; basit ve karmaşık subdomain'leri, zaman ve para invariants'ını,
birden fazla bounded context'i ve uzun süren süreçleri doğal biçimde
barındırmalıdır.

Blue Book'un geniş örneği kargo taşımacılığıdır. Aynı alanı kullanmak, DDD
bilgisini yeni probleme taşıma yeteneğini göstermeyebilir.

## Değerlendirilen seçenekler

1. Kargo/son kilometre teslimat
2. Etkinlik ve biletleme
3. Endüstriyel ekipman kiralama ve saha operasyonları

## Karar

Endüstriyel ekipman kiralama seçildi.

Alan; teklif, uygunluk, rezervasyon, bakım, teslimat, iade, hasar ve faturalama
modellerini içerir. Zaman aralığı, kapasite, para ve fiziksel durum kavramları
güçlü iş kuralları üretir.

## Sonuçlar

- Blue Book örneği kopyalanmadan bilgi transferi gösterilebilir.
- Fleet Availability ile Rentals arasında gerçek model sınırı vardır.
- CQRS takvimi, Outbox ve Process Manager ileride doğal gerekçelerle
  uygulanabilir.
- Domain uzmanı erişimi olmadığı için belirsiz kurallar hotspot olarak açıkça
  kaydedilmelidir.

## Yeniden değerlendirme koşulu

Domain'in kavramları öğretici hedefleri karşılamaz veya gerçekçi senaryolar
üretmezse ürün vizyonu değiştirilmeden önce yeni bir discovery çalışması
yapılacaktır.

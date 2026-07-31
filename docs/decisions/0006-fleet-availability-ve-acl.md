# ADR-0006 — Fleet Availability context'i ve consumer-owned ACL

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

Rentals bir siparişi onaylamak için fiziksel kapasite taahhüdüne ihtiyaç duyar.
Ancak “Rental Line” ticari bir kavram, “Availability Commitment” ise kapasite
planlama kavramıdır. İki context aynı modeli paylaşmamalıdır.

## Değerlendirilen seçenekler

1. Rentals'ın Fleet Domain projesine doğrudan referans vermesi
2. Ortak bir Equipment/Reservation modeli paylaşmak
3. Rentals'ın sahip olduğu port ve Fleet'in published contract'ı arasında ACL

## Karar

Üçüncü seçenek seçildi.

- Rentals Application `IEquipmentAvailabilityGateway` portuna bağımlıdır.
- Rentals Infrastructure içindeki `FleetAvailabilityGateway` çeviri yapar.
- Adapter yalnızca Fleet Availability Contracts assembly'sine referans verir.
- Fleet'in Domain ve Application modelleri tüketiciye açılmaz.
- Aynı GUID değerleri context'e özgü farklı kimlik sınıflarına çevrilir.

`AvailabilitySchedule`, equipment category + location çifti başına aggregate
root'tur. Aynı schedule içindeki kapasite ve commitment'lar tek tutarlılık
sınırında korunur.

Kapasite yetersizliği exception değil, beklenen ret sonucudur. Aynı external
demand'in aynı şartlarla tekrarı idempotenttir.

## Olumlu sonuçlar

- Model sınırları assembly bağımlılıklarıyla korunur.
- Rentals, Fleet'in iç dilinden etkilenmez.
- In-process çağrı daha sonra farklı transport ile değiştirilebilir.
- Retry yeni commitment üretmez.

## Olumsuz sonuçlar

- Çeviri kodu ve iki ayrı kimlik/value object kümesi vardır.
- Senkron contract şu an process içi çalışma varsayar.
- Tek schedule zamanla büyük ve contention üreten aggregate olabilir.

## Yeniden değerlendirme koşulları

- Ayrı deployment ihtiyacı
- Ölçülen yüksek write contention
- Schedule başına yönetilemez commitment sayısı
- Fleet cevap süresinin Rentals kullanılabilirliğini etkilemesi

# ADR-0014 — Ölçülmüş sürücü olmadan servis çıkarmama

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

Rentals, Fleet Availability ve Notifications bounded context sınırları
uygulamada modüller, ayrı persistence schema'ları ve published contract'larla
korunmaktadır. Bu yapı bazı context'leri bağımsız servise dönüştürmeyi teknik
olarak mümkün kılar.

Ancak repository henüz production trafik, incident, takım teslimat ritmi veya
uyumluluk gereksinimi verisine sahip değildir. Yalnızca bounded context sayısına
bakarak servis üretmek, domain sınırı ile deployment sınırını eşitlemek ve
dağıtık sistem maliyetini kanıtsız üstlenmek olur.

## Karar sürücüleri

- Modüler monolitin düşük geliştirme ve operasyon maliyetini korumak
- Bounded context izolasyonunu deployment topolojisinden bağımsız ele almak
- Gelecekteki servis çıkarma seçeneğini açık tutmak
- Mimari kararlarda ölçülmüş kanıt ile hipotezi ayırmak
- Network, broker ve dağıtık veri maliyetini yalnızca iş gerekçesiyle üstlenmek

## Değerlendirilen seçenekler

1. Her implemented bounded context'i hemen ayrı servise çıkarmak
2. Notifications'ı öğretici amaçla kanıt olmadan servis yapmak
3. Modüler monoliti süresiz ve yeniden değerlendirme ölçütü olmadan korumak
4. Modüler monoliti korumak, extraction fitness function'ları ve ölçülebilir
   karar eşikleri tanımlamak

## Karar

Dördüncü seçenek kabul edilmiştir.

Bugün hiçbir bounded context bağımsız servise çıkarılmayacaktır. Başka bir
module'e çapraz bağımlılık yalnızca o module'ün Published Contract assembly'si
üzerinden kurulabilir. Business module'ler API composition root'una bağımlı
olamaz. Contract assembly'leri transport ve persistence teknolojilerinden
bağımsız tutulacaktır.

Extraction ancak bağımsız ölçekleme, hata izolasyonu, deployment bağımsızlığı,
takım sahipliği veya güvenlik/uyumluluk sürücülerinden en az biri ölçüldüğünde
yeniden değerlendirilecektir.

## Adaylık değerlendirmesi

- Notifications, asenkron contract ve Inbox sınırı nedeniyle teknik olarak en
  düşük riskli ilk pilot adayıdır.
- Fleet Availability, farklı concurrency/scale profili kanıtlanırsa en güçlü
  operasyonel adaydır.
- Rentals core workflow ve Process Manager sahipliği nedeniyle bugün
  monolitin merkezinde kalır.
- Availability Calendar ayrı bir read model'dir; tek başına yeni bounded
  context veya microservice sayılmaz.

Bu sıralama extraction kararı değil, gelecekteki inceleme önceliğidir.

## Olumlu sonuçlar

- Repository microservice sayısını kalite göstergesi olarak kullanmaz.
- Yerel geliştirme, transaction ve debugging deneyimi basit kalır.
- Modül sınırları executable testlerle korunur.
- Gelecekteki karar için önceden tanımlı sinyal ve playbook bulunur.
- Servis çıkarma sırasında ACL, Outbox, Inbox ve idempotency yatırımları
  kullanılabilir.

## Olumsuz sonuçlar ve riskler

- Tek deployment bağımsız release ve process-level fault isolation sağlamaz.
- In-process çağrılar gerçek network latency ve partial failure davranışını
  göstermez.
- Metrics export ve production baseline olmadan eşikler değerlendirilemez.
- Sınırlar testlerle korunmazsa monolit içinde zamanla erozyon oluşabilir.

## Yeniden değerlendirme koşulları

Detaylı eşikler
[Service Extraction Değerlendirmesi](../architecture/service-extraction-assessment.md)
belgesinde tutulur. En az bir hard driver temsilî bir ölçüm penceresinde
doğrulanmalı; daha ucuz process-içi çözümün neden yeterli olmadığı
gösterilmelidir.

Bir extraction kararı verilirse bu ADR değiştirilmez. İlgili context için
migration, cutover, rollback ve sahipliği tanımlayan yeni bir ADR açılır.

## İlgili testler ve belgeler

- `ServiceExtractionFitnessTests`
- [Service Extraction Değerlendirmesi](../architecture/service-extraction-assessment.md)
- [Service Extraction Playbook](../architecture/service-extraction-playbook.md)
- [ADR-0001 — Modüler monolit ile başlama](0001-moduler-monolit-ile-baslama.md)

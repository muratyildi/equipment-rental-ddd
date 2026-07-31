# ADR-0001 — Modüler monolit ile başlama

- Durum: Accepted
- Tarih: 2026-07-29
- Kabul tarihi: 2026-07-31

## Bağlam

Örnek sistem birden fazla bounded context ve entegrasyon deseni öğretecek.
Ancak bounded context sınırları henüz keşfedilmedi. Bu aşamada her context'i
ayrı process ve veritabanıyla çalıştırmak; ağ, dağıtık transaction,
gözlemlenebilirlik ve deployment karmaşıklığını domain öğreniminin önüne
geçirebilir.

## Karar sürücüleri

- Domain modelini ve sınırlarını hızlı geri bildirimle geliştirmek
- Context izolasyonunu korumak
- Yerel geliştirme ve test maliyetini düşük tutmak
- Gelecekte servis ayırmayı imkânsızlaştırmamak
- Dağıtık sistem karmaşıklığını ancak iş gerekçesi oluştuğunda üstlenmek

## Değerlendirilen seçenekler

1. Katmansız tek monolit
2. Modüler monolit
3. Başlangıçtan itibaren microservices

## Karar

Çözüm, bounded context sınırlarını modüller olarak koruyan bir modüler monolit
şeklinde başlayacaktır. Bir modül başka modülün domain veya persistence
ayrıntılarına doğrudan erişemeyecektir. Context'ler arası temas açık
sözleşmelerle kurulacaktır.

Bu karar deployment topolojisi hakkındadır. Her modülün aynı taktiksel desenleri
veya aynı iç mimariyi kullanmasını gerektirmez.

## Olumlu sonuçlar

- Tek process ile kolay çalıştırma ve debug
- Dağıtık sistem hataları olmadan sınırları öğrenebilme
- Context'ler arası çağrıları ve sözleşmeleri görünür kılabilme
- Gerektiğinde seçili bir modülü servis olarak ayırmak için geçiş yolu

## Olumsuz sonuçlar ve riskler

- Derleme zamanı erişimi teknik olarak mümkün olduğundan sınırlar disiplin ve
  mimari testlerle korunmalıdır.
- Tek deployment, context'lerin bağımsız yayınlanmasını başlangıçta engeller.
- Process içi iletişim, gelecekteki ağ maliyetini gizleyebilir.

## Yeniden değerlendirme koşulları

- Bir context'in farklı ölçekleme ihtiyacının ölçülmesi
- Bağımsız teslimat ritmi gerektiren ayrı takım sahipliği
- Güvenlik veya uyumluluk nedeniyle process/veri izolasyonu
- Bir modülün hata veya kaynak kullanımının diğerlerini kabul edilemez biçimde
  etkilemesi

## İlgili testler

Kod iskeleti oluşturulduğunda:

- modüller arası yasak bağımlılıkları denetleyen mimari testler;
- public contract dışından erişimi engelleyen görünürlük testleri;
- her modül için bağımsız entegrasyon test fixture'ları

eklenecektir.

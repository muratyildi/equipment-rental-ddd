# 00 — Kaynak incelemesi ve DDD'ye ilk bakış

## İncelenen kaynak

- Vlad Khononov, *Learning Domain-Driven Design*
- PDF: 446 sayfa
- Yapı: 16 bölüm, dört ana kısım, bir gerçek hayat vaka çalışması ve alıştırma
  cevapları

PDF bir “Early Release” çıktısıdır. Bölüm başlarında ham/düzenlenmemiş içerik
uyarısı bulunur. Ayrıca ikinci bölüm PDF'in fiziksel 2–17. sayfalarına yerleşmiş,
önsöz ve birinci bölüm ise daha sonra gelmiştir. Bu nedenle çalışırken PDF'in
yer imlerine veya fiziksel sayfa sırasına körü körüne güvenmeyeceğiz; bölüm
başlıklarını esas alacağız.

Bu kaynak, telifli metni satır satır çevirmek için değil; kavramları anlayıp kendi
örnek alanımıza uygulamak için kullanılacaktır.

## Kitabın tezi

Yazılım projeleri çoğu zaman programlama dili veya framework yetersizliğinden
değil, yanlış problemi çözmekten ya da doğru problemi yanlış anlamaktan
başarısız olur. DDD'nin merkezindeki konu bu nedenle kod değil, **etkili
iletişimdir**.

DDD iki büyük soru grubuna ayrılır:

1. **Stratejik tasarım — Ne ve neden?**
   Hangi iş problemini çözüyoruz, işletme nerede rekabet avantajı kazanıyor,
   modelin anlam sınırları nerede ve takımlar nasıl ilişki kuruyor?
2. **Taktiksel tasarım — Nasıl?**
   Belirli bir sınırın içindeki iş kurallarını hangi kod modeliyle, mimariyle ve
   entegrasyon deseniyle uyguluyoruz?

Sıra önemlidir. Stratejik soruları atlayıp Aggregate, Repository ve CQRS ile
başlamak, DDD görünümlü fakat iş alanından kopuk bir sistem üretir.

## Bir yazılımcı için en sade DDD tanımı

DDD, yazılım modelini işletmenin gerçek çalışma biçimine ve diline yaklaştırma
disiplinidir.

“Müşterinin siparişini güncelliyoruz” demek yetersizdir. İş insanlarının
gerçekte söylediği şey şu olabilir:

> Ödemesi yetkilendirilmiş ve depoda ayrılmış en az bir kalemi bulunan sipariş
> sevkiyata hazırlanabilir; iptal edilmiş sipariş hazırlanamaz.

Bu cümlede yazılım modeline dönüşecek dil ve kurallar vardır:

- `Sipariş`
- `ÖdemeYetkilendirildi`
- `StokAyrıldı`
- `SevkiyataHazırla`
- `SiparişİptalEdildi`
- işlemi engelleyen değişmezler (invariants)

DDD'nin amacı tablo adlarını güzelleştirmek değil, bu iş bilgisini kaybetmeden
koda taşımaktır.

## Birinci temel ayrım: domain, subdomain ve bounded context

Bu üç kavram sık karıştırılır.

### Business domain

Şirketin faaliyet gösterdiği ve müşteriye değer sunduğu geniş alandır. Örneğin
bir lojistik şirketi için “gönderi taşımacılığı”.

### Subdomain

İş alanının içindeki bir iş yeteneğidir. **Keşfedilir**; organizasyonun gerçekte
yaptığı işte zaten vardır.

- **Core subdomain:** Şirketin rakiplerinden farklı yaptığı, rekabet avantajı
  üreten problem. Karmaşıktır ve sık değişebilir. En güçlü tasarım emeği buraya
  verilir.
- **Generic subdomain:** Sektörde çözümü bilinen, rekabet avantajı sağlamayan
  problem. Kimlik doğrulama veya ödeme altyapısı gibi hazır çözüm
  almak/uyarlamak çoğu zaman daha doğrudur.
- **Supporting subdomain:** İşletmenin ihtiyaç duyduğu fakat avantaj sağlamayan,
  çoğunlukla daha basit ve şirkete özel işlev. Basit CRUD veya transaction script
  yeterli olabilir.

Bu sınıflandırma teknik karmaşıklığı değil, önce stratejik değeri anlatır.

### Bounded context

Bir modelin ve ubiquitous language'ın geçerli olduğu açık sınırdır.
**Tasarlanır**; işin içinde hazır duran bir yazılım kutusu değildir.

Örneğin “müşteri” sözcüğü:

- Satış bağlamında: teklif alan, fiyat segmenti olan taraf,
- Faturalama bağlamında: vergi bilgileri ve borç bakiyesi olan hesap,
- Destek bağlamında: sözleşme seviyesi ve açık talepleri olan kullanıcı

anlamına gelebilir. Tek bir dev `Customer` sınıfı bu modelleri birleştirirse
anlamlar ve değişim nedenleri birbirine dolaşır. Her bounded context kendi
amacına uygun müşteri modelini kurar.

Özet:

| Kavram | Niteliği | Bulunma biçimi |
|---|---|---|
| Subdomain | İş problemi/yeteneği sınırı | Keşfedilir |
| Bounded context | Çözüm modeli ve dil sınırı | Tasarlanır |

Bire bir eşleşmeleri mümkündür ama zorunlu değildir.

## Ubiquitous language neden merkezde?

Domain uzmanının zihnindeki bilgi, analiz dokümanından çözüm modeline ve oradan
koda geçerken “kulaktan kulağa” oyunu gibi bozulabilir. Ubiquitous language
bu çeviri zincirini azaltır: iş uzmanı, geliştirici, test, doküman ve kod aynı
terimleri aynı anlamda kullanır.

İyi bir ubiquitous language:

- bounded context içinde tutarlıdır;
- eş anlamlı ve belirsiz ifadeleri azaltır;
- yalnızca isimleri değil, davranışları ve iş kurallarını da içerir;
- konuşmalarda, örneklerde, testlerde ve kodda görünür;
- yeni bilgi geldikçe sürekli güncellenir.

Bu nedenle proje sözlüğümüz statik bir terim listesi olmayacak. Her terim için
anlam, geçerli olduğu context, örnek senaryo ve mümkünse karşı örnek
tutacağız.

## Stratejik tasarımın tamamı

Kitabın ilk dört bölümü şu akışı kurar:

1. İş alanını ve subdomain'leri analiz et.
2. Domain uzmanlarıyla bilgiyi keşfet ve ortak dili geliştir.
3. Çelişen modelleri bounded context sınırlarıyla yönet.
4. Context'lerin takım ve güç ilişkilerine uygun entegrasyon biçimini seç.

Başlıca context ilişkileri:

- **Partnership:** İki context ve takım başarı için birlikte hareket eder.
- **Shared Kernel:** Bilinçli olarak küçük bir ortak model paylaşılır. Sıkı
  koordinasyon maliyeti vardır.
- **Conformist:** Tüketici, sağlayıcının modelini değiştirmeden kabul eder.
- **Anticorruption Layer (ACL):** Tüketici, dış modeli kendi diline çevirerek
  kendi modelini korur.
- **Open Host Service / Published Language:** Sağlayıcı, tüketiciler için kararlı
  ve açık bir sözleşme yayımlar.
- **Separate Ways:** Entegrasyon maliyeti değerinden yüksekse işlev tekrarlanır
  ve taraflar ayrılır.

Bu ilişkiler yalnızca API tekniği değildir; takımlar arasındaki işbirliği ve güç
dengesini de anlatır. Hepsi birlikte bir **context map** üzerinde gösterilir.

## Taktiksel tasarımın tamamı

Kitap, iş mantığının karmaşıklığına göre farklı uygulama modelleri önerir:

### Transaction Script

Her kullanım senaryosu bir prosedür olarak ele alınır. Basit kurallar ve basit
veri yapıları için uygundur. “DDD projesindeyiz” diye bunu yasaklamak gereksiz
karmaşıklık üretir.

### Active Record

Veri modeli hem veriyi hem de kalıcılık davranışını taşır. İş kuralları basit ama
veri yapıları daha karmaşıksa kullanılabilir.

### Domain Model

Karmaşık davranış ve kurallar nesnelerde açıkça modellenir. Başlıca yapı taşları:

- **Value Object:** Kimliğiyle değil değerleriyle tanımlanır; genellikle
  değişmezdir. Örnek: `Money`, `Address`, `DateRange`.
- **Entity:** Zaman içinde değişse de kimliği devam eder.
- **Aggregate:** Tutarlılık sınırıdır. Dış dünya yalnızca aggregate root üzerinden
  değişiklik yapar; tek transaction içinde korunması gereken kuralları kapsar.
- **Domain Service:** Tek bir entity/value object'e doğal biçimde ait olmayan
  saf domain davranışıdır.
- **Domain Event:** Domain'de gerçekleşmiş ve iş açısından anlamlı geçmiş zamanlı
  olgudur.
- **Repository:** Aggregate'in kalıcılık ayrıntılarını domain dilinden saklayan
  koleksiyon benzeri porttur.

En kritik nokta: Aggregate bir nesne grafiği veya veritabanı ilişkileri kümesi
değil, **transactional consistency boundary**'dir.

### Event-Sourced Domain Model

Güncel durumu tek gerçek olarak saklamak yerine, duruma yol açan olaylar gerçek
kaynak olur. Geçmişi yeniden kurma, denetim ve zamansal analiz güçlüdür; fakat
şema evrimi, operasyon, öğrenme eğrisi ve hata düzeltme maliyeti yüksektir.
Parasal işlemler, güçlü audit gereksinimi veya geçmiş durum analizi yoksa sırf
modern göründüğü için seçilmez.

## Mimari desenler ve doğru kapsamları

- **Layered Architecture:** Basit iş mantığı için yeterli olabilir.
- **Ports and Adapters:** Karmaşık domain modelini altyapı ayrıntılarından korur.
  Bağımlılıklar içeri, domain'e doğru yönelir.
- **CQRS:** Aynı veriye birden fazla okuma modeli gerektiğinde komut ve sorgu
  modellerini ayırır. CQRS, zorunlu olarak iki veritabanı, mesaj kuyruğu veya
  event sourcing demek değildir.

Mimari seçim sistemin tamamına tek damga olarak vurulmayacaktır. Bir bounded
context basit layered architecture kullanırken core context ports and adapters,
gereken belirli bir akış ise CQRS kullanabilir.

## Context'ler arası güvenilirlik

- **Outbox:** Veritabanı değişikliği ile mesaj yayımlama arasındaki dual-write
  sorununu önler.
- **Saga:** Uzun süren, birden fazla component'i ilgilendiren işlemlerde hata
  sonrası telafi adımlarını tanımlar.
- **Process Manager:** Sürecin durumunu taşıyan ve sonraki komutları belirleyen
  açık bir koordinatördür.

Bu desenlerle birlikte at-least-once delivery, idempotency, mesaj sıralaması ve
gözlemlenebilirlik ele alınmalıdır. “Event kullandık, gevşek bağlı olduk”
varsayımı yanlıştır; yanlış event tasarımı dağıtık bir big ball of mud üretir.

## Kitabın önemli uyarıları

1. DDD yalnızca karmaşık ve değişken iş problemlerinde maliyetini karşılar.
2. Bounded context, microservice ile eş anlamlı değildir.
3. Her context'te domain model kullanmak overengineering'dir.
4. Her domain event dışarı yayımlanmaz; domain event ile integration event
   ayrılmalıdır.
5. Event sourcing ve CQRS birbirini zorunlu kılmaz.
6. Küçük context her zaman iyi değildir; entegrasyon maliyetini artırabilir.
7. Tasarım bir defalık değildir. Domain bilgisi ve şirket stratejisi değiştikçe
   subdomain sınıfları, sınırlar ve uygulama desenleri de evrilir.

Vaka çalışmasının vardığı olgunluk özeti özellikle değerlidir: “her yerde
aggregate” yaklaşımından, “her yerde ubiquitous language” yaklaşımına geçiş.

## Bu projeye etkisi

Bu kaynak nedeniyle aşağıdaki başlangıç kararlarını alıyoruz:

1. Koddan önce örnek iş alanını ve başarı ölçütünü yazacağız.
2. Domain uzmanı rolünü senaryolar ve açık varsayımlarla simüle edeceğiz; belirsiz
   iş kurallarını teknik kararla kapatmayacağız.
3. Big-bang microservices yerine modüler monolitle başlayacağız.
4. Bounded context'ler arasında kod/model paylaşımını varsayılan yapmayacağız.
5. Her subdomain için aynı taktiksel deseni uygulamayacağız.
6. Event sourcing, CQRS, outbox ve saga gibi desenleri ayrı ayrı, gerçek bir
   problemle gerekçelendireceğiz.
7. Her önemli karar ADR ile; her domain terimi sözlük ve executable example ile
   kaydedilecek.

Henüz geri döndürülemez teknoloji veya iş alanı kararı alınmamıştır.

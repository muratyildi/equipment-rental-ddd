# ADR-0004 — İlk aşamada mediator yerine açık use-case handler'ları

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

Command ve Query ayrımı mimari bir kavramdır. MediatR benzeri bir paket ise bu
çağrıları yönlendiren teknik mekanizmadır.

İlk dikey dilimde henüz pipeline behavior, dinamik handler keşfi veya modüller
arası dispatch gereksinimi yoktur.

## Karar

Her kullanım senaryosu açık bir handler sınıfıyla temsil edilir ve API
composition root tarafından doğrudan enjekte edilir.

## Olumlu sonuçlar

- Çağrı akışı ve bağımlılıklar görünürdür.
- Domain öğrenimi framework terminolojisinin arkasında kalmaz.
- Daha az bağımlılık ve daha hızlı başlangıç.
- İleride mediator ekleme veya eklememe kararı geri döndürülebilir.

## Olumsuz sonuçlar

- Cross-cutting behavior'lar şimdilik merkezi pipeline'a sahip değildir.
- Handler sayısı arttığında composition kodu büyüyebilir.

## Yeniden değerlendirme koşulları

- Transaction, authorization, logging veya validation için tekrarlı pipeline
  gereksinimi
- Modül içi handler keşfinin elle yönetilemez hâle gelmesi
- In-process command/event dispatch'in açık değer üretmesi

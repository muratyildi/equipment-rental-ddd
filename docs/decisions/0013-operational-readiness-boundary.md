# ADR-0013: Operasyonel güvenilirlik sınırlarını açıkça uygulamak

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

Sistem güvenilir publish ve uzun süren process davranışına sahipti fakat
sınırsız retry, correlation eksikliği, readiness ayrımı ve business endpoint
koruması production operasyonunu belirsiz bırakıyordu.

## Karar

- JSON structured log ve güvenli correlation id kullanılacak.
- Vendor-neutral `ActivitySource` ve `Meter` instrumentation Building Block'ta
  tutulacak; exporter deployment adapter'ı olacaktır.
- Liveness ile database/operasyonel readiness ayrılacaktır.
- Outbox beş başarısızlıktan sonra dead-letter olacaktır.
- Process Manager beş teknik başarısızlıktan sonra
  `RequiresIntervention` olacaktır.
- Business endpoint'leri API key authentication, read/write scope ve rate
  limiting ile korunacaktır.
- Local environment API, migration job ve PostgreSQL içeren Compose ile
  çalışacaktır.

## Sonuçlar

Olumlu:

- Bir hata trace/correlation id ile takip edilebilir.
- Poison item sonsuz retry döngüsü yaratmaz.
- Orchestrator doğru liveness/readiness sinyalini alır.
- Güvenlik default-deny business endpoint sınırına taşınır.
- Migration deployment'tan ayrı ve görünürdür.

Olumsuz:

- Dead-letter ve intervention için operasyon runbook/replay endpoint'i gerekir.
- API key insan kullanıcı authorization'ı için yeterli değildir.
- In-memory metrics exporter olmadan backend'e ulaşmaz.
- Compose anahtarları production secret management değildir.

## Yeniden değerlendirme

- İnsan kullanıcı veya tenant modeli oluştuğunda OIDC/OAuth 2.0
- SLO ve trafik verisi oluştuğunda rate-limit değerleri
- Telemetry backend seçildiğinde OTLP/exporter adapter'ı
- Operasyon ekibi oluştuğunda replay/resume ve alert runbook'ları

## Kanıtlar

- Outbox dead-letter ve Process Manager intervention integration testleri
- API key validator ve endpoint policy testleri
- Correlation middleware testleri
- Beş DbContext migration ve readiness kontrolü
- Docker Compose configuration doğrulaması

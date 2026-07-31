# Operational Readiness: Observability, Resilience, Security

Bu milestone'un amacı “production-ready” etiketi yapıştırmak değildir. Sistem
çalışmadığında ne olduğunu anlayabilmek, geçici hatayla kalıcı hatayı ayırmak
ve business endpoint'lerini varsayılan olarak korumaktır.

## Observability

Observability üç sinyalden oluşur:

- **Log:** Ayrık olayların structured kaydı
- **Trace:** Bir isteğin ve alt operasyonlarının nedensel akışı
- **Metric:** Zaman içindeki sayısal sistem davranışı

### Correlation

Her HTTP isteği `X-Correlation-ID` taşır. Güvenli karakterlerden oluşan ve 128
karakteri aşmayan caller değeri korunur; eksik veya güvenli değilse trace id ya
da yeni GUID üretilir.

Değer:

- response header'a yazılır;
- `HttpContext.TraceIdentifier` olur;
- structured logging scope'una `CorrelationId` olarak eklenir;
- Problem Details ve health cevaplarında görünür.

Caller tarafından gelen değer log injection'a dönüşmesin diye satır sonu,
boşluk ve kontrol karakterleri kabul edilmez.

### Structured logs ve telemetry API

Production console logları JSON formatındadır. Background worker logları
message template kullanır; string interpolation ile aranması zor metin
üretilmez.

Building Block, vendor-neutral .NET `ActivitySource` ve `Meter` API'lerini
tanımlar. Outbox publish/failure/dead-letter ve Process Manager
transition/intervention sayaçları kaydedilir. Bir deployment OpenTelemetry,
Application Insights veya başka bir `MeterListener`/`ActivityListener`
adapter'ını composition root'a takabilir. Domain projeleri exporter paketine
bağlanmaz.

### Liveness ve readiness

```text
GET /health/live
GET /health/ready
```

Liveness yalnızca process'in cevap verebildiğini gösterir. Database kesildi
diye liveness başarısız yapılmaz; aksi hâlde orchestrator restart storm
üretebilir.

Readiness beş DbContext'in PostgreSQL bağlantısını sınar:

- bağlantı yoksa `Unhealthy / 503`;
- bağlantı var fakat dead-letter veya `RequiresIntervention` process varsa
  `Degraded / 200`;
- sorun yoksa `Healthy / 200`.

Degraded instance trafik alabilir fakat operasyon alarmı gerektirir.
Health endpoint'leri orchestrator için anonymous ve rate-limit dışıdır;
production ingress/network policy bunları yalnızca kontrol düzlemine açmalıdır.

## Resilience

### Retry bütçesi

Retry yalnızca işlemin idempotent olduğu yerde güvenlidir. Outbox publish ve
Process Manager adımları stable id/demand id kullandığı için retry edilebilir.

Sınırsız retry uygulanmaz:

| İşlem | Bütçe | Terminal state |
|---|---:|---|
| Outbox publication | 5 | `DeadLetteredAtUtc` |
| Process Manager technical failure | 5 | `RequiresIntervention` |

Outbox exponential backoff kullanır ve dead-letter mesajı otomatik seçmez.
Process Manager terminal intervention state'inde worker tarafından tekrar
claim edilmez. Böylece poison item diğer işleri sonsuza kadar meşgul etmez.

Terminal state otomatik veri silme değildir. Operatör hata sebebini çözüp
ileride eklenecek kontrollü replay/resume komutunu kullanmalıdır. Database'de
elle status değiştirmek önerilen recovery mekanizması değildir.

### Rate limiting

Business endpoint'leri kimliği doğrulanmış principal veya istemci IP'si başına
sabit pencerede dakikada 100 istekle sınırlıdır. Limit aşımı `429 Too Many
Requests` döndürür. Bu değer abuse korumasının ilk katmanıdır; gerçek production
değeri trafik ve SLO ölçümleriyle belirlenmelidir.

Rate limiting domain concurrency kontrolünün yerine geçmez. `xmin` ve
aggregate invariant'ları hâlâ son doğruluk sınırıdır.

## Security

Sistemde henüz insan kullanıcı, tenant veya identity bounded context'i yoktur.
Bu yüzden sahte bir kullanıcı/rol modeli icat edilmedi. Mevcut API,
machine-to-machine kullanım için API key authentication uygular.

İki authorization scope vardır:

- `equipment-rental.read`
- `equipment-rental.write`

GET business endpoint'leri read; state değiştiren endpoint'ler write policy
ister. Key karşılaştırması SHA-256 digest üzerinde constant-time yapılır.
Anahtarlar production configuration'a commit edilmez; environment variable veya
secret store üzerinden verilir.

Development dosyasındaki anahtarlar yalnızca local kullanım içindir.
Docker Compose anahtarları `.env` üzerinden override eder. `.env.example`
gerçek secret içermez.

API key yaklaşımının bilinçli sınırları:

- son kullanıcı kimliği ve delegated authorization sağlamaz;
- key rotation/revocation yönetim API'si yoktur;
- transport seviyesinde TLS üretmez.

İnsan kullanıcı, tenant ve fine-grained permission gereksinimi ortaya çıktığında
OIDC/OAuth 2.0 JWT doğrulamasına geçilmelidir. TLS, ingress/reverse proxy
sorumluluğudur.

## Containerized local environment

Multi-stage Dockerfile:

1. SDK image içinde restore ve Release publish;
2. ayrı migration target'ında beş DbContext migration'ı;
3. non-root ASP.NET runtime image.

Compose sırası:

```text
PostgreSQL healthy
    → one-shot migrations completed
    → API starts
```

API başlangıcında otomatik migration yapılmaz. Bu, birden fazla replica'nın
aynı anda schema değiştirme yarışını engeller ve deployment adımını görünür
kılar.

```bash
cp .env.example .env
# .env içindeki iki anahtarı değiştir
docker compose up --build
```

# ADR-0003 — .NET 10, C# 14 ve SLNX kullanmak

- Durum: Accepted
- Tarih: 2026-07-31

## Bağlam

Projenin güncel .NET yaklaşımını göstermesi ve uzun süre desteklenen bir platform
üzerinde kurulması isteniyor.

## Karar

- Target framework: `net10.0`
- Dil: C# 14
- SDK başlangıç sürümü: `10.0.103`
- Solution formatı: `.slnx`
- Central Package Management
- Nullable reference types
- Warnings as errors
- `latest-Recommended` .NET analyzer seviyesi

`global.json`, aynı major/minor içindeki daha yeni feature band'e geçişe izin
verir; preview SDK kabul etmez.

## Sonuçlar

- .NET 10 LTS tabanı kullanılır.
- SLNX, klasik SLN'e göre daha okunabilir ve diff dostudur.
- Merkezi sürüm yönetimi bağımlılık drift'ini azaltır.
- Analyzer yükseltmeleri yeni uyarılar üretebilir; bunlar bilinçli biçimde ele
  alınmalıdır.

## Seçilmemiş seçenekler

- .NET 8: Kararlı LTS olsa da kullanıcının .NET 10 hedefini karşılamıyor.
- Preview .NET 11: Portföy projesi için gereksiz sürüm riski.
- `LangVersion=latest`: Yeni SDK ile habersiz dil değişimini mümkün kılar.

# Mimari Karar Günlüğü

Architecture Decision Record (ADR), önemli bir kararın yalnızca sonucunu değil,
kararın verildiği andaki bağlamı ve bedellerini saklar.

## Durumlar

- `Proposed`: Tartışmaya açık öneri
- `Accepted`: Uygulanmasına karar verilmiş
- `Superseded`: Daha yeni bir ADR tarafından değiştirilmiş
- `Rejected`: Değerlendirilmiş fakat seçilmemiş

## Şablon

Her ADR şu başlıkları içerir:

1. Başlık ve durum
2. Bağlam / problem
3. Karar sürücüleri
4. Değerlendirilen seçenekler
5. Karar
6. Olumlu sonuçlar
7. Olumsuz sonuçlar ve riskler
8. Yeniden değerlendirme koşulları
9. İlgili domain senaryoları ve testler

## Kayıtlar

- [ADR-0001 — Modüler monolit ile başlama](0001-moduler-monolit-ile-baslama.md)
  (`Accepted`)
- [ADR-0002 — Ekipman kiralama domain'ini seçmek](0002-ekipman-kiralama-domaini.md)
  (`Accepted`)
- [ADR-0003 — .NET 10, C# 14 ve SLNX kullanmak](0003-dotnet-10-ve-slnx.md)
  (`Accepted`)
- [ADR-0004 — İlk aşamada mediator yerine açık use-case handler'ları](0004-explicit-use-case-handlers.md)
  (`Accepted`)
- [ADR-0005 — Walking skeleton için geçici in-memory repository](0005-in-memory-adapter-gecicidir.md)
  (`Superseded`)
- [ADR-0006 — Fleet Availability context'i ve consumer-owned ACL](0006-fleet-availability-ve-acl.md)
  (`Accepted`)
- [ADR-0007 — Çok satırlı availability sürecini şimdilik satır bazında yürütmek](0007-cok-satirli-commitment-sureci.md)
  (`Superseded by ADR-0012`)
- [ADR-0008 — PostgreSQL, EF Core ve aggregate optimistic concurrency](0008-postgresql-ef-core-ve-optimistic-concurrency.md)
  (`Accepted`)
- [ADR-0009 — Domain Event'leri Transactional Outbox ile yayımlamak](0009-domain-events-ve-transactional-outbox.md)
  (`Accepted`)
- [ADR-0010 — Idempotent consumer ve Transactional Inbox](0010-idempotent-consumer-ve-transactional-inbox.md)
  (`Accepted`)
- [ADR-0011 — Availability Calendar için CQRS read model](0011-cqrs-availability-calendar-read-model.md)
  (`Accepted`)
- [ADR-0012 — Rental confirmation Process Manager](0012-rental-confirmation-process-manager.md)
  (`Accepted`)
- [ADR-0013 — Operasyonel güvenilirlik sınırları](0013-operational-readiness-boundary.md)
  (`Accepted`)
- [ADR-0014 — Ölçülmüş sürücü olmadan servis çıkarmama](0014-olculmus-surucu-olmadan-servis-cikarmama.md)
  (`Accepted`)

Kaynak incelemesinden çıkan başlangıç ilkeleri
[kaynak incelemesinde](../learning/00-kaynak-incelemesi.md) kayıtlıdır.

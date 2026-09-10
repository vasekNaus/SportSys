# ADR-001: Vrstvy a jednotný composition root

- **Status:** Accepted
- **Datum:** 2026-09-09
- **Rozhodující:** správce projektu

## Kontext a problém

Razor Pages potřebují data, validaci, Identity a autorizaci, ale přímé použití
EF Core v prezentaci by rozptýlilo business pravidla mezi PageModely a
zkomplikovalo testování. Samostatná registrace DbContextu nebo Identity ve
více projektech navíc může vytvořit kolidující autentizační schémata.

## Zvažované varianty

1. Přímý přístup z Razor Pages do `SportSysDbContext`.
2. Repository rozhraní v samostatné abstrakční vrstvě.
3. Contract služby, které zapouzdřují EF Core a vracejí DTO.

## Rozhodnutí

Aktivní aplikace používá tok
`SportSys.Razor -> SportSys.Contract -> SportSys.Database`.
Razor nereferencuje Database. DbContext, Identity, authorization policies a
aplikační služby registruje jediná metoda
`SportSys.Contract.ServiceCollectionExtensions.AddSportSysServices()`.

## Důsledky

### Pozitivní

- PageModely neznají EF Core entity ani DbContext.
- Business pravidla lze testovat mimo HTTP vrstvu.
- Identity a autentizační schémata mají jeden composition root.

### Negativní

- I jednoduchý databázový dotaz vyžaduje Contract službu a DTO.
- Contract projekt obsahuje aplikační logiku i integrační registraci.

## Reference

- `src/SportSys.Contract/ServiceCollectionExtensions.cs`
- `src/SportSys.Razor/SportSys.Razor.csproj`
- `docs/architecture.md`

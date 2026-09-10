# ADR-004: Coach jako TPT potomek Identity User

- **Status:** Accepted
- **Datum:** 2026-09-09
- **Rozhodující:** správce projektu
- **Související:** ADR-002, ADR-003

## Kontext a problém

Každý trenér musí být současně uživatelem ASP.NET Core Identity a používat
stejné lokální ID, přihlašovací údaje a společná profilová pole. Původní model
ukládal `hr.Coach` jako samostatnou entitu s vlastním ID a duplicitním
`DisplayName`; připravovaná kompoziční vazba na `identity.User` nebyla
aktivována.

Projekt obecně preferuje TPC, protože konkrétní doménové typy mohou mít vlastní
tabulky bez joinu na tabulku předka. Identity hierarchie má ale jiné požadavky:
každý uživatel musí mít jeden fyzický řádek v `identity.User` a trenérské FK
musí současně směřovat na fyzickou tabulku `hr.Coach`.

## Zvažované varianty

1. Zachovat samostatný `Coach` a přidat unikátní kompoziční `UserId`.
2. Použít TPC a duplikovat Identity sloupce do `hr.Coach`.
3. Použít TPT se sdíleným PK mezi `identity.User` a `hr.Coach`.

## Rozhodnutí

`Coach` dědí z neabstraktního `User` a hierarchie používá TPT:

```text
identity.User
      │ PK = FK
      ▼
hr.Coach
```

`hr.Coach.Id` je současně primární klíč a cizí klíč na
`identity.User.Id`. Společná pole jako jméno, e-mail a telefon existují pouze
v základní tabulce. Trenérská pole, fotografie a vazby zůstávají v
`hr.Coach`.

TPC podle ADR-003 zůstává výchozí strategií pro ostatní doménové hierarchie.
Toto rozhodnutí je lokální výjimka vyvolaná požadavky Identity a neruší ani
nenahrazuje ADR-003.

Existující uživatel se povyšuje na trenéra vložením pouze derived řádku do
`hr.Coach`. EF Core neumí změnit již sledovanou instanci základního CLR typu
na odvozený typ, proto tuto jedinou operaci provádí Contract služba
parametrizovaným SQL příkazem v transakci.

## Důsledky

### Pozitivní

- Uživatel a trenér mají jedinou identitu a stejné číselné ID.
- Identity, Entra provisioning a auditní FK nadále používají
  `identity.User.Id`.
- Společná profilová pole se neduplikují.
- HR a Sport tabulky mohou vynucovat FK přímo na `hr.Coach`.
- Dotaz na `User` může korektně materializovat odvozený `Coach`.

### Negativní

- Polymorfní dotazy na `User` mohou vyžadovat join na `hr.Coach`.
- Povýšení existujícího uživatele vyžaduje explicitní SQL insert derived
  řádku.
- Převod existujících dat vyžaduje ověřenou mapu původního `Coach.Id` na
  `User.Id` a přemapování všech závislých FK.
- Rollback musí zachovat nebo obnovit původní mapu identifikátorů.

## Reference

- `src/SportSys.Database/Models/identity/User.cs`
- `src/SportSys.Database/Models/hr/Coach.cs`
- `src/SportSys.Database/Configurations/identity/UserConfiguration.cs`
- `src/SportSys.Contract/Services/CoachService.cs`
- `docs/decisions/adr-002-hybridni-identita.md`
- `docs/decisions/adr-003-tpc-se-sdilenymi-sekvencemi.md`
- `.github/tasks/hr-coach-user-tpt-inheritance.md`

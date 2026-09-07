# Modul Personalistika

## Účel

Modul `hr` spravuje trenéry, jejich personální nastavení, licence a smlouvy.
Administrační stránky jsou v Razor Area `hr` a jsou dostupné pouze uživatelům
s politikou `SystemAdmin`. Razor vrstva používá výhradně `CoachService` a DTO
z `SportSys.Contract`.

## Trenér a uživatelský účet

`hr.Coach` má povinnou unikátní vazbu na `identity.User`. Jeden uživatel tedy
může představovat nejvýše jednoho trenéra. Vytvoření trenéra pouze propojí
existující účet; nevytváří heslo ani neobchází provisioning přes Microsoft
Entra ID nebo ASP.NET Core Identity.

Stávající `Coach.Id` zůstává zachováno, protože na něj odkazují tabulky:

- `sport.CoachTraining`,
- `sport.CoachTrainingEntitlement`,
- `sport.CoachTrainingPlan`.

Zobrazované jméno, e-mail a telefon pocházejí z `identity.User`. `hr.Coach`
uchovává osobní číslo, normalizované rodné číslo a volitelnou fotografii.
Rodné číslo ani binární fotografie nejsou součástí seznamového dotazu.

Fotografie může být JPEG, PNG nebo WebP a může mít nejvýše 5 MiB. Služba
kontroluje MIME typ i signaturu souboru. Obrázek se načítá samostatným
autorizovaným GET handlerem s privátní cache; chybějící fotografie se v UI
nahrazuje statickým avatarem.

## Personální nastavení

`hr.CoachSetting` uchovává časově platné údaje:

- bankovní účet,
- adresu,
- kód zdravotní pojišťovny.

Interval je včetně obou krajních dat a otevřený interval má `ValidTo = NULL`.
Intervaly jednoho trenéra se nesmějí překrývat. Při vložení nového
chronologicky navazujícího nastavení služba uzavře předchozí otevřený interval
na den před novým `ValidFrom`.

## Licence

`hr.CoachLicense` spojuje trenéra s typem licence a intervalem platnosti.
Různé typy licencí se mohou překrývat, ale dva intervaly stejného typu licence
u stejného trenéra nikoli.

Číselník `hr.CoachLicenseType` obsahuje stabilní kódy licencí. Není seedován
přes EF Core; hodnoty se zavádějí idempotentním jednorázovým skriptem
`src/DB Model/hr.CoachLicenseType.Data.sql`.

## Smlouvy

`hr.CoachContract` eviduje smlouvu trenéra pro konkrétní
`sport.Season`. Podporované typy jsou:

| Hodnota | Název |
|---:|---|
| 1 | DPP |
| 2 | OSVČ |

Odměna musí být nezáporná. Jeden trenér může mít v jedné sezoně několik
současně aktivních smluv. Historie se zachovává změnou `IsActive`, nikoli
fyzickým smazáním.

## Administrační stránky

| Stránka | Funkce |
|---|---|
| `/hr/Coach/Index` | hledání, filtr sezony a aktivní smlouvy |
| `/hr/Coach/Edit` | základní údaje, fotografie, smlouvy, nastavení a licence |

Detail používá samostatný formulář a POST handler pro každou záložku, takže
validace skrytých částí neblokuje právě ukládaný formulář. U nového trenéra
jsou podřízené záložky dostupné až po uložení základních údajů.

## Nasazení změny existujících trenérů

Přechod existujících dat musí provést uživatelem vytvořená řízená migrace:

1. Přidat vazbu na uživatele a nové personální sloupce dočasně jako nullable.
2. Automaticky propojit pouze jednoznačné uživatele a zbytek ručně dořešit.
3. Doplnit osobní a rodná čísla a ověřit vazbu 1:1.
4. Nastavit nové povinné sloupce jako `NOT NULL`.
5. Přesunout tabulku do schématu `hr` při zachování `Coach.Id`.
6. Obnovit cizí klíče ze `sport.*` a odstranit původní jmenné sloupce.

Agent EF Core migraci nevytváří ani neupravuje.

# Implementace: #42 Vizualizace duplicitně přiřazených trenérů v tréninkových plánech

**Issue:** [#42 — Vizualizace duplicitně přiřazených trenérů v tréninkových plánech](https://github.com/vasekNaus/SportSys/issues/42)

**Plán:** [42-vizualizace-duplicitne-prirazenych-treneru-v-treninkovych-planech.md](42-vizualizace-duplicitne-prirazenych-treneru-v-treninkovych-planech.md)

## Upřesnění podle komentáře issue

Komentář `vnaus` z 26. 10. 2026 upozornil, že tučně označené záznamy nejsou
dostatečně viditelné, a požádal o chybovou ikonu. První úprava nahradila
`<strong>` ikonou Font Awesome `fa-circle-exclamation`.

[Komentář z 27. 10. 2026](https://github.com/vasekNaus/SportSys/issues/42#issuecomment-6450654482)
uvedl, že ani tato ikona není dobře vidět, a požádal o ikonu ❌ jako u
neúčasti v docházce. Aktuální implementace proto za jméno duplicitního
přiřazení vkládá ❌ ze sdílené konstanty `CoachParticipationVisual.AbsenceIcon`
ve stejném markupu `schedule-block-coach-icon` jako docházka, s přístupným
popisem `Duplicitní přiřazení trenéra`. Jméno zůstává normálním řezem písma.

## Provedené změny

- `TrainingPlanCoachPresentation.GetDisplayItems` seřadí přiřazení podle
  stávajících pravidel a označí všechny výskyty opakovaného `CoachId`.
- `TrainingPlanEventModel` nese typované položky trenérů včetně příznaku
  duplicity; původní textové shrnutí zůstává zachováno pro tooltip.
- `CoachParticipationVisual` vystavuje konstantu `AbsenceIcon` (❌), kterou
  sdílí mapování typu účasti i označení duplicit.
- Sdílený partial rozvrhu vykreslí za každým duplicitním přiřazením
  přístupnou ikonu ❌. Ostatní plány, skutečné tréninky, tooltipy
  i textový seznam si ponechávají stávající chování.
- `_schedule.scss` nastaví ikoně pouze odsazení; generované CSS
  je obnoveno přes CSS build.
- Testy ověřují identitu trenéra, zachování pořadí, explicitní a vizuální
  seskupení a vykreslení ikony v editovatelné i needitovatelné variantě.
- `docs/modules/sport.md` popisuje ikonu. Databázový model a EF migrace
  zůstávají beze změny; SCSS bylo doplněno pouze o styly ikony.

## Ověření

Úspěšně proběhlo:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingPlanCoachPresentationTests|FullyQualifiedName~TrainingScheduleComponentModelTests|FullyQualifiedName~TrainingScheduleBlockFactoryTests"
```

Výsledek: 41 testů úspěšných (včetně `CoachParticipation*`), žádný neúspěšný. Build vypisuje existující
upozornění NuGet `NU1902`/`NU1903` na zranitelné verze
`System.Security.Cryptography.Pkcs` a `System.Security.Cryptography.Xml`.

SCSS bylo následně zkompilováno příkazem `npm run build:css` v
`src/SportSys.Razor`.

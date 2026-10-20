# Implementace #34: Rozšíření evidence rolí trenérů

- `CoachTrainingPlan` a `CoachTraining` nyní obsahují povinný FK `CoachRoleId`,
  navigaci na `dbo.CoachRole` a explicitní FK index. `CoachTrainingRequirement`
  ukládá `Hours decimal(5,2)`.
- Contract služby načítají, validují a ukládají role u plánů i skutečných
  tréninků. Role jsou součástí concurrency snapshotů; duplicitní přiřazení
  trenéra se odmítá bez ohledu na roli. Docházka zůstává zachována.
- HR import při novém záznamu nastaví roli `Head`; při změně stavu existujícího
  záznamu její roli zachová.
- Razor editory plánů, skutečných tréninků a požadavků umožňují upravit role
  trenérů; u požadavků lze upravovat hodiny v rozsahu `0,00–999,99`.
- Aktualizována sportovní a HR dokumentace. Přidány testy validace rolí,
  duplicit, concurrency snapshotů, hodin a EF metadat FK/indexů/přesnosti.
- Ověření: `dotnet build SportSys.slnx -c Release --no-restore` prošel;
  `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore`
  prošel (239/239 testů).
- EF Core migrace ani model snapshot nebyly vytvořeny nebo upraveny. Uživatel
  musí vytvořit a zkontrolovat migraci včetně backfillu `CoachRoleId = 1`
  a `Hours = 0,00` pro historické záznamy.

using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Enums;
using SportSys.Database.Models.sport;
using DbMatch = SportSys.Database.Models.sport.Match;
using DbMatchResult = SportSys.Database.Models.sport.MatchResult;

namespace SportSys.Contract.Services;

/// <summary>
/// Importuje zápasy ze středníkového CSV souboru staženého ze stránek hokejového svazu.
/// </summary>
public class CsvMatchImportService
{
  private readonly SportSysDbContext _db;
  private readonly ILogger<CsvMatchImportService> _logger;

  public CsvMatchImportService(
      SportSysDbContext db,
      ILogger<CsvMatchImportService> logger)
  {
    _db = db;
    _logger = logger;
  }

  /// <summary>
  /// Importuje zápasy z CSV souboru.
  /// Kategorie sezóny se určuje automaticky z hodnoty sloupce Soutěž
  /// porovnáním s <see cref="SeasonCategory.CompetitionCode"/>.
  /// </summary>
  /// <param name="csvFilePath">Cesta ke středníkovému CSV souboru (UTF-8).</param>
  /// <param name="seasonId">ID sezóny, ke které se zápasy přiřadí.</param>
  public async Task<CsvMatchImportResult> ImportAsync(
      string csvFilePath,
      int seasonId,
      CancellationToken ct = default)
  {
    if (!File.Exists(csvFilePath))
      throw new FileNotFoundException($"CSV soubor nenalezen: {csvFilePath}", csvFilePath);

    _logger.LogInformation("Import zápasů z CSV: {FilePath}", csvFilePath);

    // ── Krok A: načtení referenčních tabulek ─────────────────────────────
    // SeasonCategory: klíč = CompetitionCode, hodnota = seznam (Code, CompetitionTeamName)
    // Jeden CompetitionCode může mít více záznamů (např. A-tým + B-tým ve stejné soutěži)
    var seasonCategoryRaw = await _db.SeasonCategories
        .Where(sc => sc.SeasonId == seasonId && sc.CompetitionCode != string.Empty)
        .Select(sc => new { CompetitionCode = sc.CompetitionCode.Trim(), CategoryCode = sc.Code, TeamName = sc.CompetitionTeamName.Trim() })
        .ToListAsync(ct);

    var seasonCategoryByCode = seasonCategoryRaw
        .GroupBy(sc => sc.CompetitionCode, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            g => g.Key,
            g => g.Select(e => (CategoryCode: e.CategoryCode, TeamName: e.TeamName)).ToList(),
            StringComparer.OrdinalIgnoreCase);

    var teamsByName = await _db.Teams
        .Select(o => new { o.Name, o.Id, o.HomeLocationId })
        .ToListAsync(ct);
    var teamsByNameDict = teamsByName.ToDictionary(
        o => o.Name.Trim(),
        o => (Id: o.Id, HomeLocationId: o.HomeLocationId),
        StringComparer.OrdinalIgnoreCase);

    var matchTypeIds = (await _db.MatchTypes
        .Select(m => m.Id)
        .ToListAsync(ct))
        .ToHashSet();

    var existingMatchCodes = (await _db.Matches
        .Where(m => m.MatchCode != null)
        .Select(m => m.MatchCode!)
        .ToListAsync(ct))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // ── Krok B: parsování CSV ─────────────────────────────────────────────
    var result = new CsvMatchImportResult();
    using var sr = new StreamReader(csvFilePath, Encoding.GetEncoding(1250));
    var headerLine = await sr.ReadLineAsync(ct);
    if (headerLine is null) return result;

    string? line;
    int lineNumber = 1;

    while ((line = await sr.ReadLineAsync(ct)) is not null)
    {
      lineNumber++;
      if (string.IsNullOrWhiteSpace(line)) continue;

      var cols = line.Split(';');
      if (cols.Length < 9)
      {
        result.AddWarning($"Řádek {lineNumber}: nedostatečný počet sloupců, přeskočen.");
        result.Skipped++;
        continue;
      }

      string den = UnquoteAndTrim(cols[0]);
      string rawDate = UnquoteAndTrim(cols[1]);
      string rawTime = UnquoteAndTrim(cols[2]);
      string soutez = UnquoteAndTrim(cols[4]);
      string matchCode = UnquoteAndTrim(cols[5]);
      string domaci = UnquoteAndTrim(cols[6]);
      string hoste = UnquoteAndTrim(cols[7]);
      string stav = UnquoteAndTrim(cols[8]);

      // ── SeasonCategory lookup + identifikace našeho týmu ─────────────────
      string soutezKey = Truncate(soutez.Trim(), 10);
      if (!seasonCategoryByCode.TryGetValue(soutezKey, out var candidates))
      {
        result.AddWarning($"Řádek {lineNumber}: soutěž '{soutez}' nemá odpovídající SeasonCategory (CompetitionCode='{soutezKey}'), přeskočen.");
        result.Skipped++;
        continue;
      }

      string? seasonCategoryCode = null;
      string? competitionTeamName = null;
      bool? isHome = null;
      foreach (var c in candidates)
      {
        if (string.IsNullOrEmpty(c.TeamName)) continue;
        if (domaci.Equals(c.TeamName, StringComparison.OrdinalIgnoreCase))
        {
          seasonCategoryCode = c.CategoryCode; competitionTeamName = c.TeamName; isHome = true; break;
        }
        if (hoste.Equals(c.TeamName, StringComparison.OrdinalIgnoreCase))
        {
          seasonCategoryCode = c.CategoryCode; competitionTeamName = c.TeamName; isHome = false; break;
        }
      }
      if (isHome is null)
      {
        _logger.LogDebug("Řádek {Line}: {Domaci} vs {Hoste} se netýká žádného z našich týmů, přeskočen.", lineNumber, domaci, hoste);
        result.Skipped++;
        continue;
      }

      // ── Datum ─────────────────────────────────────────────────────────
      DateOnly? date = ParseDate(rawDate);
      if (date is null)
      {
        result.AddWarning($"Řádek {lineNumber}: nepodařilo se parsovat datum '{rawDate}', přeskočen.");
        result.Skipped++;
        continue;
      }

      // ── Čas začátku ───────────────────────────────────────────────────
      TimeOnly? timeFrom = ParseTime(rawTime);
      if (timeFrom is null)
      {
        result.AddWarning($"Řádek {lineNumber}: nepodařilo se parsovat čas '{rawTime}', přeskočen.");
        result.Skipped++;
        continue;
      }

      // ── Detekce duplikátu ─────────────────────────────────────────────
      if (!string.IsNullOrEmpty(matchCode) && existingMatchCodes.Contains(matchCode))
      {
        _logger.LogDebug("Zápas {MatchCode} ({Date}) již v DB, přeskočen.", matchCode, date);
        result.Skipped++;
        continue;
      }

      // ── Teams (domácí + hosté) ────────────────────────────────────
      var (homeTeamId, homeLocationId, homeTeamWarning) = LookupTeam(domaci.Trim(), teamsByNameDict);
      if (homeTeamId < 0)
      {
        result.AddWarning($"Řádek {lineNumber}: {homeTeamWarning} ({date}), přeskočen.");
        result.Skipped++;
        continue;
      }

      if (homeLocationId is null)
      {
        result.AddWarning($"Řádek {lineNumber}: tým '{Truncate(domaci.Trim(), 50)}' nemá přiřazenou domácí lokalitu (HomeLocationId), přeskočen.");
        result.Skipped++;
        continue;
      }

      var (awayTeamId, _, awayTeamWarning) = LookupTeam(hoste.Trim(), teamsByNameDict);
      if (awayTeamId < 0)
      {
        result.AddWarning($"Řádek {lineNumber}: {awayTeamWarning} ({date}), přeskočen.");
        result.Skipped++;
        continue;
      }

      // ── MatchType ─────────────────────────────────────────────────────
      int matchTypeId = ResolveMatchTypeId(soutez, den, matchTypeIds);

      // ── Stav → skóre + poznámka ───────────────────────────────────────
      var (homeGoals, awayGoals, note) = ParseStav(stav);

      // ── ID ze sdílené DB sekvence ─────────────────────────────────────
      int newId = await GetNextSportEventIdAsync(ct);

      DbMatchResult? result_match = (homeGoals.HasValue || awayGoals.HasValue)
          ? new DbMatchResult { HomeGoals = homeGoals, AwayGoals = awayGoals }
          : null;

      var match = new DbMatch
      {
        Id = newId,
        SeasonId = seasonId,
        SeasonCategoryCode = seasonCategoryCode!,
        LocationId = homeLocationId.Value,
        Date = date.Value,
        TimeFrom = timeFrom.Value,
        TimeTo = ResolveImportedTimeTo(timeFrom.Value),
        Note = Truncate(note, 50),
        MatchCode = string.IsNullOrEmpty(matchCode) ? null : Truncate(matchCode, 10),
        HomeTeamId = homeTeamId,
        AwayTeamId = awayTeamId,
        Result = result_match,
        MatchTypeId = matchTypeId,
      };

      _db.Matches.Add(match);

      if (!string.IsNullOrEmpty(matchCode))
        existingMatchCodes.Add(matchCode);

      result.Inserted++;
    }

    if (result.Inserted > 0)
      await _db.SaveChangesAsync(ct);

    _logger.LogInformation(
        "Import dokončen: vloženo {Inserted}, přeskočeno {Skipped}.",
        result.Inserted, result.Skipped);

    return result;
  }

  internal static TimeOnly ResolveImportedTimeTo(TimeOnly timeFrom)
    => timeFrom;

  // ── Lookup / create helpers ───────────────────────────────────────────────

  private (int Id, int? HomeLocationId, string Warning) LookupTeam(
      string name,
      Dictionary<string, (int Id, int? HomeLocationId)> dict)
  {
    if (dict.TryGetValue(name, out var entry)) return (entry.Id, entry.HomeLocationId, string.Empty);

    // Fallback: odebrat suffix " B" / " C" / " D" a zkusit znovu
    if (name.Length > 2 && name[^2] == ' ' && "BCD".Contains(name[^1], StringComparison.OrdinalIgnoreCase))
    {
      string stripped = name[..^2].TrimEnd();
      if (dict.TryGetValue(stripped, out entry))
        return (entry.Id, entry.HomeLocationId, string.Empty);
    }

    _logger.LogWarning("Tým '{Name}' nenalezen v tabulce Team.", name);
    return (-1, null, $"Tým '{Truncate(name, 50)}' nenalezen v tabulce Team");
  }

  /// <summary>
  /// Mapuje hodnotu sloupce Soutěž/Den na ID záznamu v tabulce MatchType.
  /// MatchType je pevný číselník (Liga=1, Přátelský=2, Turnaj=3) – nové záznamy se nevytváří.
  /// </summary>
  private static int ResolveMatchTypeId(string soutez, string den, HashSet<int> validIds)
  {
    var eType = MapSoutezToMatchType(soutez, den);
    int id = (int)eType;
    // Fallback na Ligu pokud by DB z nějakého důvodu neměla daný typ
    return validIds.Contains(id) ? id : (int)EMatchType.League;
  }

  /// <summary>
  /// Odvozuje typ zápasu ze soutěžního názvu a kódu dne.
  /// "SoNe" (vícedenní turnaj) → Tournament, "přátel.*" / "příprav.*" → Friendly, ostatní → League.
  /// </summary>
  private static EMatchType MapSoutezToMatchType(string soutez, string den)
  {
    if (den.Equals("SoNe", StringComparison.OrdinalIgnoreCase))
      return EMatchType.Tournament;
    if (soutez.Contains("turnaj", StringComparison.OrdinalIgnoreCase))
      return EMatchType.Tournament;
    if (soutez.Contains("přátel", StringComparison.OrdinalIgnoreCase) ||
        soutez.Contains("příprav", StringComparison.OrdinalIgnoreCase))
      return EMatchType.Friendly;
    return EMatchType.League;
  }

  /// <summary>
  /// Vrátí další hodnotu ze sekvence sport.SportEventSeq.
  /// EF Core pro ValueGeneratedNever nepřiřadí Id automaticky → přečteme sami.
  /// </summary>
  private async Task<int> GetNextSportEventIdAsync(CancellationToken ct)
  {
    var conn = _db.Database.GetDbConnection();
    bool wasOpen = conn.State == System.Data.ConnectionState.Open;
    if (!wasOpen) await conn.OpenAsync(ct);
    try
    {
      using var cmd = conn.CreateCommand();
      cmd.CommandText = "SELECT NEXT VALUE FOR [sport].[SportEventSeq]";
      var value = await cmd.ExecuteScalarAsync(ct);
      return Convert.ToInt32(value);
    }
    finally
    {
      if (!wasOpen) await conn.CloseAsync();
    }
  }

  // ── Statické pomocné metody ───────────────────────────────────────────────

  /// <summary>
  /// Parsuje sloupec Stav a vrátí góly (home, away) a poznámku.
  /// </summary>
  private static (int? home, int? away, string note) ParseStav(string stav)
  {
    stav = stav.Trim();

    if (string.IsNullOrEmpty(stav) ||
        stav.Equals("Připraveno", StringComparison.OrdinalIgnoreCase))
      return (null, null, string.Empty);

    if (stav.Equals("Zápas zrušen", StringComparison.OrdinalIgnoreCase))
      return (null, null, "Zápas zrušen");

    // Kontumace H = výhra hostů (away); Kontumace D = výhra domácích (home)
    if (stav.Equals("Kontumace H", StringComparison.OrdinalIgnoreCase))
      return (0, 5, "Kontumace H");

    if (stav.Equals("Kontumace D", StringComparison.OrdinalIgnoreCase))
      return (5, 0, "Kontumace D");

    bool shootout = stav.EndsWith("sn", StringComparison.OrdinalIgnoreCase);
    string scoreStr = shootout ? stav[..^2] : stav;
    string note = shootout ? "Nájezdy" : string.Empty;

    var parts = scoreStr.Split(':');
    if (parts.Length == 2 &&
        int.TryParse(parts[0], out int h) &&
        int.TryParse(parts[1], out int a))
      return (h, a, note);

    // Neznámý formát – uložit jako poznámku
    return (null, null, Truncate(stav, 50));
  }

  /// <summary>
  /// Parsuje datum z formátu "DD.MM.YYYY". Rozsah "D1 - D2" → vrátí první datum.
  /// </summary>
  private static DateOnly? ParseDate(string rawDate)
  {
    rawDate = rawDate.Trim();
    if (string.IsNullOrEmpty(rawDate)) return null;

    int dashIdx = rawDate.IndexOf(" - ", StringComparison.Ordinal);
    if (dashIdx > 0) rawDate = rawDate[..dashIdx].Trim();

    if (DateOnly.TryParseExact(rawDate, ["d.M.yyyy", "dd.MM.yyyy"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
      return date;

    if (DateOnly.TryParse(rawDate, new CultureInfo("cs-CZ"), out date))
      return date;

    return null;
  }

  /// <summary>
  /// Parsuje čas z formátu "H:MM". Rozsah "T1 - T2" → vrátí první čas.
  /// </summary>
  private static TimeOnly? ParseTime(string rawTime)
  {
    rawTime = rawTime.Trim();
    if (string.IsNullOrEmpty(rawTime)) return null;

    int dashIdx = rawTime.IndexOf(" - ", StringComparison.Ordinal);
    if (dashIdx > 0) rawTime = rawTime[..dashIdx].Trim();

    if (TimeOnly.TryParseExact(rawTime, ["H:mm", "HH:mm"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
      return time;

    if (TimeOnly.TryParse(rawTime, new CultureInfo("cs-CZ"), out time))
      return time;

    return null;
  }

  /// <summary>Odstraní okolní uvozovky a bílé znaky z hodnoty CSV buňky.</summary>
  private static string UnquoteAndTrim(string col)
  {
    var s = col.Trim();
    if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
      s = s[1..^1].Trim();
    return s;
  }

  private static string Truncate(string s, int maxLength)
      => s.Length <= maxLength ? s : s[..maxLength];
}

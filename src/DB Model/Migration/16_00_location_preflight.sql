-- ============================================================
-- #16 Sjednocení míst konání: předběžná kontrola
-- Spustit před 16_01_location_migration.sql.
--
-- Skript nic nemění. Selže, pokud by převod mohl ztratit nebo
-- nejednoznačně přiřadit textovou lokalitu tréninku.
-- ============================================================

SET NOCOUNT ON;

IF OBJECT_ID(N'[sport].[IceRink]', N'U') IS NULL
    THROW 51000, N'Chybí očekávaná tabulka [sport].[IceRink].', 1;

IF OBJECT_ID(N'[sport].[Training]', N'U') IS NULL
    OR OBJECT_ID(N'[sport].[Match]', N'U') IS NULL
    OR OBJECT_ID(N'[sport].[Team]', N'U') IS NULL
    THROW 51000, N'Chybí jedna z očekávaných tabulek sport.Training, sport.Match nebo sport.Team.', 1;

IF OBJECT_ID(N'[sport].[Location]', N'U') IS NOT NULL
    THROW 51000, N'Tabulka [sport].[Location] již existuje; skript #16 nelze spustit podruhé.', 1;

IF COL_LENGTH(N'[sport].[Training]', N'Location') IS NULL
    OR COL_LENGTH(N'[sport].[Match]', N'IceRink_Id') IS NULL
    OR COL_LENGTH(N'[sport].[Team]', N'HomeIceRink_Id') IS NULL
    THROW 51000, N'Schéma neodpovídá očekávanému stavu před změnou #16.', 1;

IF EXISTS
(
    SELECT 1
    FROM [sport].[Training]
    WHERE [Location] IS NULL OR LTRIM(RTRIM([Location])) = N''
)
    THROW 51000, N'Nejméně jeden trénink nemá vyplněnou lokalitu. Nejprve jej opravte.', 1;

IF EXISTS
(
    SELECT 1
    FROM [sport].[Training]
    WHERE LEN(LTRIM(RTRIM([Location]))) > 100
)
    THROW 51000, N'Nejméně jedna lokalita tréninku má po oříznutí více než 100 znaků.', 1;

IF EXISTS
(
    SELECT 1
    FROM [sport].[IceRink] AS rink
    INNER JOIN [sport].[Training] AS training
        ON UPPER(LTRIM(RTRIM(rink.[Name]))) = UPPER(LTRIM(RTRIM(training.[Location])))
    GROUP BY UPPER(LTRIM(RTRIM(rink.[Name])))
    HAVING COUNT(DISTINCT rink.[Id]) > 1
)
    THROW 51000, N'Název některého stadionu odpovídá lokalitě tréninku vícekrát. Ručně sjednoťte nebo rozlište duplicitní stadiony.', 1;

PRINT N'=== Lokality používané tréninky ===';
SELECT
    LTRIM(RTRIM([Location])) AS [Lokalita],
    COUNT(*) AS [PocetTreninku],
    MIN([Date]) AS [PrvniTrenink],
    MAX([Date]) AS [PosledniTrenink]
FROM [sport].[Training]
GROUP BY LTRIM(RTRIM([Location]))
ORDER BY [Lokalita];

PRINT N'=== Stávající stadiony, které budou zachovány jako lokality ===';
SELECT [Id], [Name], [Street], [City], [ZipCode], [IsActive]
FROM [sport].[IceRink]
ORDER BY [City], [Name], [Id];

PRINT N'Předběžná kontrola #16 proběhla bez chyb.';

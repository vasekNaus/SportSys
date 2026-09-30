-- ============================================================
-- #16 Sjednocení míst konání: ověření po ostrém převodu
-- Spustit po 16_01_location_migration.sql s @Commit = 1.
-- Skript nic nemění.
-- ============================================================

SET NOCOUNT ON;

IF OBJECT_ID(N'[sport].[Location]', N'U') IS NULL
    OR OBJECT_ID(N'[sport].[IceRink]', N'U') IS NOT NULL
    THROW 51000, N'Očekává se pouze tabulka [sport].[Location].', 1;

IF COL_LENGTH(N'[sport].[Training]', N'Location') IS NOT NULL
    OR COL_LENGTH(N'[sport].[Training]', N'Location_Id') IS NULL
    OR COL_LENGTH(N'[sport].[Match]', N'Location_Id') IS NULL
    OR COL_LENGTH(N'[sport].[Team]', N'HomeLocation_Id') IS NULL
    THROW 51000, N'Sloupcové schéma neodpovídá výsledku převodu #16.', 1;

IF EXISTS (SELECT 1 FROM [sport].[Training] WHERE [Location_Id] IS NULL)
    THROW 51000, N'Nejméně jeden trénink nemá přiřazenou lokalitu.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_Match_Location_Location_Id'
)
    OR NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_Team_Location_HomeLocation_Id'
)
    OR NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_Training_Location_Location_Id'
)
    THROW 51000, N'Chybí alespoň jeden požadovaný FK constraint lokality.', 1;

SELECT COUNT(*) AS [PocetLokalit]
FROM [sport].[Location];

SELECT
    COUNT(*) AS [PocetTreninku],
    COUNT(DISTINCT [Location_Id]) AS [PouzitychLokalit]
FROM [sport].[Training];

SELECT
    location.[Id],
    location.[Name],
    COUNT(training.[Id]) AS [PocetTreninku]
FROM [sport].[Location] AS location
LEFT JOIN [sport].[Training] AS training
    ON training.[Location_Id] = location.[Id]
GROUP BY location.[Id], location.[Name]
ORDER BY location.[Name], location.[Id];

PRINT N'Ověření převodu #16 proběhlo bez chyb.';

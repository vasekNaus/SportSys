-- ============================================================
-- #16 Sjednocení míst konání: převod schématu a dat
-- Spustit až po 16_00_location_preflight.sql.
--
-- Výchozí režim transakci vrátí zpět. Po kontrole změňte
-- @Commit na 1 a spusťte skript znovu pro ostré provedení.
-- Nejde o EF Core migraci.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 1;

IF OBJECT_ID(N'[sport].[IceRink]', N'U') IS NULL
    THROW 51000, N'Chybí očekávaná tabulka [sport].[IceRink].', 1;

IF OBJECT_ID(N'[sport].[Location]', N'U') IS NOT NULL
    THROW 51000, N'Tabulka [sport].[Location] již existuje; skript #16 nelze spustit podruhé.', 1;

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

BEGIN TRY
    BEGIN TRANSACTION;

    -- FK se zjistí dynamicky, aby skript fungoval i s jiným názvem constraintu.
    DECLARE @DropForeignKeys nvarchar(max);

    SELECT @DropForeignKeys = STRING_AGG(
        CAST(
            N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) +
            N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) +
            N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' AS nvarchar(max)),
        CHAR(13) + CHAR(10))
    FROM sys.foreign_keys AS fk
    WHERE fk.referenced_object_id = OBJECT_ID(N'[sport].[IceRink]');

    IF @DropForeignKeys IS NOT NULL
        EXEC sys.sp_executesql @DropForeignKeys;

    EXEC sys.sp_rename N'sport.IceRink', N'Location';
    EXEC sys.sp_rename N'sport.Match.IceRink_Id', N'Location_Id', N'COLUMN';
    EXEC sys.sp_rename N'sport.Team.HomeIceRink_Id', N'HomeLocation_Id', N'COLUMN';

    -- Textové lokality tréninků nemají adresu; údaje z původních stadionů zůstávají zachovány.
    ALTER TABLE [sport].[Location] ALTER COLUMN [Street] nvarchar(200) NULL;
    ALTER TABLE [sport].[Location] ALTER COLUMN [City] nvarchar(100) NULL;
    ALTER TABLE [sport].[Location] ALTER COLUMN [ZipCode] nvarchar(100) NULL;

    DECLARE @DropDefaults nvarchar(max);

    SELECT @DropDefaults = STRING_AGG(
        CAST(
            N'ALTER TABLE [sport].[Location] DROP CONSTRAINT ' +
            QUOTENAME(defaultConstraint.[name]) + N';' AS nvarchar(max)),
        CHAR(13) + CHAR(10))
    FROM sys.default_constraints AS defaultConstraint
    INNER JOIN sys.columns AS columnDefinition
        ON columnDefinition.object_id = defaultConstraint.parent_object_id
        AND columnDefinition.column_id = defaultConstraint.parent_column_id
    WHERE defaultConstraint.parent_object_id = OBJECT_ID(N'[sport].[Location]')
        AND columnDefinition.[name] IN (N'ZipCode', N'IsActive');

    IF @DropDefaults IS NOT NULL
        EXEC sys.sp_executesql @DropDefaults;

    ALTER TABLE [sport].[Location]
        ADD CONSTRAINT [DF_Location_ZipCode] DEFAULT (N'') FOR [ZipCode];

    ALTER TABLE [sport].[Location]
        ADD CONSTRAINT [DF_Location_IsActive] DEFAULT ((1)) FOR [IsActive];

    -- Nové a přejmenované sloupce se dále používají v dynamickém SQL. SQL Server
    -- jinak zvaliduje celý batch před provedením tohoto ALTER TABLE.
    EXEC sys.sp_executesql N'
        ALTER TABLE [sport].[Training] ADD [Location_Id] int NULL;';

    ;WITH TrainingLocations AS
    (
        SELECT DISTINCT LTRIM(RTRIM([Location])) AS [Name]
        FROM [sport].[Training]
    )
    INSERT INTO [sport].[Location] ([Name], [Street], [City], [ZipCode], [IsActive], [Location])
    SELECT trainingLocation.[Name], NULL, NULL, NULL, 1, NULL
    FROM TrainingLocations AS trainingLocation
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [sport].[Location] AS sportLocation
        WHERE UPPER(LTRIM(RTRIM(sportLocation.[Name]))) = UPPER(trainingLocation.[Name])
    );

    EXEC sys.sp_executesql N'
        UPDATE training
        SET [Location_Id] = sportLocation.[Id]
        FROM [sport].[Training] AS training
        INNER JOIN [sport].[Location] AS sportLocation
            ON UPPER(LTRIM(RTRIM(sportLocation.[Name]))) = UPPER(LTRIM(RTRIM(training.[Location])));';

    DECLARE @UnmappedTrainingCount int;

    EXEC sys.sp_executesql
        N'SELECT @Count = COUNT(*)
          FROM [sport].[Training]
          WHERE [Location_Id] IS NULL;',
        N'@Count int OUTPUT',
        @Count = @UnmappedTrainingCount OUTPUT;

    IF @UnmappedTrainingCount > 0
        THROW 51000, N'Nejméně jeden trénink nelze propojit s lokalitou; transakce bude vrácena zpět.', 1;

    EXEC sys.sp_executesql N'
        ALTER TABLE [sport].[Training] ALTER COLUMN [Location_Id] int NOT NULL;
        ALTER TABLE [sport].[Training] DROP COLUMN [Location];

        ALTER TABLE [sport].[Match] WITH CHECK
            ADD CONSTRAINT [FK_Match_Location_Location_Id]
            FOREIGN KEY ([Location_Id]) REFERENCES [sport].[Location] ([Id]);

        ALTER TABLE [sport].[Team] WITH CHECK
            ADD CONSTRAINT [FK_Team_Location_HomeLocation_Id]
            FOREIGN KEY ([HomeLocation_Id]) REFERENCES [sport].[Location] ([Id]);

        ALTER TABLE [sport].[Training] WITH CHECK
            ADD CONSTRAINT [FK_Training_Location_Location_Id]
            FOREIGN KEY ([Location_Id]) REFERENCES [sport].[Location] ([Id]);';

    IF @Commit = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'Převod #16 byl potvrzen.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'Převod #16 byl úspěšně ověřen a vrácen zpět. Pro ostré provedení nastavte @Commit na 1.';
    END;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

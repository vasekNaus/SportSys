-- ============================================================
-- sport.SeasonCategory: přejmenování Name -> Code a zkrácení na varchar(10)
--
-- Mění se:
--   sport.SeasonCategory.Name                    -> Code            varchar(10)
--   sport.Training.SeasonCategory_Name           -> SeasonCategory_Code   varchar(10)
--   sport.TrainingPlan.SeasonCategory_Name       -> SeasonCategory_Code   varchar(10)
--   sport.TrainingRequirement.SeasonCategory_Name-> SeasonCategory_Code   varchar(10)
--   sport.Match.SeasonCategory_Name              -> SeasonCategory_Code   varchar(10)
-- Primární klíč PK_SeasonCategory a cizí klíče z Training, TrainingPlan
-- a TrainingRequirement se zahodí a znovu vytvoří s novými názvy sloupců.
--
-- Výchozí režim transakci vrátí zpět. Po kontrole změňte @Commit na 1
-- a spusťte skript znovu pro ostré provedení.
-- Nejde o EF Core migraci; po provedení je nutné upravit EF model.
-- Před spuštěním zálohujte databázi.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;

DECLARE @Message nvarchar(2000);
DECLARE @Sql nvarchar(max);

IF OBJECT_ID(N'[sport].[SeasonCategory]', N'U') IS NULL
    THROW 51000, N'Chybí očekávaná tabulka [sport].[SeasonCategory].', 1;

IF COL_LENGTH(N'[sport].[SeasonCategory]', N'Name') IS NULL
    OR COL_LENGTH(N'[sport].[SeasonCategory]', N'Code') IS NOT NULL
    THROW 51000, N'Schéma neodpovídá očekávanému stavu před změnou (chybí sloupec Name nebo již existuje Code).', 1;

DECLARE @Columns TABLE
(
    Id int IDENTITY(1, 1) PRIMARY KEY,
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    OldColumn sysname NOT NULL,
    NewColumn sysname NOT NULL,
    CollationName sysname NULL
);

INSERT INTO @Columns (SchemaName, TableName, OldColumn, NewColumn)
VALUES
    (N'sport', N'SeasonCategory',     N'Name',                N'Code'),
    (N'sport', N'Training',           N'SeasonCategory_Name', N'SeasonCategory_Code'),
    (N'sport', N'TrainingPlan',       N'SeasonCategory_Name', N'SeasonCategory_Code'),
    (N'sport', N'TrainingRequirement',N'SeasonCategory_Name', N'SeasonCategory_Code'),
    (N'sport', N'Match',              N'SeasonCategory_Name', N'SeasonCategory_Code');

-- ------------------------------------------------------------
-- Předběžné kontroly (nic neměnící)
-- ------------------------------------------------------------
DECLARE @Id int = 1, @MaxId int = (SELECT MAX(Id) FROM @Columns);
DECLARE @Schema sysname, @Table sysname, @Old sysname, @New sysname, @Collation sysname;
DECLARE @ObjectName nvarchar(300), @TooLong int;

WHILE @Id <= @MaxId
BEGIN
    SELECT @Schema = SchemaName, @Table = TableName, @Old = OldColumn, @New = NewColumn
    FROM @Columns WHERE Id = @Id;

    SET @ObjectName = QUOTENAME(@Schema) + N'.' + QUOTENAME(@Table);

    SELECT @Collation = c.collation_name
    FROM sys.columns AS c
    WHERE c.object_id = OBJECT_ID(@ObjectName) AND c.[name] = @Old;

    IF @Collation IS NULL
    BEGIN
        SET @Message = N'Chybí očekávaný sloupec ' + @ObjectName + N'.' + QUOTENAME(@Old) + N' (nebo není typu varchar).';
        THROW 51000, @Message, 1;
    END;

    IF COL_LENGTH(@ObjectName, @New) IS NOT NULL
    BEGIN
        SET @Message = N'Sloupec ' + @ObjectName + N'.' + QUOTENAME(@New) + N' již existuje.';
        THROW 51000, @Message, 1;
    END;

    UPDATE @Columns SET CollationName = @Collation WHERE Id = @Id;

    -- Zkrácení nesmí oříznout žádnou hodnotu.
    SET @TooLong = 0;
    SET @Sql = N'SELECT @Count = COUNT(*) FROM ' + @ObjectName +
               N' WHERE DATALENGTH(' + QUOTENAME(@Old) + N') > 10;';
    EXEC sys.sp_executesql @Sql, N'@Count int OUTPUT', @Count = @TooLong OUTPUT;

    IF @TooLong > 0
    BEGIN
        SET @Message = @ObjectName + N'.' + QUOTENAME(@Old) + N' obsahuje ' + CAST(@TooLong AS nvarchar(20)) +
                       N' hodnot delších než 10 znaků. Nejprve je opravte.';
        THROW 51000, @Message, 1;
    END;

    -- Sloupec nesmí být součástí indexu (kromě PK_SeasonCategory), CHECK ani DEFAULT constraintu,
    -- protože by bylo nutné je při změně typu ručně obnovit.
    IF EXISTS
    (
        SELECT 1
        FROM sys.index_columns AS ic
        INNER JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        INNER JOIN sys.indexes AS i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        WHERE c.object_id = OBJECT_ID(@ObjectName) AND c.[name] = @Old
            AND NOT (i.is_primary_key = 1 AND @Table = N'SeasonCategory')
    )
    OR EXISTS
    (
        SELECT 1 FROM sys.default_constraints AS dc
        INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE c.object_id = OBJECT_ID(@ObjectName) AND c.[name] = @Old
    )
    OR EXISTS
    (
        SELECT 1 FROM sys.check_constraints AS cc
        WHERE cc.parent_object_id = OBJECT_ID(@ObjectName)
            AND CHARINDEX(QUOTENAME(@Old), cc.[definition]) > 0
    )
    OR EXISTS
    (
        SELECT 1 FROM sys.computed_columns AS cc
        WHERE cc.object_id = OBJECT_ID(@ObjectName)
            AND CHARINDEX(QUOTENAME(@Old), cc.[definition]) > 0
    )
    BEGIN
        SET @Message = N'Sloupec ' + @ObjectName + N'.' + QUOTENAME(@Old) +
                       N' je použit v indexu, CHECK/DEFAULT constraintu nebo computed sloupci. Skript je nutné doplnit.';
        THROW 51000, @Message, 1;
    END;

    SET @Id += 1;
END;

-- Pohledy, procedury a funkce se po sp_rename automaticky neaktualizují.
DECLARE @BrokenModules nvarchar(max);

SELECT @BrokenModules = STRING_AGG(
    CAST(OBJECT_SCHEMA_NAME(m.object_id) + N'.' + OBJECT_NAME(m.object_id) AS nvarchar(max)), N', ')
FROM sys.sql_modules AS m
WHERE m.[definition] LIKE N'%SeasonCategory[_]Name%';

IF @BrokenModules IS NOT NULL
BEGIN
    SET @Message = N'Tyto objekty používají SeasonCategory_Name a po přejmenování by přestaly fungovat: ' +
                   LEFT(@BrokenModules, 1500) + N'. Upravte je (např. VIEW sport.SportEvent) a skript spusťte znovu.';
    THROW 51000, @Message, 1;
END;

SET @BrokenModules = NULL;

SELECT @BrokenModules = STRING_AGG(
    CAST(OBJECT_SCHEMA_NAME(d.referencing_id) + N'.' + OBJECT_NAME(d.referencing_id) AS nvarchar(max)), N', ')
FROM sys.sql_expression_dependencies AS d
WHERE d.referenced_id = OBJECT_ID(N'[sport].[SeasonCategory]')
    AND d.referencing_class = 1
    AND OBJECTPROPERTY(d.referencing_id, N'IsTable') = 0;

IF @BrokenModules IS NOT NULL
    PRINT N'VAROVÁNÍ: tyto objekty odkazují na sport.SeasonCategory, zkontrolujte použití sloupce Name: ' + @BrokenModules;

-- Očekávají se pouze FK z Training, TrainingPlan a TrainingRequirement.
IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys AS fk
    WHERE fk.referenced_object_id = OBJECT_ID(N'[sport].[SeasonCategory]')
        AND OBJECT_NAME(fk.parent_object_id) NOT IN (N'Training', N'TrainingPlan', N'TrainingRequirement')
)
    THROW 51000, N'Na sport.SeasonCategory odkazuje neočekávaný cizí klíč. Skript je nutné doplnit.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.key_constraints AS kc
    INNER JOIN sys.indexes AS i ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
    WHERE kc.parent_object_id = OBJECT_ID(N'[sport].[SeasonCategory]')
        AND kc.[name] = N'PK_SeasonCategory'
        AND kc.[type] = N'PK'
        AND i.[type] = 1
)
    THROW 51000, N'Chybí očekávaný clusterovaný primární klíč PK_SeasonCategory.', 1;

-- Cizí klíče musí mít po změně v obou tabulkách shodné kolace.
IF (SELECT COUNT(DISTINCT CollationName) FROM @Columns) > 1
    THROW 51000, N'Sloupce používají různé kolace; cizí klíče by po změně nebyly platné.', 1;

-- ------------------------------------------------------------
-- Změna schématu
-- ------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    -- FK se zjistí dynamicky, aby skript fungoval i s jiným názvem constraintu.
    DECLARE @DropForeignKeys nvarchar(max);

    SELECT @DropForeignKeys = STRING_AGG(
        CAST(
            N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) +
            N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) +
            N' DROP CONSTRAINT ' + QUOTENAME(fk.[name]) + N';' AS nvarchar(max)),
        CHAR(13) + CHAR(10))
    FROM sys.foreign_keys AS fk
    WHERE fk.referenced_object_id = OBJECT_ID(N'[sport].[SeasonCategory]');

    IF @DropForeignKeys IS NOT NULL
        EXEC sys.sp_executesql @DropForeignKeys;

    -- Zkrácení sloupce, který je součástí PK, vyžaduje PK zahodit.
    ALTER TABLE [sport].[SeasonCategory] DROP CONSTRAINT [PK_SeasonCategory];

    SET @Id = 1;

    WHILE @Id <= @MaxId
    BEGIN
        SELECT @Schema = SchemaName, @Table = TableName, @Old = OldColumn, @New = NewColumn, @Collation = CollationName
        FROM @Columns WHERE Id = @Id;

        SET @ObjectName = QUOTENAME(@Schema) + N'.' + QUOTENAME(@Table);

        SET @Sql = QUOTENAME(@Schema) + N'.' + QUOTENAME(@Table) + N'.' + QUOTENAME(@Old);
        EXEC sys.sp_rename @Sql, @New, N'COLUMN';

        -- Kolace se musí uvést explicitně, jinak by se použila kolace databáze.
        SET @Sql = N'ALTER TABLE ' + @ObjectName + N' ALTER COLUMN ' + QUOTENAME(@New) +
                   N' varchar(10) COLLATE ' + @Collation + N' NOT NULL;';
        EXEC sys.sp_executesql @Sql;

        SET @Id += 1;
    END;

    -- Nové sloupce se dále používají jen v dynamickém SQL (validace celého batche před provedením).
    EXEC sys.sp_executesql N'
        ALTER TABLE [sport].[SeasonCategory]
            ADD CONSTRAINT [PK_SeasonCategory] PRIMARY KEY CLUSTERED ([Season_Id] ASC, [Code] ASC);

        ALTER TABLE [sport].[Training] WITH CHECK
            ADD CONSTRAINT [FK_Training_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Code]
            FOREIGN KEY ([SeasonCategory_Season_Id], [SeasonCategory_Code])
            REFERENCES [sport].[SeasonCategory] ([Season_Id], [Code])
            ON UPDATE CASCADE;

        ALTER TABLE [sport].[TrainingPlan] WITH CHECK
            ADD CONSTRAINT [FK_TrainingPlan_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Code]
            FOREIGN KEY ([SeasonCategory_Season_Id], [SeasonCategory_Code])
            REFERENCES [sport].[SeasonCategory] ([Season_Id], [Code])
            ON UPDATE CASCADE;

        ALTER TABLE [sport].[TrainingRequirement] WITH CHECK
            ADD CONSTRAINT [FK_TrainingRequirement_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Code]
            FOREIGN KEY ([SeasonCategory_Season_Id], [SeasonCategory_Code])
            REFERENCES [sport].[SeasonCategory] ([Season_Id], [Code])
            ON UPDATE CASCADE;';

    -- Závěrečná kontrola výsledného stavu.
    IF EXISTS
    (
        SELECT 1
        FROM @Columns AS col
        INNER JOIN sys.columns AS c
            ON c.object_id = OBJECT_ID(QUOTENAME(col.SchemaName) + N'.' + QUOTENAME(col.TableName))
            AND c.[name] = col.NewColumn
        WHERE c.max_length <> 10 OR c.is_nullable = 1 OR c.system_type_id <> TYPE_ID(N'varchar')
    )
        THROW 51000, N'Výsledný typ některého sloupce neodpovídá varchar(10) NOT NULL.', 1;

    IF (SELECT COUNT(*) FROM sys.foreign_keys
        WHERE referenced_object_id = OBJECT_ID(N'[sport].[SeasonCategory]')
            AND is_not_trusted = 0 AND is_disabled = 0) <> 3
        THROW 51000, N'Cizí klíče na sport.SeasonCategory nejsou po změně kompletní nebo důvěryhodné.', 1;

    IF @Commit = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'Přejmenování SeasonCategory.Name -> Code bylo potvrzeno.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'Přejmenování bylo úspěšně ověřeno a vráceno zpět. Pro ostré provedení nastavte @Commit na 1.';
    END;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

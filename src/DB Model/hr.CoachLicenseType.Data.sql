SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM (VALUES
            (1, 'A'),
            (2, 'B'),
            (3, 'B_GOALKEEPER'),
            (4, 'C_PLUS_YOUTH'),
            (5, 'C_PLAYER')
        ) AS expected(Id, Code)
        INNER JOIN [hr].[CoachLicenseType] AS actual ON actual.[Id] = expected.Id
        WHERE actual.[Code] <> expected.Code
    )
        THROW 50001, 'Kolize ID v číselníku hr.CoachLicenseType.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM (VALUES
            (1, 'A'),
            (2, 'B'),
            (3, 'B_GOALKEEPER'),
            (4, 'C_PLUS_YOUTH'),
            (5, 'C_PLAYER')
        ) AS expected(Id, Code)
        INNER JOIN [hr].[CoachLicenseType] AS actual ON actual.[Code] = expected.Code
        WHERE actual.[Id] <> expected.Id
    )
        THROW 50002, 'Kolize kódu v číselníku hr.CoachLicenseType.', 1;

    INSERT INTO [hr].[CoachLicenseType] ([Id], [Code], [Name], [IsActive])
    SELECT expected.Id, expected.Code, expected.Name, CAST(1 AS bit)
    FROM (VALUES
        (1, 'A', N'Licence A'),
        (2, 'B', N'Licence B'),
        (3, 'B_GOALKEEPER', N'Licence B - brankář'),
        (4, 'C_PLUS_YOUTH', N'Licence C+ mládež'),
        (5, 'C_PLAYER', N'Licence C hráč')
    ) AS expected(Id, Code, Name)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [hr].[CoachLicenseType] AS actual
        WHERE actual.[Id] = expected.Id
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

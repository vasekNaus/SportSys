ALTER TABLE [sport].[TrainingPlan] ADD [Location_Id] int NULL;
GO

UPDATE training
        SET [Location_Id] = sportLocation.[Id]
        FROM [sport].[TrainingPlan] AS training
        INNER JOIN [sport].[Location] AS sportLocation
            ON UPPER(LTRIM(RTRIM(sportLocation.[Name]))) = UPPER(LTRIM(RTRIM(training.[Location])));
GO
UPDATE sport.TrainingPlan SET Location_Id = 1248 WHERE Location_Id IS NULL
GO
ALTER TABLE [sport].[TrainingPlan] ALTER COLUMN [Location_Id] int NOT NULL
GO
ALTER TABLE [sport].[TrainingPlan] WITH CHECK ADD CONSTRAINT [FK_TrainingPlan_Location_Location_Id]
            FOREIGN KEY ([Location_Id]) REFERENCES [sport].[Location] ([Id]);
GO
ALTER TABLE [sport].[TrainingPlan] DROP COLUMN [Location];
GO

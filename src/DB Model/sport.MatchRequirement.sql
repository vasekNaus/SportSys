-- Evidence požadovaného počtu zápasů (#25).
-- Vytváří sport.MatchRequirement a vazební tabulku sport.CoachMatchRequirement.
-- Předpoklad: sport.SeasonCategory již používá sloupec Code
-- (viz Migration/17_01_seasoncategory_name_to_code.sql).
-- Nejde o EF Core migraci; EF migraci vytváří a aplikuje uživatel.

CREATE TABLE [sport].[MatchRequirement](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SeasonCategory_Season_Id] [int] NOT NULL,
	[SeasonCategory_Code] [varchar](10) NOT NULL,
	[From] [date] NOT NULL,
	[To] [date] NOT NULL,
	[MatchCount] [int] NOT NULL,
 CONSTRAINT [PK_MatchRequirement] PRIMARY KEY CLUSTERED
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [sport].[MatchRequirement]  WITH CHECK ADD  CONSTRAINT [FK_MatchRequirement_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Code] FOREIGN KEY([SeasonCategory_Season_Id], [SeasonCategory_Code])
REFERENCES [sport].[SeasonCategory] ([Season_Id], [Code])
ON UPDATE CASCADE
GO

ALTER TABLE [sport].[MatchRequirement] CHECK CONSTRAINT [FK_MatchRequirement_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Code]
GO

CREATE NONCLUSTERED INDEX [IX_MatchRequirement_SeasonCategory_Period] ON [sport].[MatchRequirement]
(
	[SeasonCategory_Season_Id] ASC,
	[SeasonCategory_Code] ASC,
	[From] ASC,
	[To] ASC
) ON [PRIMARY]
GO

CREATE TABLE [sport].[CoachMatchRequirement](
	[Coach_Id] [int] NOT NULL,
	[MatchRequirement_Id] [int] NOT NULL,
	[CoachRole_Id] [int] NOT NULL,
 CONSTRAINT [PK_CoachMatchRequirement] PRIMARY KEY CLUSTERED
(
	[Coach_Id] ASC,
	[MatchRequirement_Id] ASC,
	[CoachRole_Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [sport].[CoachMatchRequirement]  WITH CHECK ADD  CONSTRAINT [FK_CoachMatchRequirement_Coach_Coach_Id] FOREIGN KEY([Coach_Id])
REFERENCES [hr].[Coach] ([Id])
GO

ALTER TABLE [sport].[CoachMatchRequirement] CHECK CONSTRAINT [FK_CoachMatchRequirement_Coach_Coach_Id]
GO

ALTER TABLE [sport].[CoachMatchRequirement]  WITH CHECK ADD  CONSTRAINT [FK_CoachMatchRequirement_CoachRole_CoachRole_Id] FOREIGN KEY([CoachRole_Id])
REFERENCES [dbo].[CoachRole] ([Id])
GO

ALTER TABLE [sport].[CoachMatchRequirement] CHECK CONSTRAINT [FK_CoachMatchRequirement_CoachRole_CoachRole_Id]
GO

ALTER TABLE [sport].[CoachMatchRequirement]  WITH CHECK ADD  CONSTRAINT [FK_CoachMatchRequirement_MatchRequirement_MatchRequirement_Id] FOREIGN KEY([MatchRequirement_Id])
REFERENCES [sport].[MatchRequirement] ([Id])
GO

ALTER TABLE [sport].[CoachMatchRequirement] CHECK CONSTRAINT [FK_CoachMatchRequirement_MatchRequirement_MatchRequirement_Id]
GO

CREATE NONCLUSTERED INDEX [IX_CoachMatchRequirement_MatchRequirement] ON [sport].[CoachMatchRequirement]
(
	[MatchRequirement_Id] ASC
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_CoachMatchRequirement_CoachRole] ON [sport].[CoachMatchRequirement]
(
	[CoachRole_Id] ASC
) ON [PRIMARY]
GO

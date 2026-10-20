USE [SportSys]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CoachRole]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CoachRole](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_CoachRole] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Manufacturer]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Manufacturer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Website] [nvarchar](500) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Manufacturer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [hr].[Coach]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [hr].[Coach](
	[PersonalNumber] [varchar](20) NOT NULL,
	[Photo] [varbinary](max) NULL,
	[PhotoContentType] [varchar](100) NULL,
	[PhotoFileName] [nvarchar](255) NULL,
	[Id] [int] NOT NULL,
	[IdentificationNumber] [varchar](10) NOT NULL,
 CONSTRAINT [PK_Coach] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [hr].[CoachAttendance]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [hr].[CoachAttendance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Coach_Id] [int] NOT NULL,
	[PeriodYear] [int] NOT NULL,
	[PeriodMonth] [tinyint] NOT NULL,
	[FileName] [nvarchar](255) NOT NULL,
	[ContentType] [varchar](100) NOT NULL,
	[FileContent] [varbinary](max) NOT NULL,
	[UploadedAt] [datetime2](0) NOT NULL,
	[UserUpload_Id] [int] NOT NULL,
 CONSTRAINT [PK_CoachAttendance] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [hr].[CoachContract]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [hr].[CoachContract](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Coach_Id] [int] NOT NULL,
	[Season_Id] [int] NOT NULL,
	[ContractType] [tinyint] NOT NULL,
	[RewardAmount] [decimal](18, 2) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_CoachContract] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [hr].[CoachLicense]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [hr].[CoachLicense](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Coach_Id] [int] NOT NULL,
	[CoachLicenseType_Id] [int] NOT NULL,
	[ValidFrom] [date] NOT NULL,
	[ValidTo] [date] NULL,
 CONSTRAINT [PK_CoachLicense] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [hr].[CoachLicenseType]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [hr].[CoachLicenseType](
	[Id] [int] NOT NULL,
	[Code] [varchar](30) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_CoachLicenseType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [hr].[CoachSetting]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [hr].[CoachSetting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Coach_Id] [int] NOT NULL,
	[ValidFrom] [date] NOT NULL,
	[ValidTo] [date] NULL,
	[BankAccountPrefix] [varchar](6) NULL,
	[BankAccountNumber] [varchar](10) NOT NULL,
	[BankCode] [varchar](4) NOT NULL,
	[Street] [nvarchar](200) NOT NULL,
	[City] [nvarchar](100) NOT NULL,
	[ZipCode] [varchar](10) NOT NULL,
	[HealthInsuranceCode] [varchar](3) NOT NULL,
 CONSTRAINT [PK_CoachSetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [identity].[Role]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [identity].[Role](
	[Id] [int] NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [identity].[RoleClaim]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [identity].[RoleClaim](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Role_Id] [int] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_RoleClaim] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [identity].[User]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [identity].[User](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntraOid] [nvarchar](max) NULL,
	[EntraTenantId] [nvarchar](max) NULL,
	[DisplayName] [nvarchar](max) NULL,
	[IsLocalAccount] [bit] NOT NULL,
	[LastLoginUtc] [datetime2](7) NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [identity].[UserClaim]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [identity].[UserClaim](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[User_Id] [int] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_UserClaim] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [identity].[UserLogin]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [identity].[UserLogin](
	[LoginProvider] [nvarchar](450) NOT NULL,
	[ProviderKey] [nvarchar](450) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[User_Id] [int] NOT NULL,
 CONSTRAINT [PK_UserLogin] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [identity].[UserRole]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [identity].[UserRole](
	[User_Id] [int] NOT NULL,
	[Role_Id] [int] NOT NULL,
 CONSTRAINT [PK_UserRole] PRIMARY KEY CLUSTERED 
(
	[User_Id] ASC,
	[Role_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [identity].[UserToken]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [identity].[UserToken](
	[User_Id] [int] NOT NULL,
	[LoginProvider] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_UserToken] PRIMARY KEY CLUSTERED 
(
	[User_Id] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [inventory].[Asset]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[Asset](
	[Id] [int] NOT NULL,
	[InventoryNumber] [varchar](20) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[CategoryId] [int] NOT NULL,
	[ManufacturerId] [int] NULL,
	[AssignedLocationId] [int] NULL,
	[CurrentLocationId] [int] NULL,
	[ItemStatus] [int] NOT NULL,
	[AcquisitionDate] [date] NULL,
	[AcquisitionPrice] [decimal](10, 2) NULL,
	[QRCodeValue] [varchar](500) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[CreatedByUserId] [int] NULL,
	[ModifiedAt] [datetime2](7) NULL,
	[ModifiedByUserId] [int] NULL,
	[SerialNumber] [nvarchar](100) NULL,
	[WarrantyUntil] [date] NULL,
	[ExternalId] [nvarchar](100) NULL,
 CONSTRAINT [PK_Asset] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [inventory].[Category]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[Category](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ParentCategory_Id] [int] NULL,
	[Name] [nvarchar](100) NOT NULL,
	[CategoryKindJson] [nvarchar](max) NULL,
	[SortOrder] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Category] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [inventory].[Equipment]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[Equipment](
	[Id] [int] NOT NULL,
	[InventoryNumber] [varchar](20) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[CategoryId] [int] NOT NULL,
	[ManufacturerId] [int] NULL,
	[AssignedLocationId] [int] NULL,
	[CurrentLocationId] [int] NULL,
	[ItemStatus] [int] NOT NULL,
	[AcquisitionDate] [date] NULL,
	[AcquisitionPrice] [decimal](10, 2) NULL,
	[QRCodeValue] [varchar](500) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[CreatedByUserId] [int] NULL,
	[ModifiedAt] [datetime2](7) NULL,
	[ModifiedByUserId] [int] NULL,
	[Size] [nvarchar](50) NULL,
	[ItemKind_Id] [int] NULL,
 CONSTRAINT [PK_Equipment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [inventory].[InventoryCheck]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[InventoryCheck](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InventorySession_Id] [int] NOT NULL,
	[InventoryItemId] [int] NOT NULL,
	[CheckedAt] [datetime2](7) NOT NULL,
	[CheckedByUser_Id] [int] NULL,
	[Found] [bit] NOT NULL,
	[ActualLocation_Id] [int] NULL,
	[Note] [nvarchar](500) NULL,
 CONSTRAINT [PK_InventoryCheck] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[InventoryItemPurchase]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[InventoryItemPurchase](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InventoryItemId] [int] NOT NULL,
	[PurchaseDocument_Id] [int] NOT NULL,
	[PurchasePrice] [decimal](10, 2) NOT NULL,
 CONSTRAINT [PK_InventoryItemPurchase] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[InventorySession]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[InventorySession](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[StartedAt] [datetime2](7) NOT NULL,
	[FinishedAt] [datetime2](7) NULL,
	[IsClosed] [bit] NOT NULL,
 CONSTRAINT [PK_InventorySession] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[InventoryTransaction]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[InventoryTransaction](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InventoryItemId] [int] NOT NULL,
	[TransactionType_Id] [int] NOT NULL,
	[TransactionDate] [datetime2](7) NOT NULL,
	[Quantity] [int] NOT NULL,
	[User_Id] [int] NULL,
	[Note] [nvarchar](500) NULL,
 CONSTRAINT [PK_InventoryTransaction] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[ItemKind]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[ItemKind](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ItemKind] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[ItemLocationHistory]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[ItemLocationHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InventoryItemId] [int] NOT NULL,
	[PreviousLocation_Id] [int] NULL,
	[NewLocation_Id] [int] NOT NULL,
	[ChangedAt] [datetime2](7) NOT NULL,
	[ChangedByUser_Id] [int] NULL,
	[Note] [nvarchar](500) NULL,
 CONSTRAINT [PK_ItemLocationHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[Loan]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[Loan](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InventoryItemId] [int] NOT NULL,
	[Member_Id] [int] NOT NULL,
	[LoanDate] [date] NOT NULL,
	[ExpectedReturnDate] [date] NULL,
	[ReturnedDate] [date] NULL,
	[Note] [nvarchar](500) NULL,
	[IsClosed] [bit] NOT NULL,
 CONSTRAINT [PK_Loan] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[Location]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[Location](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Location] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[PurchaseDocument]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[PurchaseDocument](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DocumentNumber] [nvarchar](100) NOT NULL,
	[SupplierName] [nvarchar](200) NOT NULL,
	[PurchaseDate] [date] NOT NULL,
	[TotalAmount] [decimal](10, 2) NOT NULL,
	[Note] [nvarchar](500) NULL,
 CONSTRAINT [PK_PurchaseDocument] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [inventory].[TransactionType]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [inventory].[TransactionType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_TransactionType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[CoachTraining]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[CoachTraining](
	[Coach_Id] [int] NOT NULL,
	[Training_Id] [int] NOT NULL,
	[ParticipationType_Id] [int] NOT NULL,
	[Note] [varchar](50) NOT NULL,
 CONSTRAINT [PK_CoachTraining] PRIMARY KEY CLUSTERED 
(
	[Coach_Id] ASC,
	[Training_Id] ASC,
	[ParticipationType_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[CoachTrainingPlan]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[CoachTrainingPlan](
	[Coach_Id] [int] NOT NULL,
	[TrainingPlan_Id] [int] NOT NULL,
	[ValidFrom] [date] NOT NULL,
	[ValidTo] [date] NOT NULL,
 CONSTRAINT [PK_CoachTrainingPlan] PRIMARY KEY CLUSTERED 
(
	[Coach_Id] ASC,
	[TrainingPlan_Id] ASC,
	[ValidFrom] ASC,
	[ValidTo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[CoachTrainingRequirement]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[CoachTrainingRequirement](
	[Coach_Id] [int] NOT NULL,
	[TrainingRequirement_Id] [int] NOT NULL,
	[CoachRole_Id] [int] NOT NULL,
 CONSTRAINT [PK_CoachTrainingRequirement] PRIMARY KEY CLUSTERED 
(
	[Coach_Id] ASC,
	[TrainingRequirement_Id] ASC,
	[CoachRole_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[Location]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[Location](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Street] [nvarchar](200) NULL,
	[City] [nvarchar](100) NULL,
	[Location] [geography] NULL,
	[ZipCode] [nvarchar](100) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_IceRink] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [sport].[Match]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[Match](
	[Id] [int] NOT NULL,
	[SeasonCategory_Season_Id] [int] NOT NULL,
	[SeasonCategory_Name] [varchar](10) NOT NULL,
	[Location_Id] [int] NOT NULL,
	[Date] [date] NOT NULL,
	[TimeFrom] [time](0) NOT NULL,
	[Note] [varchar](50) NOT NULL,
	[MatchCode] [varchar](10) NULL,
	[HomeTeam_Id] [int] NOT NULL,
	[MatchType_Id] [int] NOT NULL,
	[AwayTeam_Id] [int] NOT NULL,
	[Result] [json] NULL,
	[TimeTo] [time](0) NOT NULL,
	[DurationMinutes]  AS (datediff(minute,[TimeFrom],[TimeTo])) PERSISTED,
	[MatchState_Id] [int] NULL,
 CONSTRAINT [PK_Match] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[MatchState]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[MatchState](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_MatchState] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[MatchType]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[MatchType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_MatchType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[ParticipationType]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[ParticipationType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ParticipationType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[Season]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[Season](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[From] [date] NOT NULL,
	[To] [date] NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Season] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[SeasonCategory]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[SeasonCategory](
	[Season_Id] [int] NOT NULL,
	[Name] [varchar](20) NOT NULL,
	[Order] [int] NOT NULL,
	[BirthYears] [nvarchar](4000) NOT NULL,
	[CompetitionCode] [varchar](10) NOT NULL,
	[CompetitionTeamName] [nvarchar](100) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_SeasonCategory] PRIMARY KEY CLUSTERED 
(
	[Season_Id] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[Team]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[Team](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Address] [nvarchar](200) NOT NULL,
	[City] [nvarchar](100) NOT NULL,
	[HomeLocation_Id] [int] NULL,
	[Code] [nvarchar](5) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Opponent] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[Training]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[Training](
	[Id] [int] NOT NULL,
	[SeasonCategory_Season_Id] [int] NOT NULL,
	[SeasonCategory_Name] [varchar](20) NOT NULL,
	[TrainingType_Id] [int] NOT NULL,
	[TrainingPhase_Id] [int] NOT NULL,
	[TrainingState_Id] [int] NOT NULL,
	[TrainingPlan_Id] [int] NULL,
	[TimeFrom] [time](0) NOT NULL,
	[TimeTo] [time](0) NOT NULL,
	[Date] [date] NOT NULL,
	[DurationMinutes]  AS (datediff(minute,[TimeFrom],[TimeTo])) PERSISTED,
	[Note] [varchar](50) NOT NULL,
	[Location_Id] [int] NOT NULL,
 CONSTRAINT [PK_Training] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[TrainingGroup]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[TrainingGroup](
	[GroupId] [uniqueidentifier] NOT NULL,
	[Training_Id] [int] NOT NULL,
 CONSTRAINT [PK_TrainingGroup] PRIMARY KEY CLUSTERED 
(
	[GroupId] ASC,
	[Training_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[TrainingPhase]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[TrainingPhase](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_TrainingPhase] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[TrainingPlan]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[TrainingPlan](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SeasonCategory_Season_Id] [int] NOT NULL,
	[SeasonCategory_Name] [varchar](20) NOT NULL,
	[TrainingType_Id] [int] NOT NULL,
	[TrainingPhase_Id] [int] NOT NULL,
	[From] [date] NOT NULL,
	[To] [date] NOT NULL,
	[TimeFrom] [time](0) NOT NULL,
	[TimeTo] [time](0) NOT NULL,
	[DurationMinutes]  AS (datediff(minute,[TimeFrom],[TimeTo])) PERSISTED,
	[DayName] [varchar](10) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[Location_Id] [int] NOT NULL,
 CONSTRAINT [PK_TrainingPlan] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[TrainingPlanGroup]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[TrainingPlanGroup](
	[GroupId] [uniqueidentifier] NOT NULL,
	[TrainingPlan_Id] [int] NOT NULL,
 CONSTRAINT [PK_TrainingPlanGroup] PRIMARY KEY CLUSTERED 
(
	[GroupId] ASC,
	[TrainingPlan_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[TrainingRequirement]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[TrainingRequirement](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SeasonCategory_Season_Id] [int] NOT NULL,
	[SeasonCategory_Name] [varchar](20) NOT NULL,
	[TrainingType_Id] [int] NOT NULL,
	[TrainingPhase_Id] [int] NOT NULL,
	[From] [date] NOT NULL,
	[To] [date] NOT NULL,
	[DurationHours] [decimal](5, 2) NOT NULL,
 CONSTRAINT [PK_TrainingRequirement] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[TrainingState]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[TrainingState](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_TrainingState] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [sport].[TrainingType]    Script Date: 08.10.2026 20:33:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [sport].[TrainingType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_TrainingType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [hr].[Coach] ADD  DEFAULT ('') FOR [PersonalNumber]
GO
ALTER TABLE [hr].[Coach] ADD  DEFAULT ((0)) FOR [Id]
GO
ALTER TABLE [hr].[Coach] ADD  DEFAULT ('') FOR [IdentificationNumber]
GO
ALTER TABLE [hr].[CoachContract] ADD  CONSTRAINT [DF_CoachContract_IsActive]  DEFAULT (CONVERT([bit],(1))) FOR [IsActive]
GO
ALTER TABLE [inventory].[Asset] ADD  DEFAULT (NEXT VALUE FOR [inventory].[InventoryItemSeq]) FOR [Id]
GO
ALTER TABLE [inventory].[Equipment] ADD  DEFAULT (NEXT VALUE FOR [inventory].[InventoryItemSeq]) FOR [Id]
GO
ALTER TABLE [sport].[CoachTraining] ADD  CONSTRAINT [DF_CoachTraining_Note]  DEFAULT ('') FOR [Note]
GO
ALTER TABLE [sport].[Location] ADD  CONSTRAINT [DF_Location_ZipCode]  DEFAULT (N'') FOR [ZipCode]
GO
ALTER TABLE [sport].[Location] ADD  CONSTRAINT [DF_Location_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [sport].[Match] ADD  DEFAULT (NEXT VALUE FOR [sport].[SportEventSeq]) FOR [Id]
GO
ALTER TABLE [sport].[Match] ADD  DEFAULT ((0)) FOR [AwayTeam_Id]
GO
ALTER TABLE [sport].[Match] ADD  DEFAULT ('00:00:00') FOR [TimeTo]
GO
ALTER TABLE [sport].[Match] ADD  DEFAULT ((1)) FOR [MatchState_Id]
GO
ALTER TABLE [sport].[Season] ADD  CONSTRAINT [DF_Season_IsActive]  DEFAULT (CONVERT([bit],(1))) FOR [IsActive]
GO
ALTER TABLE [sport].[SeasonCategory] ADD  CONSTRAINT [DF_SeasonCategory_BirthYears]  DEFAULT (N'[]') FOR [BirthYears]
GO
ALTER TABLE [sport].[SeasonCategory] ADD  CONSTRAINT [DF_SeasonCategory_CompetitionCode]  DEFAULT ('') FOR [CompetitionCode]
GO
ALTER TABLE [sport].[SeasonCategory] ADD  CONSTRAINT [DF_SeasonCategory_CompetitionTeamName]  DEFAULT (N'') FOR [CompetitionTeamName]
GO
ALTER TABLE [sport].[SeasonCategory] ADD  CONSTRAINT [DF_SeasonCategory_IsActive]  DEFAULT (CONVERT([bit],(1))) FOR [IsActive]
GO
ALTER TABLE [sport].[Team] ADD  DEFAULT (N'') FOR [Code]
GO
ALTER TABLE [sport].[Team] ADD  CONSTRAINT [DF_Team_IsActive]  DEFAULT (CONVERT([bit],(1))) FOR [IsActive]
GO
ALTER TABLE [sport].[Training] ADD  CONSTRAINT [DF__Training__Id__6EC0713C]  DEFAULT (NEXT VALUE FOR [sport].[SportEventSeq]) FOR [Id]
GO
ALTER TABLE [sport].[TrainingPlan] ADD  DEFAULT (N'') FOR [Title]
GO
ALTER TABLE [hr].[Coach]  WITH CHECK ADD  CONSTRAINT [FK_Coach_User_Id] FOREIGN KEY([Id])
REFERENCES [identity].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [hr].[Coach] CHECK CONSTRAINT [FK_Coach_User_Id]
GO
ALTER TABLE [hr].[CoachAttendance]  WITH CHECK ADD  CONSTRAINT [FK_CoachAttendance_User_UserUpload_Id] FOREIGN KEY([UserUpload_Id])
REFERENCES [identity].[User] ([Id])
GO
ALTER TABLE [hr].[CoachAttendance] CHECK CONSTRAINT [FK_CoachAttendance_User_UserUpload_Id]
GO
ALTER TABLE [hr].[CoachContract]  WITH CHECK ADD  CONSTRAINT [FK_CoachContract_Coach_Coach_Id] FOREIGN KEY([Coach_Id])
REFERENCES [hr].[Coach] ([Id])
GO
ALTER TABLE [hr].[CoachContract] CHECK CONSTRAINT [FK_CoachContract_Coach_Coach_Id]
GO
ALTER TABLE [hr].[CoachContract]  WITH CHECK ADD  CONSTRAINT [FK_CoachContract_Season_Season_Id] FOREIGN KEY([Season_Id])
REFERENCES [sport].[Season] ([Id])
GO
ALTER TABLE [hr].[CoachContract] CHECK CONSTRAINT [FK_CoachContract_Season_Season_Id]
GO
ALTER TABLE [hr].[CoachLicense]  WITH CHECK ADD  CONSTRAINT [FK_CoachLicense_Coach_Coach_Id] FOREIGN KEY([Coach_Id])
REFERENCES [hr].[Coach] ([Id])
GO
ALTER TABLE [hr].[CoachLicense] CHECK CONSTRAINT [FK_CoachLicense_Coach_Coach_Id]
GO
ALTER TABLE [hr].[CoachLicense]  WITH CHECK ADD  CONSTRAINT [FK_CoachLicense_CoachLicenseType_CoachLicenseType_Id] FOREIGN KEY([CoachLicenseType_Id])
REFERENCES [hr].[CoachLicenseType] ([Id])
GO
ALTER TABLE [hr].[CoachLicense] CHECK CONSTRAINT [FK_CoachLicense_CoachLicenseType_CoachLicenseType_Id]
GO
ALTER TABLE [hr].[CoachSetting]  WITH CHECK ADD  CONSTRAINT [FK_CoachSetting_Coach_Coach_Id] FOREIGN KEY([Coach_Id])
REFERENCES [hr].[Coach] ([Id])
GO
ALTER TABLE [hr].[CoachSetting] CHECK CONSTRAINT [FK_CoachSetting_Coach_Coach_Id]
GO
ALTER TABLE [identity].[RoleClaim]  WITH CHECK ADD  CONSTRAINT [FK_RoleClaim_Role_Role_Id] FOREIGN KEY([Role_Id])
REFERENCES [identity].[Role] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [identity].[RoleClaim] CHECK CONSTRAINT [FK_RoleClaim_Role_Role_Id]
GO
ALTER TABLE [identity].[UserClaim]  WITH CHECK ADD  CONSTRAINT [FK_UserClaim_User_User_Id] FOREIGN KEY([User_Id])
REFERENCES [identity].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [identity].[UserClaim] CHECK CONSTRAINT [FK_UserClaim_User_User_Id]
GO
ALTER TABLE [identity].[UserLogin]  WITH CHECK ADD  CONSTRAINT [FK_UserLogin_User_User_Id] FOREIGN KEY([User_Id])
REFERENCES [identity].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [identity].[UserLogin] CHECK CONSTRAINT [FK_UserLogin_User_User_Id]
GO
ALTER TABLE [identity].[UserRole]  WITH CHECK ADD  CONSTRAINT [FK_UserRole_Role_Role_Id] FOREIGN KEY([Role_Id])
REFERENCES [identity].[Role] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [identity].[UserRole] CHECK CONSTRAINT [FK_UserRole_Role_Role_Id]
GO
ALTER TABLE [identity].[UserRole]  WITH CHECK ADD  CONSTRAINT [FK_UserRole_User_User_Id] FOREIGN KEY([User_Id])
REFERENCES [identity].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [identity].[UserRole] CHECK CONSTRAINT [FK_UserRole_User_User_Id]
GO
ALTER TABLE [identity].[UserToken]  WITH CHECK ADD  CONSTRAINT [FK_UserToken_User_User_Id] FOREIGN KEY([User_Id])
REFERENCES [identity].[User] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [identity].[UserToken] CHECK CONSTRAINT [FK_UserToken_User_User_Id]
GO
ALTER TABLE [inventory].[Asset]  WITH CHECK ADD  CONSTRAINT [FK_Asset_Category_CategoryId] FOREIGN KEY([CategoryId])
REFERENCES [inventory].[Category] ([Id])
GO
ALTER TABLE [inventory].[Asset] CHECK CONSTRAINT [FK_Asset_Category_CategoryId]
GO
ALTER TABLE [inventory].[Asset]  WITH CHECK ADD  CONSTRAINT [FK_Asset_Location_AssignedLocationId] FOREIGN KEY([AssignedLocationId])
REFERENCES [inventory].[Location] ([Id])
GO
ALTER TABLE [inventory].[Asset] CHECK CONSTRAINT [FK_Asset_Location_AssignedLocationId]
GO
ALTER TABLE [inventory].[Asset]  WITH CHECK ADD  CONSTRAINT [FK_Asset_Location_CurrentLocationId] FOREIGN KEY([CurrentLocationId])
REFERENCES [inventory].[Location] ([Id])
GO
ALTER TABLE [inventory].[Asset] CHECK CONSTRAINT [FK_Asset_Location_CurrentLocationId]
GO
ALTER TABLE [inventory].[Asset]  WITH CHECK ADD  CONSTRAINT [FK_Asset_Manufacturer_ManufacturerId] FOREIGN KEY([ManufacturerId])
REFERENCES [dbo].[Manufacturer] ([Id])
GO
ALTER TABLE [inventory].[Asset] CHECK CONSTRAINT [FK_Asset_Manufacturer_ManufacturerId]
GO
ALTER TABLE [inventory].[Category]  WITH CHECK ADD  CONSTRAINT [FK_Category_Category_ParentCategory_Id] FOREIGN KEY([ParentCategory_Id])
REFERENCES [inventory].[Category] ([Id])
GO
ALTER TABLE [inventory].[Category] CHECK CONSTRAINT [FK_Category_Category_ParentCategory_Id]
GO
ALTER TABLE [inventory].[Equipment]  WITH CHECK ADD  CONSTRAINT [FK_Equipment_Category_CategoryId] FOREIGN KEY([CategoryId])
REFERENCES [inventory].[Category] ([Id])
GO
ALTER TABLE [inventory].[Equipment] CHECK CONSTRAINT [FK_Equipment_Category_CategoryId]
GO
ALTER TABLE [inventory].[Equipment]  WITH CHECK ADD  CONSTRAINT [FK_Equipment_ItemKind_ItemKind_Id] FOREIGN KEY([ItemKind_Id])
REFERENCES [inventory].[ItemKind] ([Id])
GO
ALTER TABLE [inventory].[Equipment] CHECK CONSTRAINT [FK_Equipment_ItemKind_ItemKind_Id]
GO
ALTER TABLE [inventory].[Equipment]  WITH CHECK ADD  CONSTRAINT [FK_Equipment_Location_AssignedLocationId] FOREIGN KEY([AssignedLocationId])
REFERENCES [inventory].[Location] ([Id])
GO
ALTER TABLE [inventory].[Equipment] CHECK CONSTRAINT [FK_Equipment_Location_AssignedLocationId]
GO
ALTER TABLE [inventory].[Equipment]  WITH CHECK ADD  CONSTRAINT [FK_Equipment_Location_CurrentLocationId] FOREIGN KEY([CurrentLocationId])
REFERENCES [inventory].[Location] ([Id])
GO
ALTER TABLE [inventory].[Equipment] CHECK CONSTRAINT [FK_Equipment_Location_CurrentLocationId]
GO
ALTER TABLE [inventory].[Equipment]  WITH CHECK ADD  CONSTRAINT [FK_Equipment_Manufacturer_ManufacturerId] FOREIGN KEY([ManufacturerId])
REFERENCES [dbo].[Manufacturer] ([Id])
GO
ALTER TABLE [inventory].[Equipment] CHECK CONSTRAINT [FK_Equipment_Manufacturer_ManufacturerId]
GO
ALTER TABLE [inventory].[InventoryCheck]  WITH CHECK ADD  CONSTRAINT [FK_InventoryCheck_InventorySession_InventorySession_Id] FOREIGN KEY([InventorySession_Id])
REFERENCES [inventory].[InventorySession] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [inventory].[InventoryCheck] CHECK CONSTRAINT [FK_InventoryCheck_InventorySession_InventorySession_Id]
GO
ALTER TABLE [inventory].[InventoryCheck]  WITH CHECK ADD  CONSTRAINT [FK_InventoryCheck_Location_ActualLocation_Id] FOREIGN KEY([ActualLocation_Id])
REFERENCES [inventory].[Location] ([Id])
GO
ALTER TABLE [inventory].[InventoryCheck] CHECK CONSTRAINT [FK_InventoryCheck_Location_ActualLocation_Id]
GO
ALTER TABLE [inventory].[InventoryCheck]  WITH CHECK ADD  CONSTRAINT [FK_InventoryCheck_User_CheckedByUser_Id] FOREIGN KEY([CheckedByUser_Id])
REFERENCES [identity].[User] ([Id])
GO
ALTER TABLE [inventory].[InventoryCheck] CHECK CONSTRAINT [FK_InventoryCheck_User_CheckedByUser_Id]
GO
ALTER TABLE [inventory].[InventoryItemPurchase]  WITH CHECK ADD  CONSTRAINT [FK_InventoryItemPurchase_PurchaseDocument_PurchaseDocument_Id] FOREIGN KEY([PurchaseDocument_Id])
REFERENCES [inventory].[PurchaseDocument] ([Id])
GO
ALTER TABLE [inventory].[InventoryItemPurchase] CHECK CONSTRAINT [FK_InventoryItemPurchase_PurchaseDocument_PurchaseDocument_Id]
GO
ALTER TABLE [inventory].[InventoryTransaction]  WITH CHECK ADD  CONSTRAINT [FK_InventoryTransaction_TransactionType_TransactionType_Id] FOREIGN KEY([TransactionType_Id])
REFERENCES [inventory].[TransactionType] ([Id])
GO
ALTER TABLE [inventory].[InventoryTransaction] CHECK CONSTRAINT [FK_InventoryTransaction_TransactionType_TransactionType_Id]
GO
ALTER TABLE [inventory].[InventoryTransaction]  WITH CHECK ADD  CONSTRAINT [FK_InventoryTransaction_User_User_Id] FOREIGN KEY([User_Id])
REFERENCES [identity].[User] ([Id])
GO
ALTER TABLE [inventory].[InventoryTransaction] CHECK CONSTRAINT [FK_InventoryTransaction_User_User_Id]
GO
ALTER TABLE [inventory].[ItemLocationHistory]  WITH CHECK ADD  CONSTRAINT [FK_ItemLocationHistory_Location_NewLocation_Id] FOREIGN KEY([NewLocation_Id])
REFERENCES [inventory].[Location] ([Id])
GO
ALTER TABLE [inventory].[ItemLocationHistory] CHECK CONSTRAINT [FK_ItemLocationHistory_Location_NewLocation_Id]
GO
ALTER TABLE [inventory].[ItemLocationHistory]  WITH CHECK ADD  CONSTRAINT [FK_ItemLocationHistory_Location_PreviousLocation_Id] FOREIGN KEY([PreviousLocation_Id])
REFERENCES [inventory].[Location] ([Id])
GO
ALTER TABLE [inventory].[ItemLocationHistory] CHECK CONSTRAINT [FK_ItemLocationHistory_Location_PreviousLocation_Id]
GO
ALTER TABLE [inventory].[ItemLocationHistory]  WITH CHECK ADD  CONSTRAINT [FK_ItemLocationHistory_User_ChangedByUser_Id] FOREIGN KEY([ChangedByUser_Id])
REFERENCES [identity].[User] ([Id])
GO
ALTER TABLE [inventory].[ItemLocationHistory] CHECK CONSTRAINT [FK_ItemLocationHistory_User_ChangedByUser_Id]
GO
ALTER TABLE [inventory].[Loan]  WITH CHECK ADD  CONSTRAINT [FK_Loan_User_Member_Id] FOREIGN KEY([Member_Id])
REFERENCES [identity].[User] ([Id])
GO
ALTER TABLE [inventory].[Loan] CHECK CONSTRAINT [FK_Loan_User_Member_Id]
GO
ALTER TABLE [sport].[CoachTraining]  WITH CHECK ADD  CONSTRAINT [FK_CoachTraining_Coach_Coach_Id] FOREIGN KEY([Coach_Id])
REFERENCES [hr].[Coach] ([Id])
GO
ALTER TABLE [sport].[CoachTraining] CHECK CONSTRAINT [FK_CoachTraining_Coach_Coach_Id]
GO
ALTER TABLE [sport].[CoachTraining]  WITH CHECK ADD  CONSTRAINT [FK_CoachTraining_ParticipationType_ParticipationType_Id] FOREIGN KEY([ParticipationType_Id])
REFERENCES [sport].[ParticipationType] ([Id])
GO
ALTER TABLE [sport].[CoachTraining] CHECK CONSTRAINT [FK_CoachTraining_ParticipationType_ParticipationType_Id]
GO
ALTER TABLE [sport].[CoachTraining]  WITH CHECK ADD  CONSTRAINT [FK_CoachTraining_Training_Training_Id] FOREIGN KEY([Training_Id])
REFERENCES [sport].[Training] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [sport].[CoachTraining] CHECK CONSTRAINT [FK_CoachTraining_Training_Training_Id]
GO
ALTER TABLE [sport].[CoachTrainingPlan]  WITH CHECK ADD  CONSTRAINT [FK_CoachTrainingPlan_Coach_Coach_Id] FOREIGN KEY([Coach_Id])
REFERENCES [hr].[Coach] ([Id])
GO
ALTER TABLE [sport].[CoachTrainingPlan] CHECK CONSTRAINT [FK_CoachTrainingPlan_Coach_Coach_Id]
GO
ALTER TABLE [sport].[CoachTrainingPlan]  WITH CHECK ADD  CONSTRAINT [FK_CoachTrainingPlan_TrainingPlan_TrainingPlan_Id] FOREIGN KEY([TrainingPlan_Id])
REFERENCES [sport].[TrainingPlan] ([Id])
GO
ALTER TABLE [sport].[CoachTrainingPlan] CHECK CONSTRAINT [FK_CoachTrainingPlan_TrainingPlan_TrainingPlan_Id]
GO
ALTER TABLE [sport].[CoachTrainingRequirement]  WITH CHECK ADD  CONSTRAINT [FK_CoachTrainingRequirement_Coach_Coach_Id] FOREIGN KEY([Coach_Id])
REFERENCES [hr].[Coach] ([Id])
GO
ALTER TABLE [sport].[CoachTrainingRequirement] CHECK CONSTRAINT [FK_CoachTrainingRequirement_Coach_Coach_Id]
GO
ALTER TABLE [sport].[CoachTrainingRequirement]  WITH CHECK ADD  CONSTRAINT [FK_CoachTrainingRequirement_CoachRole_CoachRole_Id] FOREIGN KEY([CoachRole_Id])
REFERENCES [dbo].[CoachRole] ([Id])
GO
ALTER TABLE [sport].[CoachTrainingRequirement] CHECK CONSTRAINT [FK_CoachTrainingRequirement_CoachRole_CoachRole_Id]
GO
ALTER TABLE [sport].[CoachTrainingRequirement]  WITH CHECK ADD  CONSTRAINT [FK_CoachTrainingRequirement_TrainingRequirement_TrainingRequirement_Id] FOREIGN KEY([TrainingRequirement_Id])
REFERENCES [sport].[TrainingRequirement] ([Id])
GO
ALTER TABLE [sport].[CoachTrainingRequirement] CHECK CONSTRAINT [FK_CoachTrainingRequirement_TrainingRequirement_TrainingRequirement_Id]
GO
ALTER TABLE [sport].[Match]  WITH CHECK ADD  CONSTRAINT [FK_Match_Location_Location_Id] FOREIGN KEY([Location_Id])
REFERENCES [sport].[Location] ([Id])
GO
ALTER TABLE [sport].[Match] CHECK CONSTRAINT [FK_Match_Location_Location_Id]
GO
ALTER TABLE [sport].[Match]  WITH CHECK ADD  CONSTRAINT [FK_Match_MatchState_MatchState_Id] FOREIGN KEY([MatchState_Id])
REFERENCES [sport].[MatchState] ([Id])
GO
ALTER TABLE [sport].[Match] CHECK CONSTRAINT [FK_Match_MatchState_MatchState_Id]
GO
ALTER TABLE [sport].[Match]  WITH CHECK ADD  CONSTRAINT [FK_Match_MatchType_MatchType_Id] FOREIGN KEY([MatchType_Id])
REFERENCES [sport].[MatchType] ([Id])
GO
ALTER TABLE [sport].[Match] CHECK CONSTRAINT [FK_Match_MatchType_MatchType_Id]
GO
ALTER TABLE [sport].[Match]  WITH CHECK ADD  CONSTRAINT [FK_Match_Team_AwayTeam_Id] FOREIGN KEY([AwayTeam_Id])
REFERENCES [sport].[Team] ([Id])
GO
ALTER TABLE [sport].[Match] CHECK CONSTRAINT [FK_Match_Team_AwayTeam_Id]
GO
ALTER TABLE [sport].[Match]  WITH CHECK ADD  CONSTRAINT [FK_Match_Team_HomeTeam_Id] FOREIGN KEY([HomeTeam_Id])
REFERENCES [sport].[Team] ([Id])
GO
ALTER TABLE [sport].[Match] CHECK CONSTRAINT [FK_Match_Team_HomeTeam_Id]
GO
ALTER TABLE [sport].[SeasonCategory]  WITH CHECK ADD  CONSTRAINT [FK_SeasonCategory_Season_Season_Id] FOREIGN KEY([Season_Id])
REFERENCES [sport].[Season] ([Id])
GO
ALTER TABLE [sport].[SeasonCategory] CHECK CONSTRAINT [FK_SeasonCategory_Season_Season_Id]
GO
ALTER TABLE [sport].[Team]  WITH CHECK ADD  CONSTRAINT [FK_Team_Location_HomeLocation_Id] FOREIGN KEY([HomeLocation_Id])
REFERENCES [sport].[Location] ([Id])
GO
ALTER TABLE [sport].[Team] CHECK CONSTRAINT [FK_Team_Location_HomeLocation_Id]
GO
ALTER TABLE [sport].[Training]  WITH CHECK ADD  CONSTRAINT [FK_Training_Location_Location_Id] FOREIGN KEY([Location_Id])
REFERENCES [sport].[Location] ([Id])
GO
ALTER TABLE [sport].[Training] CHECK CONSTRAINT [FK_Training_Location_Location_Id]
GO
ALTER TABLE [sport].[Training]  WITH CHECK ADD  CONSTRAINT [FK_Training_Season_SeasonCategory_Season_Id] FOREIGN KEY([SeasonCategory_Season_Id])
REFERENCES [sport].[Season] ([Id])
GO
ALTER TABLE [sport].[Training] CHECK CONSTRAINT [FK_Training_Season_SeasonCategory_Season_Id]
GO
ALTER TABLE [sport].[Training]  WITH CHECK ADD  CONSTRAINT [FK_Training_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Name] FOREIGN KEY([SeasonCategory_Season_Id], [SeasonCategory_Name])
REFERENCES [sport].[SeasonCategory] ([Season_Id], [Name])
ON UPDATE CASCADE
GO
ALTER TABLE [sport].[Training] CHECK CONSTRAINT [FK_Training_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Name]
GO
ALTER TABLE [sport].[Training]  WITH CHECK ADD  CONSTRAINT [FK_Training_TrainingPhase_TrainingPhase_Id] FOREIGN KEY([TrainingPhase_Id])
REFERENCES [sport].[TrainingPhase] ([Id])
GO
ALTER TABLE [sport].[Training] CHECK CONSTRAINT [FK_Training_TrainingPhase_TrainingPhase_Id]
GO
ALTER TABLE [sport].[Training]  WITH CHECK ADD  CONSTRAINT [FK_Training_TrainingPlan_TrainingPlan_Id] FOREIGN KEY([TrainingPlan_Id])
REFERENCES [sport].[TrainingPlan] ([Id])
GO
ALTER TABLE [sport].[Training] CHECK CONSTRAINT [FK_Training_TrainingPlan_TrainingPlan_Id]
GO
ALTER TABLE [sport].[Training]  WITH CHECK ADD  CONSTRAINT [FK_Training_TrainingState_TrainingState_Id] FOREIGN KEY([TrainingState_Id])
REFERENCES [sport].[TrainingState] ([Id])
GO
ALTER TABLE [sport].[Training] CHECK CONSTRAINT [FK_Training_TrainingState_TrainingState_Id]
GO
ALTER TABLE [sport].[Training]  WITH CHECK ADD  CONSTRAINT [FK_Training_TrainingType_TrainingType_Id] FOREIGN KEY([TrainingType_Id])
REFERENCES [sport].[TrainingType] ([Id])
GO
ALTER TABLE [sport].[Training] CHECK CONSTRAINT [FK_Training_TrainingType_TrainingType_Id]
GO
ALTER TABLE [sport].[TrainingGroup]  WITH CHECK ADD  CONSTRAINT [FK_TrainingGroup_Training_Training_Id] FOREIGN KEY([Training_Id])
REFERENCES [sport].[Training] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [sport].[TrainingGroup] CHECK CONSTRAINT [FK_TrainingGroup_Training_Training_Id]
GO
ALTER TABLE [sport].[TrainingPlan]  WITH CHECK ADD  CONSTRAINT [FK_TrainingPlan_Location_Location_Id] FOREIGN KEY([Location_Id])
REFERENCES [sport].[Location] ([Id])
GO
ALTER TABLE [sport].[TrainingPlan] CHECK CONSTRAINT [FK_TrainingPlan_Location_Location_Id]
GO
ALTER TABLE [sport].[TrainingPlan]  WITH CHECK ADD  CONSTRAINT [FK_TrainingPlan_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Name] FOREIGN KEY([SeasonCategory_Season_Id], [SeasonCategory_Name])
REFERENCES [sport].[SeasonCategory] ([Season_Id], [Name])
ON UPDATE CASCADE
GO
ALTER TABLE [sport].[TrainingPlan] CHECK CONSTRAINT [FK_TrainingPlan_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Name]
GO
ALTER TABLE [sport].[TrainingPlan]  WITH CHECK ADD  CONSTRAINT [FK_TrainingPlan_TrainingPhase_TrainingPhase_Id] FOREIGN KEY([TrainingPhase_Id])
REFERENCES [sport].[TrainingPhase] ([Id])
GO
ALTER TABLE [sport].[TrainingPlan] CHECK CONSTRAINT [FK_TrainingPlan_TrainingPhase_TrainingPhase_Id]
GO
ALTER TABLE [sport].[TrainingPlan]  WITH CHECK ADD  CONSTRAINT [FK_TrainingPlan_TrainingType_TrainingType_Id] FOREIGN KEY([TrainingType_Id])
REFERENCES [sport].[TrainingType] ([Id])
GO
ALTER TABLE [sport].[TrainingPlan] CHECK CONSTRAINT [FK_TrainingPlan_TrainingType_TrainingType_Id]
GO
ALTER TABLE [sport].[TrainingPlanGroup]  WITH CHECK ADD  CONSTRAINT [FK_TrainingPlanGroup_TrainingPlan_TrainingPlan_Id] FOREIGN KEY([TrainingPlan_Id])
REFERENCES [sport].[TrainingPlan] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [sport].[TrainingPlanGroup] CHECK CONSTRAINT [FK_TrainingPlanGroup_TrainingPlan_TrainingPlan_Id]
GO
ALTER TABLE [sport].[TrainingRequirement]  WITH CHECK ADD  CONSTRAINT [FK_TrainingRequirement_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Name] FOREIGN KEY([SeasonCategory_Season_Id], [SeasonCategory_Name])
REFERENCES [sport].[SeasonCategory] ([Season_Id], [Name])
ON UPDATE CASCADE
GO
ALTER TABLE [sport].[TrainingRequirement] CHECK CONSTRAINT [FK_TrainingRequirement_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Name]
GO
ALTER TABLE [sport].[TrainingRequirement]  WITH CHECK ADD  CONSTRAINT [FK_TrainingRequirement_TrainingPhase_TrainingPhase_Id] FOREIGN KEY([TrainingPhase_Id])
REFERENCES [sport].[TrainingPhase] ([Id])
GO
ALTER TABLE [sport].[TrainingRequirement] CHECK CONSTRAINT [FK_TrainingRequirement_TrainingPhase_TrainingPhase_Id]
GO
ALTER TABLE [sport].[TrainingRequirement]  WITH CHECK ADD  CONSTRAINT [FK_TrainingRequirement_TrainingType_TrainingType_Id] FOREIGN KEY([TrainingType_Id])
REFERENCES [sport].[TrainingType] ([Id])
GO
ALTER TABLE [sport].[TrainingRequirement] CHECK CONSTRAINT [FK_TrainingRequirement_TrainingType_TrainingType_Id]
GO
ALTER TABLE [hr].[CoachAttendance]  WITH CHECK ADD  CONSTRAINT [CK_CoachAttendance_PeriodMonth] CHECK  (([PeriodMonth]>=(1) AND [PeriodMonth]<=(12)))
GO
ALTER TABLE [hr].[CoachAttendance] CHECK CONSTRAINT [CK_CoachAttendance_PeriodMonth]
GO
ALTER TABLE [hr].[CoachAttendance]  WITH CHECK ADD  CONSTRAINT [CK_CoachAttendance_PeriodYear] CHECK  (([PeriodYear]>=(1) AND [PeriodYear]<=(9999)))
GO
ALTER TABLE [hr].[CoachAttendance] CHECK CONSTRAINT [CK_CoachAttendance_PeriodYear]
GO
ALTER TABLE [hr].[CoachContract]  WITH CHECK ADD  CONSTRAINT [CK_CoachContract_ContractType] CHECK  (([ContractType]=(2) OR [ContractType]=(1)))
GO
ALTER TABLE [hr].[CoachContract] CHECK CONSTRAINT [CK_CoachContract_ContractType]
GO
ALTER TABLE [hr].[CoachContract]  WITH CHECK ADD  CONSTRAINT [CK_CoachContract_RewardAmount] CHECK  (([RewardAmount]>=(0)))
GO
ALTER TABLE [hr].[CoachContract] CHECK CONSTRAINT [CK_CoachContract_RewardAmount]
GO
ALTER TABLE [hr].[CoachLicense]  WITH CHECK ADD  CONSTRAINT [CK_CoachLicense_Validity] CHECK  (([ValidTo] IS NULL OR [ValidTo]>=[ValidFrom]))
GO
ALTER TABLE [hr].[CoachLicense] CHECK CONSTRAINT [CK_CoachLicense_Validity]
GO
ALTER TABLE [hr].[CoachSetting]  WITH CHECK ADD  CONSTRAINT [CK_CoachSetting_Validity] CHECK  (([ValidTo] IS NULL OR [ValidTo]>=[ValidFrom]))
GO
ALTER TABLE [hr].[CoachSetting] CHECK CONSTRAINT [CK_CoachSetting_Validity]
GO

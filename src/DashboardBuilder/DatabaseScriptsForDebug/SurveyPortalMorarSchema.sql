
/***************************************************************************************************/
/***************************************************************************************************/
/**************************************** Create the databases if needed ***************************/
/***************************************************************************************************/
/***************************************************************************************************/
/***************************************************************************************************/

IF (NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE (name = 'SurveyPortalMorar')))
	Create Database SurveyPortalMorar
GO
IF (NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE (name = 'SurveyPortalMorarTemp')))
	Create Database SurveyPortalMorarTemp
GO
USE [SurveyPortalMorar]
GO

/*
The Script below will create all necessary schema objects to allow Dashboard builder to work.
If any objects have already been created then it will generate an error.
The script has been left intentionally as close to exporting out of SSMS, so that it's easy to
update in the future.
*/

USE [SurveyPortalMorar]
GO

/*****************************************************************************************/
/*****************************************************************************************/
/****** Object:  Table [dbo].[panelRespondents]    Script Date: 28/05/2019 12:47:50 ******/
/*****************************************************************************************/
/*****************************************************************************************/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[panelRespondents](
	[panelRespondentId] [int] IDENTITY(1,1) NOT NULL,
	[companyId] [int] NULL,
	[surveyId] [int] NULL,
	[segmentId] [int] NULL,
	[languageId] [int] NULL,
	[sId] [varchar](200) NULL,
	[firstName] [nvarchar](200) NULL,
	[surname] [nvarchar](200) NULL,
	[email] [varchar](200) NULL,
	[telephone] [varchar](200) NULL,
	[mobile] [varchar](200) NULL,
	[salutation] [nvarchar](200) NULL,
	[address] [nvarchar](max) NULL,
	[emailStatusId] [int] NULL,
	[emailSendOutDate] [datetime] NULL,
	[surveyCompletionStatus] [int] NULL,
	[bounceBackCheck] [int] NOT NULL,
	[unsubscribed] [bit] NOT NULL,
	[company] [nvarchar](max) NULL,
	[role] [nvarchar](max) NULL,
	[respondentCode] [nvarchar](max) NULL,
	[notes] [nvarchar](max) NULL,
	[precodedData] [nvarchar](max) NULL,
	[projectId] [int] NULL,
	[projectRole] [nvarchar](max) NULL,
 CONSTRAINT [PK_panelRespondents_panelRespondentId] PRIMARY KEY CLUSTERED 
(
	[panelRespondentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[panelRespondents] ADD  CONSTRAINT [DF_panelRespondents_bounceBackCheck]  DEFAULT ((1)) FOR [bounceBackCheck]
GO

ALTER TABLE [dbo].[panelRespondents] ADD  CONSTRAINT [DF_panelRespondents_unsubscribed]  DEFAULT ((0)) FOR [unsubscribed]
GO

ALTER TABLE [dbo].[panelRespondents] ADD  CONSTRAINT [DF_panelRespondents_projectId]  DEFAULT ((0)) FOR [projectId]
GO
/*****************************************************************************************/
/*****************************************************************************************/
/****** Object:  Table [dbo].[surveyResponse]    Script Date: 28/05/2019 12:48:00 ********/
/*****************************************************************************************/
/*****************************************************************************************/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[surveyResponse](
	[responseId] [int] IDENTITY(1,1) NOT NULL,
	[surveyId] [int] NULL,
	[languageId] [int] NULL,
	[respondentId] [int] NULL,
	[segmentId] [int] NULL,
	[timestamp] [datetime] NULL,
	[status] [int] NULL,
	[IP] [varchar](100) NULL,
	[browserType] [varchar](max) NULL,
	[exitPoint] [varchar](100) NULL,
	[formatID] [int] NULL,
	[RefID] [int] NULL,
	[userId] [int] NULL,
	[lastChangeTime] [datetime] NULL,
	[surveyDefinitionStateId] [int] NULL,
 CONSTRAINT [PK_surveyResponse_responseId] PRIMARY KEY CLUSTERED 
(
	[responseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/*****************************************************************************************/
/*****************************************************************************************/
/****************** Object:  Table [dbo].[data]    Script Date: 28/05/2019 12:48:24 ******/
/*****************************************************************************************/
/*****************************************************************************************/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[data](
	[dataId] [bigint] IDENTITY(1,1) NOT NULL,
	[responseId] [int] NULL,
	[varCode] [varchar](100) NULL,
	[CH1] [int] NULL,
	[CH2] [int] NULL,
	[optValue] [int] NULL,
	[text] [nvarchar](max) NULL,
	[serverTimeStamp] [datetime] NULL,
 CONSTRAINT [PK_data] PRIMARY KEY NONCLUSTERED 
(
	[dataId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[data] ADD  CONSTRAINT [DF_data_bigint_ServerTimeStamp]  DEFAULT (getdate()) FOR [serverTimeStamp]
GO
/***************************************************************************************************/
/***************************************************************************************************/
/********************* Object:  Table [dbo].[workingData]    Script Date: 28/05/2019 12:48:46 ******/
/***************************************************************************************************/
/***************************************************************************************************/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[workingData](
	[dataId] [bigint] IDENTITY(1,1) NOT NULL,
	[responseId] [int] NULL,
	[varCode] [varchar](100) NULL,
	[CH1] [int] NULL,
	[CH2] [int] NULL,
	[optValue] [int] NULL,
	[text] [nvarchar](max) NULL,
	[serverTimeStamp] [datetime] NULL,
 CONSTRAINT [PK_workingData] PRIMARY KEY NONCLUSTERED 
(
	[dataId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[workingData] ADD  CONSTRAINT [DF_workingData_responseId_ServerTimeStamp]  DEFAULT (getdate()) FOR [serverTimeStamp]
GO

/****** Object:  View [dbo].[PanelRespondentIds]    Script Date: 28/05/2019 12:49:03 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [dbo].[PanelRespondentIds]
AS
SELECT        sId, panelRespondentId, surveyId, surveyCompletionStatus, precodedData, respondentCode
FROM            dbo.panelRespondents


GO

/*******************************************************************************************************/
/*******************************************************************************************************/
/****** Object:  StoredProcedure [dbo].[SurveyErrors_SaveNew]    Script Date: 03/06/2019 16:18:16 ******/
/*******************************************************************************************************/
/*******************************************************************************************************/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[SurveyErrors_SaveNew]     @errorMessage		nvarchar(max),     @description		nvarchar(max),     @assembly		nvarchar(max),     @timeStamp		datetime,     @surveyId		int,     @surveyResponseId		int,     @panelRespondentId		int,     @className		nvarchar(max),     @objectId		int,     @errorType		int,     @browserId		int  AS SET NOCOUNT ON INSERT INTO SurveyErrors (    [errorMessage],     [description],     [assembly],     [timeStamp],     [surveyId],     [surveyResponseId],     [panelRespondentId],     [className],     [objectId],     [errorType],     [browserId]) VALUES(     @errorMessage,     @description,     @assembly,     @timeStamp,     @surveyId,     @surveyResponseId,     @panelRespondentId,     @className,     @objectId,     @errorType,     @browserId ) RETURN SCOPE_IDENTITY() SET NOCOUNT OFF 
GO

/***************************************************************************************************/
/***************************************************************************************************/
/******************** Object:  Table [dbo].[SurveyErrors]    Script Date: 03/06/2019 16:22:27 ******/
/***************************************************************************************************/
/***************************************************************************************************/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SurveyErrors](
	[surveyErrorId] [int] IDENTITY(1,1) NOT NULL,
	[errorMessage] [nvarchar](max) NULL,
	[description] [nvarchar](max) NULL,
	[assembly] [nvarchar](max) NULL,
	[timeStamp] [datetime] NULL,
	[surveyId] [int] NULL,
	[surveyResponseId] [int] NULL,
	[panelRespondentId] [int] NULL,
	[className] [nvarchar](max) NULL,
	[objectId] [int] NULL,
	[errorType] [int] NULL,
	[browserId] [int] NULL,
 CONSTRAINT [PK_SurveyErrors] PRIMARY KEY CLUSTERED 
(
	[surveyErrorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


CREATE TABLE [dbo].[surveys](
	[surveyId] [int] IDENTITY(1,1) NOT NULL,
	[surveyStructureId] [int] NULL,
	[companyId] [int] NULL,
	[startDate] [datetime] NULL,
	[endDate] [datetime] NULL,
	[sendOutTypeId] [int] NULL,
	[allowPaper] [bit] NULL,
	[reportingTemplate] [varchar](100) NULL,
	[status] [int] NULL,
	[checkCustomQuestions] [bit] NULL,
	[checkMessages] [bit] NULL,
	[checkSchedule] [bit] NULL,
	[emailSendOutDate] [datetime] NULL,
	[stage1_complete] [bit] NOT NULL,
	[stage2_complete] [bit] NULL,
	[stage3_complete] [bit] NULL,
	[stage4_complete] [bit] NULL,
	[stage5_complete] [bit] NULL,
	[initiatedDate] [datetime] NULL,
	[uniqueSurveyId] [varchar](50) NULL,
	[emailSendOutStatusId] [int] NULL,
	[createdByUserId] [int] NULL,
	[customSurveyTags] [nvarchar](max) NULL,
	[salt] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_surveys] PRIMARY KEY CLUSTERED 
(
	[surveyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


CREATE TABLE [dbo].[surveySegments](
	[surveySegmentId] [int] IDENTITY(0,1) NOT NULL,
	[surveyStructureId] [int] NULL,
	[segmentName] [varchar](100) NULL,
	[uniqueSegmentId] [varchar](100) NULL,
	[customSegmentTags] [nvarchar](max) NULL,
 CONSTRAINT [PK_surveySegments] PRIMARY KEY CLUSTERED 
(
	[surveySegmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


CREATE TABLE [dbo].[surveyStructures](
	[surveyStructureId] [int] IDENTITY(1,1) NOT NULL,
	[portalId] [int] NULL,
	[name] [varchar](100) NULL,
	[surveyURL] [varchar](200) NULL,
	[allowPaper] [bit] NULL,
	[status] [int] NULL,
	[description] [varchar](2000) NULL,
	[output] [varchar](1000) NULL,
	[cost] [varchar](500) NULL,
	[longDescription] [varchar](max) NULL,
	[swfBackgroundColor] [varchar](200) NULL,
	[allowHTML] [bit] NULL,
	[templateSWF] [varchar](100) NULL,
	[customStructureTags] [nvarchar](max) NULL,
	[scriptFileName] [varchar](1000) NULL,
 CONSTRAINT [PK_surveyStructures] PRIMARY KEY CLUSTERED 
(
	[surveyStructureId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

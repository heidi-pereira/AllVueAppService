CREATE TABLE [CommonLog] 
(
    [ID] BIGINT IDENTITY (1, 1) NOT NULL,
    [TimeStamp] DATETIME2(7) NOT NULL,
    [Level] VARCHAR(50) NOT NULL,
    [Logger] VARCHAR(MAX) NOT NULL,
    [Message] VARCHAR(MAX) NOT NULL,
    [Exception] VARCHAR(MAX) NULL
)
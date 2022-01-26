CREATE TABLE [dbo].[OlsFile]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1)
	,DateProcessed DATETIME2(6) NOT NULL
	,[FileName] NVARCHAR(255) NOT NULL
	,FileHash CHAR(64) NOT NULL
	,FileType NVARCHAR(20) NOT NULL
	,Warnings NVARCHAR(MAX) NULL
	,CONSTRAINT chk_FileType CHECK (FileType IN ('Authorized', 'NonFinancial', 'Posted'))
)

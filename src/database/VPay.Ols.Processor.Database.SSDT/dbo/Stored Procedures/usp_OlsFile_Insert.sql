CREATE PROCEDURE [dbo].[usp_OlsFile_Insert]
	@FileName NVARCHAR(255)
	,@FileHash CHAR(64)
	,@FileType NVARCHAR(20)	
AS
BEGIN
	SET XACT_ABORT ON;
	SET NOCOUNT ON;

	INSERT INTO OlsFile (
		DateProcessed
		,[FileName]
		,FileHash
		,FileType		
	)
	VALUES (
		GETDATE()
		,@FileName
		,@FileHash
		,@FileType		
	)
END

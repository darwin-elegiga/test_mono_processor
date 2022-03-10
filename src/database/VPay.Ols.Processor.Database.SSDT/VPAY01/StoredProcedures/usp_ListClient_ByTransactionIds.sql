CREATE PROCEDURE [dbo].[usp_ListClient_ByTransactionIds]
	@transactionIds [dbo].[udt_TransactionIdLookup] READONLY
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		[TMCLIC] as ClientCode
		,[TMTXID] as TransactionId
	FROM [VPAY01].[SEWCPS_CPSTMF]
	WHERE [TMTXID] IN (
		SELECT [TransactionId] FROM @transactionIds
	)
END

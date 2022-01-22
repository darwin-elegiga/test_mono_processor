CREATE   PROCEDURE [dbo].[sp_select_unacked_transactions_restricted]
(
	@pMinId bigint
)
AS
BEGIN
SET NOCOUNT ON;

	SELECT
		id,
		concat_ws('|',
			cast(id as varchar(20)),
			action,
			data,
			'OLS2',
			'EOF'
		)
	FROM TriggeredTranLog
	WHERE id > @pMinId and
		itc not in ('100.30', '200.1S', '200.2S', '304.30', '304.301', '304.302', '304.305')

END;

GO


CREATE   PROCEDURE [dbo].[sp_update_ack_status]
(
	@pID bigint,
	@pStatus char(1)
)
AS
BEGIN
SET NOCOUNT ON;

  UPDATE [dbo].[TriggeredTranLog]
  SET
    ack_status = @pStatus,
    ack_ts = GETDATE()
  WHERE
    id = @pID;

END;

GO


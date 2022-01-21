/*
======================================================================================================
Created by: Jonathan Surrells
Created date: 2021-09-28

Detail: Converted from MySQL.
======================================================================================================
*/

CREATE   PROCEDURE [dbo].[sp_update_send_status]
(
	@pID bigint,
	@pStatus char(1)
)
AS
BEGIN
SET NOCOUNT ON;

  UPDATE [dbo].[TriggeredTranLog]
  SET
    sent_status = @pStatus,
    sent_ts = GETDATE()
  WHERE
    id = @pID;

END;

GO


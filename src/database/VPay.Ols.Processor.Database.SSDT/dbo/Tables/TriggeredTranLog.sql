CREATE TABLE [dbo].[TriggeredTranLog](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[createTS] [datetime2](0) NOT NULL,
	[action] [char](1) NOT NULL,
	[sent_status] [char](1) NULL,
	[sent_ts] [datetime2](0) NULL,
	[ack_status] [char](1) NULL,
	[ack_ts] [datetime2](0) NULL,
	[eod_status] [char](1) NULL,
	[eod_ts] [datetime2](0) NULL,
	[data] [varchar](4000) NOT NULL,
	[olsid] [bigint] NOT NULL,
	[itc] [varchar](16) NULL,
 CONSTRAINT [PK_TriggeredTranLog] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[TriggeredTranLog] ADD  DEFAULT (getdate()) FOR [createTS]
GO


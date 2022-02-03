using System;

namespace VPay.Ols.Processor.Models.Authorization;

public sealed class AuthorizationHeader
{
    public string RecordName { get; set; }
    public string ProcessorName { get; set; }
    public string ReportName { get; set; }
    public DateOnly FileDate { get; set; }
    public DateOnly RunBeginDate { get; set; }
    public DateOnly RunEndDate { get; set; }

    public AuthorizationHeader()
    {
        RecordName = string.Empty;
        ProcessorName = string.Empty;
        ReportName = string.Empty;
    }

    internal AuthorizationHeader Copy()
    {
        return new AuthorizationHeader
        {
            RecordName = RecordName,
            ProcessorName = ProcessorName,
            ReportName = ReportName,
            FileDate = FileDate,
            RunBeginDate = RunBeginDate,
            RunEndDate = RunEndDate
        };
    }
}

using System;

namespace VPay.Ols.Processor.Models.NonFinancial;

public class NonFinancialHeader
{
    public string RecordName { get; set; } = string.Empty;
    public string ProcessorName { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public DateOnly FileDate { get; set; }
    public DateOnly RunBeginDate { get; set; }
    public DateOnly RunEndDate { get; set; }
    public string FileFormat { get; set; } = string.Empty;
}

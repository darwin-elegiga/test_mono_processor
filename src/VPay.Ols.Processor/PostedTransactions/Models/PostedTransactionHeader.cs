using System;

namespace VPay.Ols.Processor.PostedTransactions.Models;

public class PostedTransactionHeader
{
    public string RecordName { get; set; } = string.Empty;
    public string ProcessorName { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public DateTime FileDate { get; set; }
    public DateTime RunBeginDate { get; set; }
    public DateTime RunEndDate { get; set; }
    public string FileFormat { get; set; } = string.Empty;
}

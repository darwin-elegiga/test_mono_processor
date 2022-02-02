using System.Collections.Generic;

namespace VPay.Ols.Processor.Models.NonFinancial;

public class NonFinancialFile
{
    public NonFinancialHeader Header { get; set; }
    public List<NonFinancialDetail> Details { get; set; } = new List<NonFinancialDetail>();
    public NonfinancialTrailer Trailer { get; set; }
}


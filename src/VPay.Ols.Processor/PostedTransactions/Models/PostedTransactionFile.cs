using System.Collections.Generic;

namespace VPay.Ols.Processor.PostedTransactions.Models;

public class PostedTransactionFile
{
    public PostedTransactionHeader Header { get; set; }
    public List<Detail> Details { get; set; } = new List<Detail>();
    public PostedTransactionTrailer Trailer { get; set; }
}

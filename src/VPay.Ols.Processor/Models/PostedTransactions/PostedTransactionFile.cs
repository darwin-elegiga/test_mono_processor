using System.Collections.Generic;

namespace VPay.Ols.Processor.Models.PostedTransactions;

public class PostedTransactionFile
{
    public PostedTransactionHeader Header { get; set; }
    public List<PostedTransactionDetail> Details { get; set; } = new List<PostedTransactionDetail>();
    public PostedTransactionTrailer Trailer { get; set; }
}

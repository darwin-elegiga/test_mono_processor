using System.IO;
using VPay.Ols.Processor.PostedTransactions.Models;

namespace VPay.Ols.Processor.Parsers;

public interface IPostedTransactionsParser
{
    PostedTransactionFile ParseFile(StreamReader fileContent);
}

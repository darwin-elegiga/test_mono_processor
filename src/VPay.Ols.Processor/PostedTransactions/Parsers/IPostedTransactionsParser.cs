using System.IO;
using VPay.Ols.Processor.PostedTransactions.Models;

namespace VPay.Ols.Processor.PostedTransactions.Parsers;

public interface IPostedTransactionsParser
{
    PostedTransactionFile ParseFile(StreamReader fileContent);
}

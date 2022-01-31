using System.IO;
using VPay.Ols.Processor.Models.PostedTransactions;

namespace VPay.Ols.Processor.Parsers;

public interface IPostedTransactionsParser
{
    PostedTransactionFile ParseFile(StreamReader fileContent);
}

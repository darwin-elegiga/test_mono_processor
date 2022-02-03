using System.IO;
using VPay.Ols.Processor.Models.NonFinancial;

namespace VPay.Ols.Processor.Parsers;

public interface INonFinancialParser
{
    NonFinancialFile ParseFile(StreamReader fileContent);
}

using VPay.Ols.Processor.Models.NonFinancial;

namespace VPay.Ols.Processor.Writers;

public interface INonFinancialFileWriter
{
    public string WriteNonFinancialFile(NonFinancialFile file);
}

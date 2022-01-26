using VPay.Ols.Processor.Models.PostedTransactions;

namespace VPay.Ols.Processor.Writers;

public interface IPostedTransactionFileWriter
{
    public string WritePostedTransactionFile(PostedTransactionFile file);
}

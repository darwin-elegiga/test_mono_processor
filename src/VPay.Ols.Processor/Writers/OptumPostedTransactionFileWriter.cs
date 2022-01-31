using System.Text;
using VPay.Ols.Processor.Models.PostedTransactions;

namespace VPay.Ols.Processor.Writers;

public class OptumPostedTransactionFileWriter : IPostedTransactionFileWriter
{
    public string WritePostedTransactionFile(PostedTransactionFile file)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{file.Header.RecordName}|{file.Header.ProcessorName}|{file.Header.ReportName}|{file.Header.FileDate:MMddyyyy}|{file.Header.RunBeginDate:MMddyyyy}|{file.Header.RunEndDate:MMddyyyy}|{file.Header.FileFormat}");

        foreach (var detailRow in file.Details)
        {
            sb.AppendLine($"{detailRow.CardNumber}|{detailRow.TransactionDate}|{detailRow.TransactionCode}|{detailRow.TransactionAmount}|{detailRow.TransactionAmountSign}|{detailRow.TransactionCurrencyCode}|{detailRow.AuthorizationCode}|{detailRow.PostDate}|{detailRow.NetworkCode}|{detailRow.MerchantNumber}|{detailRow.MerchantName}|{detailRow.MerchantCategoryCode}|{detailRow.MerchantCountryCode}|{detailRow.InterchangeFeeAmount}|{detailRow.AchRoutingNumber}|{detailRow.LinkedCard}|{detailRow.AchConfirmationCode}|{detailRow.SeExternalIdNumber}|{detailRow.Bin}|{detailRow.TPA}|{detailRow.FileName}");
        }

        sb.AppendLine($"{file.Trailer.RecordName}|{file.Trailer.RecordCount}");

        return sb.ToString();
    }
}

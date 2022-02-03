using System.Text;
using VPay.Ols.Processor.Models.Authorization;

namespace VPay.Ols.Processor.Writers;

public sealed class AuthorizationFileWriter : IAuthorizationFileWriter
{
    public string WriteAuthorizationFile(AuthorizationFile file)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"{file.Header.RecordName}|{file.Header.ProcessorName}|{file.Header.ReportName}|{file.Header.FileDate:MMddyyyy}|{file.Header.RunBeginDate:MMddyyyy}|{file.Header.RunEndDate:MMddyyyy}");

        foreach (AuthorizationDetail detail in file.Details)
        {
            builder.AppendLine($"{detail.CardNumber}|{detail.TransactionDateTime:MMddyyyy HH:mm:ss}|{detail.TransactionCurrencyCode}|{detail.AddressVerificationResponse}|{detail.AuthorizationResponse}|{detail.AuthorizationAmount}|{detail.AuthorizationCode}|{detail.NetworkCode}|{detail.MerchantNumber}|{detail.MerchantName}|{detail.MerchantCategoryCode}|{detail.MerchantCountryCode}|{detail.SEExternalId}|{detail.Bin}|{detail.ClientCode}|{detail.FileName}");
        }

        builder.AppendLine($"{file.Trailer.RecordName}|{file.Trailer.Count}");

        return builder.ToString();
    }
}

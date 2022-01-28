using System;
using System.IO;
using VPay.Ols.Processor.Models.PostedTransactions;

namespace VPay.Ols.Processor.Parsers;

public class PostedTransactionsParser : IPostedTransactionsParser
{
    public PostedTransactionFile ParseFile(StreamReader fileContent)
    {
        var file = new PostedTransactionFile();

        var lineNumber = 0;
        string? line;
        while ((line = fileContent.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var data = line.Split("|");

            if (lineNumber == 0)
            {
                file.Header = new PostedTransactionHeader
                {
                    RecordName = data[(int)PostedTransactionFileEnum.Header.RecordName],
                    ProcessorName = data[(int)PostedTransactionFileEnum.Header.ProcessorName],
                    ReportName = data[(int)PostedTransactionFileEnum.Header.ReportName],
                    FileDate = DateTime.ParseExact(data[(int)PostedTransactionFileEnum.Header.FileDate], "MMddyyyy", null),
                    RunBeginDate = DateTime.ParseExact(data[(int)PostedTransactionFileEnum.Header.RunBeginDate], "MMddyyyy", null),
                    RunEndDate = DateTime.ParseExact(data[(int)PostedTransactionFileEnum.Header.RunEndDate], "MMddyyyy", null),
                    FileFormat = data[(int)PostedTransactionFileEnum.Header.FileFormat]
                };
            }
            else if (data.Length == 2)
            {
                file.Trailer = new PostedTransactionTrailer(data[(int)PostedTransactionFileEnum.Trailer.RecordName], int.Parse(data[(int)PostedTransactionFileEnum.Trailer.RecordCount]));
            }
            else
            {
                var detail = new PostedTransactionDetail
                {
                    LineNumber = lineNumber,
                    CardNumber = data[(int)PostedTransactionFileEnum.Details.CardNumber],
                    TransactionDate = data[(int)PostedTransactionFileEnum.Details.TransactionDate],
                    TransactionCode = data[(int)PostedTransactionFileEnum.Details.TransactionCode],
                    TransactionAmountSign = data[(int)PostedTransactionFileEnum.Details.TransactionAmountSign],
                    AuthorizationCode = data[(int)PostedTransactionFileEnum.Details.AuthorizationCode],
                    PostDate = data[(int)PostedTransactionFileEnum.Details.PostDate],
                    NetworkCode = data[(int)PostedTransactionFileEnum.Details.NetworkCode],
                    MerchantNumber = data[(int)PostedTransactionFileEnum.Details.MerchantNumber],
                    MerchantName = data[(int)PostedTransactionFileEnum.Details.MerchantName],
                    MerchantCategoryCode = data[(int)PostedTransactionFileEnum.Details.MerchantCategoryCode],
                    MerchantCountryCode = data[(int)PostedTransactionFileEnum.Details.MerchantCountryCode],
                    InterchangeFeeAmount = data[(int)PostedTransactionFileEnum.Details.InterchangeFeeAmount],
                    AchRoutingNumber = data[(int)PostedTransactionFileEnum.Details.AchRoutingNumber],
                    LinkedCard = data[(int)PostedTransactionFileEnum.Details.LinkedCard],
                    AchConfirmationCode = data[(int)PostedTransactionFileEnum.Details.AchConfirmationCode],
                    SeExternalIdNumber = data[(int)PostedTransactionFileEnum.Details.SeExternalIdNumber],
                    Bin = data[(int)PostedTransactionFileEnum.Details.Bin]
                };

                if (decimal.TryParse(data[(int)PostedTransactionFileEnum.Details.TransactionAmount], out decimal amount))
                {
                    detail.TransactionAmount = amount;
                }

                if (int.TryParse(data[(int)PostedTransactionFileEnum.Details.TransactionCurrencyCode], out int code))
                {
                    detail.TransactionCurrencyCode = code;
                }

                file.Details.Add(detail);
            }

            lineNumber++;
        }

        return file;
    }
}

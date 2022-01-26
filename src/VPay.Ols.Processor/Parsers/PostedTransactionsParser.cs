using System;
using System.IO;
using VPay.Ols.Processor.PostedTransactions.Models;

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
                    FileDate = DateTime.Parse(data[(int)PostedTransactionFileEnum.Header.FileDate]),
                    RunBeginDate = DateTime.Parse(data[(int)PostedTransactionFileEnum.Header.RunBeginDate]),
                    RunEndDate = DateTime.Parse(data[(int)PostedTransactionFileEnum.Header.RunEndDate]),
                    FileFormat = data[(int)PostedTransactionFileEnum.Header.FileFormat]
                };
            }
            else if (data.Length == 2)
            {
                file.Trailer = new PostedTransactionTrailer(data[(int)PostedTransactionFileEnum.Trailer.RecordName], int.Parse(data[(int)PostedTransactionFileEnum.Trailer.RecordName]));
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
                    AchRoutingNumber = data[(int)PostedTransactionFileEnum.Details.AchRoutingNumber],
                    LinkedCard = data[(int)PostedTransactionFileEnum.Details.LinkedCard],
                    AchConfirmationCode = data[(int)PostedTransactionFileEnum.Details.AchConfirmationCode],
                    SeExternalIdNumber = data[(int)PostedTransactionFileEnum.Details.SeExternalIdNumber],
                    Bin = data[(int)PostedTransactionFileEnum.Details.Bin],
                    TPA = data[(int)PostedTransactionFileEnum.Details.TPA],
                    FileName = data[(int)PostedTransactionFileEnum.Details.FileName]
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
        }

        return file;
    }
}

using System;
using System.IO;
using VPay.Ols.Processor.Models.NonFinancial;

namespace VPay.Ols.Processor.Parsers;

public class NonFinancialParser : INonFinancialParser
{
    public NonFinancialFile ParseFile(StreamReader fileContent)
    {
        var file = new NonFinancialFile();

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
                file.Header = new NonFinancialHeader
                {
                    RecordName = data[(int)NonFinancialFileEnum.Header.RecordName],
                    ProcessorName = data[(int)NonFinancialFileEnum.Header.ProcessorName],
                    ReportName = data[(int)NonFinancialFileEnum.Header.ReportName],
                    FileDate = DateTime.ParseExact(data[(int)NonFinancialFileEnum.Header.FileDate], "MMddyyyy", null),
                    RunBeginDate = DateTime.ParseExact(data[(int)NonFinancialFileEnum.Header.RunBeginDate], "MMddyyyy", null),
                    RunEndDate = DateTime.ParseExact(data[(int)NonFinancialFileEnum.Header.RunEndDate], "MMddyyyy", null),
                    FileFormat = data[(int)NonFinancialFileEnum.Header.FileFormat]
                };
            }
            else if (data.Length == 2)
            {
                file.Trailer = new NonFinancialTrailer(data[(int)NonFinancialFileEnum.Trailer.RecordName], int.Parse(data[(int)NonFinancialFileEnum.Trailer.RecordCount]));
            }
            else
            {
                var detail = new NonFinancialDetail
                {
                    LineNumber = lineNumber,
                    CardNumber = data[(int)NonFinancialFileEnum.Details.CardNumber],
                    CardOpenDate = data[(int)NonFinancialFileEnum.Details.CardOpenDate],
                    CardExpirationDate = data[(int)NonFinancialFileEnum.Details.CardExpirationDate],
                    CardholderIdCode = data[(int)NonFinancialFileEnum.Details.CardHolderIdCode],
                    CardholderIdValue = data[(int)NonFinancialFileEnum.Details.CardholderIdValue],
                    CardholderFirstName = data[(int)NonFinancialFileEnum.Details.CardholderFirstName],
                    CardholderLastName = data[(int)NonFinancialFileEnum.Details.CardholderLastName],
                    CardholderAddress1 = data[(int)NonFinancialFileEnum.Details.CardholderAddress1],
                    CardholderAddress2 = data[(int)NonFinancialFileEnum.Details.CardholderAddress2],
                    CardholderCity = data[(int)NonFinancialFileEnum.Details.CardholderCity],
                    CardholderState = data[(int)NonFinancialFileEnum.Details.CardholderState],
                    CardholderZip = data[(int)NonFinancialFileEnum.Details.CardholderZip],
                    CardholderCountry = data[(int)NonFinancialFileEnum.Details.CardholderCountryCode],
                    CardholderPrimaryPhone = data[(int)NonFinancialFileEnum.Details.CardholderPrimaryPhone],
                    CardholderSecondaryPhone = data[(int)NonFinancialFileEnum.Details.CardholderSecondaryPhone],
                    CardholderDOB = data[(int)NonFinancialFileEnum.Details.CardholderDOB],
                    Status = data[(int)NonFinancialFileEnum.Details.Status],
                    BalanceSign = data[(int)NonFinancialFileEnum.Details.BalanceSign],
                    ProgramId = data[(int)NonFinancialFileEnum.Details.ProgramId],
                    SubProgramId = data[(int)NonFinancialFileEnum.Details.SubProgramId],
                    PseudoDDANumber = data[(int)NonFinancialFileEnum.Details.PseudoDDANumber],
                    CustomerId = data[(int)NonFinancialFileEnum.Details.CustomerId],
                    LinkedAccounts = data[(int)NonFinancialFileEnum.Details.LinkedAccounts],
                    CreditLine = data[(int)NonFinancialFileEnum.Details.CreditLine],
                    CashAdvanceOutstanding = data[(int)NonFinancialFileEnum.Details.CashAdvanceOutstanding],
                    DaysDelinquent = data[(int)NonFinancialFileEnum.Details.DaysDelinquent],
                    AmountDelinquent = data[(int)NonFinancialFileEnum.Details.AmountDelinquent],
                    LastReageDate = data[(int)NonFinancialFileEnum.Details.LastReageDate],
                    LastStatementDate = data[(int)NonFinancialFileEnum.Details.LastStatementDate],
                    CurrentPaymentDueDate = data[(int)NonFinancialFileEnum.Details.CurrentPaymentDueDate],
                    SeExternalIdNumber = data[(int)NonFinancialFileEnum.Details.SeExternalIdNumber],
                    Bin = data[(int)NonFinancialFileEnum.Details.Bin]
                };

                if (decimal.TryParse(data[(int)NonFinancialFileEnum.Details.CurrentBalance], out decimal currentBalance))
                {
                    detail.CurrentBalance = currentBalance;
                }

                if (decimal.TryParse(data[(int)NonFinancialFileEnum.Details.AvailableBalance], out decimal availableBalance))
                {
                    detail.AvailableBalance = availableBalance;
                }

                file.Details.Add(detail);
            }

            lineNumber++;
        }

        return file;
    }
}

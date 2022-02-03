using System;
using System.Text;
using VPay.Ols.Processor.Models.NonFinancial;

namespace VPay.Ols.Processor.Writers;

public class NonFinancialFileWriter : INonFinancialFileWriter
{
    public string WriteNonFinancialFile(NonFinancialFile file)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{file.Header.RecordName}|{file.Header.ProcessorName}|{file.Header.ReportName}|{file.Header.FileDate:MMddyyyy}|{file.Header.RunBeginDate:MMddyyyy}|{file.Header.RunEndDate:MMddyyyy}|{file.Header.FileFormat}");

        foreach (var detailRow in file.Details)
        {
            sb.AppendLine($"{detailRow.CardOpenDate}|{detailRow.CardExpirationDate}|{detailRow.CardHolderIdCode}|{detailRow.CardholderIdValue}|{detailRow.CardholderFirstName}|{detailRow.CardholderLastName}|{detailRow.CardholderAddress1}|{detailRow.CardholderAddress2}|{detailRow.CardholderCity}|{detailRow.CardholderState}|{detailRow.CardholderZip}|{detailRow.CardholderPrimaryPhone}|{detailRow.CardholderSecondaryPhone}|{detailRow.CardholderDOB}|{detailRow.Status}|{detailRow.CurrentBalance}|{detailRow.BalanceSign}|{detailRow.ProgramId}|{detailRow.SubProgramId}|{detailRow.PseudoDDANumber}|{detailRow.CustomerId}|{detailRow.LinkedAccounts}|{detailRow.CreditLine}|{detailRow.AvailableBalance}|{detailRow.CashAdvanceOutstanding}|{detailRow.DaysDelinquent}|{detailRow.AmountDelinquent}|{detailRow.LastReageDate}|{detailRow.LastStatementDate}|{detailRow.CurrentPaymentDueDate}|{detailRow.SeExternalIdNumber}|{detailRow.Bin}|{detailRow.TPA}|{detailRow.FileName}");
        }

        sb.AppendLine($"{file.Trailer.RecordName}|{file.Trailer.RecordCount}");

        return sb.ToString();
    }
}

namespace VPay.Ols.Processor.Models.NonFinancial;

public static class NonFinancialFileEnum
{
    public enum Header
    {
        RecordName = 0,
        ProcessorName,
        ReportName,
        FileDate,
        RunBeginDate,
        RunEndDate,
        FileFormat
    }

    public enum Details
    {
        CardNumber = 0,
        CardOpenDate,
        CardExpirationDate,
        CardHolderIdCode,
        CardholderIdValue,
        CardholderFirstName,
        CardholderLastName,
        CardholderAddress1,
        CardholderAddress2,
        CardholderCity,
        CardholderState,
        CardholderZip,
        CardholderPrimaryPhone,
        CardholderSecondaryPhone,
        CardholderDOB,
        Status,
        CurrentBalance,
        BalanceSign,
        ProgramId,
        SubProgramId,
        PseudoDDANumber,
        CustomerId,
        LinkedAccounts,
        CreditLine,
        AvailableBalance,
        CashAdvanceOutstanding,
        DaysDelinquent,
        AmountDelinquent,
        LastReageDate,
        LastStatementDate,
        CurrentPaymentDueDate,
        SeExternalIdNumber,
        Bin,
        //TPA,
        //FileName
    }

    public enum Trailer
    {
        RecordName = 0,
        RecordCount
    }
}

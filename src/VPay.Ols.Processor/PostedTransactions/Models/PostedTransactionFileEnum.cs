namespace VPay.Ols.Processor.PostedTransactions.Models;

public static class PostedTransactionFileEnum
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
        TransactionDate,
        TransactionCode,
        TransactionAmount,
        TransactionAmountSign,
        TransactionCurrencyCode,
        AuthorizationCode,
        PostDate,
        NetworkCode,
        MerchantNumber,
        MerchantName,
        MerchantCategoryCode,
        MerchantCountryCode,
        AchRoutingNumber,
        LinkedCard,
        AchConfirmationCode,
        SeExternalIdNumber,
        Bin,
        TPA,
        FileName
    }

    public enum Trailer
    {
        RecordName = 0,
        RecordCount
    }
}



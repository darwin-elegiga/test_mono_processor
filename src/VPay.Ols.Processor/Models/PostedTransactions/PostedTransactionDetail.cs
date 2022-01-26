namespace VPay.Ols.Processor.Models.PostedTransactions;

public class PostedTransactionDetail
{
    public int LineNumber { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string TransactionDate { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public decimal? TransactionAmount { get; set; }
    public string TransactionAmountSign { get; set; } = string.Empty;
    public int? TransactionCurrencyCode { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
    public string PostDate { get; set; } = string.Empty;
    public string NetworkCode { get; set; } = string.Empty;
    public string MerchantNumber { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCategoryCode { get; set; } = string.Empty;
    public string MerchantCountryCode { get; set; } = string.Empty;
    public string AchRoutingNumber { get; set; } = string.Empty;
    public string LinkedCard { get; set; } = string.Empty;
    public string AchConfirmationCode { get; set; } = string.Empty;
    public string SeExternalIdNumber { get; set; } = string.Empty;
    public string Bin { get; set; } = string.Empty;
    public string TPA { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

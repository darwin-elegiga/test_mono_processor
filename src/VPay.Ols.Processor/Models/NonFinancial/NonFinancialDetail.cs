namespace VPay.Ols.Processor.Models.NonFinancial;

public class NonFinancialDetail
{
    public int LineNumber { get; set; } = 0;
    public string CardNumber { get; set; } = string.Empty;
    public string CardOpenDate { get; set; } = string.Empty;
    public string CardExpirationDate { get; set; } = string.Empty;
    public string CardholderIdCode { get; set; } = string.Empty;
    public string CardholderIdValue { get; set; } = string.Empty;
    public string CardholderFirstName { get; set; } = string.Empty;
    public string CardholderLastName { get; set; } = string.Empty;
    public string CardholderAddress1 { get; set; } = string.Empty;
    public string CardholderAddress2 { get; set; } = string.Empty;
    public string CardholderCity { get; set; } = string.Empty;
    public string CardholderState { get; set; } = string.Empty;
    public string CardholderZip { get; set; } = string.Empty;
    public string CardholderCountry { get; set; } = string.Empty;
    public string CardholderPrimaryPhone { get; set; } = string.Empty;
    public string CardholderSecondaryPhone { get; set; } = string.Empty;
    public string CardholderDOB { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BalanceSign { get; set; } = string.Empty;
    public string ProgramId { get; set; } = string.Empty;
    public string SubProgramId { get; set; } = string.Empty;
    public string PseudoDDANumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string LinkedAccounts { get; set; } = string.Empty;
    public string CreditLine { get; set; } = string.Empty;
    public decimal? AvailableBalance { get; set; }
    public decimal? CurrentBalance { get; set; }
    public string CashAdvanceOutstanding { get; set; } = string.Empty;
    public string DaysDelinquent { get; set; } = string.Empty;
    public string AmountDelinquent { get; set; } = string.Empty;
    public string LastReageDate { get; set; } = string.Empty;
    public string LastStatementDate { get; set; } = string.Empty;
    public string CurrentPaymentDueDate { get; set; } = string.Empty;
    public string SeExternalIdNumber { get; set; } = string.Empty;
    public string Bin { get; set; } = string.Empty;
    public string TPA { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

}

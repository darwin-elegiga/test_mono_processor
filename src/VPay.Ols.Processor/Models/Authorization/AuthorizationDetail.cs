using System;

namespace VPay.Ols.Processor.Models.Authorization;

public sealed class AuthorizationDetail
{
    public int LineNumber { get; set; }
    public string CardNumber { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public int TransactionCurrencyCode { get; set; }
    public string AddressVerificationResponse { get; set; }
    public string AuthorizationResponse { get; set; }
    public decimal AuthorizationAmount { get; set; }
    public string AuthorizationCode { get; set; }
    public string NetworkCode { get; set; }
    public string MerchantNumber { get; set; }
    public string MerchantName { get; set; }
    public string MerchantCategoryCode { get; set; }
    public string MerchantCountryCode { get; set; }
    public int SEExternalId { get; set; }
    public int Bin { get; set; }
    public string ClientCode { get; set; }
    public string FileName { get; set; }

    public AuthorizationDetail()
    {
        CardNumber = string.Empty;
        AddressVerificationResponse = string.Empty;
        AuthorizationResponse = string.Empty;
        AuthorizationCode = string.Empty;
        NetworkCode = string.Empty;
        MerchantNumber = string.Empty;
        MerchantName = string.Empty;
        MerchantCategoryCode = string.Empty;
        MerchantCountryCode = string.Empty;
        ClientCode = string.Empty;
        FileName = string.Empty;
    }

    internal AuthorizationDetail Copy()
    {
        return new AuthorizationDetail
        {
            LineNumber = LineNumber,
            CardNumber = CardNumber,
            TransactionDateTime = TransactionDateTime,
            TransactionCurrencyCode = TransactionCurrencyCode,
            AddressVerificationResponse = AddressVerificationResponse,
            AuthorizationResponse = AuthorizationResponse,
            AuthorizationAmount = AuthorizationAmount,
            AuthorizationCode = AuthorizationCode,
            NetworkCode = NetworkCode,
            MerchantNumber = MerchantNumber,
            MerchantName = MerchantName,
            MerchantCategoryCode = MerchantCategoryCode,
            MerchantCountryCode = MerchantCountryCode,
            SEExternalId = SEExternalId,
            Bin = Bin,
            ClientCode = ClientCode,
            FileName = FileName
        };
    }
}

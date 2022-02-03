namespace VPay.Ols.Processor.Messages;

public sealed class AuthorizationMessage
{
    public string FileName { get; set; }

    public AuthorizationMessage()
    {
        FileName = string.Empty;
    }
}

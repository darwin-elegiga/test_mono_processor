namespace VPay.Ols.Processor.Models.Authorization;

public sealed class AuthorizationFileSettings
{
    public string WorkingDirectory { get; set; }
    public string OutputDirectory { get; set; }

    public AuthorizationFileSettings()
    {
        WorkingDirectory = string.Empty;
        OutputDirectory = string.Empty;
    }
}

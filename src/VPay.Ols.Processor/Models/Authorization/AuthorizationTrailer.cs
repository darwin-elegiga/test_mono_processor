namespace VPay.Ols.Processor.Models.Authorization;

public sealed class AuthorizationTrailer
{
    public string RecordName { get; set; }
    public int Count { get; set; }

    public AuthorizationTrailer()
    {
        RecordName = string.Empty;
    }

    internal AuthorizationTrailer Copy()
    {
        return new AuthorizationTrailer
        {
            RecordName = RecordName,
            Count = Count
        };
    }
}

namespace VPay.Ols.Processor.Models.Constants;

public static class NonFinancialFileConstants
{
    public static class OptumHeaderValues
    {
        public const string RecordName = "HEADER";
        public const string ProcessorName = "VPAY, INC";
        public const string ReportName = "NON-FINANCIAL";
        public const string FileFormat = "3";
    }

    public static class OptumDetailValues
    {
        public const string CardholderFirstName = "VPay, Inc";
        public const string CardholderAddressLine1 = "3701 W. Plano Pkwy, #200";
        public const string CardholderCity = "Plano";
        public const string CardholderZip = "750757837";
        public const string CardholderPrimaryPhone = "4695436500";
    }

    public static class OptumTrailerValues
    {
        public const string RecordName = "TRAILER";
    }
}

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
    public static class OptumTrailerValues
    {
        public const string RecordName = "TRAILER";
    }
}

namespace VPay.Ols.Processor.PostedTransactions.Constants;

public static class Optum
{
    public static class OptumHeaderValues
    {
        public const string RecordName = "HEADER";
        public const string ProcessorName = "VPAY, INC";
        public const string ReportName = "AUTHORIZED";
        public const string FileFormat = "2";
    }

    public static class OptumTrailerValues
    {
        public const string RecordName = "TRAILER";
    }
}

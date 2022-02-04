namespace VPay.Ols.Processor.QueueConsumers.Consumers;

public sealed class ConsumerRetrySettings
{
    public int RetryLimit { get; set; }
    public int InitialIntervalInSeconds { get; set; }
    public int IntervalIncrementInSeconds { get; set; }
}

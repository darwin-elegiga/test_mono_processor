namespace VPay.Ols.Processor.QueueConsumers.Consumers.NonFinancialFile;

public class NonFinancialFileConsumerSettings
{
    public string QueueName { get; set; } = string.Empty;
    public ConsumerRetrySettings ConsumerRetrySettings { get; set; } = new ConsumerRetrySettings();
}

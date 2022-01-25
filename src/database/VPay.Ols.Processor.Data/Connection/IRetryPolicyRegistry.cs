using Polly;

namespace VPay.Ols.Processor.Data.Connection;

public interface IRetryPolicyRegistry
{
    IAsyncPolicy GetStandardRetryPolicy();

    IAsyncPolicy GetStandardSaveQueryRetryPolicy();

    IAsyncPolicy GetOpenConnectionRetryPolicy();
}

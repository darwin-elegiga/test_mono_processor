using System.Collections.Generic;

namespace VPay.Ols.Processor.Models;

public interface IResult
{
    /// <summary>
    /// Gets a value indicating whether the result is a 'success'
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the error associated with a <see cref="Failure"/>
    /// </summary>
    string Error { get; }

    /// <summary>
    /// Gets details for the error associated with a <see cref="Failure"/>
    /// </summary>
    List<string>? ErrorDetails { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a 'failure'
    /// </summary>
    bool Failure { get; }
}

public class Result : IResult
{
    /// <summary>
    /// Gets a value indicating whether the result is a 'success'
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the error associated with a <see cref="Failure"/>
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a 'failure'
    /// </summary>
    public bool Failure => !Success;

    public List<string>? ErrorDetails { get; }

    protected Result(
        bool success,
        string error,
        List<string>? errorDetails = null)
    {
        Success = success;
        Error = error;
        ErrorDetails = errorDetails;
    }

    /// <summary>
    /// Creates a failure <see cref="Result"/>
    /// </summary>
    /// <param name="message">The message associated with the failure</param>
    /// <returns>An <see cref="Result"/> instance set to a failure.</returns>
    public static Result Fail(string message) => new Result(false, message);

    /// <summary>
    /// Creates a failure <see cref="Result"/>
    /// </summary>
    /// <param name="message">The message associated with the failure</param>
    /// <param name="errorDetails">Details about the error associated with the failure</param>
    /// <returns></returns>
    public static Result Fail(string message, List<string> errorDetails) => new Result(false, message, errorDetails);

    /// <summary>
    /// Creates a success <see cref="Result"/>
    /// </summary>
    /// <returns>An <see cref="Result"/> instance set to a success.</returns>
    public static Result Ok() => new Result(true, string.Empty, null);
}

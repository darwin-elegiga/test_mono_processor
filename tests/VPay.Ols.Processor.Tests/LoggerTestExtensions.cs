using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace VPay.Ols.Processor.Tests;

public static class LoggerTestExtensions
{
    #region Trace

    public static Mock<ILogger<T>> VerifyTraceWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Trace),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }

    #endregion Trace

    #region Debug

    public static Mock<ILogger<T>> VerifyDebugWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Debug),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }

    public static Mock<ILogger<T>> VerifyDebugWasCalled<T>(this Mock<ILogger<T>> logger)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Debug),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }

    #endregion Debug

    #region Info

    public static Mock<ILogger<T>> VerifyInfoWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }

    public static Mock<ILogger<T>> VerifyInfoWasCalled<T>(this Mock<ILogger<T>> logger, ArgumentCaptor<string?> messageCaptor)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => messageCaptor.SaveValue(v.ToString())),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }
    #endregion Info

    #region Warning

    public static Mock<ILogger<T>> VerifyWarningWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }

    #endregion

    #region Error

    public static Mock<ILogger<T>> VerifyErrorWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }

    public static Mock<ILogger<T>> VerifyErrorWasCalled<T>(this Mock<ILogger<T>> logger, Exception exception, string expectedMessage)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                exception,
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }

    public static Mock<ILogger<T1>> VerifyErrorWasCalled<T1, T2>(this Mock<ILogger<T1>> logger, string expectedMessage) where T2 : Exception
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                It.IsAny<T2>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            )
        );

        return logger;
    }
    #endregion Error

    public static Mock<ILogger<T>> VerifyLogging<T>(this Mock<ILogger<T>> logger, string expectedMessage, LogLevel expectedLogLevel = LogLevel.Debug, Times? times = null)
    {
        logger.Verify(
            mock => mock.Log(
                It.Is<LogLevel>(level => level == expectedLogLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => expectedMessage.CompareTo(v.ToString()) == 0),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            ), times ?? Times.Once()
        );

        return logger;
    }
}

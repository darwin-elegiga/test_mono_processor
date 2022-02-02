using System;
using System.Collections.Generic;
using FluentAssertions;
using VPay.Ols.Processor.Models.Constants;
using VPay.Ols.Processor.Models.NonFinancial;
using VPay.Ols.Processor.Writers;
using Xunit;


namespace VPay.Ols.Processor.Tests.Writers;

public class OptumNonFinancialFileWriterTests
{
    [Fact]
    public void WithInput_WritesStringOutput()
    {
        var genFileName = "20220202120954_Optum_nonfinancial_processor.TXT";

        var input = new NonFinancialFile
        {
            Header = new NonFinancialHeader
            {
                RecordName = "HEADER",
                ProcessorName = "VPAY, INC",
                ReportName = "NON-FINANCIAL",
                FileDate = new DateTime(2022, 1, 24),
                RunBeginDate = new DateTime(2022, 1, 13),
                RunEndDate = new DateTime(2022, 1, 14),
                FileFormat = "3"
            },
            Details = new List<NonFinancialDetail>
            {
                new NonFinancialDetail
                {
                    LineNumber = 1,
                    CardNumber = "546893XXXXXX1234",
                    CardOpenDate = "01122022",
                    CardExpirationDate = "02282022",
                    CardholderCountry = "US",
                    Status = "PYNNN",
                    AvailableBalance = 0.10m,
                    BalanceSign = "+",
                    CurrentBalance = 0.10m,
                    CardholderState = "TX",
                    CardholderFirstName = NonFinancialFileConstants.OptumDetailValues.CardholderFirstName,
                    CardholderAddress1 = NonFinancialFileConstants.OptumDetailValues.CardholderAddressLine1,
                    CardholderZip = NonFinancialFileConstants.OptumDetailValues.CardholderZip,
                    CardholderCity = NonFinancialFileConstants.OptumDetailValues.CardholderCity,
                    CardholderPrimaryPhone = NonFinancialFileConstants.OptumDetailValues.CardholderPrimaryPhone,
                    SeExternalIdNumber = "1",
                    TPA = "ABC",
                    Bin = "546893",
                    FileName = genFileName
                },
                new NonFinancialDetail
                {
                    LineNumber= 2,
                    CardNumber = "546893XXXXXX4321",
                    CardOpenDate = "01122022",
                    CardExpirationDate = "02282022",
                    CardholderCountry = "US",
                    Status = "PYNNN",
                    AvailableBalance = 0.10m,
                    BalanceSign = "+",
                    CurrentBalance = 0.10m,
                    CardholderState = "TX",
                    CardholderFirstName = NonFinancialFileConstants.OptumDetailValues.CardholderFirstName,
                    CardholderAddress1 = NonFinancialFileConstants.OptumDetailValues.CardholderAddressLine1,
                    CardholderZip = NonFinancialFileConstants.OptumDetailValues.CardholderZip,
                    CardholderCity = NonFinancialFileConstants.OptumDetailValues.CardholderCity,
                    CardholderPrimaryPhone = NonFinancialFileConstants.OptumDetailValues.CardholderPrimaryPhone,
                    SeExternalIdNumber = "2",
                    TPA = "DEF",
                    Bin = "546893",
                    FileName = genFileName
                },
                new NonFinancialDetail
                {
                    LineNumber = 3,
                    CardNumber = "546893XXXXXX4444",
                    CardOpenDate = "01122022",
                    CardExpirationDate = "02282022",
                    CardholderCountry = "US",
                    Status = "PYNNN",
                    AvailableBalance = 0.10m,
                    BalanceSign = "+",
                    CurrentBalance = 0.10m,
                    CardholderState = "TX",
                    CardholderFirstName = NonFinancialFileConstants.OptumDetailValues.CardholderFirstName,
                    CardholderAddress1 = NonFinancialFileConstants.OptumDetailValues.CardholderAddressLine1,
                    CardholderZip = NonFinancialFileConstants.OptumDetailValues.CardholderZip,
                    CardholderCity = NonFinancialFileConstants.OptumDetailValues.CardholderCity,
                    CardholderPrimaryPhone = NonFinancialFileConstants.OptumDetailValues.CardholderPrimaryPhone,
                    SeExternalIdNumber = "",
                    TPA = "",
                    Bin = "546893",
                    FileName = genFileName
                }
            },
            Trailer = new NonFinancialTrailer("TRAILER", 3)
        };

        var expectedOutput = @"HEADER|VPAY, INC|NON-FINANCIAL|01242022|01132022|01142022|3
546893XXXXXX1234|01122022|02282022|||VPay, Inc||3701 W. Plano Pkwy, #200||Plano|TX|750757837|US|4695436500||PYNNN|0.10|+||||||||0.10|||||||1|546893|ABC|20220202120954_Optum_nonfinancial_processor.TXT
546893XXXXXX4321|01122022|02282022|||VPay, Inc||3701 W. Plano Pkwy, #200||Plano|TX|750757837|US|4695436500||PYNNN|0.10|+||||||||0.10|||||||2|546893|DEF|20220202120954_Optum_nonfinancial_processor.TXT
546893XXXXXX4444|01122022|02282022|||VPay, Inc||3701 W. Plano Pkwy, #200||Plano|TX|750757837|US|4695436500||PYNNN|0.10|+||||||||0.10||||||||546893||20220202120954_Optum_nonfinancial_processor.TXT
TRAILER|3
";

        var writer = new OptumNonFinancialFileWriter();

        var result = writer.WriteNonFinancialFile(input);

        result.Should().Be(expectedOutput);
    }
}

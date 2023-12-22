using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VPay.Ols.Processor.Models.Authorization;

namespace VPay.Ols.Processor.Parsers;

public sealed class AuthorizationParser : IAuthorizationParser
{
    private static readonly Regex _headerRegex;
    private static readonly Regex _detailRegex;
    private static readonly Regex _trailerRegex;

    private static readonly AuthorizationHeader _headerGroup;
    private static readonly AuthorizationDetail _detailGroup;
    private static readonly AuthorizationTrailer _trailerGroup;

    static AuthorizationParser()
    {
        _headerGroup = new AuthorizationHeader();
        _detailGroup = new AuthorizationDetail();
        _trailerGroup = new AuthorizationTrailer();

        // header
        var headerPropertyNames = new string[]
        {
            nameof(_headerGroup.RecordName),
            nameof(_headerGroup.ProcessorName),
            nameof(_headerGroup.ReportName),
            nameof(_headerGroup.FileDate),
            nameof(_headerGroup.RunBeginDate),
            nameof(_headerGroup.RunEndDate)
        };

        string headerRegex = "^" + string.Join("\\|", headerPropertyNames.Select(name => $"(?<{name}>[^|]*)")) + "$";
        _headerRegex = new Regex(headerRegex, RegexOptions.Compiled);

        // detail
        var detailPropertyNames = new string[]
        {
            nameof(_detailGroup.CardNumber),
            nameof(_detailGroup.TransactionDateTime),
            nameof(_detailGroup.TransactionCurrencyCode),
            nameof(_detailGroup.AddressVerificationResponse),
            nameof(_detailGroup.AuthorizationResponse),
            nameof(_detailGroup.AuthorizationAmount),
            nameof(_detailGroup.AuthorizationCode),
            nameof(_detailGroup.NetworkCode),
            nameof(_detailGroup.MerchantNumber),
            nameof(_detailGroup.MerchantName),
            nameof(_detailGroup.MerchantCategoryCode),
            nameof(_detailGroup.MerchantCountryCode),
            nameof(_detailGroup.SEExternalId),
            nameof(_detailGroup.Bin)
        };

        string detailRegex = "^" + string.Join("\\|", detailPropertyNames.Select(name => $"(?<{name}>[^|]*)")) + "$";
        _detailRegex = new Regex(detailRegex, RegexOptions.Compiled);

        // trailer
        var trailerPropertyNames = new string[]
        {
            nameof(_trailerGroup.RecordName),
            nameof(_trailerGroup.Count)
        };

        string trailerRegex = "^" + string.Join("\\|", trailerPropertyNames.Select(name => $"(?<{name}>[^|]*)")) + "$";
        _trailerRegex = new Regex(trailerRegex, RegexOptions.Compiled);
    }

    public AuthorizationFile ParseFile(StreamReader stream)
    {
        AuthorizationHeader header = ParseHeader(stream);
        (List<AuthorizationDetail> details, string nextLine) = ParseDetails(stream);
        AuthorizationTrailer trailer = ParseTrailer(nextLine);

        ValidateRemaining(stream);

        return new AuthorizationFile(header, details, trailer);
    }

    private static AuthorizationHeader ParseHeader(StreamReader stream)
    {
        string line = ReadNextLine(stream) ?? throw new Exception("File does not contain a header.");

        Match match = _headerRegex.Match(line);

        if (!match.Success)
        {
            throw new Exception("File contains an ill-formatted header.");
        }

        return new AuthorizationHeader
        {
            RecordName = match.Groups[nameof(_headerGroup.RecordName)].Value,
            ProcessorName = match.Groups[nameof(_headerGroup.ProcessorName)].Value,
            ReportName = match.Groups[nameof(_headerGroup.ReportName)].Value,
            FileDate = DateOnly.ParseExact(match.Groups[nameof(_headerGroup.FileDate)].Value, "MMddyyyy"),
            RunBeginDate = DateOnly.ParseExact(match.Groups[nameof(_headerGroup.RunBeginDate)].Value, "MMddyyyy"),
            RunEndDate = DateOnly.ParseExact(match.Groups[nameof(_headerGroup.RunEndDate)].Value, "MMddyyyy")
        };
    }

    private static (List<AuthorizationDetail>, string) ParseDetails(StreamReader stream)
    {
        List<AuthorizationDetail> details = new();
        int lineNumber = 1;

        while (true)
        {
            string line = ReadNextLine(stream) ?? throw new Exception("File does not contain a trailer.");

            if (_detailRegex.IsMatch(line))
            {
                AuthorizationDetail detail = ParseDetail(line, lineNumber);
                details.Add(detail);
            }
            else
            {
                return (details, line);
            }

            lineNumber++;
        }
    }

    private static AuthorizationDetail ParseDetail(string line, int lineNumber)
    {
        Match match = _detailRegex.Match(line);

        if (!match.Success)
        {
            throw new InvalidOperationException("Parsing a detail should only happen after validating that the line is a detail.");
        }

        return new AuthorizationDetail
        {
            LineNumber = lineNumber,
            CardNumber = match.Groups[nameof(_detailGroup.CardNumber)].Value,
            TransactionDateTime = DateTime.ParseExact(match.Groups[nameof(_detailGroup.TransactionDateTime)].Value, "MMddyyyy HH:mm:ss", null),
            TransactionCurrencyCode = int.Parse(match.Groups[nameof(_detailGroup.TransactionCurrencyCode)].Value),
            AddressVerificationResponse = match.Groups[nameof(_detailGroup.AddressVerificationResponse)].Value,
            AuthorizationResponse = match.Groups[nameof(_detailGroup.AuthorizationResponse)].Value,
            AuthorizationAmount = decimal.Parse(match.Groups[nameof(_detailGroup.AuthorizationAmount)].Value),
            AuthorizationCode = match.Groups[nameof(_detailGroup.AuthorizationCode)].Value,
            NetworkCode = match.Groups[nameof(_detailGroup.NetworkCode)].Value,
            MerchantNumber = match.Groups[nameof(_detailGroup.MerchantNumber)].Value,
            MerchantName = match.Groups[nameof(_detailGroup.MerchantName)].Value,
            MerchantCategoryCode = match.Groups[nameof(_detailGroup.MerchantCategoryCode)].Value,
            MerchantCountryCode = match.Groups[nameof(_detailGroup.MerchantCountryCode)].Value,
            SEExternalId = long.Parse(match.Groups[nameof(_detailGroup.SEExternalId)].Value),
            Bin = int.Parse(match.Groups[nameof(_detailGroup.Bin)].Value)
        };
    }

    private static AuthorizationTrailer ParseTrailer(string line)
    {
        Match match = _trailerRegex.Match(line);

        if (!match.Success)
        {
            throw new Exception("File contains an ill-formatted trailer.");
        }

        return new AuthorizationTrailer
        {
            RecordName = match.Groups[nameof(_trailerGroup.RecordName)].Value,
            Count = int.Parse(match.Groups[nameof(_trailerGroup.Count)].Value)
        };
    }

    private static void ValidateRemaining(StreamReader stream)
    {
        string remaining = stream.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(remaining))
        {
            throw new Exception("File contains content after the trailer.");
        }
    }

    private static string? ReadNextLine(StreamReader stream)
    {
        string? line;

        do
        {
            line = stream.ReadLine();

            if (line == null)
            {
                return null;
            }
        }
        while (string.IsNullOrWhiteSpace(line));

        return line;
    }
}

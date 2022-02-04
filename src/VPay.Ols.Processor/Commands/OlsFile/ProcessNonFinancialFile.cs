using System;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Ols.Processor.Hashing;
using VPay.Ols.Processor.Models;
using VPay.Ols.Processor.Models.Constants;
using VPay.Ols.Processor.Models.NonFinancial;
using VPay.Ols.Processor.Parsers;
using VPay.Ols.Processor.Writers;

namespace VPay.Ols.Processor.Commands.OlsFile;

public static class ProcessNonFinancialFile
{
    public record Command(string FilePath) : IRequest<Result>;

    public class CommandHandler : IRequestHandler<Command, Result>
    {
        internal readonly int[] OPTUM_BINS = new int[] { 528972, 532086, 546893, 547117 };

        private readonly ILogger<CommandHandler> _logger;
        private readonly NonFinancialFileSettings _settings;
        private readonly IFileSystem _fileSystem;
        private readonly IMediator _mediator;
        private readonly INonFinancialFileWriter _writer;
        private readonly INonFinancialParser _parser;
        private readonly IHashingService<SHA256CryptoServiceProvider> _hashingService;

        public CommandHandler(
            ILogger<CommandHandler> logger,
            IMediator mediator,
            IFileSystem fs,
            INonFinancialFileWriter writer,
            INonFinancialParser parser,
            IHashingService<SHA256CryptoServiceProvider> hashingService,
            NonFinancialFileSettings settings)
        {
            _logger = logger;
            _mediator = mediator;
            _parser = parser;
            _writer = writer;
            _hashingService = hashingService;
            _fileSystem = fs;
            _settings = settings;
        }

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
            var fileName = _fileSystem.Path.GetFileName(request.FilePath);

            NonFinancialFile originalFile;
            string fileHash;
            try
            {
                using var fileContent = _fileSystem.File.OpenText(request.FilePath);
                originalFile = _parser.ParseFile(fileContent);
                fileContent.BaseStream.Position = 0;
                fileHash = _hashingService.ComputeHash(fileContent.BaseStream);
            }
            catch (Exception e)
            {
                return Result.Fail($"Unable to read posted transactions file. {e.Message}");
            }

            var generatedFilename = $"{DateTime.Now:yyyyMMddHHmmss}_Optum_nonfinancial_se_debit.TXT";

            originalFile.Header.RecordName = NonFinancialFileConstants.OptumHeaderValues.RecordName;
            originalFile.Header.ProcessorName = NonFinancialFileConstants.OptumHeaderValues.ProcessorName;
            originalFile.Header.ReportName = NonFinancialFileConstants.OptumHeaderValues.ReportName;
            originalFile.Header.FileFormat = NonFinancialFileConstants.OptumHeaderValues.FileFormat;

            originalFile.Details.RemoveAll(d => !OPTUM_BINS.Contains(int.Parse(d.Bin)));

            //var tpaResults = await _mediator.Send(new GetClientForTransactions.Query(originalFile.Details.Where(d => !string.IsNullOrWhiteSpace(d.SeExternalIdNumber)).Select(d => int.Parse(d.SeExternalIdNumber)).ToList()), cancellationToken).ConfigureAwait(false);
            //foreach (var detailRecord in originalFile.Details)
            //{
            //    detailRecord.CardNumber = $"{detailRecord.CardNumber[..6]}XXXXXX{detailRecord.CardNumber[^4..]}";
            //    detailRecord.FileName = generatedFilename;

            //    if (!string.IsNullOrWhiteSpace(detailRecord.SeExternalIdNumber))
            //    {
            //        var tpaResult = tpaResults.Find(t => t.TransactionId == int.Parse(detailRecord.SeExternalIdNumber));

            //        if (tpaResult == null)
            //        {
            //            _logger.LogWarning("Row {LineNumber} with SE External Id {SEExternalId} did not match any known transaction.", detailRecord.LineNumber, detailRecord.SeExternalIdNumber);
            //            continue;
            //        }

            //        detailRecord.TPA = tpaResult.ClientCode;
            //    }
            //}

            originalFile.Trailer = new NonfinancialTrailer(NonFinancialFileConstants.OptumTrailerValues.RecordName, originalFile.Details.Count);

            var outputPath = _fileSystem.Path.Combine(_settings.OutputDirectory, generatedFilename);
            _fileSystem.Directory.CreateDirectory(_settings.OutputDirectory);

            _fileSystem.File.WriteAllText(outputPath, _writer.WriteNonFinancialFile(originalFile));  //await _fileSystem.File.WriteAllTextAsync(outputPath, _writer.WritePostedTransactionFile(originalFile), cancellationToken).ConfigureAwait(false);

            return await _mediator.Send(new AddOlsFile.Command(fileName, fileHash, OlsFileType.Posted), cancellationToken).ConfigureAwait(false);

            //////////////////
            //string[] fileLines;
            //try
            //{
            //    var filePath = _fs.Path.Combine(_settings.WorkingDirectory, request.FileName);
            //    _logger.LogInformation("Beginning processing of file {FilePath}", filePath);

            //    fileLines = _fs.File.ReadAllLines(filePath);
            //    fileLines = fileLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "File error during processing");
            //    return Result.Fail("File error during processing");
            //}

            //var headerValidationResult = ValidateHeader(fileLines);
            //if (headerValidationResult.Failure)
            //{
            //    return headerValidationResult;
            //}

            //var footerValidationResult = ValidateFooter(fileLines);
            //if (footerValidationResult.Failure)
            //{
            //    return footerValidationResult;
            //}

            //await BuildAdjustedFile(fileLines, cancellationToken).ConfigureAwait(false);

            //return Result.Ok();
        }

        //private async Task<Result> BuildAdjustedFile(IReadOnlyList<string> originalFileLines, CancellationToken cancellationToken)
        //{
        //    var now = DateTime.Now;
        //    var fileName = $"{now.ToString("yyyyMMddHHmmss")}_Optum_nonfinancial_processor.TXT";
        //    var filePath = _fileSystem.Path.Combine(_settings.WorkingDirectory, fileName);
        //    using (var newfile = _fileSystem.File.Create(filePath))
        //    {
        //        await newfile.WriteAsync(ToReadOnlyMemoryBytes(originalFileLines[0]), cancellationToken).ConfigureAwait(false);

        //        foreach (var line in originalFileLines)
        //        {
        //            await newfile.WriteAsync(ConvertFileLine($"\n{line}"), cancellationToken).ConfigureAwait(false);
        //        }

        //        await newfile.WriteAsync(ToReadOnlyMemoryBytes($"\n{originalFileLines[^1]}"), cancellationToken).ConfigureAwait(false);
        //    }

        //    return Result.Ok();
        //}

        //private ReadOnlyMemory<byte> ConvertFileLine(string line)
        //{
        //    return ToReadOnlyMemoryBytes(line);
        //}

        //private ReadOnlyMemory<byte> ToReadOnlyMemoryBytes(string val)
        //{
        //    return Encoding.UTF8.GetBytes(val);
        //}

        ///// <summary>
        ///// Validate that the header is correct.
        ///// Expected format:
        /////     HEADER|VPAY, INC|NON-FINANCIAL|<MMddyyyy>|<MMddyyyy>|<MMddyyyy>|3
        ///// </summary>
        ///// <param name="fileLines">The lines in the file</param>
        //private Result ValidateHeader(IReadOnlyList<string> fileLines)
        //{
        //    var errors = new List<string>()!;
        //    var headerLine = fileLines[0].Split('|', StringSplitOptions.TrimEntries);

        //    // Validate length of the header
        //    if (headerLine.Length != 7)
        //    {
        //        errors.Add($"Expected {7} but got {headerLine.Length} values");
        //    }
        //    else
        //    {
        //        // Validate the expected constant values (first 3 and last)
        //        if (!headerLine.Take(3).SequenceEqual(new[] { "HEADER", "VPAY, INC", "NON-FINANCIAL", }) || headerLine.Last() != "3")
        //        {
        //            errors.Add("Invalid expected constant value(s)");
        //        }

        //        // Validate each of the date values
        //        if (headerLine.Skip(3).Take(3).Any(dateStr => !DateTime.TryParseExact(dateStr, "MMddyyyyy", new CultureInfo("en-US"), DateTimeStyles.None, out _)))
        //        {
        //            errors.Add("Date value(s) not in the expected <MMddyyyy> format");
        //        }
        //    }

        //    if (errors.Any())
        //    {
        //        _logger.LogError("Bad file content header caused processing to failed: {HeaderErrors}", errors);
        //        return Result.Fail(errors!.ToString() ?? "");
        //    }

        //    return Result.Ok();
        //}

        ///// <summary>
        ///// Validate that the footer is correct.
        ///// Expected format:
        /////     TRAILER|<RECORD_COUNT>
        ///// </summary>
        ///// <param name="fileLines">The lines in the file</param>
        //private Result ValidateFooter(IReadOnlyList<string> fileLines)
        //{
        //    var errors = new List<string>();

        //    var lastLine = fileLines[^1].Split('|');
        //    var recordCount = fileLines.Count - 2;

        //    var expected = new[] { "TRAILER", recordCount.ToString() };

        //    if (lastLine.Length != 2)
        //    {
        //        errors.Add($"Expected {2} but got {lastLine.Length} values");
        //    }
        //    else if (!expected.SequenceEqual(new[] { lastLine[0], lastLine[1] }))
        //    {
        //        errors.Add($"Expected \"TRAILER|{recordCount}\" but got \"{lastLine[0]}|{lastLine[1]}\"");
        //    }

        //    if (errors.Any())
        //    {
        //        _logger.LogError("Bad file content trailer caused processing to failed: {TrailerErrors}", errors);
        //        return Result.Fail(errors!.ToString() ?? "");
        //    }

        //    return Result.Ok();
        //}
    }
}

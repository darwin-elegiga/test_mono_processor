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
using VPay.Ols.Processor.Queries;
using VPay.Ols.Processor.Writers;

namespace VPay.Ols.Processor.Commands.OlsFile;

public static class ProcessNonFinancial
{
    public record Command(string FilePath) : IRequest<Result>;

    public class Handler : IRequestHandler<Command, Result>
    {
        internal readonly int[] OPTUM_BINS = new int[] { 528972, 532086, 546893, 547117 };

        private readonly ILogger<Handler> _logger;
        private readonly NonFinancialFileSettings _settings;
        private readonly IFileSystem _fileSystem;
        private readonly IMediator _mediator;
        private readonly INonFinancialFileWriter _writer;
        private readonly INonFinancialParser _parser;
        private readonly IHashingService<SHA256CryptoServiceProvider> _hashingService;

        public Handler(
            ILogger<Handler> logger,
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
                return Result.Fail($"Unable to read non-financial file. {e.Message}");
            }

            var generatedFilename = $"{DateTime.Now:yyyyMMddHHmmss}_Optum_nonfinancial_op_debit.TXT";

            originalFile.Header.RecordName = NonFinancialFileConstants.OptumHeaderValues.RecordName;
            originalFile.Header.ProcessorName = NonFinancialFileConstants.OptumHeaderValues.ProcessorName;
            originalFile.Header.ReportName = NonFinancialFileConstants.OptumHeaderValues.ReportName;
            originalFile.Header.FileFormat = NonFinancialFileConstants.OptumHeaderValues.FileFormat;

            originalFile.Details.RemoveAll(d => !OPTUM_BINS.Contains(int.Parse(d.Bin)));

            var tpaResults = await _mediator.Send(new GetClientForTransactions.Query(originalFile.Details.Where(d => !string.IsNullOrWhiteSpace(d.SeExternalIdNumber)).Select(d => int.Parse(d.SeExternalIdNumber)).ToList()), cancellationToken).ConfigureAwait(false);
            foreach (var detailRecord in originalFile.Details)
            {
                detailRecord.CardNumber = $"{detailRecord.CardNumber[..6]}XXXXXX{detailRecord.CardNumber[^4..]}";
                detailRecord.FileName = generatedFilename;

                detailRecord.CardholderFirstName = NonFinancialFileConstants.OptumDetailValues.CardholderFirstName;
                detailRecord.CardholderAddress1 = NonFinancialFileConstants.OptumDetailValues.CardholderAddressLine1;
                detailRecord.CardholderCity = NonFinancialFileConstants.OptumDetailValues.CardholderCity;
                detailRecord.CardholderZip = NonFinancialFileConstants.OptumDetailValues.CardholderZip;
                detailRecord.CardholderPrimaryPhone = NonFinancialFileConstants.OptumDetailValues.CardholderPrimaryPhone;

                if (!string.IsNullOrWhiteSpace(detailRecord.SeExternalIdNumber))
                {
                    var tpaResult = tpaResults.Find(t => t.TransactionId == int.Parse(detailRecord.SeExternalIdNumber));

                    if (tpaResult == null)
                    {
                        _logger.LogWarning("Row {LineNumber} with SE External Id {SEExternalId} did not match any known transaction.", detailRecord.LineNumber, detailRecord.SeExternalIdNumber);
                        continue;
                    }

                    detailRecord.TPA = tpaResult.ClientCode;
                }
            }

            originalFile.Trailer = new NonFinancialTrailer(NonFinancialFileConstants.OptumTrailerValues.RecordName, originalFile.Details.Count);

            var outputPath = _fileSystem.Path.Combine(_settings.OutputDirectory, generatedFilename);
            _fileSystem.Directory.CreateDirectory(_settings.OutputDirectory);

            await _fileSystem.File.WriteAllTextAsync(outputPath, _writer.WriteNonFinancialFile(originalFile), cancellationToken).ConfigureAwait(false);
            await _mediator.Send(new SendToFileTransferService.Command(_fileSystem.FileInfo.FromFileName(outputPath)), cancellationToken).ConfigureAwait(false);

            return await _mediator.Send(new AddOlsFile.Command(fileName, fileHash, OlsFileType.NonFinancial), cancellationToken).ConfigureAwait(false);
        }
    }
}

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
using VPay.Ols.Processor.Models.PostedTransactions;
using VPay.Ols.Processor.Parsers;
using VPay.Ols.Processor.Queries;
using VPay.Ols.Processor.Writers;

namespace VPay.Ols.Processor.Commands.OlsFile;

public static class ProcessPostedTransactions
{
    public record Command(string FilePath) : IRequest<Result>;

    public class Handler : IRequestHandler<Command, Result>
    {
        internal readonly int[] OPTUM_BINS = new int[] { 528972, 532086, 546893, 547117 };

        private readonly IFileSystem _fileSystem;
        private readonly IPostedTransactionsParser _parser;
        private readonly IHashingService<SHA256CryptoServiceProvider> _hashingService;
        private readonly IMediator _mediator;
        private readonly IPostedTransactionFileWriter _writer;
        private readonly ILogger<Handler> _logger;
        private readonly PostedTransactionsFileSettings _postedTransactionsSettings;

        public Handler(IFileSystem fileSystem, IPostedTransactionsParser parser, IHashingService<SHA256CryptoServiceProvider> hashingService, IMediator mediator, IPostedTransactionFileWriter writer, ILogger<Handler> logger, PostedTransactionsFileSettings postedTransactionsSettings)
        {
            _fileSystem = fileSystem;
            _parser = parser;
            _hashingService = hashingService;
            _mediator = mediator;
            _writer = writer;
            _logger = logger;
            _postedTransactionsSettings = postedTransactionsSettings;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var fileName = _fileSystem.Path.GetFileName(request.FilePath);

            PostedTransactionFile originalFile;
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

            var generatedFilename = $"{DateTime.Now:yyyyMMddHHmmss}_Optum_posted_op_debit.TXT";

            originalFile.Header.RecordName = PostedTransactionFileConstants.OptumHeaderValues.RecordName;
            originalFile.Header.ProcessorName = PostedTransactionFileConstants.OptumHeaderValues.ProcessorName;
            originalFile.Header.ReportName = PostedTransactionFileConstants.OptumHeaderValues.ReportName;
            originalFile.Header.FileFormat = PostedTransactionFileConstants.OptumHeaderValues.FileFormat;

            originalFile.Details.RemoveAll(d => !OPTUM_BINS.Contains(int.Parse(d.Bin)));

            var tpaResults = await _mediator.Send(new GetClientForTransactions.Query(originalFile.Details.Where(d => !string.IsNullOrWhiteSpace(d.SeExternalIdNumber)).Select(d => long.Parse(d.SeExternalIdNumber)).ToList()), cancellationToken).ConfigureAwait(false);
            foreach (var detailRecord in originalFile.Details)
            {
                detailRecord.CardNumber = $"{detailRecord.CardNumber[..6]}XXXXXX{detailRecord.CardNumber[^4..]}";
                detailRecord.FileName = generatedFilename;

                if (!string.IsNullOrWhiteSpace(detailRecord.SeExternalIdNumber))
                {
                    var tpaResult = tpaResults.Find(t => t.TransactionId == long.Parse(detailRecord.SeExternalIdNumber));

                    if (tpaResult == null)
                    {
                        _logger.LogWarning("Row {LineNumber} with SE External Id {SEExternalId} did not match any known transaction.", detailRecord.LineNumber, detailRecord.SeExternalIdNumber);
                        continue;
                    }

                    detailRecord.TPA = tpaResult.ClientCode;
                }
            }

            originalFile.Trailer = new PostedTransactionTrailer(PostedTransactionFileConstants.OptumTrailerValues.RecordName, originalFile.Details.Count);

            var outputPath = _fileSystem.Path.Combine(_postedTransactionsSettings.OutputDirectory, generatedFilename);
            _fileSystem.Directory.CreateDirectory(_postedTransactionsSettings.OutputDirectory);

            await _fileSystem.File.WriteAllTextAsync(outputPath, _writer.WritePostedTransactionFile(originalFile), cancellationToken).ConfigureAwait(false);
            await _mediator.Send(new SendToFileTransferService.Command(_fileSystem.FileInfo.FromFileName(outputPath)), cancellationToken).ConfigureAwait(false);

            return await _mediator.Send(new AddOlsFile.Command(fileName, fileHash, OlsFileType.Posted), cancellationToken).ConfigureAwait(false);
        }
    }
}

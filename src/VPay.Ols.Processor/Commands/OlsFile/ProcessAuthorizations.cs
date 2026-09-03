using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Ols.Processor.Hashing;
using VPay.Ols.Processor.Models;
using VPay.Ols.Processor.Models.Authorization;
using VPay.Ols.Processor.Models.Constants;
using VPay.Ols.Processor.Parsers;
using VPay.Ols.Processor.Queries;
using VPay.Ols.Processor.Writers;

namespace VPay.Ols.Processor.Commands.OlsFile;

public static class ProcessAuthorizations
{
    public record Command(string FilePath) : IRequest<Result>;

    public class Handler : IRequestHandler<Command, Result>
    {
        internal readonly int[] OPTUM_BINS = new int[] { 528972, 532086, 546893, 547117 };

        private readonly IFileSystem _fileSystem;
        private readonly IAuthorizationParser _parser;
        private readonly IHashingService<SHA256CryptoServiceProvider> _hashingService;
        private readonly IMediator _mediator;
        private readonly IAuthorizationFileWriter _writer;
        private readonly ILogger<Handler> _logger;
        private readonly AuthorizationFileSettings _authorizationSettings;

        public Handler(IFileSystem fileSystem, IAuthorizationParser parser, IHashingService<SHA256CryptoServiceProvider> hashingService, IMediator mediator, IAuthorizationFileWriter writer, ILogger<Handler> logger, AuthorizationFileSettings authorizationSettings)
        {
            _fileSystem = fileSystem;
            _parser = parser;
            _hashingService = hashingService;
            _mediator = mediator;
            _writer = writer;
            _logger = logger;
            _authorizationSettings = authorizationSettings;
        }

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                (AuthorizationFile originalFile, string fileHash) = ReadFile(command.FilePath);

                string generatedFileName = $"{DateTime.Now:yyyyMMddHHmmss}_Optum_authorized_op_debit.TXT";

                AuthorizationFile processedFile = await EnsureValues(originalFile, generatedFileName, cancellationToken).ConfigureAwait(false);

                var outputPath = await WriteFile(processedFile, generatedFileName, cancellationToken).ConfigureAwait(false);

                await _mediator.Send(new SendToFileTransferService.Command(_fileSystem.FileInfo.New(outputPath)), cancellationToken).ConfigureAwait(false);

                return await AddFileToDatabase(command.FilePath, fileHash, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Unable to read authorization file. {ex.Message}");
            }
        }

        private (AuthorizationFile, string) ReadFile(string filePath)
        {
            using StreamReader fileContent = _fileSystem.File.OpenText(filePath);

            AuthorizationFile originalFile = _parser.ParseFile(fileContent);

            // Reset the stream
            fileContent.BaseStream.Position = 0;

            string fileHash = _hashingService.ComputeHash(fileContent.BaseStream);

            return (originalFile, fileHash);
        }

        private async Task<AuthorizationFile> EnsureValues(AuthorizationFile originalFile, string generatedFileName, CancellationToken cancellationToken)
        {
            AuthorizationFile processedFile = originalFile.Copy();

            processedFile.Header.RecordName = PostedTransactionFileConstants.OptumHeaderValues.RecordName;
            processedFile.Header.ProcessorName = PostedTransactionFileConstants.OptumHeaderValues.ProcessorName;
            processedFile.Header.ReportName = PostedTransactionFileConstants.OptumHeaderValues.ReportName;

            processedFile.Details.RemoveAll(detail => !OPTUM_BINS.Contains(detail.Bin));

            List<TransactionClient> clients = await GetClientsForTransactions(processedFile.Details, cancellationToken).ConfigureAwait(false);
            foreach (AuthorizationDetail detail in processedFile.Details)
            {
                detail.CardNumber = $"{detail.CardNumber[..6]}XXXXXX{detail.CardNumber[^4..]}";

                TransactionClient? transactionClient = clients.FirstOrDefault(transaction => transaction.TransactionId == detail.SEExternalId);
                if (transactionClient == null)
                {
                    _logger.LogWarning("Row {LineNumber} with SE External Id {SEExternalId} did not match any known transaction.", detail.LineNumber, detail.SEExternalId);
                }
                else
                {
                    detail.ClientCode = transactionClient.ClientCode;
                }

                detail.FileName = generatedFileName;
            }

            processedFile.Trailer.RecordName = PostedTransactionFileConstants.OptumTrailerValues.RecordName;
            processedFile.Trailer.Count = processedFile.Details.Count;

            return processedFile;
        }

        private async Task<List<TransactionClient>> GetClientsForTransactions(List<AuthorizationDetail> details, CancellationToken cancellationToken)
        {
            List<long> transactionIds = details.ConvertAll(detail => detail.SEExternalId);
            var query = new GetClientForTransactions.Query(transactionIds);

            return await _mediator.Send(query, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> WriteFile(AuthorizationFile processedFile, string generatedFileName, CancellationToken cancellationToken)
        {
            string outputPath = _fileSystem.Path.Combine(_authorizationSettings.OutputDirectory, generatedFileName);
            _fileSystem.Directory.CreateDirectory(_authorizationSettings.OutputDirectory);
            string output = _writer.WriteAuthorizationFile(processedFile);

            await _fileSystem.File.WriteAllTextAsync(outputPath, output, cancellationToken).ConfigureAwait(false);
            return outputPath;
        }

        private async Task<Result> AddFileToDatabase(string filePath, string fileHash, CancellationToken cancellationToken)
        {
            string fileName = _fileSystem.Path.GetFileName(filePath);
            var command = new AddOlsFile.Command(fileName, fileHash, OlsFileType.Authorized);

            return await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
        }
    }
}

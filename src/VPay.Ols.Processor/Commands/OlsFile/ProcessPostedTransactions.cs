using System;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
        private readonly IFileSystem _fileSystem;
        private readonly IPostedTransactionsParser _parser;
        private readonly IHashingService<SHA256CryptoServiceProvider> _hashingService;
        private readonly IMediator _mediator;
        private readonly IPostedTransactionFileWriter _writer;

        public Handler(IFileSystem fileSystem, IPostedTransactionsParser parser, IHashingService<SHA256CryptoServiceProvider> hashingService, IMediator mediator, IPostedTransactionFileWriter writer)
        {
            _fileSystem = fileSystem;
            _parser = parser;
            _hashingService = hashingService;
            _mediator = mediator;
            _writer = writer;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var fileName = _fileSystem.Path.GetFileName(request.FilePath);
            var directory = _fileSystem.Path.GetDirectoryName(request.FilePath);

            PostedTransactionFile originalFile;
            string fileHash;
            try
            {
                using var fileContent = _fileSystem.File.OpenText(request.FilePath);
                originalFile = _parser.ParseFile(fileContent);
                fileContent.BaseStream.Position = 0;
                fileHash = _hashingService.ComputeHash(fileContent.BaseStream);
            }
            catch(Exception e)
            {
                return Result.Fail($"Unable to read posted transactions file. {e}");
            }                        
            
            originalFile.Header.RecordName = PostedTransactionFileConstants.OptumHeaderValues.RecordName;
            originalFile.Header.ProcessorName = PostedTransactionFileConstants.OptumHeaderValues.ProcessorName;
            originalFile.Header.ReportName = PostedTransactionFileConstants.OptumHeaderValues.ReportName;
            originalFile.Header.FileFormat = PostedTransactionFileConstants.OptumHeaderValues.FileFormat;            

            var tpaResults = await _mediator.Send(new GetTPAForTransactions.Query(originalFile.Details.Select(d => int.Parse(d.SeExternalIdNumber)).ToList()), cancellationToken).ConfigureAwait(false);
            foreach(var detailRecord in originalFile.Details)
            {
                detailRecord.CardNumber = $"{detailRecord.CardNumber[..6]}XXXXXX{detailRecord.CardNumber[^4..]}";

                var tpaResult = tpaResults.FirstOrDefault(t => t.TransactionId == int.Parse(detailRecord.SeExternalIdNumber));

                if(tpaResult == null)
                {
                    // todo: Log this?
                    continue;                    
                }

                detailRecord.TPA = tpaResult.TPA;
            }

            originalFile.Trailer = new PostedTransactionTrailer(PostedTransactionFileConstants.OptumTrailerValues.RecordName, originalFile.Details.Count);

            var outputPath = $"{directory}/optum_out/{fileName}";
            _fileSystem.Directory.CreateDirectory(outputPath);

            await _fileSystem.File.WriteAllTextAsync(outputPath, _writer.WritePostedTransactionFile(originalFile), cancellationToken).ConfigureAwait(false);

            // todo: Send to balancing?

            return await _mediator.Send(new AddOlsFile.Command(fileName, fileHash, OlsFileType.Posted), cancellationToken).ConfigureAwait(false);
        }
    }
}

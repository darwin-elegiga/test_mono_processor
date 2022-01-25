using System;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VPay.Ols.Processor.Hashing;
using VPay.Ols.Processor.Models;
using VPay.Ols.Processor.PostedTransactions.Constants;
using VPay.Ols.Processor.PostedTransactions.Models;
using VPay.Ols.Processor.PostedTransactions.Parsers;

namespace VPay.Ols.Processor.PostedTransactions.Commands;

public static class ProcessPostedTransactions
{
    public record Command(string FilePath) : IRequest<Result>;

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly IFileSystem _fileSystem;
        private readonly IPostedTransactionsParser _parser;
        private readonly IHashingService<SHA256CryptoServiceProvider> _hashingService;
        private readonly IMediator _mediator;

        public Handler(IFileSystem fileSystem, IPostedTransactionsParser parser, IHashingService<SHA256CryptoServiceProvider> hashingService, IMediator mediator)
        {
            _fileSystem = fileSystem;
            _parser = parser;
            _hashingService = hashingService;
            _mediator = mediator;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
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

            // todo: Log the inbound file information

            // todo: Validate the inbound file

            // todo: Transform the inbound file
            originalFile.Header.RecordName = Optum.OptumHeaderValues.RecordName;
            originalFile.Header.ProcessorName = Optum.OptumHeaderValues.ProcessorName;
            originalFile.Header.ReportName = Optum.OptumHeaderValues.ReportName;
            originalFile.Header.FileFormat = Optum.OptumHeaderValues.FileFormat;

            originalFile.Details.ForEach(d => d.CardNumber = $"{d.CardNumber[..6]}XXXXXX{d.CardNumber[^4..]}");

            // todo: Add lookup for TPA by txid

            originalFile.Trailer = new PostedTransactionTrailer(Optum.OptumTrailerValues.RecordName, originalFile.Details.Count);

            // todo: Save the inbound file somewhere

            // todo: Transfer the inbound file (TBD - May just leave it somewhere and let AB deal with it)

            // todo: Send to balancing?

            return Result.Ok();
        }
    }
}

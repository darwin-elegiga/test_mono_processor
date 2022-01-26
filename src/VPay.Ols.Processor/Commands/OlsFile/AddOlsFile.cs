using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Ols.Processor.Data.Connection;
using VPay.Ols.Processor.Models;

namespace VPay.Ols.Processor.Commands.OlsFile;

public static class AddOlsFile
{
    public record Command(string FileName, string FileHash, OlsFileType FileType) : IRequest<Result>;    

    public class Handler : IRequestHandler<Command, Result>
    {
        internal static readonly string Sproc = "[dbo].[usp_OlsFile_Insert]";

        private readonly IDataConnection<SqlConnection> _connection;

        public Handler(IDataConnection<SqlConnection> connection)
        {
            _connection = connection;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                await _connection.ExecuteAsync(
                    Sproc,
                    new {
                        request.FileName,
                        request.FileHash,
                        FileType = request.FileType.ToString()                        
                    },
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch(Exception e)
            {
                return Result.Fail(e.Message);
            }

            return Result.Ok();
        }
    }
}

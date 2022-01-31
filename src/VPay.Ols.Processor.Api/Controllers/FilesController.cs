using System.IO.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.Ols.Processor.Messages;
using VPay.Ols.Processor.Models.PostedTransactions;

namespace VPay.Ols.Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : Controller
{
    private readonly IFileSystem _fileSystem;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly PostedTransactionsFileSettings _postedTransactionsSettings;

    public FilesController(IFileSystem fileSystem, IPublishEndpoint publishEndpoint, PostedTransactionsFileSettings postedTransactionSettings)
    {
        _fileSystem = fileSystem;
        _postedTransactionsSettings = postedTransactionSettings;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Accepts the 'Posted Transactions' file and submits it for post-processing
    /// </summary>
    /// <param name="file">The 'Posted Transactions' file</param>
    /// <param name="token">A token that can be used to cancel the work</param>
    /// <response code="2020">The file has been submitted for post-processing</response>
    [HttpPost("ingest-posted-transactions")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> IngestPostedTransactionsFile(IFormFile file, CancellationToken token = default)
    {
        var fileName = _fileSystem.Path.GetFileName(file.FileName);
        var filePath = _fileSystem.Path.Combine(_postedTransactionsSettings.WorkingDirectory, fileName);

        _fileSystem.Directory.CreateDirectory(_postedTransactionsSettings.WorkingDirectory);

        await using (var stream = _fileSystem.File.Create(filePath))
        {
            await file.CopyToAsync(stream, token).ConfigureAwait(false);
        }

        await _publishEndpoint.Publish<PostedTransactionFileProcessed>(new { Filename = fileName }, token).ConfigureAwait(false);

        return Accepted();
    }
}

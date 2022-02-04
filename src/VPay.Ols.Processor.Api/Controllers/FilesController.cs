using System.IO;
using System.IO.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.Ols.Processor.Messages;
using VPay.Ols.Processor.Models.Authorization;
using VPay.Ols.Processor.Models.NonFinancial;
using VPay.Ols.Processor.Models.PostedTransactions;

namespace VPay.Ols.Processor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : Controller
{
    private readonly IFileSystem _fileSystem;
    private readonly IPublishEndpoint _publishEndpoint;

    private readonly AuthorizationFileSettings _authorizationSettings;
    private readonly PostedTransactionsFileSettings _postedTransactionsSettings;
    private readonly NonFinancialFileSettings _nonFinancialSettings;

    public FilesController(IFileSystem fileSystem, IPublishEndpoint publishEndpoint, PostedTransactionsFileSettings postedTransactionSettings, NonFinancialFileSettings nonFinancialFileSettings, AuthorizationFileSettings authorizationSettings)
    {
        _fileSystem = fileSystem;
        _postedTransactionsSettings = postedTransactionSettings;
        _nonFinancialSettings = nonFinancialFileSettings;
        _publishEndpoint = publishEndpoint;

        _authorizationSettings = authorizationSettings;
        _postedTransactionsSettings = postedTransactionSettings;
    }

    /// <summary>
    /// Accepts the 'Authorization' file and submits it for post-processing
    /// </summary>
    /// <param name="file">The 'Authorization' file</param>
    /// <param name="token">A token that can be used to cancel the work</param>
    /// <response code="202">The file has been submitted for post-processing</response>
    [HttpPost("ingest-authorizations")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> IngestAuthorizationFile(IFormFile file, CancellationToken token = default)
    {
        string fileName = _fileSystem.Path.GetFileName(file.FileName);
        string filePath = _fileSystem.Path.Combine(_authorizationSettings.WorkingDirectory, fileName);

        _fileSystem.Directory.CreateDirectory(_authorizationSettings.WorkingDirectory);

        await using (Stream stream = _fileSystem.File.Create(filePath))
        {
            await file.CopyToAsync(stream, token).ConfigureAwait(false);
        }

        var message = new AuthorizationMessage { FileName = fileName };

        await _publishEndpoint.Publish(message, token).ConfigureAwait(false);

        return Accepted();
    }

    /// <summary>
    /// Accepts the 'Posted Transactions' file and submits it for post-processing
    /// </summary>
    /// <param name="file">The 'Posted Transactions' file</param>
    /// <param name="token">A token that can be used to cancel the work</param>
    /// <response code="202">The file has been submitted for post-processing</response>
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

    /// <summary>
    /// Accepts the 'Non Financial' file and submits it for post-processing
    /// </summary>
    /// <param name="file">The 'Non Financial' file</param>
    /// <param name="token">A token that can be used to cancel the work</param>
    /// <response code="202">The file has been submitted for post-processing</response>
    [HttpPost("ingest-non-financial")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> IngestNonFinancialFile(IFormFile file, CancellationToken token = default)
    {
        var fileName = _fileSystem.Path.GetFileName(file.FileName);
        var filePath = _fileSystem.Path.Combine(_nonFinancialSettings.WorkingDirectory, fileName);

        _fileSystem.Directory.CreateDirectory(_nonFinancialSettings.WorkingDirectory);

        await using (var stream = _fileSystem.File.Create(filePath))
        {
            await file.CopyToAsync(stream, token).ConfigureAwait(false);
        }

        await _publishEndpoint.Publish<NonFinancialFileProcessed>(new { FileName = fileName }, token).ConfigureAwait(false);

        return Accepted();
    }
}

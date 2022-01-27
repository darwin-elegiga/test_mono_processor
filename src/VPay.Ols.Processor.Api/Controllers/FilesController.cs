using Microsoft.AspNetCore.Mvc;

namespace VPay.Ols.Processor.Api.Controllers;
public class FilesController : Controller
{
    [HttpPost("ingest-posted-transactions")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> IngestPostedTransactionsFile(IFormFile file, CancellationToken token = default)
    {

    }
}

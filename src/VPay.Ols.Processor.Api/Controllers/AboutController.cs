using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.Ols.Processor.Api.Models;

namespace VPay.Ols.Processor.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(System.Net.Mime.MediaTypeNames.Application.Json)]
[Consumes(System.Net.Mime.MediaTypeNames.Application.Json)]
public sealed class AboutController : ControllerBase
{
    /// <summary>
    /// About the build that created the service
    /// </summary>
    /// <returns>Information about the build that created the service</returns>
    /// <response code="200">Information about the build that created the service</response>
    [HttpGet]
    [ProducesResponseType(typeof(AboutInfo), StatusCodes.Status200OK)]
    public AboutInfo GetDetail() => AboutInfo.GetBuildAboutInfo();
}

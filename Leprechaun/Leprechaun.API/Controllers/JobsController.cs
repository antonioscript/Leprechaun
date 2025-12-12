using Leprechaun.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Leprechaun.API.Controllers;

[ApiController]
[Route("jobs")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpPost("relatorio-email")]
    public async Task<IActionResult> RunReportEmail(CancellationToken cancellationToken)
    {
        await _jobService.RunJob("/relatorio_email", cancellationToken);
        return Ok();
    }
}

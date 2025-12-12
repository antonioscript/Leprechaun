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

    [HttpPost("pepsi-salary-friday")]
    public async Task<IActionResult> RunNotificationPepsiSalary(CancellationToken cancellationToken)
    {
        await _jobService.RunJob("/pepsi-salary-friday", cancellationToken);
        return Ok();
    }
    
    [HttpPost("biweekly-salary")]
    public async Task<IActionResult> RunSalaryBiweekly(CancellationToken cancellationToken)
    {
        await _jobService.RunJob("/biweekly-salary", cancellationToken);
        return Ok();
    }
}

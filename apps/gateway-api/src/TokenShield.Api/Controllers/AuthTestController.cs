using Microsoft.AspNetCore.Mvc;
using TokenShield.Application.Common.Interfaces;

namespace TokenShield.Api.Controllers;

[ApiController]
[Route("v1/auth-test")]
public class AuthTestController : ControllerBase
{
    private readonly IRequestContext _requestContext;

    public AuthTestController(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            authenticated = true,
            tenantId = _requestContext.TenantId,
            clientApplicationId = _requestContext.ClientApplicationId,
            apiKeyId = _requestContext.ApiKeyId,
            correlationId = _requestContext.CorrelationId
        });
    }
}

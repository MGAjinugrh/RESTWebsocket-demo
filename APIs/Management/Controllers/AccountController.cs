using Common.Facades;
using Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Common.Controllers;

[ApiController, Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IUserManagementFacade _userFacade;

    public AccountController(IUserManagementFacade userFacade)
    {
        _userFacade = userFacade;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromHeader] uint userId, CancellationToken ct)
    {
        var result = await _userFacade.GetAllAsync(userId, ct);
        return SwitchResponseCodeWithBody(result.Item1, result.Item2);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(uint id, CancellationToken ct)
    {
        var result = await _userFacade.GetByIdAsync(id, ct);
        return SwitchResponseCodeWithBody(result.Item1, result.Item2);
    }

    [HttpHead("{id}")]
    public async Task<IActionResult> CheckExistAsync(uint id, CancellationToken ct)
    {
        var result = await _userFacade.GetByIdAsync(id, ct);
        return SwitchResponseCode(result.Item1);
    }

    [HttpPost]
    public async Task<IActionResult> AddAsync([FromHeader] uint userId, [FromBody] ReqAddUser req, CancellationToken ct)
    {
        var (status, user) = await _userFacade.AddAsync(userId, req, ct);
        if (status == ReturnStatus.Created && user != null)
        {
            Response.Headers.Append("Location", $"api/account/{user.Id}");
        }

        return SwitchResponseCodeWithBody(status, user);
    }

    [HttpPatch("{id}/password")]
    public async Task<IActionResult> ChangePasswordAsync([FromHeader] uint userId, uint id, [FromBody] ReqUpdateUserPw req, CancellationToken ct)
    {
        var result = await _userFacade.ChangePasswordAsync(userId, id, req, ct);
        return SwitchResponseCode(result);
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActiveAsync([FromHeader] uint userId, uint id, CancellationToken ct)
    {
        var result = await _userFacade.ToggleActiveAsync(userId, id, ct);
        return SwitchResponseCode(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync([FromHeader] uint userId, uint id, CancellationToken ct)
    {
        var result = await _userFacade.DeleteAsync(userId, id, ct);
        return SwitchResponseCode(result);
    }

    private IActionResult SwitchResponseCode(ReturnStatus status)
        => status switch
        {
            ReturnStatus.BadRequest => BadRequest(),
            ReturnStatus.Unauthorized => StatusCode(StatusCodes.Status401Unauthorized),
            ReturnStatus.NotFound => NotFound(),
            ReturnStatus.Duplicate => Conflict(),
            ReturnStatus.Failed => StatusCode(StatusCodes.Status500InternalServerError),
            ReturnStatus.Created => Created(),
            ReturnStatus.SuccessNoContent => NoContent(),
            _ => Ok()
        };

    private IActionResult SwitchResponseCodeWithBody<T>(ReturnStatus status, T body)
        => status switch
        {
            ReturnStatus.BadRequest => BadRequest(),
            ReturnStatus.Unauthorized => StatusCode(StatusCodes.Status401Unauthorized),
            ReturnStatus.NotFound => NotFound(),
            ReturnStatus.Duplicate => Conflict(),
            ReturnStatus.Failed => StatusCode(StatusCodes.Status500InternalServerError),
            ReturnStatus.Created => StatusCode(StatusCodes.Status201Created, body),
            ReturnStatus.SuccessNoContent => NoContent(),
            _ => Ok(body)
        };
}

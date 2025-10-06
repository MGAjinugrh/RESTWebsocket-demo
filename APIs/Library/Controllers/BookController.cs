using Common;
using Common.Models;
using Library.Data;
using Library.Facades;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[ApiController, Route("api/book")]
public class BookController : ControllerBase
{
    private readonly IBookFacade _bookFacade;

    public BookController(LibraryDbContext db, SocketConnectionManager ws) {
        _bookFacade = new BookFacade(db, ws);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct)
    {
        var result = await _bookFacade.GetAllAsync(ct);
        return SwitchResponseCodeWithBody(result.Item1, result.Item2);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(uint id, CancellationToken ct)
    {
        var result = await _bookFacade.GetByIdAsync(id, ct);
        return SwitchResponseCodeWithBody(result.Item1, result.Item2);
    }

    [HttpHead("{id}")]
    public async Task<IActionResult> CheckExistAsync(uint id, CancellationToken ct)
    {
        var result = await _bookFacade.GetByIdAsync(id, ct);
        return SwitchResponseCode(result.Item1);
    }

    [HttpPost]
    public async Task<IActionResult> AddAsync([FromHeader] uint userId, [FromBody] ReqAddBook req, CancellationToken ct)
    {
        var (status, book) = await _bookFacade.AddAsync(userId, req, ct);
        if (status == ReturnStatus.Created && book != null)
        {
            Response.Headers.Append("Location", $"api/book/{book.Id}");
        }

        return SwitchResponseCodeWithBody(status, book);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpsertAsync([FromHeader] uint userId, uint id, [FromBody] ReqUpdateBook req, CancellationToken ct)
    {
        var result = await _bookFacade.UpdateAsync(userId, id, req, ct);
        return SwitchResponseCodeWithBody(result.Item1, result.Item2);
    }

    [HttpPatch("{id}/toggle-availability")]
    public async Task<IActionResult> ToggleAvailability([FromHeader] uint userId, uint id, CancellationToken ct)
    {
        var result = await _bookFacade.ToggleAvailabilityAsync(userId, id, ct);
        return SwitchResponseCode(result);
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive([FromHeader] uint userId, uint id, CancellationToken ct)
    {
        var result = await _bookFacade.ToggleActiveAsync(userId, id, ct);
        return SwitchResponseCode(result);
    }

    [HttpDelete("{id}")] //soft-deletion. This process is irreversible.
    public async Task<IActionResult> ToggleDeleted([FromHeader] uint userId, uint id, CancellationToken ct)
    {
        var result = await _bookFacade.DeleteAsync(userId, id, ct);
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
            ReturnStatus.Created => StatusCode(StatusCodes.Status201Created),
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

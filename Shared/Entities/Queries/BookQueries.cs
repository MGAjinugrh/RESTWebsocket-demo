using Common;
using Common.Models;
using Entities.Objects;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Entities.Queries;

public class BookQueries
{
    private IBookDbContext _db;

    public BookQueries (IBookDbContext db) => _db = db;

    /// <summary>
    /// Will return only !IsDeleted data
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Books>> GetListAsync(CancellationToken ct)
        => await _db.Books.Where(u => !u.IsDeleted).ToListAsync(ct);

    /// <summary>
    /// Will return only !IsDeleted data
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Books> GetDetailByIdAsync(uint id, CancellationToken ct)
        => await _db.Books
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
    
    /// <summary>
    /// Will return only if row is active
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Books> GetActiveByIdAsync(uint id, CancellationToken ct)
        => await _db.Books
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted && u.IsActive, ct);

    public async Task<Tuple<ReturnStatus, Books?>> AddAsync(uint userId, ReqAddBook req, CancellationToken ct)
    {
        // insensitive-case comparison for text-based fields since we use utf8mb4_general_ci collation
        string summary = string.IsNullOrWhiteSpace(req.Summary) ? "" : req.Summary!;
        summary = summary.Trim();
        if (_db.Books.Any(b => !b.IsDeleted
                           && b.Title == req.Title.Trim()
                           && b.Author == req.Author.Trim()
                           && b.Summary == summary
                           && b.DatePublished == req.DatePublished))
            return new Tuple<ReturnStatus, Books?>(ReturnStatus.Duplicate, null);

        var newBook = new Books
        {
            Title = req.Title.Trim(),
            Author = !string.IsNullOrWhiteSpace(req.Author) ? req.Author.Trim() : null,
            DatePublished = req.DatePublished,
            Summary = summary,
            CreatedAt = DateTime.UtcNow,
            CreatorId = userId
        };

        _db.Books.Add(newBook);

        try
        {
            var result = await _db.SaveChangesAsync(ct);
            if(result == 1)
            {
                var getBook = await GetDetailByIdAsync(newBook.Id, ct);
                return new Tuple<ReturnStatus, Books?>(ReturnStatus.Created, getBook);
            }
            else
            {
                return new Tuple<ReturnStatus, Books?>(ReturnStatus.Failed, null);
            }
        }
        catch (DbUpdateException dbx) when (dbx.InnerException is MySqlException mySqlEx && mySqlEx.Number == 1062) // 1062 = Duplicate entry
        {
            return new Tuple<ReturnStatus, Books?>(ReturnStatus.Duplicate, null);
        }
        catch (Exception)
        {
            return new Tuple<ReturnStatus, Books?>(ReturnStatus.Failed, null);
        }
    }

    public async Task<Tuple<ReturnStatus, Books?>> UpdateAsync(uint userId, uint id, ReqUpdateBook req, CancellationToken ct)
    {
        var book = await GetDetailByIdAsync(id, ct);

        if (book == null)
        {
            string author = req.Author ?? "-";
            var newReq = new ReqAddBook
            {
                Title = req.Title?.Trim() ?? throw new ArgumentNullException(nameof(req.Title)),
                Author = string.IsNullOrWhiteSpace(author) ? "-" : author.Trim(),
                DatePublished = req.DatePublished,
                Summary = !string.IsNullOrWhiteSpace(req.Summary) ? req.Summary?.Trim() : null,
            };
            return await AddAsync(userId, newReq, ct);
        }

        if (!string.IsNullOrWhiteSpace(req.Title))
            book.Title = req.Title!.Trim();
        if (!string.IsNullOrWhiteSpace(req.Author))
            book.Author = req.Author?.Trim();
        if (req.DatePublished.HasValue)
            book.DatePublished = req.DatePublished.Value;
        if (!string.IsNullOrWhiteSpace(req.Summary))
            book.Summary = req.Summary?.Trim();

        book.UpdatedAt = DateTime.UtcNow;
        book.UpdaterId = userId;

        try
        {
            var result = await _db.SaveChangesAsync(ct);
            if (result == 1)
            {
                return new Tuple<ReturnStatus, Books?>(ReturnStatus.Success, book);
            }
            else
            {
                return new Tuple<ReturnStatus, Books?>(ReturnStatus.Failed, null);
            }
        }
        catch (DbUpdateException dbx) when (dbx.InnerException is MySqlException mySqlEx && mySqlEx.Number == 1062) // 1062 = Duplicate entry
        {
            return new Tuple<ReturnStatus, Books?>(ReturnStatus.Duplicate, null);
        }
        catch (Exception)
        {
            return new Tuple<ReturnStatus, Books?>(ReturnStatus.Failed, null);
        }
    }

    public async Task<ReturnStatus> ToggleAvailabilityAsync(uint currentUserId, Books book, CancellationToken ct)
    {
        book.Status = (BookStatus)book.Status == BookStatus.Available ?
                            (int)BookStatus.NotAvailable : (int)BookStatus.Available; // will always produce the opposite value
        book.UpdatedAt = DateTime.UtcNow;
        book.UpdaterId = currentUserId;

        var result = await _db.SaveChangesAsync(ct);
        return result == 1 ? ReturnStatus.Success : ReturnStatus.Failed;
    }

    public async Task<ReturnStatus> ToggleActiveAsync(uint currentUserId, Books book, CancellationToken ct)
    {
        book.IsActive = !book.IsActive; // will always produce the opposite bool value
        book.UpdatedAt = DateTime.UtcNow;
        book.UpdaterId = currentUserId;

        var result = await _db.SaveChangesAsync(ct);
        return result == 1 ? ReturnStatus.Success : ReturnStatus.Failed;
    }

    public async Task<ReturnStatus> DeleteAsync(uint currentUserId, uint id, CancellationToken ct)
    {
        var book = await GetDetailByIdAsync(id, ct);

        if (book == null)
            return ReturnStatus.NotFound;

        book.IsDeleted = true;
        book.UpdatedAt = DateTime.UtcNow;
        book.UpdaterId = currentUserId;

        var result = await _db.SaveChangesAsync(ct);
        return result == 1 ? ReturnStatus.Success : ReturnStatus.Failed;
    }
}

using Common;
using Common.Models;
using Entities.Objects;
using Microsoft.EntityFrameworkCore;

namespace Entities.Queries;

public class UserQueries
{
    private IUserDbContext _db;

    public UserQueries(IUserDbContext db) => _db = db;

    public async Task<IEnumerable<Users>> GetListAsync(CancellationToken ct)
        => await _db.Users.Include(u => u.Role).Where(u => !u.IsDeleted).ToListAsync(ct);

    public async Task<Users> GetDetailByIdAsync(uint id, CancellationToken ct)
        => await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

    public async Task<Users> GetActiveUserByIdAsync(uint id, CancellationToken ct)
        => await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted && u.IsActive, ct);

    public async Task<Tuple<ReturnStatus, Users?>> AddAsync(int roleId, string username, string password, uint createdBy, CancellationToken ct)
    {
        if (_db.Users.Any(u => u.Username == username && !u.IsDeleted))
            return new Tuple<ReturnStatus, Users?>(ReturnStatus.Duplicate, null);

        var newUser = new Users
        {
            RoleId = roleId,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            CreatorId = createdBy
        };

        _db.Users.Add(newUser);

        try
        {
            var result = await _db.SaveChangesAsync(ct);
            if (result == 1)
            {
                var getUser = await GetDetailByIdAsync(newUser.Id, ct);
                return new Tuple<ReturnStatus, Users?>(ReturnStatus.Created, getUser);
            }
            else
            {
                return new Tuple<ReturnStatus, Users?>(ReturnStatus.Failed, null);
            }
        }
        catch (Exception)
        {
            return new Tuple<ReturnStatus, Users?>(ReturnStatus.Failed, null);
        }
    }

    public async Task<ReturnStatus> ChangePasswordAsync(uint currentUserId, uint id, string password, CancellationToken ct)
    {
        var user = await GetDetailByIdAsync(id, ct);

        if(user == null)
            return ReturnStatus.NotFound;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdaterId = currentUserId;

        var result = await _db.SaveChangesAsync(ct);
        return result == 1 ? ReturnStatus.SuccessNoContent : ReturnStatus.Failed;
    }

    public async Task<ReturnStatus> ToggleActiveAsync(uint currentUserId, uint id, CancellationToken ct)
    {
        var user = await GetDetailByIdAsync(id, ct);

        if (user == null)
            return ReturnStatus.NotFound;

        user.IsActive = !user.IsActive; // will always produce the opposite bool value
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdaterId = currentUserId;

        var result = await _db.SaveChangesAsync(ct);
        return result == 1 ? ReturnStatus.SuccessNoContent : ReturnStatus.Failed;
    }

    public async Task<ReturnStatus> DeleteAsync(uint currentUserId, uint id, CancellationToken ct)
    {
        var user = await GetDetailByIdAsync(id, ct);

        if (user == null)
            return ReturnStatus.NotFound;

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdaterId = currentUserId;

        var result = await _db.SaveChangesAsync(ct);
        return result == 1 ? ReturnStatus.SuccessNoContent : ReturnStatus.Failed;
    }
}

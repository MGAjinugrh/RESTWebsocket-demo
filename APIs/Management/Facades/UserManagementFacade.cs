using Entities.Objects;
using Entities.Queries;
using Common.Data;
using Common.Models;

namespace Common.Facades;

public interface IUserManagementFacade
{
    Task<Tuple<ReturnStatus, IEnumerable<PresentUser>?>> GetAllAsync(uint currentUserId, CancellationToken ct);
    Task<Tuple<ReturnStatus, PresentUser?>> GetByIdAsync(uint id, CancellationToken ct);
    Task<Tuple<ReturnStatus, PresentUser?>> AddAsync(uint currentUserId, ReqAddUser user, CancellationToken ct);
    Task<ReturnStatus> ChangePasswordAsync(uint currentUserId, uint id, ReqUpdateUserPw updatedUser, CancellationToken ct);
    Task<ReturnStatus> ToggleActiveAsync(uint currentUserId, uint id, CancellationToken ct);
    Task<ReturnStatus> DeleteAsync(uint currentUserId, uint id, CancellationToken ct);
}

public class UserManagementFacade : IUserManagementFacade
{
    private readonly UserQueries _userQry;
    public UserManagementFacade(ManagementDbContext db)
    {
        _userQry = new UserQueries(db);
    }

    /// <summary>
    /// In real case scenario, this feature would be only accessible to admins so that they can manage each user later on
    /// </summary>
    /// <param name="currentUserId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Tuple<ReturnStatus,IEnumerable<PresentUser>?>> GetAllAsync(uint currentUserId, CancellationToken ct)
    {
        var result = (IEnumerable<PresentUser>?)null;
        try
        {
            Users currentUserDetail = await _userQry.GetActiveUserByIdAsync(currentUserId, ct);

            if (currentUserDetail == null || currentUserDetail.RoleId != 1)
                return new Tuple<ReturnStatus, IEnumerable<PresentUser>?>(ReturnStatus.Unauthorized, (IEnumerable<PresentUser>?)null);

            var users = await _userQry.GetListAsync(ct);

            if (users == null || !users.Any())
                return new Tuple<ReturnStatus, IEnumerable<PresentUser>?>(ReturnStatus.SuccessNoContent, (IEnumerable<PresentUser>?)null);

            result = users.Select(u => new PresentUser
            {
                Id = u.Id,
                Role = u.Role.Name,
                Username = u.Username,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted,
                CreatedAt = u.CreatedAt,
                CreatedBy = Helpers.GetStringValByUInt(u.CreatorId, users), // Helper function below
                ModifiedAt = u.UpdatedAt,
                ModifiedBy = Helpers.GetStringValByUInt(u.CreatorId, users) // Also reuse helper
            }).ToList();
        }
        catch (Exception)
        {
            return new Tuple<ReturnStatus, IEnumerable<PresentUser>?>(ReturnStatus.Failed, (IEnumerable<PresentUser>?)null);
        }

        return new Tuple<ReturnStatus, IEnumerable<PresentUser>?>(ReturnStatus.Success, (IEnumerable<PresentUser>?)result);
    }

    /// <summary>
    /// To get specific user based by ID, only accessible to admin role
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Tuple<ReturnStatus,PresentUser?>> GetByIdAsync(uint id, CancellationToken ct)
    {
        PresentUser? result = null;
        try
        {
            var user = await _userQry.GetDetailByIdAsync(id, ct);
            if (user == null)
                return new Tuple<ReturnStatus, PresentUser?>(ReturnStatus.SuccessNoContent, result);

            result = new PresentUser
            {
                Id = user.Id,
                Role = user.Role.Name,
                Username = user.Username,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                CreatedAt = user.CreatedAt,
                CreatedBy = await GetCreatorUsernameDbAsync(user.CreatorId),
                ModifiedAt = user.UpdatedAt,
                ModifiedBy = await GetCreatorUsernameDbAsync(user.UpdaterId)
            };
        }
        catch (Exception)
        {
            return new Tuple<ReturnStatus, PresentUser?>(ReturnStatus.Failed, result);
        }

        return new Tuple<ReturnStatus, PresentUser?>(ReturnStatus.Success, result);
    }

    /// <summary>
    /// Adding new user, only accessible to admin role
    /// </summary>
    /// <param name="currentUserId"></param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Tuple<ReturnStatus, PresentUser?>> AddAsync(uint currentUserId, ReqAddUser req, CancellationToken ct)
    {
        try
        {
            var verifyPassword = VerifyPassword(req.Password, req.RePassword);
            if (verifyPassword != ReturnStatus.Success) 
                return new Tuple<ReturnStatus, PresentUser?>(verifyPassword, null);

            var currentUserDetail = await _userQry.GetActiveUserByIdAsync(currentUserId, ct);

            if (currentUserDetail == null || currentUserDetail.RoleId != 1) 
                return new Tuple<ReturnStatus, PresentUser?>(ReturnStatus.Unauthorized, null);

            var addQry = await _userQry.AddAsync(req.RoleId, req.UserName, req.Password, currentUserDetail.Id, ct);
            if(addQry.Item1 == ReturnStatus.Created && addQry.Item2 != null)
            {
                var addedUser = addQry.Item2;

                var presentUser = new PresentUser
                {
                    Id = addedUser.Id,
                    Role = addedUser.Role.Name,
                    Username = addedUser.Username,
                    IsActive = addedUser.IsActive,
                    IsDeleted = addedUser.IsDeleted,
                    CreatedAt = addedUser.CreatedAt,
                    CreatedBy = await GetCreatorUsernameDbAsync(addedUser.CreatorId),
                    ModifiedAt = addedUser.UpdatedAt,
                    ModifiedBy = await GetCreatorUsernameDbAsync(addedUser.UpdaterId)
                };

                return new Tuple<ReturnStatus, PresentUser?>(addQry.Item1, presentUser);
            }
            else
            {
                return new Tuple<ReturnStatus, PresentUser?>(addQry.Item1, null);
            }
        }catch (Exception) {
            return new Tuple<ReturnStatus, PresentUser?>(ReturnStatus.Failed,null);
        }
    }

    /// <summary>
    /// Change user password, only admin allowed
    /// </summary>
    /// <param name="currentUserId"></param>
    /// <param name="id"></param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ReturnStatus> ChangePasswordAsync(uint currentUserId, uint id, ReqUpdateUserPw req, CancellationToken ct)
    {
        try
        {
            var verifyPassword = VerifyPassword(req.Password, req.RePassword);
            if (verifyPassword != ReturnStatus.Success) return verifyPassword;

            Users currentUserDetail = await _userQry.GetActiveUserByIdAsync(currentUserId, ct);

            if (currentUserDetail == null || currentUserDetail.RoleId != 1) return ReturnStatus.Unauthorized;

            return await _userQry.ChangePasswordAsync(currentUserDetail.Id, id, req.Password, ct);
        }
        catch (Exception) {
            return ReturnStatus.Failed; 
        }
    }

    /// <summary>
    /// Activating or deactivating an account, only admins are able to do this.
    /// However, for the demo purpose an inactive admin can self-reactivate
    /// </summary>
    /// <param name="currentUserId"></param>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ReturnStatus> ToggleActiveAsync(uint currentUserId, uint id, CancellationToken ct)
    {
        try
        {
            Users currentUserDetail = await _userQry.GetDetailByIdAsync(currentUserId, ct);

            if (currentUserDetail == null || currentUserDetail.RoleId != 1) return ReturnStatus.Unauthorized;

            return await _userQry.ToggleActiveAsync(currentUserDetail.Id, id, ct);
        }
        catch (Exception) { 
            return ReturnStatus.Failed; 
        }
    }

    /// <summary>
    /// Perform soft-delete on a user (irreversible), admin privilege.
    /// </summary>
    /// <param name="currentUserId"></param>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ReturnStatus> DeleteAsync(uint currentUserId, uint id, CancellationToken ct)
    {
        try
        {
            Users currentUserDetail = await _userQry.GetActiveUserByIdAsync(currentUserId, ct);

            if (currentUserDetail == null || currentUserDetail.RoleId != 1) return ReturnStatus.Unauthorized;

            return await _userQry.DeleteAsync(currentUserDetail.Id, id, ct);
        }
        catch (Exception) { 
            return ReturnStatus.Failed; 
        }
    }

    private async Task<string> GetCreatorUsernameDbAsync(uint? id)
    {
        if (id == null || id == 0) return "-";
        var user = await _userQry.GetDetailByIdAsync((uint)id, CancellationToken.None);
        return user?.Username ?? "-";
    }

    private ReturnStatus VerifyPassword(string password, string rePassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(rePassword))
            return ReturnStatus.BadRequest;

        password = password.Trim();
        rePassword = rePassword.Trim();

        if (password != rePassword)
            return ReturnStatus.BadRequest;

        return ReturnStatus.Success;
    }
}

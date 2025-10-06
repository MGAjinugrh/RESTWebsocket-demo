using Entities.Queries;
using Library.Data;

namespace Library.Facades
{
    public interface IUserFacade
    {
        Task<List<uint>?> GetListUserIdsAsync(CancellationToken ct);
    }

    public class UserFacade :IUserFacade
    {
        private readonly UserQueries _userQry;

        public UserFacade(LibraryDbContext db)
        {
            _userQry = new UserQueries(db);
        }

        public async Task<List<uint>?> GetListUserIdsAsync(CancellationToken ct)
        {
            var users = await _userQry.GetListAsync(ct);
            return users.Select(x => x.Id).ToList();
        }
    }
}

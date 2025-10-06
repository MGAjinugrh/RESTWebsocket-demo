using Entities.Objects;
using Entities.Queries;
using Library.Data;
using Common;
using Common.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Library.Facades
{
    public interface IBookFacade
    {
        Task<Tuple<ReturnStatus, IEnumerable<PresentBook>?>> GetAllAsync(CancellationToken ct);
        Task<Tuple<ReturnStatus, PresentBook?>> GetByIdAsync(uint id, CancellationToken ct);
        Task<Tuple<ReturnStatus, PresentBook?>> AddAsync(uint userId, ReqAddBook req, CancellationToken ct);
        Task<Tuple<ReturnStatus, PresentBook?>> UpdateAsync(uint userId, uint id, ReqUpdateBook req, CancellationToken ct);
        Task<ReturnStatus> ToggleAvailabilityAsync(uint currentUserId, uint id, CancellationToken ct);
        Task<ReturnStatus> ToggleActiveAsync(uint currentUserId, uint id, CancellationToken ct);
        Task<ReturnStatus> DeleteAsync(uint currentUserId, uint id, CancellationToken ct);
    }
    
    public class BookFacade : IBookFacade
    {
        private readonly UserQueries _userQry;
        private readonly BookQueries _bookQry;
        private readonly SocketConnectionManager _ws;

        public BookFacade(LibraryDbContext db, SocketConnectionManager ws) {
            _bookQry = new BookQueries(db);
            _userQry = new UserQueries(db);
            _ws = ws;
        }

        /// <summary>
        /// In real case scenario, this feature would be only accessible to admins so that they can manage each user later on
        /// </summary>
        /// <param name="currentUserId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Tuple<ReturnStatus, IEnumerable<PresentBook>?>> GetAllAsync(CancellationToken ct)
        {
            var result = (IEnumerable<PresentBook>?)null;
            try
            {
                var books = await _bookQry.GetListAsync(ct);

                if (books == null || !books.Any())
                    return Tuple.Create(ReturnStatus.SuccessNoContent, (IEnumerable<PresentBook>?)null);

                result = books.Select(b => new PresentBook
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author ?? "-",
                    DatePublished = b.DatePublished,
                    Summary = b.Summary ?? "-",
                    Status = (BookStatus)b.Status,
                    IsActive = b.IsActive,
                    IsDeleted = b.IsDeleted,
                    CreatedAt = b.CreatedAt,
                    CreatedBy = Helpers.GetStringValByUInt(b.CreatorId, books),
                    ModifiedAt = b.UpdatedAt,
                    ModifiedBy = Helpers.GetStringValByUInt(b.UpdaterId, books)
                }).ToList();
            }
            catch (Exception)
            {
                return Tuple.Create(ReturnStatus.Failed, (IEnumerable<PresentBook>?)null);
            }

            return Tuple.Create(ReturnStatus.Success, (IEnumerable<PresentBook>?)result);
        }

        /// <summary>
        /// To get specific user based by ID, only accessible to admin role
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Tuple<ReturnStatus, PresentBook?>> GetByIdAsync(uint id, CancellationToken ct)
        {
            PresentBook? result = null;
            try
            {
                var book = await _bookQry.GetDetailByIdAsync(id, ct);
                if (book == null)
                    return new Tuple<ReturnStatus, PresentBook?>(ReturnStatus.SuccessNoContent, result);

                var creator = book.UpdaterId == null
                                ? "-"
                                : (await _userQry.GetDetailByIdAsync(book.CreatorId, ct))?.Username;
                var updater = book.UpdaterId == null
                                ? "-"
                                : (await _userQry.GetDetailByIdAsync((uint)book.UpdaterId, ct))?.Username;

                result = new PresentBook
                {
                    Id = book.Id,
                    Title = book.Title,
                    Author = book.Author ?? "-",
                    DatePublished = book.DatePublished,
                    Summary = book.Summary ?? "-",
                    Status = (BookStatus)book.Status,
                    IsActive = book.IsActive,
                    IsDeleted = book.IsDeleted,
                    CreatedAt = book.CreatedAt,
                    CreatedBy = creator ?? "-",
                    ModifiedAt = book.UpdatedAt,
                    ModifiedBy = updater ?? "-"
                };
            }
            catch (Exception)
            {
                return new Tuple<ReturnStatus, PresentBook?>(ReturnStatus.Failed, result);
            }

            return new Tuple<ReturnStatus, PresentBook?>(ReturnStatus.Success, result);
        }

        /// <summary>
        /// Add new book. Active admin privilege.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Tuple<ReturnStatus, PresentBook?>> AddAsync(uint userId, ReqAddBook req, CancellationToken ct)
        {
            try
            {
                var userDetail = await _userQry.GetActiveUserByIdAsync(userId, ct);

                if (userDetail == null || userDetail.RoleId != 1)
                    return new Tuple<ReturnStatus, PresentBook?>(ReturnStatus.Unauthorized, null);

                var addQry = await _bookQry.AddAsync(userId, req, ct);
                if (addQry.Item1 == ReturnStatus.Created && addQry.Item2 != null)
                {
                    var addedBook = addQry.Item2;

                    var presentBook = new PresentBook
                    {
                        Id = addedBook.Id,
                        Title = addedBook.Title,
                        Author = addedBook.Author ?? "-",
                        DatePublished = addedBook.DatePublished,
                        Summary = addedBook.Summary ?? "-",
                        Status = (BookStatus)addedBook.Status,
                        IsActive = addedBook.IsActive,
                        IsDeleted = addedBook.IsDeleted,
                        CreatedAt = addedBook.CreatedAt,
                        CreatedBy = userDetail.Username ?? "-",
                        ModifiedAt = addedBook.UpdatedAt,
                        ModifiedBy = "-"
                    };

                    await SendWsEvent(BookWsEvent.Add, addedBook.Id, ct);

                    return new Tuple<ReturnStatus, PresentBook?>(addQry.Item1, presentBook);
                }
                else
                {
                    return new Tuple<ReturnStatus, PresentBook?>(addQry.Item1, null);
                }

            }
            catch (Exception)
            {
                return new Tuple<ReturnStatus, PresentBook?>(ReturnStatus.Failed, null);
            }
        }

        /// <summary>
        /// Upsert a book (PUT)
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Tuple<ReturnStatus, PresentBook?>> UpdateAsync(uint userId, uint id, ReqUpdateBook req, CancellationToken ct)
        {
            try
            {
                var userDetail = await _userQry.GetActiveUserByIdAsync(userId, ct);

                if (userDetail == null || userDetail.RoleId != 1)
                    return new Tuple<ReturnStatus, PresentBook?>(ReturnStatus.Unauthorized, null);

                var updateQry = await _bookQry.UpdateAsync(userId, id, req, ct);

                if((updateQry.Item1 == ReturnStatus.Success || updateQry.Item1 == ReturnStatus.Created)
                    && updateQry.Item2 != null)
                {
                    var presentBook = new PresentBook
                    {
                        Id = updateQry.Item2.Id,
                        Title = updateQry.Item2.Title,
                        Author = updateQry.Item2.Author ?? "-",
                        DatePublished = updateQry.Item2.DatePublished,
                        Summary = updateQry.Item2.Summary ?? "-",
                        Status = (BookStatus)updateQry.Item2.Status,
                        IsActive = updateQry.Item2.IsActive,
                        IsDeleted = updateQry.Item2.IsDeleted,
                        CreatedAt = updateQry.Item2.CreatedAt,
                        CreatedBy = userDetail.Username ?? "-",
                        ModifiedAt = updateQry.Item2.UpdatedAt,
                        ModifiedBy = "-"
                    };

                    if(updateQry.Item1 == ReturnStatus.Created)
                        await SendWsEvent(BookWsEvent.Add, id, ct);

                    return new Tuple<ReturnStatus, PresentBook?>(updateQry.Item1, presentBook);
                }
                else
                {
                    return new Tuple<ReturnStatus, PresentBook?>(updateQry.Item1, null);
                }
            }
            catch (Exception)
            {
                return new Tuple<ReturnStatus, PresentBook?>(ReturnStatus.Failed, null);
            }
        }

        /// <summary>
        /// Set Available of a book. Active admin privilege.
        /// </summary>
        /// <param name="currentUserId"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ReturnStatus> ToggleAvailabilityAsync(uint currentUserId, uint id, CancellationToken ct)
        {
            try
            {
                Users currentUserDetail = await _userQry.GetActiveUserByIdAsync(currentUserId, ct);

                if (currentUserDetail == null || currentUserDetail.RoleId != 1) return ReturnStatus.Unauthorized;

                var book = await _bookQry.GetActiveByIdAsync(id, ct);

                if (book == null)
                    return ReturnStatus.NotFound;

                if (book.Status == (int)BookStatus.Available)
                {
                    await SendWsEvent(BookWsEvent.Unavailable, id, ct);
                }
                else
                {
                    await SendWsEvent(BookWsEvent.Available, id, ct);
                }

                return await _bookQry.ToggleAvailabilityAsync(currentUserDetail.Id, book, ct);
            }
            catch (Exception)
            {
                return ReturnStatus.Failed;
            }
        }

        /// <summary>
        /// Set Active/Inactive of a book. Active admin privilege.
        /// </summary>
        /// <param name="currentUserId"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ReturnStatus> ToggleActiveAsync(uint currentUserId, uint id, CancellationToken ct)
        {
            try
            {
                Users currentUserDetail = await _userQry.GetActiveUserByIdAsync(currentUserId, ct);

                if (currentUserDetail == null || currentUserDetail.RoleId != 1) return ReturnStatus.Unauthorized;

                var book = await _bookQry.GetDetailByIdAsync(id, ct);

                if (book == null)
                    return ReturnStatus.NotFound;

                if (book.IsActive)
                {
                    await SendWsEvent(BookWsEvent.Unavailable, id, ct);
                }
                else
                {
                    await SendWsEvent(BookWsEvent.Available, id, ct);
                }

                return await _bookQry.ToggleActiveAsync(currentUserDetail.Id, book, ct);
            }
            catch (Exception) { 
                return ReturnStatus.Failed; 
            }
        }

        /// <summary>
        /// Soft delete a particular book. Irreversible. Active admin privilege.
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

                await SendWsEvent(BookWsEvent.Remove, id, ct);

                return await _bookQry.DeleteAsync(currentUserDetail.Id, id, ct);
            }
            catch (Exception) { 
                return ReturnStatus.Failed; 
            }
        }

        /// <summary>
        /// Broadcast event regarding book collection to active users
        /// </summary>
        /// <param name="wsEvent"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SendWsEvent(BookWsEvent wsEvent, uint id, CancellationToken ct)
        {
            var users = await _userQry.GetListAsync(ct);
            var userIds = users.Select(x => x.Id).ToList();

            var bookDetail = await _bookQry.GetDetailByIdAsync(id, ct);

            string msg = string.Empty;
            switch (wsEvent)
            {
                case BookWsEvent.Add:
                    msg = $"New entry \"{bookDetail.Title}\" has been added into catalogue.";
                    break;
                case BookWsEvent.Available:
                    msg = $"New entry \"{bookDetail.Title}\" is available to be borrowed.";
                    break;
                case BookWsEvent.Unavailable:
                    msg = $"New entry \"{bookDetail.Title}\" is unavailable to be borrowed at the moment.";
                    break;
                case BookWsEvent.Remove:
                    msg = $"New entry \"{bookDetail.Title}\" has been removed from the catalogue.";
                    break;
            }

            await _ws.BroadcastEventAsync(wsEvent.GetDisplayName(), userIds, msg);
        }
    }
}

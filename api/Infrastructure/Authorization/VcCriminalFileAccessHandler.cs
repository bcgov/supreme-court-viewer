using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Scv.Api.Helpers.Extensions;
using Scv.Db.Models;

namespace Scv.Api.Infrastructure.Authorization
{
    public class VcCriminalFileAccessHandler
    {

        private ScvDbContext Db { get; }
        public VcCriminalFileAccessHandler(ScvDbContext db)
        {
            Db = db;
        }
        public async Task<bool> HasCriminalFileAccess(ClaimsPrincipal user, string criminalFileId)
        {
            var userId = user.UserId();
            var now = DateTimeOffset.UtcNow;
            var fileAccess = await Db.RequestFileAccess
                .AnyAsync(r => r.UserId == userId && r.Expires > now && r.FileId == criminalFileId);
            return fileAccess;
        }
    }
}

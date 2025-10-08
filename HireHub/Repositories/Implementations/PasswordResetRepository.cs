using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HireHub.API.Repositories.Implementations
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly HireHubContext _ctx;
        public PasswordResetRepository(HireHubContext ctx) { _ctx = ctx; }

        public async Task<PasswordReset> AddAsync(PasswordReset reset)
        {
            _ctx.PasswordResets.Add(reset);
            await _ctx.SaveChangesAsync();
            return reset;
        }

        public async Task<PasswordReset?> GetByTokenHashAsync(string tokenHash)
        {
            return await _ctx.PasswordResets
                .FirstOrDefaultAsync(p => p.TokenHash == tokenHash && !p.Used);
        }

        public async Task MarkUsedAsync(PasswordReset reset)
        {
            reset.Used = true;
            _ctx.PasswordResets.Update(reset);
            await _ctx.SaveChangesAsync();
        }
    }
}

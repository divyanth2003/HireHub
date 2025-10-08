using HireHub.API.Models;
using System.Threading.Tasks;

namespace HireHub.API.Repositories.Interfaces
{
    public interface IPasswordResetRepository
    {
        Task<PasswordReset> AddAsync(PasswordReset reset);
        Task<PasswordReset?> GetByTokenHashAsync(string tokenHash);
        Task MarkUsedAsync(PasswordReset reset);
    }
}

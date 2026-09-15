using IsoTreatmentProcessSupportAPI.Entities;
using IsoTreatmentProcessSupportAPI.Exceptions;
using IsoTreatmentProcessSupportAPI.Gateways;
using IsoTreatmentProcessSupportAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace IsoTreatmentProcessSupportAPI.Services
{
    public interface IReminderService
    {
        Task<IEnumerable<ReminderDto>> GetAllAsync(string token, CancellationToken cancellationToken = default);
        Task<ReminderDto> GetByIdAsync(string token, int id, CancellationToken cancellationToken = default);
        Task<ReminderDto> AddAsync(string token, CreateAndUpdateReminderDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(string token, int id, CancellationToken cancellationToken = default);
        Task<ReminderDto> UpdateAsync(string token, int id, CreateAndUpdateReminderDto dto, CancellationToken cancellationToken = default);
    }
    public class ReminderService : IReminderService
    {
        private readonly IsoSupportDbContext _dbContext;
        private readonly IReminderGateway _reminderGateway;
        private readonly ITokenService _tokenService;
        public ReminderService(IsoSupportDbContext dbContext, IReminderGateway reminderGateway, ITokenService tokenService)
        {
            _dbContext = dbContext;
            _reminderGateway = reminderGateway;
            _tokenService = tokenService;
        }
        public async Task<ReminderDto> AddAsync(string token, CreateAndUpdateReminderDto dto, CancellationToken cancellationToken = default)
        {
            int userId = await GetExistingUserIdAsync(token, cancellationToken);

            return await _reminderGateway.AddAsync(userId, dto.Time, cancellationToken);
        }

        public async Task DeleteAsync(string token, int id, CancellationToken cancellationToken = default)
        {
            int userId = await GetExistingUserIdAsync(token, cancellationToken);

            if (!await _reminderGateway.DeleteAsync(id, userId, cancellationToken))
            {
                throw new NotFoundException("Reminder not found");
            }
        }

        public async Task<IEnumerable<ReminderDto>> GetAllAsync(string token, CancellationToken cancellationToken = default)
        {
            int userId = await GetExistingUserIdAsync(token, cancellationToken);

            return await _reminderGateway.GetAllForUserAsync(userId, cancellationToken);
        }

        public async Task<ReminderDto> GetByIdAsync(string token, int id, CancellationToken cancellationToken = default)
        {
            int userId = await GetExistingUserIdAsync(token, cancellationToken);

            return await _reminderGateway.GetByIdForUserAsync(id, userId, cancellationToken)
                ?? throw new NotFoundException("Reminder not found");
        }

        public async Task<ReminderDto> UpdateAsync(string token, int id, CreateAndUpdateReminderDto dto, CancellationToken cancellationToken = default)
        {
            int userId = await GetExistingUserIdAsync(token, cancellationToken);

            return await _reminderGateway.UpdateAsync(id, userId, dto.Time, cancellationToken)
                ?? throw new NotFoundException("Reminder not found");
        }

        // The monolith still owns Users, so checking that the user exists stays here.
        // IReminderGateway covers reminder storage only.
        private async Task<int> GetExistingUserIdAsync(string token, CancellationToken cancellationToken)
        {
            int userId = _tokenService.GetUserIdFromToken(token);

            if (!await _dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            {
                throw new NotFoundException("User not found");
            }

            return userId;
        }
    }
}

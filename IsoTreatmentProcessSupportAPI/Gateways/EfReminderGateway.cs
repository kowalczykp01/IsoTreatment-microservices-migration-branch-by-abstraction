using AutoMapper;
using IsoTreatmentProcessSupportAPI.Entities;
using IsoTreatmentProcessSupportAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace IsoTreatmentProcessSupportAPI.Gateways
{
    public class EfReminderGateway : IReminderGateway
    {
        private readonly IsoSupportDbContext _dbContext;
        private readonly IMapper _mapper;
        public EfReminderGateway(IsoSupportDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ReminderDto>> GetAllForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var reminders = await _dbContext.Reminders
                .Where(r => r.UserId == userId)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<ReminderDto>>(reminders);
        }

        public async Task<ReminderDto?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var reminder = await FindForUserAsync(id, userId, cancellationToken);

            return reminder is null ? null : _mapper.Map<ReminderDto>(reminder);
        }

        public async Task<ReminderDto> AddAsync(int userId, TimeOnly time, CancellationToken cancellationToken = default)
        {
            var reminder = new Reminder { UserId = userId, Time = time };

            _dbContext.Reminders.Add(reminder);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ReminderDto>(reminder);
        }

        public async Task<ReminderDto?> UpdateAsync(int id, int userId, TimeOnly time, CancellationToken cancellationToken = default)
        {
            var reminder = await FindForUserAsync(id, userId, cancellationToken);

            if (reminder is null)
            {
                return null;
            }

            reminder.Time = time;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ReminderDto>(reminder);
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var reminder = await FindForUserAsync(id, userId, cancellationToken);

            if (reminder is null)
            {
                return false;
            }

            _dbContext.Reminders.Remove(reminder);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        private Task<Reminder?> FindForUserAsync(int id, int userId, CancellationToken cancellationToken) =>
            _dbContext.Reminders.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);
    }
}

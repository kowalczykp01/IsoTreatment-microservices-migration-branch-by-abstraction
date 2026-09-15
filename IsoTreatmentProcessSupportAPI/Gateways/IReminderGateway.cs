using IsoTreatmentProcessSupportAPI.Models;

namespace IsoTreatmentProcessSupportAPI.Gateways
{
    public interface IReminderGateway
    {
        Task<IReadOnlyList<ReminderDto>> GetAllForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<ReminderDto?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default);
        Task<ReminderDto> AddAsync(int userId, TimeOnly time, CancellationToken cancellationToken = default);
        Task<ReminderDto?> UpdateAsync(int id, int userId, TimeOnly time, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    }
}

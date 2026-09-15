using IsoTreatmentProcessSupportAPI.Models;
using Microsoft.AspNetCore.Authentication;
using System.Net;
using System.Net.Http.Headers;

namespace IsoTreatmentProcessSupportAPI.Gateways
{
    public class TreatmentServiceReminderGateway : IReminderGateway
    {
        private const string RemindersPath = "api/reminder";

        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TreatmentServiceReminderGateway(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        // The Treatment service identifies the user by the token alone. The userId parameters
        // are already checked by ReminderService against Users and are not sent over the wire.
        public async Task<IReadOnlyList<ReminderDto>> GetAllForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Get, RemindersPath, null, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<ReminderDto>>(cancellationToken) ?? new List<ReminderDto>();
        }

        public async Task<ReminderDto?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Get, $"{RemindersPath}/{id}", null, cancellationToken);

            return await ReadReminderOrNullAsync(response, cancellationToken);
        }

        public async Task<ReminderDto> AddAsync(int userId, TimeOnly time, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Post, RemindersPath, time, cancellationToken);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<ReminderDto>(cancellationToken))!;
        }

        public async Task<ReminderDto?> UpdateAsync(int id, int userId, TimeOnly time, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Put, $"{RemindersPath}/{id}", time, cancellationToken);

            return await ReadReminderOrNullAsync(response, cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Delete, $"{RemindersPath}/{id}", null, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, TimeOnly? time, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, path);

            // The monolith has already validated the token for the incoming request; it is
            // forwarded as a plain bearer header, not as the cookie the frontend sends.
            var token = await _httpContextAccessor.HttpContext!.GetTokenAsync("access_token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (time is not null)
            {
                request.Content = JsonContent.Create(new CreateAndUpdateReminderDto { Time = time.Value });
            }

            return await _httpClient.SendAsync(request, cancellationToken);
        }

        private static async Task<ReminderDto?> ReadReminderOrNullAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReminderDto>(cancellationToken);
        }
    }
}

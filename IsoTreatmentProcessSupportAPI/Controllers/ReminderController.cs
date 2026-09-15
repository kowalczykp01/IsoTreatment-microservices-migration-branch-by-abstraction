using IsoTreatmentProcessSupportAPI.Models;
using IsoTreatmentProcessSupportAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoTreatmentProcessSupportAPI.Controllers
{
    [Route("api/reminder")]
    [ApiController]
    [Authorize]
    public class ReminderController : ControllerBase
    {
        private readonly IReminderService _reminderService;
        public ReminderController(IReminderService reminderService)
        {
            _reminderService = reminderService;
        }
        [HttpPost()]
        public async Task<ActionResult<ReminderDto>> Add([FromBody] CreateAndUpdateReminderDto dto, CancellationToken cancellationToken)
        {
            var token = HttpContext.Request.Cookies["token"];

            var addedReminder = await _reminderService.AddAsync(token, dto, cancellationToken);

            return Ok(addedReminder);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
        {
            var token = HttpContext.Request.Cookies["token"];

            await _reminderService.DeleteAsync(token, id, cancellationToken);

            return NoContent();
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<ReminderDto>> Get([FromRoute] int id, CancellationToken cancellationToken)
        {
            var token = HttpContext.Request.Cookies["token"];

            ReminderDto reminder = await _reminderService.GetByIdAsync(token, id, cancellationToken);

            return Ok(reminder);
        }
        [HttpGet()]
        public async Task<ActionResult<IEnumerable<ReminderDto>>> GetAll(CancellationToken cancellationToken)
        {
            var token = HttpContext.Request.Cookies["token"];

            var reminders = await _reminderService.GetAllAsync(token, cancellationToken);

            return Ok(reminders);
        }
        [HttpPut("{id}")]
        public async Task<ActionResult<ReminderDto>> Update([FromRoute] int id, [FromBody] CreateAndUpdateReminderDto dto, CancellationToken cancellationToken)
        {
            var token = HttpContext.Request.Cookies["token"];

            var updatedReminder = await _reminderService.UpdateAsync(token, id, dto, cancellationToken);

            return Ok(updatedReminder);
        }
    }
}

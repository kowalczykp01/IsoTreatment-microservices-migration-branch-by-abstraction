using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreatmentService.Api.Models;
using TreatmentService.Application.Abstractions;
using TreatmentService.Application.Exceptions;
using TreatmentService.Application.Reminders.Commands.AddReminder;
using TreatmentService.Application.Reminders.Commands.DeleteReminder;
using TreatmentService.Application.Reminders.Commands.UpdateReminder;
using TreatmentService.Application.Reminders.Models;
using TreatmentService.Application.Reminders.Queries.GetAllReminders;
using TreatmentService.Application.Reminders.Queries.GetById;

namespace TreatmentService.Api.Controllers;

[Route("api/reminder")]
[ApiController]
[Authorize]
public sealed class ReminderController(
    IQueryHandler<GetAllRemindersQueryParams, IEnumerable<ReminderDto>> getAllReminders,
    IQueryHandler<GetReminderByIdQueryParams, ReminderDto> getReminderById,
    ICommandHandler<AddReminderCommandParams, ReminderDto> addReminder,
    ICommandHandler<UpdateReminderCommandParams, ReminderDto> updateReminder,
    ICommandHandler<DeleteReminderCommandParams> deleteReminder)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReminderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var reminders = await getAllReminders.HandleAsync(
            new GetAllRemindersQueryParams(CurrentUserId), cancellationToken);

        return Ok(reminders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReminderDto>> Get([FromRoute] int id, CancellationToken cancellationToken)
    {
        var reminder = await getReminderById.HandleAsync(
            new GetReminderByIdQueryParams(id, CurrentUserId), cancellationToken);

        return Ok(reminder);
    }

    [HttpPost]
    public async Task<ActionResult<ReminderDto>> Add(
        [FromBody] CreateAndUpdateReminderRequest request,
        CancellationToken cancellationToken)
    {
        var reminder = await addReminder.HandleAsync(
            new AddReminderCommandParams(CurrentUserId, request.Time), cancellationToken);

        return Ok(reminder);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReminderDto>> Update(
        [FromRoute] int id,
        [FromBody] CreateAndUpdateReminderRequest request,
        CancellationToken cancellationToken)
    {
        var reminder = await updateReminder.HandleAsync(
            new UpdateReminderCommandParams(id, CurrentUserId, request.Time), cancellationToken);

        return Ok(reminder);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        await deleteReminder.HandleAsync(
            new DeleteReminderCommandParams(id, CurrentUserId), cancellationToken);

        return NoContent();
    }

    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UserNotFoundException();
}

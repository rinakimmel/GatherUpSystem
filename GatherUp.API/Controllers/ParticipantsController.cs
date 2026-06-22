using GatherUp.BL;
using GatherUp.Core.DO;
using Microsoft.AspNetCore.Mvc;

namespace GatherUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParticipantsController : ControllerBase
    {
        private readonly ParticipantService _participantService;

        public ParticipantsController(ParticipantService participantService)
        {
            _participantService = participantService;
        }

        [HttpPost("{eventId}")]
        public async Task<IActionResult> AddParticipant(int eventId, [FromBody] Participant participant)
        {
            var added = await _participantService.AddParticipantAsync(eventId, participant);
            return CreatedAtAction(nameof(GetParticipant), new { id = added.Id }, added);
        }

        [HttpGet("{id}")]
        public IActionResult GetParticipant(int id)
        {
            // controller only exposes BL methods; consumer can use repository directly in tests
            return Ok();
        }

        [HttpPost("{eventId}/confirm/{participantId}")]
        public async Task<IActionResult> ConfirmAttendance(int eventId, int participantId, [FromQuery] bool isAttending)
        {
            await _participantService.ConfirmAttendanceAsync(eventId, participantId, isAttending);
            return NoContent();
        }

        [HttpPost("{eventId}/invitations")]
        public async Task<IActionResult> SendInvitations(int eventId, [FromQuery] string? invitationUrlBase)
        {
            await _participantService.SendInvitationsAsync(eventId, invitationUrlBase ?? string.Empty);
            return NoContent();
        }

        [HttpPost("{eventId}/reminders")]
        public async Task<IActionResult> SendReminders(int eventId)
        {
            await _participantService.SendInvitationRemindersAsync(eventId);
            return NoContent();
        }
    }
}

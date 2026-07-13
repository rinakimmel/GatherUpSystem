using GatherUp.BL;
using GatherUp.Core;
using GatherUp.Core.DO;
using Microsoft.AspNetCore.Mvc;

namespace GatherUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParticipantsController : ControllerBase
    {
        private readonly ParticipantService _participantService;
        private readonly IRepository<Participant> _participantRepo;
        private readonly IRepository<Event> _eventRepo;

        public ParticipantsController(
            ParticipantService participantService,
            IRepository<Participant> participantRepo,
            IRepository<Event> eventRepo)
        {
            _participantService = participantService;
            _participantRepo = participantRepo;
            _eventRepo = eventRepo;
        }

        [HttpPost("{eventId}")]
        public async Task<IActionResult> AddParticipant(int eventId, [FromBody] Participant participant)
        {
            var added = await _participantService.AddParticipantAsync(eventId, participant);
            return CreatedAtAction(nameof(GetParticipant), new { id = added.Id }, added);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetParticipant(int id)
        {
            var p = await _participantRepo.GetByIdAsync(id);
            if (p == null) return NotFound(new { error = $"Participant {id} not found." });
            return Ok(new
            {
                p.Id, p.Name, p.Email,
                p.IsAttending, p.HasPaid, p.AmountContributed,
                MailingPreferences = p.MailingPreferences.ToString()
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var all = await _participantRepo.GetAllAsync();
            return Ok(all.Select(p => new
            {
                p.Id, p.Name, p.Email,
                p.IsAttending, p.HasPaid, p.AmountContributed,
                MailingPreferences = p.MailingPreferences.ToString()
            }));
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

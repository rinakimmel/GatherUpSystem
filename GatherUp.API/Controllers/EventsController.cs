using GatherUp.Core;
using GatherUp.Core.DO;
using Microsoft.AspNetCore.Mvc;

namespace GatherUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IRepository<Event> _eventRepo;
        private readonly IRepository<EventManager> _managerRepo;
        private readonly IRepository<EventHost> _hostRepo;
        private readonly IRepository<Participant> _participantRepo;
        private readonly IRepository<Poll> _pollRepo;

        public EventsController(
            IRepository<Event> eventRepo,
            IRepository<EventManager> managerRepo,
            IRepository<EventHost> hostRepo,
            IRepository<Participant> participantRepo,
            IRepository<Poll> pollRepo)
        {
            _eventRepo = eventRepo;
            _managerRepo = managerRepo;
            _hostRepo = hostRepo;
            _participantRepo = participantRepo;
            _pollRepo = pollRepo;
        }

        // GET /api/events
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var events = await _eventRepo.GetAllAsync();
            var result = new List<object>();

            foreach (var ev in events)
            {
                var manager = ev.EventManagerId > 0 ? await _managerRepo.GetByIdAsync(ev.EventManagerId) : null;
                var host = ev.EventHostId > 0 ? await _hostRepo.GetByIdAsync(ev.EventHostId) : null;
                result.Add(new
                {
                    ev.Id,
                    ev.Name,
                    ev.Description,
                    ev.Date,
                    ev.Location,
                    ev.PricePerParticipant,
                    ev.PaymentMethods,
                    ParticipantCount = ev.ParticipantIds.Count,
                    PollCount = ev.PollIds.Count,
                    ManagerName = manager?.Name,
                    HostName = host?.Name,
                    VendorCount = ev.Vendors.Count
                });
            }

            return Ok(result);
        }

        // GET /api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id);
            if (ev == null) return NotFound(new { error = $"Event {id} not found." });

            var manager = ev.EventManagerId > 0 ? await _managerRepo.GetByIdAsync(ev.EventManagerId) : null;
            var host = ev.EventHostId > 0 ? await _hostRepo.GetByIdAsync(ev.EventHostId) : null;

            var participants = await Task.WhenAll(
                ev.ParticipantIds.Select(pid => _participantRepo.GetByIdAsync(pid)));

            var polls = await Task.WhenAll(
                ev.PollIds.Select(pid => _pollRepo.GetByIdAsync(pid)));

            return Ok(new
            {
                ev.Id,
                ev.Name,
                ev.Description,
                ev.Date,
                ev.Location,
                ev.PricePerParticipant,
                ev.PaymentMethods,
                Manager = manager == null ? null : new { manager.Id, manager.Name, manager.Email },
                Host = host == null ? null : new { host.Id, host.Name, host.Email },
                Participants = participants
                    .Where(p => p != null)
                    .Select(p => new { p!.Id, p.Name, p.Email, p.IsAttending, p.HasPaid, p.AmountContributed }),
                Polls = polls
                    .Where(p => p != null)
                    .Select(p => new { p!.Id, p.Name, p.Description }),
                Vendors = ev.Vendors.Select(v => new { v.VendorName, v.AmountOwed, v.ReceiptsReceived })
            });
        }

        // POST /api/events
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
        {
            var ev = new Event(0, dto.Name, dto.Description ?? string.Empty)
            {
                Date = dto.Date,
                Location = dto.Location ?? string.Empty,
                PricePerParticipant = dto.PricePerParticipant,
                PaymentMethods = dto.PaymentMethods ?? string.Empty
            };

            if (dto.ManagerId > 0)
            {
                var mgr = await _managerRepo.GetByIdAsync(dto.ManagerId);
                if (mgr != null) ev.EventManagerId = mgr.Id;
            }

            if (dto.HostId > 0)
            {
                var host = await _hostRepo.GetByIdAsync(dto.HostId);
                if (host != null) ev.EventHostId = host.Id;
            }

            await _eventRepo.AddAsync(ev);
            return CreatedAtAction(nameof(GetById), new { id = ev.Id }, new { ev.Id, ev.Name });
        }

        // PUT /api/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateEventDto dto)
        {
            var ev = await _eventRepo.GetByIdAsync(id);
            if (ev == null) return NotFound(new { error = $"Event {id} not found." });

            ev.Name = dto.Name;
            ev.Description = dto.Description ?? ev.Description;
            ev.Date = dto.Date ?? ev.Date;
            ev.Location = dto.Location ?? ev.Location;
            ev.PricePerParticipant = dto.PricePerParticipant ?? ev.PricePerParticipant;
            ev.PaymentMethods = dto.PaymentMethods ?? ev.PaymentMethods;

            await _eventRepo.UpdateAsync(ev);
            return Ok(new { ev.Id, ev.Name });
        }

        // DELETE /api/events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id);
            if (ev == null) return NotFound(new { error = $"Event {id} not found." });
            await _eventRepo.DeleteAsync(id);
            return NoContent();
        }

        // GET /api/events/{id}/participants
        [HttpGet("{id}/participants")]
        public async Task<IActionResult> GetParticipants(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id);
            if (ev == null) return NotFound(new { error = $"Event {id} not found." });

            var participants = await Task.WhenAll(ev.ParticipantIds.Select(pid => _participantRepo.GetByIdAsync(pid)));

            return Ok(participants
                .Where(p => p != null)
                .Select(p => new
                {
                    p!.Id,
                    p.Name,
                    p.Email,
                    p.IsAttending,
                    p.HasPaid,
                    p.AmountContributed,
                    MailingPreferences = p.MailingPreferences.ToString()
                }));
        }

        // POST /api/events/{id}/host
        [HttpPost("{id}/host")]
        public async Task<IActionResult> SetHost(int id, [FromBody] CreateHostDto dto)
        {
            var ev = await _eventRepo.GetByIdAsync(id);
            if (ev == null) return NotFound(new { error = $"Event {id} not found." });

            var host = new EventHost(0, dto.Name, dto.Email);
            await _hostRepo.AddAsync(host);

            ev.EventHostId = host.Id;
            await _eventRepo.UpdateAsync(ev);

            return Ok(new { host.Id, host.Name, host.Email });
        }

        // GET /api/events/{id}/polls
        [HttpGet("{id}/polls")]
        public async Task<IActionResult> GetPolls(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id);
            if (ev == null) return NotFound(new { error = $"Event {id} not found." });

            var polls = await Task.WhenAll(ev.PollIds.Select(pid => _pollRepo.GetByIdAsync(pid)));

            return Ok(polls
                .Where(p => p != null)
                .Select(p => new { p!.Id, p.Name, p.Description, QuestionCount = p.Questions.Count }));
        }
    }

    public record CreateEventDto(
        string Name,
        string? Description,
        DateTime? Date,
        string? Location,
        decimal? PricePerParticipant,
        string? PaymentMethods,
        int ManagerId,
        int HostId);

    public record CreateHostDto(string Name, string Email);
}

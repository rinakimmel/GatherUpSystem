using GatherUp.BL;
using GatherUp.Core.DO;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace GatherUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PollsController : ControllerBase
    {
        private readonly PollService _pollService;

        public PollsController(PollService pollService) => _pollService = pollService;

        [HttpPost("{eventId}")]
        public async Task<IActionResult> CreatePoll(int eventId, [FromBody] PollDto dto)
        {
            var q = dto.Questions.Select((qText, idx) => (Question: qText.Question, Options: qText.Options.AsEnumerable()));
            var poll = await _pollService.CreatePollAsync(eventId, dto.Name, dto.Description, q);
            return CreatedAtAction(nameof(GetPoll), new { id = poll.Id }, poll);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPoll(int id)
        {
            var res = await _pollService.GetPollResultsAsync(id);
            return Ok(res);
        }

        [HttpPost("{pollId}/vote")]
        public async Task<IActionResult> Vote(int pollId, [FromBody] VoteDto vote)
        {
            await _pollService.SubmitVoteAsync(pollId, vote.QuestionId, vote.ParticipantId, vote.Choice);
            return NoContent();
        }

        [HttpGet("{pollId}/results")]
        public async Task<IActionResult> GetResults(int pollId)
        {
            var res = await _pollService.GetPollResultsAsync(pollId);
            return Ok(res);
        }
    }

    public record PollDto(string Name, string Description, List<PollQuestionDto> Questions);
    public record PollQuestionDto(string Question, List<string> Options);
    public record VoteDto(int QuestionId, int ParticipantId, string Choice);
}
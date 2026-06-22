using System.Threading.Tasks;

namespace GatherUp.Core
{
    public interface IMailService
    {
        void Send(string toEmail, string subject, string body);
        Task SendAsync(string toEmail, string subject, string body);
    }
}

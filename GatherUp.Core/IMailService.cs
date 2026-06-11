namespace GatherUp.Core
{
    public interface IMailService
    {
        void Send(string toEmail, string subject, string body);
    }
}

using GatherUp.Core;

namespace GatherUp.Infrastructure
{
    public class FileMailService : IMailService
    {
        private readonly string _logPath;

        public FileMailService(string logFilePath)
        {
            _logPath = logFilePath;
        }

        public void Send(string toEmail, string subject, string body)
        {
            var entry = $"""
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]
                TO: {toEmail}
                SUBJECT: {subject}
                BODY: {body}
                {new string('-', 60)}

                """;
            File.AppendAllText(_logPath, entry);
        }
    }
}

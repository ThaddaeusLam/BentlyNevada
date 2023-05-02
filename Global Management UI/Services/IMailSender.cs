//Code written by Caleb Stickler

using System.Threading.Tasks;

namespace Global_Management_UI.Services
{
    public interface IMailSender
    {
        Task SendEmailAsync(string appuser, string email, string subject, string HtmlMessage);
    }
}

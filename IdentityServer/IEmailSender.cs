using System.Threading.Tasks;

namespace IdentityServer
{
  public interface IEmailSender
  {
    Task SendEmailAsync(string email, string subject, string message);
  }
}

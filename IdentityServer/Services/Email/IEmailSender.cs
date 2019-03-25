using System.Threading.Tasks;

namespace IdentityServer3.Services.Email
{
  public interface IEmailSender
  {
    Task SendEmailAsync(string email, string subject, string message);
  }
}

using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using FluentEmail.Mailgun;
using Microsoft.AspNetCore.Identity.UI.Services;
using Mail = FluentEmail.Core.Email;

namespace IdentityServer.Services.Email
{
  //----------------------------------------------------------------------------------------------
  // This class is used by the application to send email for account confirmation and password 
  // reset.
  //
  // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
  //----------------------------------------------------------------------------------------------

  public class EmailSender : IEmailSender
  {
    public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor)
    {
      Options = optionsAccessor.Value;
    }

    public AuthMessageSenderOptions Options { get; } //set only via Secret Manager

    public Task SendEmailAsync(string toEmailAddress, string subject, string message)
    {
      // TODO: Localize?
      var email = Mail.From("noreply@citizens.exchange", "Citizens' Exchange")
                      .To(toEmailAddress)
                      .Subject(subject)
                      .Body(message);

      email.Sender = new MailgunSender(Options.MailgunDomain, Options.MailgunApiKey);

      var response = email.SendAsync();

      return response;
    }
  }
}

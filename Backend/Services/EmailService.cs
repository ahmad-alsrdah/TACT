using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TACT.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
        emailMessage.To.Add(new MailboxAddress("", toEmail));
        emailMessage.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
        emailMessage.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        
        await client.ConnectAsync(emailSettings["Host"], int.Parse(emailSettings["Port"]!), MailKit.Security.SecureSocketOptions.Auto);
        
        await client.AuthenticateAsync(emailSettings["Username"], emailSettings["Password"]);
        
        await client.SendAsync(emailMessage);
        await client.DisconnectAsync(true);
    }
}
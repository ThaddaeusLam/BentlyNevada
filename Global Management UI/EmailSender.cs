//Caleb Stickler, code taken from https://ffimnsr.medium.com/sending-email-using-mailkit-in-asp-net-core-web-api-71b946380442

using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Global_Management_UI.Services
{
    public class EmailSender : IMailSender
    {
        private string host;
        private int port;
        private bool enableSSL;
        private string sender;
        private string username;
        private string password;

        public EmailSender(string _host, int _port, bool _ssl, string _sender, string _username, string _password)
        {
            host = _host;
            port = _port;
            enableSSL = _ssl;
            sender = _sender;
            username = _username;
            password = _password;
        }

        public async Task SendEmailAsync(string appuser, string email, string subject, string htmlMessage)
        {
            try
            {
                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress(sender, username));
                msg.To.Add(new MailboxAddress(appuser, email));
                msg.Subject = subject;
                msg.Body = new TextPart("html")
                {
                    Text = htmlMessage
                };

                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(username, password);
                    await client.SendAsync(msg);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message);
            }
        }
    }
}

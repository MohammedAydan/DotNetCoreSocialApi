using Microsoft.Extensions.Options;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Social.Infrastucture.Repositories
{
    public class EmailRepository : IEmailRepository
    {
        private readonly EmailSettings _emailSettings;

        public EmailRepository(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
                    throw new ArgumentException("Invalid email address.");

                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                {
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                    client.EnableSsl = _emailSettings.EnableSSL;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (SmtpException e)
            {
                Console.WriteLine($"SMTP Error: {e.Message}, StatusCode: {e.StatusCode}");
                if (e.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {e.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            if (!Uri.IsWellFormedUriString(resetLink, UriKind.Absolute))
                throw new ArgumentException("Invalid reset link.");

            string html = $@"
            <html>
              <head>
                <style>
                  @media only screen and (max-width: 600px) {{
                    .container {{ padding: 20px; }}
                    .button {{ padding: 10px 20px; font-size: 14px; }}
                  }}
                </style>
              </head>
              <body style='font-family: Roboto, Arial, sans-serif; margin: 0; padding: 20px;'>
                <div class='container' style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 30px; border-radius: 10px;'>
                  <h2 style='color: #333333; font-size: 24px; margin-bottom: 20px;'>Password Reset Request</h2>
                  <p style='font-size: 16px; color: #555555; line-height: 1.6; margin-bottom: 20px;'>Hi {toEmail},</p>
                  <p style='font-size: 16px; color: #555555; line-height: 1.6; margin-bottom: 20px;'>We received a request to reset your password. Click the button below to set a new one.</p>
                  <p style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' class='button' style='background-color: #e71e4f; color: #ffffff; padding: 12px 24px; border-radius: 15px; text-decoration: none; font-weight: bold; display: inline-block;'>Reset Password</a>
                  </p>
                  <p style='font-size: 14px; color: #777777; line-height: 1.6; margin-bottom: 20px;'>If you didn't request this, you can safely ignore this email.</p>
                  <p style='font-size: 12px; color: #999999; text-align: center; margin-top: 30px; border-top: 1px solid #eeeeee; padding-top: 20px;'>&copy; {DateTime.UtcNow.Year} SOCIAL. All rights reserved.</p>
                </div>
              </body>
            </html>";

            await SendEmailAsync(toEmail, "Reset Your Password - SOCIAL", html);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string loginLink)
        {
            if (!Uri.IsWellFormedUriString(loginLink, UriKind.Absolute))
                throw new ArgumentException("Invalid login link.");

            string html = $@"
            <html>
              <head>
                <style>
                  @media only screen and (max-width: 600px) {{
                    .container {{ padding: 20px; }}
                    .button {{ padding: 10px 20px; font-size: 14px; }}
                  }}
                </style>
              </head>
              <body style='font-family: Roboto, Arial, sans-serif; margin: 0; padding: 20px;'>
                <div class='container' style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 30px; border-radius: 10px;'>
                  <h2 style='color: #333333; font-size: 24px; margin-bottom: 20px;'>Welcome to SOCIAL!</h2>
                  <p style='font-size: 16px; color: #555555; line-height: 1.6; margin-bottom: 20px;'>Hi {toEmail},</p>
                  <p style='font-size: 16px; color: #555555; line-height: 1.6; margin-bottom: 20px;'>Thank you for joining SOCIAL! We're thrilled to have you. Start exploring your account and connect with others today.</p>
                  <p style='text-align: center; margin: 30px 0;'>
                    <a href='{loginLink}' class='button' style='background-color: #e71e4f; color: #ffffff; padding: 12px 24px; border-radius: 15px; text-decoration: none; font-weight: bold; display: inline-block;'>Log In to Your Account</a>
                  </p>
                  <p style='font-size: 14px; color: #777777; line-height: 1.6; margin-bottom: 20px;'>Have questions? Our support team is here to help.</p>
                  <p style='font-size: 12px; color: #999999; text-align: center; margin-top: 30px; border-top: 1px solid #eeeeee; padding-top: 20px;'>&copy; {DateTime.UtcNow.Year} SOCIAL. All rights reserved.</p>
                </div>
              </body>
            </html>";

            await SendEmailAsync(toEmail, "Welcome to SOCIAL!", html);
        }

        public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
        {
            if (!Uri.IsWellFormedUriString(verificationLink, UriKind.Absolute))
                throw new ArgumentException("Invalid verification link.");

            string html = $@"
            <html>
              <head>
                <style>
                  @media only screen and (max-width: 600px) {{
                    .container {{ padding: 20px; }}
                    .button {{ padding: 10px 20px; font-size: 14px; }}
                  }}
                </style>
              </head>
              <body style='font-family: Roboto, Arial, sans-serif; margin: 0; padding: 20px;'>
                <div class='container' style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 30px; border-radius: 10px;'>
                  <h2 style='color: #333333; font-size: 24px; margin-bottom: 20px;'>Verify Your Email Address</h2>
                  <p style='font-size: 16px; color: #555555; line-height: 1.6; margin-bottom: 20px;'>Hi {toEmail},</p>
                  <p style='font-size: 16px; color: #555555; line-height: 1.6; margin-bottom: 20px;'>Thank you for registering with SOCIAL. Please verify your email address to activate your account.</p>
                  <p style='text-align: center; margin: 30px 0;'>
                    <a href='{verificationLink}' class='button' style='background-color: #e71e4f; color: #ffffff; padding: 12px 24px; border-radius: 15px; text-decoration: none; font-weight: bold; display: inline-block;'>Verify Email</a>
                  </p>
                  <p style='font-size: 14px; color: #777777; line-height: 1.6; margin-bottom: 20px;'>If you didn't create an account, you can ignore this email.</p>
                  <p style='font-size: 12px; color: #999999; text-align: center; margin-top: 30px; border-top: 1px solid #eeeeee; padding-top: 20px;'>&copy; {DateTime.UtcNow.Year} SOCIAL. All rights reserved.</p>
                </div>
              </body>
            </html>";

            await SendEmailAsync(toEmail, "Verify Your Email - SOCIAL", html);
        }

        public async Task SendPasswordChangeConfirmationAsync(string toEmail)
        {
            string html = $@"
                <html>
                   <head>
                     <style>
                       @media only screen and (max-width: 600px) {{
                         .container {{ padding: 20px; }}
                         .button {{ padding: 10px 20px; font-size: 14px; }}
                       }}
                     </style>
                   </head>
                   <body style='font-family: Roboto, Arial, sans-serif; margin: 0; padding: 20px;'>
                     <div class='container' style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 30px; border-radius: 10px;'>
                       <h2 style='color: #333333; font-size: 24px; margin-bottom: 20px;'>Password Changed Successfully</h2>
                       <p style='font-size: 16px; color: #555555; line-height: 1.6; margin-bottom: 20px;'>Hi {toEmail},</p>
                       <p style='font-size: 16px; color: #555555; line-height: 1.6; margin-bottom: 20px;'>Your password for SOCIAL has been successfully changed. No further action is required.</p>
                       <p style='font-size: 12px; color: #999999; text-align: center; margin-top: 30px; border-top: 1px solid #eeeeee; padding-top: 20px;'>&copy; {DateTime.UtcNow.Year} SOCIAL. All rights reserved.</p>
                     </div>
                   </body>
                 </html>";

            await SendEmailAsync(toEmail, "Password Changed - SOCIAL", html);
        }
    }
}
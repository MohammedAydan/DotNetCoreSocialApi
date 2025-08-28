using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface IEmailRepository
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendWelcomeEmailAsync(string toEmail, string loginLink);
        Task SendVerificationEmailAsync(string toEmail, string verificationLink);
        Task SendPasswordChangeConfirmationAsync(string toEmail);
    }
}

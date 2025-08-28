using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Users.Commands
{
    public record ResetPasswordCommand(ResetPasswordRequest ResetPasswordRequest) : IRequest<bool>;

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailRepository _emailRepository;

        public ResetPasswordCommandHandler(IUserRepository userRepository, IEmailRepository emailRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _emailRepository = emailRepository ?? throw new ArgumentNullException(nameof(emailRepository));
        }

        public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || request.ResetPasswordRequest == null)
                {
                    throw new ArgumentNullException(nameof(request), "Request or ResetPasswordRequest cannot be null.");
                }

                var resetRequest = request.ResetPasswordRequest;

                if (string.IsNullOrWhiteSpace(resetRequest.Email) /* || !resetRequest.Email.Contains("@") */ )
                {
                    throw new ArgumentException("Invalid email address.", nameof(resetRequest.Email));
                }

                if (string.IsNullOrWhiteSpace(resetRequest.Token))
                {
                    throw new ArgumentException("Reset token is required.", nameof(resetRequest.Token));
                }

                if (resetRequest.Password != resetRequest.ConfirmPassword)
                {
                    throw new ArgumentException("Passwords do not match.", nameof(resetRequest.ConfirmPassword));
                }

                bool result = await _userRepository.ResetPasswordAsync(
                    resetRequest.Email,
                    resetRequest.Password,
                    resetRequest.Token
                );

                if (!result)
                {
                    return false;
                }

                var emailDecodedBytes = WebEncoders.Base64UrlDecode(resetRequest.Email);
                var decodedEmail = Encoding.UTF8.GetString(emailDecodedBytes);
                // Send confirmation email after successful password reset
                await _emailRepository.SendPasswordChangeConfirmationAsync(decodedEmail);

                return true;
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Argument null error: {ex.Message}");
                throw;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in ResetPasswordCommandHandler: {ex.Message}");
                throw new InvalidOperationException("Failed to process password reset.", ex);
            }
        }
    }
}
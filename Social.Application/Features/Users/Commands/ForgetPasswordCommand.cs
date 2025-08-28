using MediatR;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Users.Commands
{
    public record ForgetPasswordCommand(ForgetPasswordRequest ForgetPasswordRequest) : IRequest<string?>;

    public class ForgetPasswordCommandHandler : IRequestHandler<ForgetPasswordCommand, string?>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailRepository _emailRepository;

        public ForgetPasswordCommandHandler(IUserRepository userRepository, IEmailRepository emailRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _emailRepository = emailRepository ?? throw new ArgumentNullException(nameof(emailRepository));
        }

        public async Task<string?> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || request.ForgetPasswordRequest == null)
                {
                    throw new ArgumentNullException(nameof(request), "Request or ForgetPasswordRequest cannot be null.");
                }

                if (string.IsNullOrWhiteSpace(request.ForgetPasswordRequest.email) ||
                    !request.ForgetPasswordRequest.email.Contains("@"))
                {
                    throw new ArgumentException("Invalid email address.", nameof(request.ForgetPasswordRequest.email));
                }

                string? resetUrl = await _userRepository.GeneratePasswordResetUrlAsync(request.ForgetPasswordRequest.email);

                if (string.IsNullOrEmpty(resetUrl))
                {
                    return null; // User not found or reset URL generation failed
                }

                await _emailRepository.SendPasswordResetEmailAsync(request.ForgetPasswordRequest.email, resetUrl);
                return resetUrl;
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
                Console.WriteLine($"Unexpected error in ForgetPasswordCommandHandler: {ex.Message}");
                throw new InvalidOperationException("Failed to process password reset request.", ex);
            }
        }
    }
}
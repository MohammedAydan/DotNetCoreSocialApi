using MediatR;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Users.Commands
{
    public record ChangePasswordCommand(string userId, ChangePasswordRequest ChangePasswordRequest) : IRequest<bool>;

    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
    {
        private readonly Core.Interfaces.IUserRepository _userRepository;
        private readonly IEmailRepository _emailRepository;
        public ChangePasswordCommandHandler(Core.Interfaces.IUserRepository userRepository, IEmailRepository emailRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _emailRepository = emailRepository;
        }

        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            if (request.ChangePasswordRequest == null)
            {
                throw new ArgumentNullException(nameof(request.ChangePasswordRequest), "ChangePasswordRequest cannot be null");
            }
            var result = await _userRepository.ChangePassword(
                request.userId,
                request.ChangePasswordRequest.CurrentPassword,
                request.ChangePasswordRequest.NewPassword,
                request.ChangePasswordRequest.ConfirmPassword
            );

            //if(result == true)
            //{
            //    //
            //}

            return result;
        }
    }
}

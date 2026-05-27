using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Users.Commands
{
    public class UpdateUserCommand : IRequest<IResponseWrapper>
    {
        public UpdateUserRequest UpdateUser { get; set; }
    }

    public class UpdateUserCommandHanlder(IUserService userService) : IRequestHandler<UpdateUserCommand, IResponseWrapper>
    {
        private readonly IUserService _userService = userService;

        public async Task<IResponseWrapper> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var userId = await _userService.UpdateAsync(request.UpdateUser);
            return await ResponseWrapper<string>.SuccessAsync(data: userId, message: "Utilisateur mise à jour avec succès.");
        }
    }
}

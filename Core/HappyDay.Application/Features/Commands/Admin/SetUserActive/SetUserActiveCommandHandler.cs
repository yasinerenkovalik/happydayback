using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Admin.SetUserActive;

public class SetUserActiveCommandHandler
    : IRequestHandler<SetUserActiveCommand, GeneralResponse<SetUserActiveResponse>>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthSessionRepository _sessions;

    public SetUserActiveCommandHandler(IUserRepository users, ICurrentUserService currentUser, IAuthSessionRepository sessions)
    {
        _sessions = sessions;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<GeneralResponse<SetUserActiveResponse>> Handle(
        SetUserActiveCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return new GeneralResponse<SetUserActiveResponse>
            {
                isSuccess = false,
                Message = "Kullanici Id'si zorunludur."
            };
        }

        // Admin kendini pasifleştirirse panele bir daha giremez
        if (!request.IsActivated && request.UserId == _currentUser.UserId)
        {
            return new GeneralResponse<SetUserActiveResponse>
            {
                isSuccess = false,
                Message = "Kendi hesabınızı pasifleştiremezsiniz."
            };
        }

        // Pasif kullanicilar da tekrar aktiflestirilebilmeli.
        var user = await _users.GetByIdIncludingInactiveAsync(request.UserId);

        if (user is null)
        {
            return new GeneralResponse<SetUserActiveResponse>
            {
                isSuccess = false,
                Message = "Kullanici bulunamadi."
            };
        }

        user.IsActivated = request.IsActivated;

        if (request.IsActivated)
        {
            user.DeleteDate = default;
        }

        await _users.UpdateAsync(user);

        // Pasifleştirilen kullanıcının açık oturumları hemen kapanır
        if (!request.IsActivated)
            await _sessions.RevokeAllAsync(user.Id, "deactivated");

        return new GeneralResponse<SetUserActiveResponse>
        {
            Data = new SetUserActiveResponse
            {
                UserId = user.Id,
                IsActivated = user.IsActivated
            },
            isSuccess = true,
            Message = request.IsActivated ? "Kullanici aktiflestirildi." : "Kullanici pasiflestirildi."
        };
    }
}

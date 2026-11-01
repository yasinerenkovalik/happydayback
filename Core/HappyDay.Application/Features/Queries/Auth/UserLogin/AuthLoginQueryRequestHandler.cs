using MapsterMapper;
using HappyDay.Application.Common.Security;           // IPasswordHasher
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.AutLogin
{
    public class AuthLoginQueryRequestHandler
        : IRequestHandler<AuthLoginQueryRequest, GeneralResponse<AuthLoginQueryResponse>>
    {
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IAuthSessionService _sessions;
        private readonly IPasswordHasher _passwordHasher;

        public AuthLoginQueryRequestHandler(
            IMapper mapper,
            IUserRepository userRepository,
            IAuthSessionService sessions,
            IPasswordHasher passwordHasher)             // <-- bcrypt hasher'ı enjekte ediyoruz
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _sessions = sessions;
            _passwordHasher = passwordHasher;
        }

        public async Task<GeneralResponse<AuthLoginQueryResponse>> Handle(
            AuthLoginQueryRequest request,
            CancellationToken cancellationToken)
        {
            // E-postayı normalize et
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

            // Kullanıcıyı getir (repo metodun ct almıyorsa ikinci argümanı kaldır)
            var user = await _userRepository.GetByEmailAsync(email /*, cancellationToken*/);
            if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return new GeneralResponse<AuthLoginQueryResponse>
                {
                    Message = Messages.MessageConstants.InvalidUserData,
                    isSuccess = false
                };
            }

            // Kilitliyken şifreye bakılmaz; doğru şifre de reddedilir
            if (LoginLockout.IsLockedOut(user))
            {
                return new GeneralResponse<AuthLoginQueryResponse>
                {
                    Message = LoginLockout.LockedMessage(user),
                    isSuccess = false
                };
            }

            // BCRYPT doğrulaması
            var passOk = _passwordHasher.Verify(request.Password ?? string.Empty, user.PasswordHash);
            if (!passOk)
            {
                LoginLockout.RegisterFailure(user);
                await _userRepository.UpdateAsync(user);

                return new GeneralResponse<AuthLoginQueryResponse>
                {
                    Message = LoginLockout.IsLockedOut(user)
                        ? LoginLockout.LockedMessage(user)
                        : Messages.MessageConstants.InvalidUserData,
                    isSuccess = false
                };
            }

            if (LoginLockout.Reset(user))
                await _userRepository.UpdateAsync(user);

            // Oturum aç — firmaya bağlı kullanıcı Company rolü alır; Admin yalnızca api/auth/login'den
            var tokens = await _sessions.StartAsync(AccountPrincipal.ForUser(user, allowAdmin: false));

            return new GeneralResponse<AuthLoginQueryResponse>
            {
                Message = Messages.MessageConstants.UserLogin,
                Data = new AuthLoginQueryResponse { Token = tokens.AccessToken, Tokens = tokens },
                isSuccess = true
            };
        }
    }
}

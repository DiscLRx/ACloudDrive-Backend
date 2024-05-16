using IdentityService.Controllers;
using IdentityService.Protos.Clients;
using Infrastructure.Databases;
using Infrastructure.Utils;
using SystemConfig = Infrastructure.Configuration.SystemConfig;

namespace IdentityService.Services
{
    public class SignUpService(
        MsSqlContext msSqlContext,
        VerificationCodeService verificationCodeService,
        UserCheckService userCheckService,
        GrpcRootDirectoryInitializerClient rootDirectoryInitializerClient,
        SystemConfig systemConfig)
    {
        private readonly MsSqlContext _msSqlContext = msSqlContext;
        private readonly VerificationCodeService _verificationCodeService = verificationCodeService;
        private readonly UserCheckService _userCheckService = userCheckService;
        private readonly GrpcRootDirectoryInitializerClient _rootDirectoryInitializerClient =
            rootDirectoryInitializerClient;
        private readonly SystemConfig _systemConfig = systemConfig;

        /// <summary>
        /// 发送邮件验证码
        /// </summary>
        /// <param name="emailArg"></param>
        /// <returns></returns>
        public async Task<AppResponse> SignUpSendEmailAsync(EmailArg emailArg)
        {
            var email = emailArg.Email;
            if (!_userCheckService.FormatChecker.EmailCheck(email))
            {
                return AppResponse.InvalidArgument("Bad Email Format");
            }

            if (!await _userCheckService.UniqueChecker.EmailCheckAsync(email))
            {
                return AppResponse.DuplicateEmail();
            }

            var vKey = await _verificationCodeService.MakeVerificationCodeAsync(email);
            return AppResponse.Success(new { Key = vKey });
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="signUpArgs"></param>
        /// <returns></returns>
        public async Task<AppResponse> SignUpAsync(SignUpArgs signUpArgs)
        {
            if (!_userCheckService.FormatChecker.CommonCheck(signUpArgs.Username)
                || !_userCheckService.FormatChecker.CommonCheck(signUpArgs.Password)
                || !_userCheckService.FormatChecker.DisplayNameCreateCheck(signUpArgs.DisplayName)
               )
            {
                return AppResponse.InvalidArgument("用户名、密码或昵称格式不正确");
            }

            if (!await _userCheckService.UniqueChecker.UsernameCheckAsync(signUpArgs.Username))
            {
                return AppResponse.DuplicateUsername();
            }

            var email = signUpArgs.Email;
            if (!await _verificationCodeService.VerifyCodeAsync(email, signUpArgs.Key, signUpArgs.Code))
            {
                return AppResponse.WrongVerificationCode();
            }

            var displayName = signUpArgs.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = signUpArgs.Username;
            }

            var user = new User
            {
                Username = signUpArgs.Username,
                Password = PasswordHelper.Create(signUpArgs.Password),
                DisplayName = displayName,
                Email = email,
                Enable = true,
                Role = "USER",
                TotalSpace = _systemConfig.UserDefaultSpace,
                UsedSpace = 0
            };
            await _msSqlContext.Users.AddAsync(user);
            await _msSqlContext.SaveChangesAsync();
            await _rootDirectoryInitializerClient.RootDirectoryInitializerAsync(user.Id);
            return AppResponse.Success();
        }
    }
}
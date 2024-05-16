using IdentityService.Controllers;
using Infrastructure.Databases;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services
{
    public class ModifyPasswordService(VerificationCodeService verificationCodeService, MsSqlContext msSqlContext, UserCheckService userCheckService)
    {
        private readonly VerificationCodeService _verificationCodeService = verificationCodeService;
        private readonly MsSqlContext _msSqlContext = msSqlContext;
        private readonly UserCheckService _userCheckService = userCheckService;

        public async Task<AppResponse> ForgetPasswordModifyAsync(ForgetPasswordModifyArgs forgetPasswordModifyArgs)
        {
            var email = forgetPasswordModifyArgs.Email;
            if (!await _verificationCodeService.VerifyCodeAsync(email, forgetPasswordModifyArgs.Key, forgetPasswordModifyArgs.Code))
            {
                return AppResponse.WrongVerificationCode();
            }

            var password = forgetPasswordModifyArgs.Password;
            if (!_userCheckService.FormatChecker.CommonCheck(password))
            {
                return AppResponse.InvalidArgument("不合法的密码格式");
            }

            var user = await _msSqlContext.Users.SingleAsync(u => u.Email == email);
            user.Password =  PasswordHelper.Create(password);
            await _msSqlContext.SaveChangesAsync();

            return AppResponse.Success();
        }

        public async Task<AppResponse> ForgetPasswordSendEmailAsync(EmailArg emailArg)
        {
            var email = emailArg.Email;
            var user = await _msSqlContext.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                return AppResponse.NoSuchUser();
            }

            var vKey = await _verificationCodeService.MakeVerificationCodeAsync(email);
            return AppResponse.Success(new { Key = vKey });
        }
    }
}

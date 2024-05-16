using IdentityService.Controllers;
using Infrastructure.Databases;
using Infrastructure.Services;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

public class UserService(
    HttpContextService httpContextService,
    MsSqlContext msSqlContext,
    UserCheckService userCheckService,
    VerificationCodeService verificationCodeService)
{
    private readonly HttpContextService _httpContextService = httpContextService;
    private readonly MsSqlContext _msSqlContext = msSqlContext;
    private readonly UserCheckService _userCheckService = userCheckService;
    private readonly VerificationCodeService _verificationCodeService = verificationCodeService;


    public async Task<AppResponse> GetUserInfoAsync()
    {
        var uid = _httpContextService.Uid();
        var user = await _msSqlContext.Users.SingleAsync(u => u.Id == uid);
        return AppResponse.Success(new
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            TotalSpace = user.TotalSpace,
            UsedSpace = user.UsedSpace
        });
    }

    public async Task<AppResponse> GetUsersAsync()
    {
        var users = await _msSqlContext.Users.Select(u => new
        {
            u.Id,
            u.Username,
            u.DisplayName,
            u.Email,
            u.Enable,
            u.UsedSpace,
            u.TotalSpace,
            u.Role
        }).ToListAsync();
        return AppResponse.Success(users);
    }

    public async Task<AppResponse> UpdateUsernameAsync(StringArg stringArg)
    {
        var username = stringArg.Value;
        if (!_userCheckService.FormatChecker.CommonCheck(username))
        {
            return AppResponse.InvalidArgument("用户名格式不正确");
        }

        if (!await _userCheckService.UniqueChecker.UsernameCheckAsync(username))
        {
            return AppResponse.DuplicateUsername();
        }

        var uid = _httpContextService.Uid();
        var user = await _msSqlContext.Users.SingleAsync(u => u.Id == uid);
        user.Username = username;
        await _msSqlContext.SaveChangesAsync();
        return AppResponse.Success();
    }

    public async Task<AppResponse> UpdateDisplayNameAsync(StringArg stringArg)
    {
        var displayName = stringArg.Value;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return AppResponse.InvalidArgument("昵称格式不正确");
        }

        var uid = _httpContextService.Uid();
        var user = await _msSqlContext.Users.SingleAsync(u => u.Id == uid);
        user.DisplayName = displayName;
        await _msSqlContext.SaveChangesAsync();
        return AppResponse.Success();
    }

    public async Task<AppResponse> UpdatePasswordAsync(StringArg stringArg)
    {
        var password = stringArg.Value;
        if (!_userCheckService.FormatChecker.CommonCheck(password))
        {
            return AppResponse.InvalidArgument("密码格式不正确");
        }

        var encPassword = PasswordHelper.Create(password);
        var uid = _httpContextService.Uid();
        var user = await _msSqlContext.Users.SingleAsync(u => u.Id == uid);
        user.Password = encPassword;
        await _msSqlContext.SaveChangesAsync();
        return AppResponse.Success();
    }

    public async Task<AppResponse> SendVerificationCodeOnUpdateEmailAsync(StringArg stringArg)
    {
        var email = stringArg.Value;
        if (!_userCheckService.FormatChecker.EmailCheck(email))
        {
            return AppResponse.InvalidArgument("邮箱格式不正确");
        }

        if (!await _userCheckService.UniqueChecker.EmailCheckAsync(email))
        {
            return AppResponse.DuplicateEmail();
        }

        var vKey = await _verificationCodeService.MakeVerificationCodeAsync(email);
        return AppResponse.Success(new { Key = vKey });
    }

    public async Task<AppResponse> UpdateEmailAsync(UpdateEmailArgs updateEmailArgs)
    {
        var email = updateEmailArgs.Email;
        var key = updateEmailArgs.Key;
        var code = updateEmailArgs.Code;
        if (!await _verificationCodeService.VerifyCodeAsync(email, key, code))
        {
            return AppResponse.WrongVerificationCode();
        }

        var uid = _httpContextService.Uid();
        var user = await _msSqlContext.Users.SingleAsync(u => u.Id == uid);
        user.Email = email;
        await _msSqlContext.SaveChangesAsync();
        return AppResponse.Success();
    }

    private static bool CheckString(params string[] strs) =>
        strs.All(str => !string.IsNullOrWhiteSpace(str) && str.Length <= 50);


    private static bool CheckTotalSpace(long size) => size >= 0;

    public async Task<AppResponse> EditUserAsync(long uid, EditUserArgs editUserArgs)
    {
        var (usernameArgs, passwordArgs, displayNameArgs, emailArgs,
            enableArgs, totalSpaceArgs, roleArgs) = editUserArgs;

        var checkResult = _userCheckService.FormatChecker.CommonCheck(usernameArgs)
                          && _userCheckService.FormatChecker.DisplayNameUpdateCheck(displayNameArgs)
                          && _userCheckService.FormatChecker.EmailCheck(emailArgs)
                          && _userCheckService.FormatChecker.TotalSpaceCheck(totalSpaceArgs)
                          && _userCheckService.FormatChecker.RoleCheck(roleArgs);
        if (!checkResult)
        {
            return AppResponse.InvalidArgument();
        }

        var user = await _msSqlContext.Users.SingleAsync(u => u.Id == uid);

        if (!string.IsNullOrWhiteSpace(passwordArgs))
        {
            if (!_userCheckService.FormatChecker.CommonCheck(passwordArgs))
            {
                return AppResponse.InvalidArgument();
            }

            user.Password = PasswordHelper.Create(passwordArgs);
        }

        if (user.Username != usernameArgs)
        {
            if (!await _userCheckService.UniqueChecker.UsernameCheckAsync(usernameArgs))
            {
                return AppResponse.DuplicateUsername();
            }

            user.Username = usernameArgs;
        }

        if (user.Email != emailArgs)
        {
            if (!await _userCheckService.UniqueChecker.EmailCheckAsync(emailArgs))
            {
                return AppResponse.DuplicateEmail();
            }

            user.Email = emailArgs;
        }

        user.DisplayName = displayNameArgs;
        user.Enable = enableArgs;
        user.TotalSpace = totalSpaceArgs;
        user.Role = roleArgs;
        await _msSqlContext.SaveChangesAsync();
        return AppResponse.Success();
    }
}
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

public record EditUserArgs(
    string Username,
    string Password,
    string DisplayName,
    string Email,
    bool Enable,
    long TotalSpace,
    string Role);

public record EmailArg(string Email);

public record SignUpArgs(
    string Username,
    string Password,
    string DisplayName,
    string Email,
    string Key,
    string Code);

public record StringArg(string Value);

public record UpdateEmailArgs(string Email, string Key, string Code);

public record ForgetPasswordModifyArgs(string Password, string Email, string Key, string Code);

[Route("api/user")]
[ApiController]
public class UserController(
    SignUpService signUpService,
    ModifyPasswordService modifyPasswordService,
    UserService userService) : ControllerBase
{
    private readonly SignUpService _signUpService = signUpService;
    private readonly ModifyPasswordService _modifyPasswordService = modifyPasswordService;
    private readonly UserService _userService = userService;

    [HttpPost("sign-up")]
    public async Task<AppResponse> SignUpAsync([FromBody] SignUpArgs signUpArgs)
    {
        return await _signUpService.SignUpAsync(signUpArgs);
    }

    [HttpPost("sign-up/email-verify")]
    public async Task<AppResponse> SignUpSendEmailAsync([FromBody] EmailArg emailArg)
    {
        return await _signUpService.SignUpSendEmailAsync(emailArg);
    }

    [HttpPut("forget-password/modify")]
    public async Task<AppResponse> ForgetPasswordModifyAsync([FromBody] ForgetPasswordModifyArgs forgetPasswordModifyArgs)
    {
        return await _modifyPasswordService.ForgetPasswordModifyAsync(forgetPasswordModifyArgs);
    }

    [HttpPost("forget-password/email-verify")]
    public async Task<AppResponse> ForgetPasswordSendEmailAsync([FromBody] EmailArg emailArg)
    {
        return await _modifyPasswordService.ForgetPasswordSendEmailAsync(emailArg);
    }

    [HttpGet("current")]
    [Authorize(Roles = "USER")]
    public async Task<AppResponse> GetUserInfoAsync()
    {
        return await _userService.GetUserInfoAsync();
    }

    [HttpPut("current/username")]
    [Authorize(Roles = "USER")]
    public async Task<AppResponse> UpdateUsernameAsync([FromBody] StringArg stringArg)
    {
        return await _userService.UpdateUsernameAsync(stringArg);
    }

    [HttpPut("current/password")]
    [Authorize(Roles = "USER")]
    public async Task<AppResponse> UpdatePasswordAsync([FromBody] StringArg stringArg)
    {
        return await _userService.UpdatePasswordAsync(stringArg);
    }

    [HttpPut("current/display-name")]
    [Authorize(Roles = "USER")]
    public async Task<AppResponse> UpdateDisplayNameAsync([FromBody] StringArg stringArg)
    {
        return await _userService.UpdateDisplayNameAsync(stringArg);
    }

    [HttpPost("current/email/vcode")]
    [Authorize(Roles = "USER")]
    public async Task<AppResponse> SendVerificationCodeOnUpdateEmailAsync([FromBody] StringArg stringArg)
    {
        return await _userService.SendVerificationCodeOnUpdateEmailAsync(stringArg);
    }

    [HttpPut("current/email")]
    [Authorize(Roles = "USER")]
    public async Task<AppResponse> UpdateEmailAsync([FromBody] UpdateEmailArgs updateEmailArgs)
    {
        return await _userService.UpdateEmailAsync(updateEmailArgs);
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<AppResponse> GetUsersAsync()
    {
        return await _userService.GetUsersAsync();
    }

    [HttpPut("{uid:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<AppResponse> EditUserAsync([FromRoute] long uid, [FromBody] EditUserArgs editUserArgs)
    {
        return await _userService.EditUserAsync(uid, editUserArgs);
    }
}
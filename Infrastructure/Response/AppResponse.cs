namespace Infrastructure.Response;

public sealed class ResponseStandard
    {
        /// <summary>
        /// 成功
        /// </summary>
        public sealed class Success
        {
            public const int Code = 0;
            public const string Message = "OK";
        }

        /// <summary>
        /// 登录失败
        /// </summary>
        public sealed class SignInFailed
        {
            public const int Code = 1000;
            public const string Message = "登录失败";
        }

        /// <summary>
        /// 令牌已过期
        /// </summary>
        public sealed class TokenExpired
        {
            public const int Code = 1001;
            public const string Message = "令牌已过期";
        }

        /// <summary>
        /// 需要重新登录
        /// </summary>
        public sealed class RequireLogBackIn
        {
            public const int Code = 1002;
            public const string Message = "需要重新登录";
        }

        /// <summary>
        /// 接口授权未通过
        /// </summary>
        public sealed class Unauthorized
        {
            public const int Code = 1003;
            public const string Message = "接口授权未通过";
        }

        /// <summary>
        /// 用户不可用
        /// </summary>
        public sealed class DisabledUser
        {
            public const int Code = 1004;
            public const string Message = "用户不可用";
        }

        /// <summary>
        /// 拒绝访问
        /// </summary>
        public sealed class PermissionDenied
        {
            public const int Code = 1005;
            public const string Message = "拒绝访问";
        }

        /// <summary>
        /// 文件不存在，无法进行哈希上传，需要进行物理上传
        /// </summary>
        public sealed class NeedPyhsicalUpload
        {
            public const int Code = 2000;
            public const string Message = "需要物理上传";
        }

        /// <summary>
        /// 用户空间不足
        /// </summary>
        public sealed class SpaceNotEnough
        {
            public const int Code = 2001;
            public const string Message = "用户空间不足";
        }

        /// <summary>
        /// 等待保存到对象存储
        /// </summary>
        public sealed class WaitingSaveToOss
        {
            public const int Code = 2002;
            public const string Message = "等待保存到对象存储";
        }

        /// <summary>
        /// 不合法的参数
        /// </summary>
        public sealed class InvalidArgument
        {
            public const int Code = 3000;
            public const string Message = "参数不合法";
        }

        /// <summary>
        /// 用户名已存在
        /// </summary>
        public sealed class DuplicateUsername
        {
            public const int Code = 3001;
            public const string Message = "用户名重复";
        }

        /// <summary>
        /// 邮箱已存在
        /// </summary>
        public sealed class DuplicateEmail
        {
            public const int Code = 3002;
            public const string Message = "邮箱重复";
        }

        /// <summary>
        /// 提供了不合法的文件或目录路径
        /// </summary>
        public sealed class WrongFileOrDirectoryPath
        {
            public const int Code = 3003;
            public const string Message = "提供了不合法的文件或目录路径";
        }

        /// <summary>
        /// 目录项重名，禁止创建
        /// </summary>
        public sealed class DuplicateDirectoryItem
        {
            public const int Code = 3004;
            public const string Message = "目录项名称重复";
        }

        /// <summary>
        /// 上传的文件不完整
        /// </summary>
        public sealed class IncompleteFile
        {
            public const int Code = 3005;
            public const string Message = "上传的文件不完整";
        }

        /// <summary>
        /// 验证码错误或不存在
        /// </summary>
        public sealed class WrongVerificationCode
        {
            public const int Code = 3006;
            public const string Message = "Wrong Verification Code";
        }

        /// <summary>
        ///  用户不存在
        /// </summary>
        public sealed class NoSuchUser
        {
            public const int Code = 3007;
            public const string Message = "用户不存在";
        }

        /// <summary>
        ///  目标路径不可用
        /// </summary>
        public sealed class InvalidPath
        {
            public const int Code = 3008;
            public const string Message = "目标路径不可用";
        }

        /// <summary>
        ///  文件不可用
        /// </summary>
        public sealed class FileDisabled
        {
            public const int Code = 3009;
            public const string Message = "文件不可用";
        }
    }

    public partial class AppResponse(int code, string message)
    {
        public int Code = code;

        public string Message = message;
    }

    public sealed partial class AppResponse<T>(int code, string message, T data) : AppResponse(code, message)
    {
        public T Data { get; set; } = data;
    }

    public partial class AppResponse
    {
        public static AppResponse Success(string message = ResponseStandard.Success.Message) =>
            new(ResponseStandard.Success.Code, message);

        public static AppResponse<T> Success<T>(T data, string message = ResponseStandard.Success.Message) =>
            new(ResponseStandard.Success.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse WaitingSaveToOss(string message = ResponseStandard.WaitingSaveToOss.Message) =>
            new(ResponseStandard.WaitingSaveToOss.Code, message);

        public static AppResponse<T> WaitingSaveToOss<T>(T data, string message = ResponseStandard.WaitingSaveToOss.Message) =>
            new(ResponseStandard.WaitingSaveToOss.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse FileDisabled(string message = ResponseStandard.FileDisabled.Message) =>
            new(ResponseStandard.FileDisabled.Code, message);

        public static AppResponse<T> FileDisabled<T>(T data, string message = ResponseStandard.FileDisabled.Message) =>
            new(ResponseStandard.FileDisabled.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse RequireLogBackIn(string message = ResponseStandard.RequireLogBackIn.Message) =>
            new(ResponseStandard.RequireLogBackIn.Code, message);

        public static AppResponse<T> RequireLogBackIn<T>(T data,
            string message = ResponseStandard.RequireLogBackIn.Message) =>
            new(ResponseStandard.RequireLogBackIn.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse PermissionDenied(string message = ResponseStandard.PermissionDenied.Message) =>
            new(ResponseStandard.PermissionDenied.Code, message);

        public static AppResponse<T> PermissionDenied<T>(T data,
            string message = ResponseStandard.PermissionDenied.Message) =>
            new(ResponseStandard.PermissionDenied.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse InvalidArgument(string message = ResponseStandard.InvalidArgument.Message) =>
            new(ResponseStandard.InvalidArgument.Code, message);

        public static AppResponse<T> InvalidArgument<T>(T data,
            string message = ResponseStandard.InvalidArgument.Message) =>
            new(ResponseStandard.InvalidArgument.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse DuplicateUsername(string message = ResponseStandard.DuplicateUsername.Message) =>
            new(ResponseStandard.DuplicateUsername.Code, message);

        public static AppResponse<T> DuplicateUsername<T>(T data,
            string message = ResponseStandard.DuplicateUsername.Message) =>
            new(ResponseStandard.DuplicateUsername.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse DuplicateEmail(string message = ResponseStandard.DuplicateEmail.Message) =>
            new(ResponseStandard.DuplicateEmail.Code, message);

        public static AppResponse<T>
            DuplicateEmail<T>(T data, string message = ResponseStandard.DuplicateEmail.Message) =>
            new(ResponseStandard.DuplicateEmail.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse
            WrongVerificationCode(string message = ResponseStandard.WrongVerificationCode.Message) =>
            new(ResponseStandard.WrongVerificationCode.Code, message);

        public static AppResponse<T> WrongVerificationCode<T>(T data,
            string message = ResponseStandard.WrongVerificationCode.Message) =>
            new(ResponseStandard.WrongVerificationCode.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse SignInFailed(string message = ResponseStandard.SignInFailed.Message) =>
            new(ResponseStandard.SignInFailed.Code, message);

        public static AppResponse<T> SignInFailed<T>(T data, string message = ResponseStandard.SignInFailed.Message) =>
            new(ResponseStandard.SignInFailed.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse DisabledUser(string message = ResponseStandard.DisabledUser.Message) =>
            new(ResponseStandard.DisabledUser.Code, message);

        public static AppResponse<T> DisabledUser<T>(T data, string message = ResponseStandard.DisabledUser.Message) =>
            new(ResponseStandard.DisabledUser.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse NoSuchUser(string message = ResponseStandard.NoSuchUser.Message) =>
            new(ResponseStandard.NoSuchUser.Code, message);

        public static AppResponse<T> NoSuchUser<T>(T data, string message = ResponseStandard.NoSuchUser.Message) =>
            new(ResponseStandard.NoSuchUser.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse NeedPyhsicalUpload(string message = ResponseStandard.NeedPyhsicalUpload.Message) =>
            new(ResponseStandard.NeedPyhsicalUpload.Code, message);

        public static AppResponse<T> NeedPyhsicalUpload<T>(T data,
            string message = ResponseStandard.NeedPyhsicalUpload.Message) =>
            new(ResponseStandard.NeedPyhsicalUpload.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse WrongFileOrDirectoryPath(
            string message = ResponseStandard.WrongFileOrDirectoryPath.Message) =>
            new(ResponseStandard.WrongFileOrDirectoryPath.Code, message);

        public static AppResponse<T> WrongFileOrDirectoryPath<T>(T data,
            string message = ResponseStandard.WrongFileOrDirectoryPath.Message) =>
            new(ResponseStandard.WrongFileOrDirectoryPath.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse DuplicateDirectoryItem(
            string message = ResponseStandard.DuplicateDirectoryItem.Message) =>
            new(ResponseStandard.DuplicateDirectoryItem.Code, message);

        public static AppResponse<T> DuplicateDirectoryItem<T>(T data,
            string message = ResponseStandard.DuplicateDirectoryItem.Message) =>
            new(ResponseStandard.DuplicateDirectoryItem.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse IncompleteFile(string message = ResponseStandard.IncompleteFile.Message) =>
            new(ResponseStandard.IncompleteFile.Code, message);

        public static AppResponse<T>
            IncompleteFile<T>(T data, string message = ResponseStandard.IncompleteFile.Message) =>
            new(ResponseStandard.IncompleteFile.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse SpaceNotEnough(string message = ResponseStandard.SpaceNotEnough.Message) =>
            new(ResponseStandard.SpaceNotEnough.Code, message);

        public static AppResponse<T>
            SpaceNotEnough<T>(T data, string message = ResponseStandard.SpaceNotEnough.Message) =>
            new(ResponseStandard.SpaceNotEnough.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse TokenExpired(string message = ResponseStandard.TokenExpired.Message) =>
            new(ResponseStandard.TokenExpired.Code, message);

        public static AppResponse<T> TokenExpired<T>(T data, string message = ResponseStandard.TokenExpired.Message) =>
            new(ResponseStandard.TokenExpired.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse InvalidPath(string message = ResponseStandard.InvalidPath.Message) =>
            new(ResponseStandard.InvalidPath.Code, message);

        public static AppResponse<T> InvalidPath<T>(T data, string message = ResponseStandard.InvalidPath.Message) =>
            new(ResponseStandard.InvalidPath.Code, message, data);
    }

    public partial class AppResponse
    {
        public static AppResponse Unauthorized(string message = ResponseStandard.Unauthorized.Message) =>
            new(ResponseStandard.Unauthorized.Code, message);

        public static AppResponse<T> Unauthorized<T>(T data, string message = ResponseStandard.Unauthorized.Message) =>
            new(ResponseStandard.Unauthorized.Code, message, data);
    }
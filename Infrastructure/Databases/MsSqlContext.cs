using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Databases
{
    public class MsSqlContext(DbContextOptions<MsSqlContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<DirectoryItem> DirectoryItems { get; set; }
        public DbSet<Share> Shares { get; set; }
        public DbSet<RecycleBin> RecycleBins { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<FileServiceLog> FileServiceLogs { get; set; }
        public DbSet<SystemServiceLog> SystemServiceLogs { get; set; }
        public DbSet<IdentityServiceLog> IdentityServiceLogs { get; set; }
    }

    [Table("user", Schema = "a_cloud_drive")]
    [Index(nameof(Username), IsUnique = true, Name = "UX_USER_USERNAME")]
    [Index(nameof(Email), IsUnique = true, Name = "UX_USER_EMAIL")]
    public class User
    {
        [Key]
        [Required]
        [Column("id", TypeName = "bigint")]
        public long Id { get; set; }

        [Required]
        [Column("role", TypeName = "varchar(10)")]
        public string Role { get; set; }

        [Required]
        [Column("username", TypeName = "varchar(100)")]
        public string Username { get; set; }

        [Required]
        [Column("password", TypeName = "binary(640)")]
        public byte[] Password { get; set; }

        [Required]
        [Column("display_name", TypeName = "nvarchar(100)")]
        public string DisplayName { get; set; }

        [Required]
        [Column("email", TypeName = "varchar(100)")]
        public string Email { get; set; }

        [Required]
        [Column("enable", TypeName = "bit")]
        public bool Enable { get; set; }

        [Required]
        [Column("total_space", TypeName = "bigint")]
        public long TotalSpace { get; set; }

        [Required]
        [Column("used_space", TypeName = "bigint")]
        public long UsedSpace { get; set; }
    }

    [Table("file", Schema = "a_cloud_drive")]
    [Index(nameof(HeadHash), nameof(EntiretyHash), IsUnique = true, Name = "UX_FILE_HEADHASH_ENTIRETYHASH")]
    public class File
    {
        [Key]
        [Required]
        [Column("id", TypeName = "uniqueidentifier")]
        public Guid Id { get; set; }

        [Required]
        [Column("head_hash", TypeName = "char(64)")]
        public string HeadHash { get; set; }

        [Required]
        [Column("entirety_hash", TypeName = "char(64)")]
        public string EntiretyHash { get; set; }

        [Required]
        [Column("reference_count", TypeName = "bigint")]
        public long ReferenceCount { get; set; }

        [Required]
        [Column("file_size", TypeName = "bigint")]
        public long FileSize { get; set; }

        /// <summary>
        /// 文件上传状态锁
        /// 为true时指示该文件正处于上传状态，不可访问
        /// </summary>
        [Required]
        [Column("uploading_lock", TypeName = "bit")]
        public bool UploadingLock { get; set; }

        /// <summary>
        /// 文件删除状态标识
        /// 为true时指示该文件正处于准备删除状态
        /// </summary>
        [Required]
        [Column("delete_flag", TypeName = "bit")]
        public bool DeleteFlag { get; set; }

        [Required]
        [Column("enable", TypeName = "bit")]
        public bool Enable { get; set; }
    }

    [Table("directory_item", Schema = "a_cloud_drive")]
    [Index(nameof(Name), IsUnique = false, Name = "UX_DIRECTORYITEM_NAME")]
    [Index(nameof(Uid), IsUnique = false, Name = "UX_DIRECTORYITEM_UID")]
    [Index(nameof(ParentId), IsUnique = false, Name = "UX_DIRECTORYITEM_PARENTID")]
    public class DirectoryItem
    {
        [Key]
        [Required]
        [Column("id", TypeName = "uniqueidentifier")]
        public Guid Id { get; set; }

        [Column("parent_id", TypeName = "uniqueidentifier")]
        public Guid? ParentId { get; set; }

        [Required]
        [Column("uid", TypeName = "bigint")]
        public long Uid { get; set; }

        [Required]
        [Column("name", TypeName = "nvarchar(300)")]
        public string Name { get; set; } = "";

        [Required]
        [Column("is_file", TypeName = "bit")]
        public bool IsFile { get; set; }

        [Column("file_id", TypeName = "uniqueidentifier")]
        public Guid? FileId { get; set; }
    }

    [Table("recycle_bin", Schema = "a_cloud_drive")]
    public class RecycleBin
    {
        [Key]
        [Required]
        [Column("id", TypeName = "uniqueidentifier")]
        public Guid Id { get; set; }

        [Required]
        [Column("dir_item_id", TypeName = "uniqueidentifier")]
        public Guid DirItemId { get; set; }

        [Required]
        [Column("parent_id", TypeName = "uniqueidentifier")]
        public Guid ParentId { get; set; }

        [Required]
        [Column("uid", TypeName = "bigint")]
        public long Uid { get; set; }

        [Required]
        [Column("name", TypeName = "nvarchar(300)")]
        public string Name { get; set; } = "";

        [Required]
        [Column("is_file", TypeName = "bit")]
        public bool IsFile { get; set; }

        [Column("file_id", TypeName = "uniqueidentifier")]
        public Guid? FileId { get; set; }

        [Required]
        [Column("is_recycle_root", TypeName = "bit")]
        public bool IsRecycleRoot { get; set; }

        [Required]
        [Column("delete_date", TypeName = "datetime2")]
        public DateTime DeleteDate { get; set; }

        [Required]
        [Column("dir_path", TypeName = "nvarchar(max)")]
        public string DirPath { get; set; }
    }

    [Table("share", Schema = "a_cloud_drive")]
    public class Share
    {
        [Key]
        [Required]
        [Column("id", TypeName = "uniqueidentifier")]
        public Guid Id { get; set; }

        [Required]
        [Column("dir_item_id", TypeName = "uniqueidentifier")]
        public Guid DirItemId { get; set; }

        [Required]
        [Column("key", TypeName = "char(128)")]
        public string Key { get; set; }

        [Required]
        [Column("expire_date", TypeName = "datetime2")]
        public DateTime ExpireDate { get; set; }
    }

    [Table("system_config", Schema = "a_cloud_drive")]
    public class SystemConfig
    {
        [Key]
        [Required]
        [Column("config_key", TypeName = "varchar(255)")]
        public string ConfigKey { get; set; }

        [Required]
        [Column("config_value", TypeName = "varchar(255)")]
        public string ConfigValue { get; set; }
    }

    public class Log
    {
        [Key]
        [Required]
        [Column("id", TypeName = "bigint")]
        public long Id { get; set; }

        [Column("message", TypeName = "nvarchar(max)")]
        public string? Message { get; set; }

        [Column("level", TypeName = "nvarchar")]
        public string? Level { get; set; }

        [Required]
        [Column("date", TypeName = "datetime2")]
        public DateTime Date { get; set; }

        [Column("exception", TypeName = "nvarchar(max)")]
        public string? Exception { get; set; }

        [Column("trace_id", TypeName = "varchar(40)")]
        public string? TraceId { get; set; }

        [Column("operator_id", TypeName = "bigint")]
        public long? OperatorId { get; set; }
    }

    [Table("system_service_log", Schema = "a_cloud_drive")]
    public class SystemServiceLog : Log
    {
    }

    [Table("identity_service_log", Schema = "a_cloud_drive")]
    public class IdentityServiceLog : Log
    {
    }

    [Table("file_service_log", Schema = "a_cloud_drive")]
    public class FileServiceLog : Log
    {
    }
}
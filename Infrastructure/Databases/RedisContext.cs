using StackExchange.Redis;

namespace Infrastructure.Databases
{
    public class RedisContext
    {
        public IDatabase VerificationCode { get; }
        public IDatabase FileUploadMetaData { get; }

        public RedisContext(string host, int port, string password)
        {
            var options = new ConfigurationOptions();
            options.EndPoints.Add($"{host}:{port}");
            if (!string.IsNullOrWhiteSpace(password))
            {
                options.Password = password;
            }

            var conn = ConnectionMultiplexer.Connect(options);

            VerificationCode = conn.GetDatabase(0);
            FileUploadMetaData = conn.GetDatabase(1);
        }

    }
}

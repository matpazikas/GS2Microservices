using StackExchange.Redis;

namespace GS_Microservices_Sem2.Services
{
    public class RedisService
    {
        private readonly ConnectionMultiplexer _redis;

        public RedisService(string connectionString)
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
        }

        public IDatabase GetDatabase() => _redis.GetDatabase();
    }
}

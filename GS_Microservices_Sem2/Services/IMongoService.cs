using MongoDB.Driver;

namespace GS_Microservices_Sem2.Services
{
    public interface IMongoService
    {
        IMongoCollection<T> GetCollection<T>(string collectionName);
    }
}

using Fedido.Server.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Fedido.Server.Repositories;

public class MongoDbRepository : IMongoDbRepository
{
    private readonly IMongoClient _client;
    private readonly ILogger<MongoDbRepository> _logger;

    public MongoDbRepository(ILogger<MongoDbRepository> logger, IMongoClient client)
    {
        _logger = logger;
        _client = client;

        _logger.LogTrace($"Created {nameof(MongoDbRepository)}");
    }

    public async Task Create<T>(T item, string databaseName, string collectionName)
    {
        _logger.LogTrace($"Creating item with type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        await collection.InsertOneAsync(item);

        _logger.LogTrace($"Finished creating item with type: {typeof(T)}");
    }

    // public async Task Delete<T>(string id, string databaseName, string collectionName) where T : IStandardItem
    //     {
    //         _logger.LogInformation($"Deleting item with type: {typeof(T)} and id: {id}");
    //
    //         var database = _client.GetDatabase(databaseName);
    //         IMongoCollection<T> collection = database.GetCollection<T>(collectionName);
    //         
    //         FilterDefinitionBuilder<T> filterDefinitionBuilder = Builders<T>.Filter;
    //         var filter = filterDefinitionBuilder.Eq(item => item.Id, id);
    //         await collection.DeleteOneAsync(filter);
    //
    //         _logger.LogInformation($"Finished deleting item with type: {typeof(T)} and id: {id}");
    //     }

    public async Task<IEnumerable<T>> GetAll<T>(string databaseName, string collectionName)
    {
        _logger.LogTrace($"Getting all items of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        var result = (await collection.FindAsync(new BsonDocument())).ToList();

        _logger.LogTrace($"Finished getting all items of type: {typeof(T)}");

        return result;
    }

    public async Task<T> GetSpecificItem<T>(FilterDefinition<T> filter, string databaseName, string collectionName)
    {
        _logger.LogTrace($"Getting specific item with type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);
        var result = (await collection.FindAsync(filter)).SingleOrDefault();

        _logger.LogTrace($"Returning specific item with type: {typeof(T)}");

        return result;
    }

    public async Task<IEnumerable<T>> GetSpecificItems<T>(FilterDefinition<T> filter, string databaseName,
        string collectionName)
    {
        _logger.LogTrace($"Getting specific item with type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);
        var result = (await collection.FindAsync(filter)).ToList();

        _logger.LogTrace($"Returning specific item with type: {typeof(T)}");

        return result;
    }

    public async Task Update<T>(T item, FilterDefinition<T> filter, string databaseName, string collectionName)
    {
        _logger.LogInformation($"Updating item of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        await collection.ReplaceOneAsync(filter, item);

        _logger.LogInformation($"Finished updating item of type: {typeof(T)}");
    }
}
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class FollowerRepository : IFollowerRepository
{
    private readonly ILogger<FollowerRepository> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly FilterDefinition<Activity> _filterDefinition;

    public FollowerRepository(ILogger<FollowerRepository> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
        
        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        _filterDefinition = filterBuilder.Where(i => i.Type == "Accept");
    }

    public async Task<IEnumerable<Activity>> GetFollowersPagedAsync(string actorId, int page)
    {
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var accepts = await _repository.GetSpecificPaged(DatabaseLocations.Activity.Database,
            actorId, page, 20, sort, _filterDefinition);
        
        return follows;
    }

    public async Task<long> CountFollowersAsync(string actorId)
    {
        var postCount = await _repository.CountSpecific(DatabaseLocations.Activity.Database, actorId, _filterDefinition);

        return postCount;
    }
}
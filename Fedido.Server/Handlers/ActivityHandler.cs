using CommonExtensions;
using Fedido.Server.Extensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Authentication;
using Fedido.Server.Model.Helpers;
using MongoDB.Driver;

namespace Fedido.Server.Handlers;

public class ActivityHandler : IActivityHandler
{
    private readonly IActivityAPI _activityApi;
    private readonly IActorAPI _actorApi;
    private readonly ILogger<ActivityHandler> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly IKnownSharedInboxHandler _sharedInboxHandler;

    public ActivityHandler(ILogger<ActivityHandler> logger, IMongoDbRepository repository, IActorAPI actorApi,
        IActivityAPI activityApi, IKnownSharedInboxHandler sharedInboxHandler)
    {
        _logger = logger;
        _repository = repository;
        _actorApi = actorApi;
        _activityApi = activityApi;
        _sharedInboxHandler = sharedInboxHandler;
    }

    public async Task<Actor> GetActor(Guid userId)
    {
        var filterActorDefinitionBuilder = Builders<Actor>.Filter;
        var filterActor = filterActorDefinitionBuilder.Eq(i => i.Id,
            new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}"));
        var actor = await _repository.GetSpecificItem(filterActor, "ActivityPub", "Actors");
        return actor;
    }

    public async Task SendActivities(Activity activity, User user, Actor actor)
    {
        var targets = new HashSet<ServerNameInboxPair>();

        var receivers = new List<string>();

        if (activity.To.IsNotNullOrEmpty()) receivers.AddRange(activity.To);
        if (activity.Bcc.IsNotNullOrEmpty()) receivers.AddRange(activity.Bcc);
        if (activity.Audience.IsNotNullOrEmpty()) receivers.AddRange(activity.Audience);
        if (activity.Bto.IsNotNullOrEmpty()) receivers.AddRange(activity.Bto);
        if (activity.Bcc.IsNotNullOrEmpty()) receivers.AddRange(activity.Bcc);

        if (activity.IsActivityPublic()) // Public Post
        {
            // Send to all receivers and to all known SharedInboxes

            foreach (var item in receivers)
            {
                if (item is "https://www.w3.org/ns/activitystreams#Public" or "as:Public" or "public") continue;

                var serverNameInboxPair = await GetServerNameInboxPair(new Uri(item), true);
                targets.Add(serverNameInboxPair);
            }

            foreach (var item in await _sharedInboxHandler.GetSharedInboxes())
                targets.Add(new ServerNameInboxPair
                {
                    Inbox = item,
                    ServerName = item.Host
                });
        }
        else // Private Post
        {
            // Send to all receivers

            foreach (var item in receivers)
            {
                var serverNameInboxPair = await GetServerNameInboxPair(new Uri(item), false);
                targets.Add(serverNameInboxPair);
            }
        }

        // This List is only needed to make sure the HasSet works as expected
        // If you are sure it works you can remove it
        var inboxes = new List<Uri>();

        foreach (var target in targets)
        {
            if (inboxes.Contains(target.Inbox))
            {
                _logger.LogWarning($"Duplicate found in {nameof(inboxes)} / {nameof(targets)}");

                continue;
            }

            inboxes.Add(target.Inbox);

            for (var i = 0; i < 5; i++)
            {
                if (await _activityApi.SendActivity(activity, user, target, actor)) break;

                Thread.Sleep(10000);
            }
        }
    }

    private async Task<ServerNameInboxPair?> GetServerNameInboxPair(Uri actorUri, bool isPublic)
    {
        var actor = await _actorApi.GetActor(actorUri);

        if (isPublic) // Public Activity
        {
            var sharedInbox = actor?.Endpoints?.SharedInbox;

            if (sharedInbox.IsNull())
            {
                if (actor.Inbox.IsNull()) return null;

                return new ServerNameInboxPair
                {
                    Inbox = actor?.Inbox,
                    ServerName = actor?.Inbox?.Host
                };
            }

            await _sharedInboxHandler.AddSharedInbox(sharedInbox);

            return new ServerNameInboxPair
            {
                Inbox = sharedInbox,
                ServerName = sharedInbox?.Host
            };
        }

        // Private Activity
        return new ServerNameInboxPair
        {
            Inbox = actor?.Inbox,
            ServerName = actor?.Inbox?.Host
        };
    }
}
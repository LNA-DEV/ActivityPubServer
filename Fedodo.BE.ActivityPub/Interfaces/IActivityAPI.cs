using Fedodo.BE.ActivityPub.Model.ActivityPub;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.Common.Models;

namespace Fedodo.BE.ActivityPub.Interfaces;

public interface IActivityAPI
{
    public string ComputeHash(string jsonData);

    public Task<bool> SendActivity(Activity activity, User user, ServerNameInboxPair serverInboxPair, Actor actor);
}
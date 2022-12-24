namespace ActivityPubServer.Interfaces;

public interface IKnownServersHandler
{
    public Task<IEnumerable<Model.ActivityPubServer>> GetAll();
    public Task Add(string postTo);
}
using Chinook.ClientModels;

namespace Chinook.Core;

public interface IPlaylistService
{
    Task<Playlist> GetPlaylistByIdAsync(long playlistId, string currentUserId, CancellationToken cancellationToken = default);
    IObservable<IList<Playlist>> MyPlayLists { get; }
    Task<IList<Playlist>> GetCurrentUserPlayListsAsync(string currentUserId, CancellationToken cancellationToken = default);
}
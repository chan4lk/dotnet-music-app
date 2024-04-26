using Chinook.ClientModels;

namespace Chinook.Core;

public interface ITrackService
{
    Task<IList<PlaylistTrack>> GetTracksByArtistIdAsync(long artistId, string currentUserId, CancellationToken cancellationToken);
    Task<PlaylistTrack> ToggleFavoriteTrack(string userId, long trackId, CancellationToken cancellationToken = default);
    Task<Playlist> AddToPlaylist(string? playlistName, string userId, long trackId, long? playlistId = null,
        CancellationToken cancellationToken = default);
    Task RemoveFromPlaylist(long playlistId, string userId, long trackId, CancellationToken cancellationToken = default);
}
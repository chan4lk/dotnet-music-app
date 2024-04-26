using AutoMapper;
using Chinook.ClientModels;
using Chinook.Core;
using Microsoft.EntityFrameworkCore;

namespace Chinook.Services;

public class TrackService(IDbContextFactory<ChinookContext> dbFactory, IMapper mapper) : ITrackService
{
    /// <summary>
    /// Load tracks for ArtistsPage.
    /// </summary>
    /// <param name="artistId">Selected artist id.</param>
    /// <param name="currentUserId">Logged user id.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>List of tracks.</returns>
    public async Task<IList<PlaylistTrack>> GetTracksByArtistIdAsync(long artistId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        var tracks = await dbContext.Tracks
            .Where(a => a.Album != null &&  a.Album.ArtistId == artistId)
            .Include(a => a.Album)
            .Include(a => a.Playlists)
            .ThenInclude(a => a.UserPlaylists)
            .ToListAsync(cancellationToken);
        
        // user id has to passed to auto mapper.
        return mapper.Map<List<PlaylistTrack>>(
            tracks, 
            opts => opts.Items[Constants.CURRENT_USER_ID] = currentUserId);
    }
    
    /// <summary>
    /// User toggles track as favorite.
    /// </summary>
    /// <param name="userId">Logged user id.</param>
    /// <param name="trackId">Selected track id.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>Updated Track status.</returns>
    /// <exception cref="KeyNotFoundException">Throw error when track is not found in db.</exception>
    public async Task<PlaylistTrack> ToggleFavoriteTrack(string userId, long trackId, CancellationToken cancellationToken = default)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        var playlist = await EnsureFavoritesPlaylistInDb(userId, dbContext, cancellationToken);

        var track = await dbContext.Tracks.FindAsync(new object?[] { trackId }, cancellationToken: cancellationToken);
        if (track == null) throw new KeyNotFoundException("Track not found");

        var isFavorite = true; // did we mark it as favorite ?
        
        // Toggle happens here.
        if (AlreadyFavoriteTrack(trackId, playlist))
        {
            playlist?.Tracks.Remove(track);
            isFavorite = false;
        }
        else
        {
            playlist?.Tracks.Add(track);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var playlistTrack = new PlaylistTrack()
        {
            TrackId = track.TrackId,
            IsFavorite = isFavorite
        };

        return playlistTrack;

    }

    /// <summary>
    /// Add the selected track to a selected playlist. The playlist can be a new one or existing one.
    /// </summary>
    /// <param name="playlistName">New playlist name.</param>
    /// <param name="userId">Logged in user id.</param>
    /// <param name="trackId">Selected track id.</param>
    /// <param name="playlistId">Playlist from exiting list.</param>
    /// <param name="cancellationToken">The cancellation token for request.</param>
    /// <returns>The new playlist if created or exiting one if updated.</returns>
    /// <exception cref="ArgumentNullException">New Playlist name should be provided if existing playlist is not selected.</exception>
    /// <exception cref="KeyNotFoundException">The track does not exist.</exception>
    public async Task<Playlist> AddToPlaylist(string? playlistName, string userId, long trackId, long? playlistId = null, CancellationToken cancellationToken = default)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        var userPlaylist =
            await dbContext.UserPlaylists
                // if the playlist already exists we will add the track there.
                .Where(up => up.UserId == userId && (up.Playlist.Name == playlistName || up.PlaylistId == playlistId))
                .Include(x => x.Playlist)
                .ThenInclude(u => u.Tracks)
                .SingleOrDefaultAsync(cancellationToken);

        var playlist = userPlaylist?.Playlist;

        if (!IsExistingPlaylist(playlist)) // if the playlist does not exist we will create new one.
        {
            if (string.IsNullOrEmpty(playlistName)) throw new ArgumentNullException(nameof(playlistName));

            playlist = await CreatePlaylistInDb(userId, playlistName, dbContext, cancellationToken);
        }

        var track = await dbContext.Tracks.FindAsync(trackId);
        if (track == null) throw new KeyNotFoundException("Track not found");
        playlist?.Tracks.Add(track);

        await dbContext.SaveChangesAsync(cancellationToken);

        return mapper.Map<Playlist>(playlist, opts => opts.Items[Constants.CURRENT_USER_ID] = userId);
    }

    /// <summary>
    /// Remove the selected track from playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id for current track.</param>
    /// <param name="userId">Logged in user id.</param>
    /// <param name="trackId">Track to remove.</param>
    /// <param name="cancellationToken">The cancellation token for request.</param>
    /// <exception cref="KeyNotFoundException">Track is not in mentioned playlist.</exception>
    public async Task RemoveFromPlaylist(long playlistId, string userId, long trackId, CancellationToken cancellationToken = default)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        var userPlaylist =
            await dbContext.UserPlaylists
                .Where(up => up.UserId == userId && up.PlaylistId == playlistId)
                .Include(x => x.Playlist)
                .ThenInclude(u => u.Tracks)
                .SingleOrDefaultAsync(cancellationToken);

        var playlist = userPlaylist?.Playlist ?? throw new KeyNotFoundException("Playlist not found");

        var track = playlist.Tracks.SingleOrDefault(t => t.TrackId == trackId) ?? throw new KeyNotFoundException("Track not found"); ;

        playlist.Tracks.Remove(track);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Check if playlist exists.
    /// </summary>
    /// <param name="playlist">The playlist.</param>
    /// <returns><c>true</c> if existing playlist. if not <c>false</c></returns>
    private static bool IsExistingPlaylist(Models.Playlist? playlist)
    {
        return playlist != null;
    }

    /// <summary>
    /// Check if track is already in the playlist.
    /// </summary>
    /// <param name="trackId">The track id.</param>
    /// <param name="playlist">The playlist.</param>
    /// <returns><c>true</c> if existing track in playlist. if not <c>false</c></returns>
    private static bool AlreadyFavoriteTrack(long trackId, Models.Playlist? playlist)
    {
        return playlist?.Tracks.FirstOrDefault(t => t.TrackId == trackId) != null;
    }
    
    /// <summary>
    /// Creates new user playlist.
    /// </summary>
    /// <param name="userId">Logged in user id.</param>
    /// <param name="playlistName">The new name for the playlist.</param>
    /// <param name="dbContext">The db context.</param>
    /// <param name="cancellationToken">Cancellation token for request.</param>
    /// <returns>New Playlist.</returns>
    /// <exception cref="ArgumentException">User id is not correct.</exception>
    private static async Task<Models.Playlist?> CreatePlaylistInDb(string userId, string playlistName, ChinookContext dbContext, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync(userId, cancellationToken) ?? throw new ArgumentException("user does no exist");
        var nextId = await GetNextPlaylistId(dbContext, cancellationToken);
        var playlist = new Models.Playlist() { PlaylistId = nextId, Name = playlistName };

        var userPlaylist = new Models.UserPlaylist()
        {
            User = user,
            Playlist = playlist
        };

        dbContext.Playlists.Add(playlist);
        dbContext.UserPlaylists.Add(userPlaylist);
        return playlist;
    }

    /// <summary>
    /// Generate next playlist id with count of records in the table.
    /// </summary>
    /// <param name="dbContext">The db context.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>Next ID.</returns>
    private static async Task<int> GetNextPlaylistId(ChinookContext dbContext, CancellationToken cancellationToken)
    {
        return await dbContext.Playlists.CountAsync(cancellationToken) + 1;
    }

    /// <summary>
    /// Creates the favorites playlist when user toggle the favorite button for the very first time.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="dbContext">The db context.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>New Playlist if the first time. If not exiting favorites list.</returns>
    private static async Task<Models.Playlist?> EnsureFavoritesPlaylistInDb(string userId, ChinookContext dbContext,
        CancellationToken cancellationToken)
    {
        var userPlaylist =
                    await dbContext.UserPlaylists
                        .Where(up => up.UserId == userId && up.Playlist.Name == Constants.FAVORITES)
                        .Include(x => x.Playlist)
                        .ThenInclude(u => u.Tracks)
                        .SingleOrDefaultAsync(cancellationToken);

        Models.Playlist? playlist = userPlaylist?.Playlist;

        if (userPlaylist == null) // if it's the first time we will create the favorites playlist in db.
        {
            playlist = await CreatePlaylistInDb(userId, Constants.FAVORITES, dbContext, cancellationToken);
        }

        return playlist;
    }
    #endregion
}
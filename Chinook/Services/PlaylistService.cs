using System.Reactive.Linq;
using System.Reactive.Subjects;
using AutoMapper;
using Chinook.Core;
using Microsoft.EntityFrameworkCore;
using Playlist = Chinook.ClientModels.Playlist;

namespace Chinook.Services;

public class PlaylistService : IPlaylistService
{
    private readonly IDbContextFactory<ChinookContext> dbFactory;
    private readonly IMapper mapper;
    private readonly Subject<IList<Playlist>> myPlayLists = new();

    /// <summary>
    /// Used to update sidebar when new playlist is created.
    /// </summary>
    public IObservable<IList<Playlist>> MyPlayLists { get; }

    /// <summary>
    /// Creates a new instance of <c>PlaylistService</c>
    /// </summary>
    /// <param name="dbFactory">Db factory to initiate the connection.</param>
    /// <param name="mapper">Automapper instance.</param>
    public PlaylistService(IDbContextFactory<ChinookContext> dbFactory, IMapper mapper)
    {
        this.dbFactory = dbFactory;
        this.mapper = mapper;
        MyPlayLists = myPlayLists.AsObservable(); // expose the observable so that UI can subscribe to changes.
    }
    
    /// <summary>
    /// Get tracks for the playlist page. 
    /// </summary>
    /// <param name="playlistId">The selected playlist id.</param>
    /// <param name="currentUserId">Logged in user id.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>All track for with the playlist instance.</returns>
    public async Task<Playlist> GetPlaylistByIdAsync(long playlistId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        var playlistModel = await dbContext.Playlists
            .Include(a => a.UserPlaylists)
            .Include(a => a.Tracks)
            .ThenInclude(a => a.Album)
            .ThenInclude(a => a.Artist)
            .Where(p => p.PlaylistId == playlistId)
            .FirstOrDefaultAsync(cancellationToken);

        return mapper.Map<Playlist>(
            playlistModel, 
            opts => opts.Items[Constants.CURRENT_USER_ID] = currentUserId); // automapper needs the current user id.
    }
    
    /// <summary>
    /// Current user's playlists for sidebar bar.
    /// </summary>
    /// <param name="currentUserId">Logged in user id.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>All the user playlists.</returns>
    public async Task<IList<Playlist>> GetCurrentUserPlayListsAsync(string currentUserId, CancellationToken cancellationToken = default)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        var playlistModel = await dbContext.Playlists
            .Include(a => a.UserPlaylists)
            .Include(a => a.Tracks)
            .ThenInclude(a => a.Album)
            .ThenInclude(a => a.Artist)
            .Where(p => p.UserPlaylists.Any(up => up.UserId == currentUserId))
            .Select(p => p)
            .OrderBy(p => p.Name != Constants.FAVORITES)
            .ThenBy(p => p.PlaylistId)
            .ToListAsync(cancellationToken);

        var playlists = mapper.Map<List<Playlist>>(
            playlistModel, 
            opts => opts.Items[Constants.CURRENT_USER_ID] = currentUserId); // automapper needs the current user id.
        
        myPlayLists.OnNext(playlists); // the observable is updated so the sidebar will update.
        
        return playlists;
    }
}
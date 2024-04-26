using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Chinook.ClientModels;
using Chinook.Core;
using Chinook.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Chinook.Pages;

public partial class ArtistPage : ReactiveComponentBase
{
    [Parameter] public long ArtistId { get; set; }
#pragma warning disable CS8618 // Blazor Injection not recognized from IDE.
    [CascadingParameter] private Task<AuthenticationState> AuthenticationState { get; set; }
    [Inject] IArtistService ArtistService { get; set; }
    [Inject] ITrackService TrackService { get; set; }
    [Inject] IPlaylistService PlaylistService { get; set; }

    [Inject] private ILogger<ArtistPage> Logger { get; set; }
#pragma warning restore CS8618 // Blazor Injection not recognized from IDE.
    private Modal? PlaylistDialog { get; set; }

    private Artist? artist;
    private IList<PlaylistTrack> tracks = new List<PlaylistTrack>();
    private PlaylistTrack? selectedTrack;
    private IList<Playlist> Playlists { get; set; } = new List<Playlist>();

    private string? currentUserId;
    private string selectedPlaylistId = "0";
    private string? NewPlayListName { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await InvokeAsync(StateHasChanged);

        // listen to changes on the observable for user playlists.
        PlaylistService.MyPlayLists
            .TakeUntil(Disposed)
            .Select(lists => Playlists = lists)
            .Do(_ => InvokeAsync(StateHasChanged))
            .Subscribe();

        // when the route change load tracks for selected artist.
        ParametersSet.Select(_ => (ArtistId))
            .DistinctUntilChanged()
            .TakeUntil(Disposed)
            .Select(_ => Observable.FromAsync(Load))
            .Switch()
            .Catch<Unit, Exception>(AndShowError)
            .Subscribe();
    }

    /// <summary>
    /// Load all tack and artist information.
    /// Also load user playlist for add to playlist popup.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    private async Task Load(CancellationToken cancellationToken)
    {
        currentUserId = await AuthenticationState.GetUserId();
        artist = await ArtistService.GetByIdAsync(ArtistId, cancellationToken);
        tracks = await TrackService.GetTracksByArtistIdAsync(ArtistId, currentUserId, cancellationToken);
        await PlaylistService.GetCurrentUserPlayListsAsync(currentUserId, cancellationToken);  // refresh playlists dropdown
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Action method for mark favorite button.
    /// </summary>
    /// <param name="trackId">Selected track id.</param>
    private async Task FavoriteTrack(long trackId)
    {
        CloseInfoMessage();

        var track = tracks.FirstOrDefault(t => t.TrackId == trackId);

        InfoMessage =
            $"Track {track?.ArtistName} - {track?.AlbumTitle} - {track?.TrackName} added to playlist Favorites.";

        // Add the selected track to users favorites playlist. if the playlist does not exist, we will create one.
        await ToggleFavorite(trackId, track);

        if (!Playlists.Any(p => p.Name == Constants.FAVORITES) && currentUserId != null) // load users playlist if new playlist is created for the first time.
            await PlaylistService.GetCurrentUserPlayListsAsync(currentUserId);
    }

    /// <summary>
    /// Action method for  Remove the selected track from favorites playlist.
    /// </summary>
    /// <param name="trackId">Selected track id.</param>
    private async Task UnfavoriteTrack(long trackId)
    {
        CloseInfoMessage();

        var track = tracks.FirstOrDefault(t => t.TrackId == trackId);

        InfoMessage =
            $"Track {track?.ArtistName} - {track?.AlbumTitle} - {track?.TrackName} removed from playlist Favorites.";

        await ToggleFavorite(trackId, track);
    }

    /// <summary>
    /// Action method to open add to playlist button.
    /// </summary>
    /// <param name="trackId">Selected track id.</param>
    private void OpenPlaylistDialog(long trackId)
    {
        CloseInfoMessage();
        NewPlayListName = string.Empty;
        selectedPlaylistId = "0";
        selectedTrack = tracks.FirstOrDefault(t => t.TrackId == trackId);
        PlaylistDialog?.Open();
    }

    /// <summary>
    /// Calls when "Save" button is click on the add to playlist popup.
    /// </summary>
    private void AddTrackToPlaylist()
    {
        CloseInfoMessage();
        long.TryParse(this.selectedPlaylistId, out var playlistId);
        
        if (selectedTrack != null && currentUserId != null)
            Observable.FromAsync(token =>
                    // Add current track to a new playlist if name is given. if not to exiting selected list.
                    TrackService.AddToPlaylist(
                        NewPlayListName,
                        currentUserId,
                        selectedTrack.TrackId,
                        string.IsNullOrWhiteSpace(NewPlayListName) ? playlistId : null, // send the selected playlist id if name is not given.
                        token))
                .TakeUntil(Disposed)
                .Do(pl => NewPlayListName = pl.Name)
                .Do(_ => PlaylistService.GetCurrentUserPlayListsAsync(currentUserId)) // refresh playlists dropdown
                .Do(_ => InvokeAsync(StateHasChanged))
                .Subscribe();

        InfoMessage =
            $"Track {artist?.Name} - {selectedTrack?.AlbumTitle} - {selectedTrack?.TrackName} added to playlist {NewPlayListName}.";

        PlaylistDialog?.Close();
    }

    /// <summary>
    /// Helper method to toggle favorite track.
    /// </summary>
    /// <param name="trackId">Selected track id.</param>
    /// <param name="track">The track.</param>
    private async Task ToggleFavorite(long trackId, PlaylistTrack? track)
    {
        if (currentUserId != null)
        {
            var updateTrack = await TrackService.ToggleFavoriteTrack(currentUserId, trackId);

            if (track != null)
                track.IsFavorite = updateTrack.IsFavorite;  // Update UI with star icon.

            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Show the error message when initial loading for tacks failed.
    /// For other actions like toggling favorites and add to playlist the error boundry will catch the errors.
    /// </summary>
    /// <param name="exception">Exception from backend.</param>
    /// <returns>Observable to handle the error gracefully.</returns>
    private IObservable<Unit> AndShowError(Exception exception)
    {
        IsError = true;
        InfoMessage = Constants.LOADING_TRACKS_FAILED_MESSAGE;
        Logger.LogError(Constants.LOADING_TRACKS_FAILED_MESSAGE, exception);
        return new Subject<Unit>();
    }
}
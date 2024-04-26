using System.Reactive.Linq;
using System.Reactive.Subjects;
using Chinook.ClientModels;
using Chinook.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Chinook.Pages;

public partial class PlaylistPage : ReactiveComponentBase
{
    [Parameter] public long PlaylistId { get; set; }

#pragma warning disable CS8618 // Blazor Injection not recognized from IDE.
    [Inject] IPlaylistService PlaylistService { get; set; }
    [Inject] ITrackService TrackService { get; set; }
    [CascadingParameter] private Task<AuthenticationState> AuthenticationState { get; set; }
#pragma warning restore CS8618 // Blazor Injection not recognized from IDE.

    private Playlist? playlist;
    private string? currentUserId;

    /// <summary>
    /// Get all tracks for the playlist page.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        currentUserId = await AuthenticationState.GetUserId();

        ParametersSet.Select(_ => PlaylistId)
            .DistinctUntilChanged() // when route param changed this will trigger.
            .TakeUntil(Disposed)
            .Select(id => Observable.FromAsync(token => PlaylistService.GetPlaylistByIdAsync(id, currentUserId, token)))
            .Switch()
            .Do(list => playlist = list)
            .Do(_ => CloseInfoMessage())
            .Do(_ => InvokeAsync(StateHasChanged))
            .Catch<Playlist, Exception>(AndShowError) // Catch errors gracefully.
            .Subscribe();
    }

    /// <summary>
    /// Action method for mark favorite button.
    /// </summary>
    /// <param name="trackId">Selected track id.</param>
    private async Task FavoriteTrack(long trackId)
    {
        CloseInfoMessage();

        var track = playlist?.Tracks.FirstOrDefault(t => t.TrackId == trackId);

        if (track != null)
        {
            InfoMessage =
                $"Track {track.ArtistName} - {track.AlbumTitle} - {track.TrackName} added to playlist Favorites.";

            await ToggleFavorite(trackId, track);
        }
    }

    /// <summary>
    /// Action method for Remove the selected track from favorites playlist.
    /// </summary>
    /// <param name="trackId">Selected track id.</param>
    private async Task UnfavoriteTrack(long trackId)
    {
        CloseInfoMessage();

        var track = playlist?.Tracks.FirstOrDefault(t => t.TrackId == trackId);

        if (track != null)
        {
            InfoMessage =
                $"Track {track.ArtistName} - {track.AlbumTitle} - {track.TrackName} removed from playlist Favorites.";

            await ToggleFavorite(trackId, track);

            if (playlist?.Name == Constants.FAVORITES) // if this is the favorites list we will hide the current track.
            {
                playlist.Tracks.Remove(track);
            }
        }
    }

    /// <summary>
    /// Remove track from current playlist.
    /// </summary>
    /// <param name="trackId">The track id.</param>
    private async Task RemoveTrack(long trackId)
    {
        CloseInfoMessage();

        var track = playlist?.Tracks.FirstOrDefault(t => t.TrackId == trackId);
        if (track != null && currentUserId != null)
        {
            await TrackService.RemoveFromPlaylist(PlaylistId, currentUserId, trackId);
            playlist?.Tracks.Remove(track); // remove from client side list as well.
            InfoMessage =
                $"Track {track.ArtistName} - {track.AlbumTitle} - {track.TrackName} removed from playlist {playlist?.Name}.";
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Helper method to toggle favorite track.
    /// </summary>
    /// <param name="trackId">Selected track id.</param>
    /// <param name="track">The track.</param>
    private async Task ToggleFavorite(long trackId, PlaylistTrack? track)
    {
        if (track != null && currentUserId != null)
        {
            var updateTrack = await TrackService.ToggleFavoriteTrack(currentUserId, trackId);
            track.IsFavorite = updateTrack.IsFavorite;
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Show the error message when initial loading for tacks failed.
    /// For other actions like toggling favorites and add to playlist the error boundry will catch the errors.
    /// </summary>
    /// <param name="exception">Exception from backend.</param>
    /// <returns>Observable to handle the error gracefully.</returns>
    private IObservable<Playlist> AndShowError(Exception exception)
    {
        InfoMessage = Constants.LOADING_PLAYLISTS_FAILED_MESSAGE;
        IsError = true;
        return new Subject<Playlist>();
    }
}
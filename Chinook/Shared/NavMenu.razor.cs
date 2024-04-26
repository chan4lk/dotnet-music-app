using System.Reactive.Linq;
using Chinook.ClientModels;
using Chinook.Core;
using Chinook.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Chinook.Shared;

public partial class NavMenu : ReactiveComponentBase
{
    private bool collapseNavMenu = true;

    private IList<Playlist> Playlists { get; set; } = new List<Playlist>();
    
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    
    [Inject]
    private IPlaylistService? PlaylistService { get; set; }

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }

    protected override async Task OnInitializedAsync()
    {
        var userId = await AuthenticationState.GetUserId();

        if (PlaylistService != null)
        {
            PlaylistService.MyPlayLists
                .DistinctUntilChanged()
                .TakeUntil(Disposed)
                .Do(playlists => Playlists = playlists)
                .Do(_ => InvokeAsync(StateHasChanged))
                .Subscribe();

            Observable.FromAsync(token => PlaylistService.GetCurrentUserPlayListsAsync(userId, token))
                .TakeUntil(Disposed)
                .Catch((Exception _) => Observable.Return(new List<Playlist>()))
                .Subscribe();
        }
    }
}
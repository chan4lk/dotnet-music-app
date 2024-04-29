using System.Reactive.Linq;
using System.Reactive.Subjects;
using Chinook.ClientModels;
using Chinook.Core;
using Microsoft.AspNetCore.Components;

namespace Chinook.Pages;

public partial class Home : ReactiveComponentBase
{
    private IList<Artist>? artists; 
    private IList<Artist> filteredArtists = [];
#pragma warning disable CS8618 // Blazor Injection not recognized from IDE.
    [Inject]
    private IArtistService ArtistService { get; set; }
    
    [Inject]
    private ILogger<Home> Logger { get; set; }
#pragma warning restore CS8618 // Blazor Injection not recognized from IDE.
    
    /// <summary>
    /// Load all the artists at the beginning.
    /// </summary>
    /// <returns>The task.</returns>
    protected override Task OnInitializedAsync()
    {
        Observable.FromAsync(Load)
            .TakeUntil(Disposed)
            .Catch<IList<Artist>?, Exception>(AndShowError)
        .Subscribe(artist => artists = artist);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show error at top if exception occurs.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Observable to handle the exception gracefully.</returns>
    private IObservable<IList<Artist>?> AndShowError(Exception exception)
    {
        Logger.LogError(exception, Constants.LOADING_ARTISTS_FAILED_MESSAGE);

        InfoMessage = Constants.LOADING_ARTISTS_FAILED_MESSAGE;
        IsError = true;
        
        return new Subject<IList<Artist>>();
    }

    /// <summary>
    /// Load all the artists.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>All the artists.</returns>
    private async Task<IList<Artist>?> Load(CancellationToken cancellationToken)
    {
        var list = await ArtistService.GetAllArtistsAsync(cancellationToken);
        if (list == null) return list;
        filteredArtists = list; 
        await InvokeAsync(StateHasChanged);
        return list;
    }

    /// <summary>
    /// Search is done at client side. list of artists are filtered based on the user's input.
    /// </summary>
    /// <param name="event">The event with user input.</param>
    private void Search(ChangeEventArgs @event)
    {
        var text = @event.Value as string; // this will contain what user type in search box.
        if (artists == null) return;
        
        if (!string.IsNullOrWhiteSpace(text))
        {
            // Filter without case sensitivity.
            filteredArtists = artists.Where(
                    a => a.Name != null && a.Name.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }
        else // reset the list if search box is cleared.
        {
            filteredArtists = artists;
        }
    }
}
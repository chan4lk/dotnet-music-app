using Chinook.Core;
using Chinook.Services;

namespace Chinook;

public static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<ITrackService, TrackService>();
        services.AddScoped<IPlaylistService, PlaylistService>();
        services.AddAutoMapper(typeof(Program));
    }
}
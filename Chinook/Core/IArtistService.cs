using Chinook.ClientModels;

namespace Chinook.Core;

public interface IArtistService
{
    Task<Artist?> GetByIdAsync(long artistId, CancellationToken cancellationToken = default);
    Task<IList<Artist>?> GetAllArtistsAsync(CancellationToken cancellationToken = default);
}
using AutoMapper;
using Chinook.ClientModels;
using Chinook.Core;
using Microsoft.EntityFrameworkCore;

namespace Chinook.Services;

public class ArtistService(IDbContextFactory<ChinookContext> dbFactory, IMapper mapper) : IArtistService
{
    /// <summary>
    ///  Get Tracks for artists page.
    /// </summary>
    /// <param name="artistId">The selected artist id.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>Artist instance with all the tracks.</returns>
    public async Task<Artist?> GetByIdAsync(long artistId, CancellationToken cancellationToken = default)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var artist = await dbContext.Artists.SingleOrDefaultAsync(a => a.ArtistId == artistId, cancellationToken);
        return mapper.Map<Artist>(artist);
    }

    /// <summary>
    /// Load artists for the home page.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>All the artists in the db.</returns>
    public async Task<IList<Artist>?> GetAllArtistsAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Artists
            .Include(a => a.Albums)
            .Select(a => mapper.Map<Artist>(a))
            .ToListAsync(cancellationToken);
    }
}
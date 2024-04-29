using AutoMapper;
using Chinook.Core;

namespace Chinook.ClientModels.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Models.Album, Album>();

            CreateMap<Models.Artist, Artist>();

            CreateMap<Models.Playlist, Playlist>();

            CreateMap<Models.Track, PlaylistTrack>()
               .ForMember(dest => dest.AlbumTitle,
                    opt => 
                        opt.MapFrom(src => src.Album != null ? src.Album.Title : string.Empty))
               .ForMember(dest => dest.ArtistName,
                    opt => 
                        opt.MapFrom(src => src.Album != null ? src.Album.Artist.Name : string.Empty))
               .ForMember(dest => dest.TrackName,
                    opt => 
                        opt.MapFrom(src => src.Name))
               .ForMember(dest => dest.IsFavorite,
                    opt => 
                        opt.MapFrom((src, dest, destMember, context) => src.Playlists
                        .Any(p => p.UserPlaylists
                            .Any(up => up.UserId == context.Items[Constants.CURRENT_USER_ID] as string && // user id passed as runtime argument
                                       up.Playlist.Name == Constants.FAVORITES))));


        }
    }
}

namespace Chinook.ClientModels;

public class Playlist
{
    public string Name { get; set; }
    
    public long PlayListId { get; set; }
    public List<PlaylistTrack> Tracks { get; set; }
}
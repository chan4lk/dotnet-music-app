namespace Chinook.Core;

public static class Constants
{
    public static readonly string FAVORITES = "My favorite tracks";

    /// <summary>
    /// Automapper needs runtime argument to mark the track as favorite.
    /// </summary>
    public static readonly string CURRENT_USER_ID = "UserId";

    public static string COMMON_EXCEPTION_MESSAGE = "Something went wrong. Please try again";
    public static string LOADING_PLAYLISTS_FAILED_MESSAGE = "Loading Playlists failed. Please refresh.";
    public static string LOADING_ARTISTS_FAILED_MESSAGE = "Loading Artists failed. Please refresh.";
    public static string LOADING_TRACKS_FAILED_MESSAGE = "Loading Tracks failed. Please refresh.";
}
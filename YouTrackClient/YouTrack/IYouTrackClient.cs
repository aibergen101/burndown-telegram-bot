using Models;

namespace YouTrackData.Clients
{
    public interface IYouTrackClient
    {
        Task<Sprint> GetSprint();
        Task<Issue[]> GetSprintIssues();
    }
}
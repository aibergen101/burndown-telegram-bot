using Models;

namespace YouTrackData
{
    public interface IYouTrackClient
    {
        Task<Sprint> GetSprintDuration();
        Task<Issue[]> GetSprintIssues();
    }
}
using Models;

namespace YouTrackData.Clients
{
    public interface IYouTrackClient
    {
        // Какой именно спринт? Тут кажется должно быть что-то типа GetCurrentSprint
        Task<Sprint> GetSprint();
        // По нэймингу кажется, что надо передать спринт в параметрах. А если не надо, то назвать по другому - типа GetCurrentSprintIssues
        Task<Issue[]> GetSprintIssues();
    }
}
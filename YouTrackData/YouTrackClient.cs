using Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace YouTrackData
{
    public class YouTrackClient
    {
        private readonly HttpClient _httpClient;
        public YouTrackClient(string youTrackToken)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", youTrackToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<Sprint> GetSprintDuration()
        {
            var sprintDuration = await _httpClient.GetAsync("https://acquirica.youtrack.cloud/api/agiles/176-23/sprints/current?fields=start,finish");
            var outputDuration = await sprintDuration.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Sprint>(outputDuration, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<Issue[]> GetSprintIssues()
        {
            var sprintName = Uri.EscapeDataString("Sprints: {2026.19T} Story points: *");
            var sprintStoryPoints = await _httpClient.GetAsync($"https://acquirica.youtrack.cloud/api/issues?fields=id,resolved,customFields(name,value)&customFields=Story%20points&query={sprintName}");
            var outputStoryPoints = await sprintStoryPoints.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Issue[]>(outputStoryPoints, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
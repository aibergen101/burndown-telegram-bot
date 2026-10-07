namespace YouTrackData
{
    public interface IDiagramGenerator
    {
        Task GenerateAndSendDiagram(string chatId);
    }
}
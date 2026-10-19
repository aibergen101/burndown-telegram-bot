namespace YouTrackData
{
    public interface IDiagramGenerator
    {
        byte[] GenerateDiagram(BurndownData burndownData);
    }
}
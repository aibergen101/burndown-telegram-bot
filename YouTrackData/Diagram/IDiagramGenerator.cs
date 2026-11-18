using TelegramClient;

namespace YouTrackData
{
    public interface IDiagramGenerator
    {
        // Наверное было бы понятнее, если бы это было GenerateDiagramImage, иначе не очень понятно, почему ты запрашивашеь диаграмму а получаешь набор байт
        byte[] GenerateDiagram(BurndownData burndownData, ChartTheme theme);
    }
}
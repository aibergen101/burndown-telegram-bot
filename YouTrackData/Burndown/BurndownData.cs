namespace YouTrackData
{
    // Такие объекты лучше делать через конструктор или создавать через Factory method (см паттерн)
    // Потому что сейчас этот объект тебе никто не мешает создать неправильно, например не заполнив часть полей
    public class BurndownData
    {
        public double[] XDays { get; set; }
        public double[] YDays { get; set; }
        public double[] IdealLine { get; set; }
        public double[] ProgressLine { get; set; }
    }
}
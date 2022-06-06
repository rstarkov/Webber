namespace Webber.Client.Models;

public record RouterBlockDto : BaseDto
{
    public int RxLast { get; set; }
    public int TxLast { get; set; }
    public int RxAverageRecent { get; set; }
    public int TxAverageRecent { get; set; }
    public HistoryPoint[] HistoryRecent { get; set; }
    public HistoryPoint[] HistoryHourly { get; set; }

    public record HistoryPoint
    {
        public double TxRate { get; set; }
        public double RxRate { get; set; }
    }
}

namespace Webber.Client.Models;

public record HttpingDto : BaseDto
{
    public HttpingTargetDto[] Targets { get; set; }
}

public record HttpingTargetDto
{
    public string Name { get; set; }
    public int[] Recent { get; set; } // 0 = error, 65535 = timeout, -1 = missing data
    public HttpingIntervalDto[] Twominutely { get; set; }
    public HttpingIntervalDto[] Hourly { get; set; }
    public HttpingIntervalDto[] Daily { get; set; }
    public HttpingIntervalDto[] Monthly { get; set; }
    public HttpingIntervalDto Last30m { get; set; }
    public HttpingIntervalDto Last24h { get; set; }
    public HttpingIntervalDto Last30d { get; set; }
}

public record struct HttpingIntervalDto
{
    public ushort MsResponsePrc01 { get; set; }
    public ushort MsResponsePrc25 { get; set; }
    public ushort MsResponsePrc50 { get; set; }
    public ushort MsResponsePrc75 { get; set; }
    public ushort MsResponsePrc95 { get; set; }
    public ushort MsResponsePrc99 { get; set; }
    public int TotalCount { get; set; } // 0 = missing data
    public int TimeoutCount { get; set; }
    public int ErrorCount { get; set; }

    public bool CountSample(ushort msResponse)
    {
        TotalCount++;
        if (msResponse == 65535)
            TimeoutCount++;
        else if (msResponse == 0)
            ErrorCount++;
        else
            return true;
        return false;
    }
}


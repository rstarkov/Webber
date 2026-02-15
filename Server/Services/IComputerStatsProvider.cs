using Webber.Client.Models;
using Webber.Server.Blocks;

namespace Webber.Server.Services;

interface IComputerStatsProvider
{
    /// <summary>
    /// Fetch current stats. Throws on failure (caller handles offline logic).
    /// </summary>
    Task<ComputerStats> FetchStatsAsync(ComputerConfig config);
}

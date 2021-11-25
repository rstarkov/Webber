using LibreHardwareMonitor.Hardware;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

internal class HwInfoBlockServer : SimpleBlockServerBase<HwInfoBlockDto>
{
    private Computer _computer;
    private Queue<double[]> _historyCpuCoreHeatmap = new();
    private Queue<TimedMetric> _historyCpuTotalLoad = new();
    private Queue<TimedMetric> _historyCpuPackageTemp = new();
    private Queue<TimedMetric> _historyGpuLoad = new();
    private Queue<TimedMetric> _historyGpuTemp = new();
    private Queue<TimedMetric> _historyNetworkUp = new();
    private Queue<TimedMetric> _historyNetworkDown = new();
    private Queue<TimedMetric> _historyNetworkPing = new();

    static readonly int METRIC_REFRESH_INTERVAL = 500;
    static readonly int METRIC_CAPACITY = (int) Math.Ceiling((1000d / METRIC_REFRESH_INTERVAL) * 40d);

    private readonly PingBlockServer _pingProvider;

    public HwInfoBlockServer(IServiceProvider sp, PingBlockServer pingProvider) : base(sp, METRIC_REFRESH_INTERVAL)
    {
        this._pingProvider = pingProvider;
    }

    public override void Start()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = false,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = true,
            IsStorageEnabled = false,
        };
        _computer.Open();
        _computer.Accept(new UpdateVisitor());
        base.Start();
    }

    protected override HwInfoBlockDto Tick()
    {
        _computer.Accept(new UpdateVisitor());

        var time = DateTime.UtcNow;
        var hardware = _computer.Hardware;

        var cpuSensors = hardware.First(s => s.HardwareType == HardwareType.Cpu).Sensors;
        var gpuSensors = hardware.First(s => s.HardwareType == HardwareType.GpuNvidia).Sensors;

        // CPU
        var cores = cpuSensors
            .Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Core"))
            .Select(s => (double) (s.Value ?? 0d))
            .ToArray();
        var cpuHeat = _historyCpuCoreHeatmap.EnqueueWithMaxCapacity(cores, METRIC_CAPACITY);

        var cputotal = cpuSensors
            .Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Total"))
            .Select(s => (double) (s.Value ?? 0d))
            .First();
        var cpuLoad = _historyCpuTotalLoad.EnqueueWithMaxCapacity(new TimedMetric(time, cputotal), METRIC_CAPACITY);

        var cpupackagetemp = cpuSensors
            .Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Package"))
            .Select(s => (double) (s.Value ?? 0d))
            .First();
        var cpuTemp = _historyCpuPackageTemp.EnqueueWithMaxCapacity(new TimedMetric(time, cpupackagetemp), METRIC_CAPACITY);

        // GPU
        var gputotal = gpuSensors
            .Where(s => s.SensorType == SensorType.Load)
            .Select(s => (double) (s.Value ?? 0d))
            .Max();
        var gpuLoad = _historyGpuLoad.EnqueueWithMaxCapacity(new TimedMetric(time, gputotal), METRIC_CAPACITY);

        var gpupackagetemp = gpuSensors
            .Where(s => s.SensorType == SensorType.Temperature)
            .Select(s => (double) (s.Value ?? 0d))
            .Max();
        var gpuTemp = _historyGpuTemp.EnqueueWithMaxCapacity(new TimedMetric(time, gpupackagetemp), METRIC_CAPACITY);

        // NETWORK
        var netquery = hardware
            .Where(h => h.HardwareType == HardwareType.Network)
            .Where(h => h.Name == "ASUS STRIX")
            .Single().Sensors
            .Where(s => s.SensorType == SensorType.Throughput);

        var netup = netquery.Where(s => s.Name.Contains("Upload")).Sum(s => (double) (s.Value ?? 0d));
        var netdown = netquery.Where(s => s.Name.Contains("Download")).Sum(s => (double) (s.Value ?? 0d));
        var networkUp = _historyNetworkUp.EnqueueWithMaxCapacity(new TimedMetric(time, netup), METRIC_CAPACITY);
        var networkDown = _historyNetworkDown.EnqueueWithMaxCapacity(new TimedMetric(time, netdown), METRIC_CAPACITY);

        double GetLastAverage<T>(T[] queue, Func<T, double> selector)
        {
            if (queue.Length == 0) return 0d;
            int numberToAvg = Math.Min(queue.Length - 1, (int) Math.Ceiling(4000d / METRIC_REFRESH_INTERVAL));
            var slice = queue[^numberToAvg..];
            var avg = slice.Select(selector).Sum() / numberToAvg;
            return avg;
        }

        double[] coresAvg = new double[cores.Length];
        for (int i = 0; i < cores.Length; i++)
            coresAvg[i] = GetLastAverage(cpuHeat, a => a[i]);

        var rping = _pingProvider.LastUpdate.Last ?? 99;
        var hping = _historyNetworkPing.Count == 0 || DateTime.UtcNow - _historyNetworkPing.Last().TimeUtc > TimeSpan.FromSeconds(5)
            ? _historyNetworkPing.EnqueueWithMaxCapacity(new TimedMetric(time, rping), METRIC_CAPACITY)
            : _historyNetworkPing.ToArray();

        return new HwInfoBlockDto
        {
            CpuCoreHeatmap = coresAvg,
            CpuTotalLoad = GetLastAverage(cpuLoad, m => m.Value),
            CpuTotalLoadHistory = cpuLoad,
            CpuPackageTemp = GetLastAverage(cpuTemp, m => m.Value),
            CpuPackageTempHistory = cpuTemp,

            GpuLoad = GetLastAverage(gpuLoad, m => m.Value),
            GpuLoadHistory = gpuLoad,
            GpuTemp = GetLastAverage(gpuTemp, m => m.Value),
            GpuTempHistory = gpuTemp,

            NetworkDown = GetLastAverage(networkDown, m => m.Value),
            NetworkDownHistory = networkDown,
            NetworkUp = GetLastAverage(networkUp, m => m.Value),
            NetworkUpHistory = networkUp,
            NetworkPing = GetLastAverage(hping, m => m.Value),
            NetworkPingHistory = hping,
        };
    }

    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    public static void Unregister()
    {
        var c = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true,
        };
        c.Close(); // unregister everything
    }
}

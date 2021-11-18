using Microsoft.Data.Sqlite;
using Webber.Client.Models;
using LibreHardwareMonitor.Hardware;

namespace Webber.Server.Blocks;

public class HwInfoBlockServer : SimpleBlockServerBase<HwInfoBlockDto>
{
    private Computer _computer;
    private Queue<TimedMetric> _historyCpuTotalLoad = new();
    private Queue<TimedMetric> _historyCpuPackageTemp = new();

    public HwInfoBlockServer(IServiceProvider sp) : base(sp, 1000)
    {

    }

    public override bool MigrateSchema(SqliteConnection db, int curVersion)
    {
        _computer = null;
        throw new NotImplementedException();
    }

    public override void Start()
    {
        base.Start();
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = false,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsStorageEnabled = false,
        };
        _computer.Open();
        _computer.Accept(new UpdateVisitor());
    }

    public override HwInfoBlockDto Tick()
    {
        _computer.Accept(new UpdateVisitor());

        var time = DateTime.UtcNow;
        var hardware = _computer.Hardware;

        var cores = hardware
            .First(s => s.HardwareType == HardwareType.Cpu).Sensors
            .Where(s => s.SensorType == SensorType.Load)
            .Where(s => s.Name.Contains("Core"))
            .Select(s => s.Value)
            .Where(s => s.HasValue)
            .Select(s => (double) s.Value)
            .ToArray();

        var cputotal = hardware
            .First(s => s.HardwareType == HardwareType.Cpu).Sensors
            .Where(s => s.SensorType == SensorType.Load)
            .Where(s => s.Name.Contains("Total"))
            .Select(s => s.Value)
            .Where(s => s.HasValue)
            .Select(s => (double) s.Value)
            .First();

        //var cpupackagetemp = hardware
        //    .First(s => s.HardwareType == HardwareType.Cpu).Sensors
        //    .Where(s => s.SensorType == SensorType.Temperature)
        //    .Where(s => s.Name.Contains("Package"))
        //    .Select(s => s.Value)
        //    .Where(s => s.HasValue)
        //    .Select(s => (double) s.Value)
        //    .First();

        _historyCpuTotalLoad.EnqueueWithMaxCapacity(new TimedMetric(time, cputotal), 100);
        //_historyCpuPackageTemp.EnqueueWithMaxCapacity(new TimedMetric(time, cpupackagetemp), 100);

        return new HwInfoBlockDto
        {
            CpuCoreHeatmap = cores,
            CpuTotalLoadHistory = _historyCpuTotalLoad.ToArray(),
        };

        // hub number?
        // timed db metric
        // valid until?
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

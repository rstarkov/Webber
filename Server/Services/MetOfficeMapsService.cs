using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Webber.Server.Services;

public class MetOfficeMapsService
{
    private HttpClient _hc = new();

    public Dictionary<string, Model> Models { get; private set; }

    public void Refresh()
    {
        var fc = XElement.Parse(_hc.GetStringAsync("https://maps.consumer-digital.api.metoffice.gov.uk/v1/config/get-capabilities/fc.xml").GetAwaiter().GetResult());
        var ob = XElement.Parse(_hc.GetStringAsync("https://maps.consumer-digital.api.metoffice.gov.uk/v1/config/get-capabilities/ob.xml").GetAwaiter().GetResult());
        Models = fc.Elements().Select(x => new Model(x, x.Element("long"), x.Element("short")))
            .Concat(ob.Elements().Select(x => new Model(x, x)))
            .ToDictionary(m => m.Name);
    }

    public class Model
    {
        public string Name { get; init; }
        public List<Timestep> Timesteps { get; init; }

        public Model(XElement xml, params XElement[] submodels)
        {
            Name = xml.Name.LocalName;
            var result = new Dictionary<DateTime, Timestep>();
            foreach (var sm in submodels)
            {
                var parsed = proc(sm);
                foreach (var ts in parsed)
                    result[ts.Time] = ts;
            }
            Timesteps = result.Values.OrderBy(x => x.Time).ToList();
        }

        private List<Timestep> proc(XElement xml)
        {
            var submodel = xml.Name.LocalName;
            var runs = xml.Element("model_runs").Elements("model_run").Select(x => (x.Value, parseDateTime(x.Value))).OrderBy(x => x.Item2).ToList();
            if (xml.Element("timesteps") != null)
            {
                // merge all of them but latest model runs take precedence (lets us get forecasts for further in the past than the latest run offers)
                var result = new Dictionary<DateTime, Timestep>();
                foreach (var run in runs)
                    foreach (var ts in xml.Element("timesteps").Elements("timestep").Select(ts => new Timestep(ts.Value, run.Item2, $"/wms_fc/single/high-res/{submodel}/{Name}/{run.Item1}/{ts.Value}", Name)))
                        result[ts.Time] = ts;
                return result.Values.OrderBy(ts => ts.Time).ToList();
            }
            else
            {
                return runs.Select(r => new Timestep("PT0S", r.Item2, $"/wms_ob/single/high-res/{Name}/{r.Item1}", Name)).ToList();
            }
        }

        private DateTime parseDateTime(string value)
        {
            // the only format that actually *returns* a UTC datetime for a Zulu time all by itself is "O", but it also requires 8 decimal places after seconds, so AdjustToUniversal is the best option
            return DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ssZ", null, DateTimeStyles.AdjustToUniversal); // this will fail if Z is missing so the result will never be Unspecified
        }
    }

    public class Timestep
    {
        public string Url { get; set; }
        public string ModelName { get; set; }
        public DateTime ModelRun { get; init; }
        public DateTime Time { get; init; }

        public Timestep(string ts, DateTime modelRun, string url, string modelName)
        {
            Url = url;
            ModelRun = modelRun;
            ModelName = modelName;
            // PT0S, PT15M, PT2H, PT2H15M, PT108H, P5D,
            var match = Regex.Match(ts, @"PT?((?<d>\d+)D)?((?<h>\d+)H)?((?<m>\d+)M)?((?<s>\d+)S)?");
            if (!match.Success)
                throw new Exception($"Can't parse timestamp: '{ts}'");
            int g(string name) => match.Groups[name].Success ? int.Parse(match.Groups[name].Value) : 0;
            var val = new TimeSpan(g("d"), g("h"), g("m"), g("s"));
            Time = modelRun + val;
        }
    }

    public async Task<byte[]> DownloadImage(Timestep ts)
    {
        var url = $"https://maps.consumer-digital.api.metoffice.gov.uk{ts.Url}.png";
        var resp = await _hc.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync();
    }
}

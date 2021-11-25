using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

internal class TimeUntilBlockServer : SimpleBlockServerBase<TimeUntilBlockDto>
{
    static readonly string[] Scopes = { CalendarService.Scope.CalendarReadonly };
    static readonly string ApplicationName = "TimeUntilBlockServer";
    static readonly string CredentialsJson = "{\"installed\":{\"client_id\":\"130261896764-m7jl26tiob0gdrjqrmgjm0fcqqg0nkh2.apps.googleusercontent.com\",\"project_id\":\"titanium-cacao-318415\",\"auth_uri\":\"https://accounts.google.com/o/oauth2/auth\",\"token_uri\":\"https://oauth2.googleapis.com/token\",\"auth_provider_x509_cert_url\":\"https://www.googleapis.com/oauth2/v1/certs\",\"client_secret\":\"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\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\",\"http://localhost\"]}}";

    private CalendarService _svc;

    public TimeUntilBlockServer(IServiceProvider sp) : base(sp, TimeSpan.FromMinutes(1))
    {

    }

    public override void Start()
    {
        UserCredential credential;

        // The file token.json stores the user's access and refresh tokens, and is created
        // automatically when the authorization flow completes for the first time.
        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(CredentialsJson))).Secrets,
            Scopes, "user", CancellationToken.None,
            new FileDataStore("calendar-token", true)).Result;

        _svc = new CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });

        base.Start();
    }

    protected override TimeUntilBlockDto Tick()
    {
        // bins = 2kvskj1am9j51bn2ob1dre7437oeil5g@import.calendar.google.com
        // work = caelan.sayler@bluestone.co.uk

        EventsResource.ListRequest request1 = _svc.Events.List("primary");
        request1.TimeMin = DateTime.Now;
        request1.ShowDeleted = false;
        request1.SingleEvents = true;
        request1.MaxResults = 10;
        request1.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        EventsResource.ListRequest request2 = _svc.Events.List("caelan.sayler@bluestone.co.uk");
        request2.TimeMin = DateTime.Now;
        request2.ShowDeleted = false;
        request2.SingleEvents = true;
        request2.MaxResults = 10;
        request2.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        bool checkSelfRsvpNotDeclined(Event i)
        {
            if (i == null || i.Attendees == null) return true;
            var me = i.Attendees.FirstOrDefault(a => a.Self == true);
            if (me == null) return true;
            return me.ResponseStatus != "declined";
        }

        var events = request1.Execute().Items.Concat(request2.Execute().Items).ToArray();

        var candidates = events
            .Where(i => i.Start != null && i.Start.DateTime != null) // eliminate all day events
            .Where(i => i.EventType == "default") // filter out OOO
            .Where(checkSelfRsvpNotDeclined) // filter out events I have declined
            .Where(i => !String.IsNullOrWhiteSpace(i.Summary)) // no title?
            .Select(i => new CalendarEvent() { DisplayName = i.Summary ?? i.Description, Time = i.Start.DateTime.Value.ToUniversalTime() })
            .Concat(new[] { new CalendarEvent() { DisplayName = "Sleep!", Time = DateTime.UtcNow.Date.AddHours(22.5) } }) // sleep!
            .OrderBy(i => i.Time)
            .ToArray();

        return new TimeUntilBlockDto()
        {
            NextEvent = candidates.First(),
            SecondEvent = candidates.Skip(1).First(),
        };
    }
}

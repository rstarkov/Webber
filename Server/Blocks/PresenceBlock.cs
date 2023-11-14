using Microsoft.AspNetCore.Mvc;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

class PresenceBlockConfig
{
    public string ApiKey { get; set; }
}

class PresenceBlockServer : SimpleBlockServerBase<PresenceBlockDto>
{
    private PresenceBlockConfig _config;
    private bool _sessionUnlocked = true;
    private bool _presenceDetected = true;

    public PresenceBlockServer(IServiceProvider sp, PresenceBlockConfig config)
        : base(sp, TimeSpan.FromHours(1))
    {
        _config = config;
    }

    public override void Init(WebApplication app)
    {
        base.Init(app);
        app.MapGet("/presence/lock", ([FromQuery] string auth) => Handle(auth, () => _sessionUnlocked = false));
        app.MapGet("/presence/unlock", ([FromQuery] string auth) => Handle(auth, () => _sessionUnlocked = true));
        app.MapGet("/presence/detected", ([FromQuery] string auth) => Handle(auth, () => _presenceDetected = true));
        app.MapGet("/presence/undetected", ([FromQuery] string auth) => Handle(auth, () => _presenceDetected = false));
    }

    protected override PresenceBlockDto Tick()
    {
        return new PresenceBlockDto
        {
            PresenceDetected = _presenceDetected,
            SessionUnlocked = _sessionUnlocked
        };
    }

    private IResult Handle(string auth, Action act)
    {
        if (auth != _config.ApiKey)
            return Results.Unauthorized();

        act();
        SendUpdate(Tick());
        return Results.Ok();
    }
}

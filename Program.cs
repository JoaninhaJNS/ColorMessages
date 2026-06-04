using Xabbo;
using Xabbo.GEarth;
using Xabbo.Messages.Flash;

var ext = new GEarthExtension(new GEarthOptions
{
    Name = "ColorMessages",
    Description = "Chat with colors, requires HC",
    Author = "JoaninhaJNS",
    Version = "1.0.1"
});

string? activeColor = null;
string[] colors = ["red", "purple", "blue", "cyan", "green"];
bool? isUserVip = null;
bool shoutAlways = false;

void infoMsg(string message)
{
    ext.Send(In.Chat, -1, $"[b][ColorMessages]: {message}[/b]", 0, 34, 0, 0);
}

ext.Connected += e =>
{
    if (e.PreEstablished)
        ext.Send(Out.ScrGetUserInfo, "habbo_club");
};

ext.Disconnected += () =>
{
    isUserVip = null;
};

ext.Intercept(In.ScrSendUserInfo, e =>
{
    e.Packet.Read<string>();
    e.Packet.Read<int>();
    e.Packet.Read<int>();
    e.Packet.Read<int>();
    e.Packet.Read<int>();
    e.Packet.Read<bool>();
    e.Packet.Read<bool>();
    e.Packet.Read<int>();
    e.Packet.Read<int>();
    int minutesLeft = e.Packet.Read<int>();
    isUserVip = minutesLeft > 0;
});

ext.Intercept([Out.Chat, Out.Shout, Out.Whisper], e =>
{
    string message = e.Packet.Read<string>();
    string input = message.Trim();

    if (input.StartsWith(":colormsg", StringComparison.OrdinalIgnoreCase))
    {
        if (isUserVip == false || isUserVip is null)
        {
            infoMsg("An active Habbo Club subscription is required to use the extension...");
            e.Block();
            return;
        }
        string args = input[9..].Trim();

        if (string.IsNullOrEmpty(args))
        {
            infoMsg("Commands available:\n\n" +
                       "[red]:colormsg red[/red]\n" +
                       "[purple]:colormsg purple[/purple]\n" +
                       "[blue]:colormsg blue[/blue]\n" +
                       "[cyan]:colormsg cyan[/cyan]\n" +
                       "[green]:colormsg green[/green]\n" +
                       ":colormsg default\n" +
                       ":colormsg shoutalways - chat messages become shouts");
            e.Block();
            return;
        }

        string color = args.ToLower();

        if (color == "default")
        {
            activeColor = null;
            infoMsg("Default color restored!");
            e.Block();
            return;
        }

        if (color == "shoutalways")
        {
            shoutAlways = !shoutAlways;
            infoMsg(shoutAlways ? "[green]Shout always enabled![/green]" : "[red]Shout always disabled![/red]");
            e.Block();
            return;
        }

        if (colors.Contains(color))
        {
            activeColor = color;
            string colorName = char.ToUpper(color[0]) + color[1..];
            infoMsg($"[{color}]{colorName} color enabled![/{color}]");
            e.Block();
            return;
        }
        else
        {
            infoMsg("Invalid color!\n" +
                       "Available colors:\n" +
                       "[red]red[/red], [purple]purple[/purple], [blue]blue[/blue], " +
                       "[cyan]cyan[/cyan], [green]green[/green], default");
            e.Block();
            return;
        }
    }

    if (activeColor is not null)
    {
        if (ext.Messages.Is(e.Packet.Header, Out.Whisper))
        {
            string[] parts = message.Split(' ', 2);
            if (parts.Length == 2)
                e.Packet.ReplaceAt<string>(0, $"{parts[0]} @{activeColor}@ {parts[1]}");
        }
        else
        {
            e.Packet.ReplaceAt<string>(0, $"@{activeColor}@ {message}");
        }
    }

    if (shoutAlways && ext.Messages.Is(e.Packet.Header, Out.Chat))
    {
        e.Packet.Header = ext.Messages.Resolve(Out.Shout);
    }
});

ext.Run();
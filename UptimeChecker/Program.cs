using System.Net.NetworkInformation;
using System.Text;

using Microsoft.Extensions.Configuration;

using Serilog;

using UptimeChecker;

var failures = new Stack<(DateTime OutageStart, int FailuresSince)>();

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var settings = config
    .GetSection(nameof(Settings))
    .Get<Settings>()
    .Validate();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

_ = Task.Run(UptimeJob);

while (true)
{
    // Any key draws the graph
    Console.ReadKey(true);
    DrawLineGraph();
}

async Task UptimeJob()
{
    var offline = false;
    var offlineCount = 0;
    var lastOutageStart = DateTime.MinValue;
    var outageDuration = default(TimeSpan);

    await BeepAsync();

    while (true)
    {
        var pingSuccess = default(bool);
        var pingResponseTime = default(long);

        try
        {
            var pingReply = new Ping().Send(settings!.PingTargetHost!, settings.PingTimeoutMs!.Value);
            pingResponseTime = pingReply.RoundtripTime;
            pingSuccess = pingReply.Status == IPStatus.Success;
        }
        catch
        {
            pingSuccess = false;
        }

        if (pingSuccess)
        {
            // We are online, but we were offline before
            if (offline)
            {
                offline = false;
                outageDuration = DateTime.Now - lastOutageStart;
                lastOutageStart = DateTime.MinValue;

                Log.Warning(
                    "[{0:O}] - Back Online - Outage Count: {1}, Last Outage Duration: {2}, Response time: {3}ms",
                    DateTime.Now, offlineCount, outageDuration, pingResponseTime);

                failures.Push((DateTime.Now, 0)); // Add a zero count for successful ping

                await BeepAsync();
            }
            else
            {
                Log.Information(
                    "[{0:O}] - Online - Outage Count: {1}, Last Outage Duration: {2}, Response time: {3}ms",
                    DateTime.Now, offlineCount, outageDuration, pingResponseTime);
            }
        }
        else
        {
            // We are offline, but we were online before
            if (!offline)
            {
                offline = true;
                offlineCount++;
                lastOutageStart = DateTime.Now;
                failures.Push((DateTime.Now, 1)); // Add a one count for failed ping
                await BeepAsync();
            }
            else
            {
                var lastFailure = failures.Pop();
                lastFailure.FailuresSince++; // Increment the count for the current outage
                failures.Push(lastFailure);
            }

            var currentOutageDuration = DateTime.Now - lastOutageStart;

            Console.ForegroundColor = ConsoleColor.Red;
            Log.Error("[{0:O}] - Offline - Outage Count: {1}, Current Outage Duration: {2}",
                DateTime.Now, offlineCount, currentOutageDuration);
        }

        await Task.Delay(settings.PingFrequencyMs!.Value);
    }
}

static async Task BeepAsync()
{
    for (int i = 0; i < 4; i++)
    {
        Console.Beep(800, 100);
        await Task.Delay(25);
    }
}

void DrawLineGraph()
{
    var sb = new StringBuilder("Failed Ping Counts Line Graph:")
        .AppendLine();

    if (failures.Count == 0)
    {
        sb.Append("No failures have happened.");
        Log.Information(sb.ToString());
        return;
    }

    foreach (var (OutageStart, FailuresSince) in failures)
    {
        sb.Append($"{OutageStart} {FailuresSince} ");
        for (int i = 0; i < FailuresSince; i++)
        {
            sb.Append('#');
        }
        sb.AppendLine();
    }
    Log.Information(sb.ToString()[..^1]); // Trim the last newline
}

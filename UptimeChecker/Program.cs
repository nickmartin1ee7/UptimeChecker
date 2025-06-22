using System.Collections.Concurrent;
using System.Net.NetworkInformation;

using Microsoft.Extensions.Configuration;

using Serilog;

using Spectre.Console;

using UptimeChecker;

var failures = new ConcurrentStack<(DateTime OutageStart, DateTime OutageEnd, int FailuresSince)>();

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
    PrintOutageReport();
}

async Task UptimeJob()
{
    var offline = false;
    var offlineCount = 0;
    var lastOutageStart = DateTime.MinValue;
    var outageDuration = default(TimeSpan);

    await BeepAsync(false);
    await BeepAsync(true);

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
                var outageEnd = DateTime.Now;
                offline = false;
                outageDuration = outageEnd - lastOutageStart;
                lastOutageStart = DateTime.MinValue;

                Log.Warning(
                    "[{0:O}] - Back Online - Outage Count: {1}, Last Outage Duration: {2}, Response time: {3}ms",
                    outageEnd, offlineCount, outageDuration, pingResponseTime);

                if (failures.TryPop(out var lastFailure))
                {
                    lastFailure.OutageEnd = outageEnd;
                    if (lastFailure.FailuresSince > 1)
                    {
                        await BeepAsync(true);
                        failures.Push(lastFailure);
                    }

                }
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
                failures.Push((DateTime.Now, DateTime.MaxValue, 1)); // Add a one count for failed ping
            }
            else if (failures.TryPop(out var lastFailure))
            {
                lastFailure.FailuresSince++; // Increment the count for the current outage
                failures.Push(lastFailure);

                if (lastFailure.FailuresSince == 2)
                {
                    await BeepAsync(false);
                }
            }

            var currentOutageDuration = DateTime.Now - lastOutageStart;

            Console.ForegroundColor = ConsoleColor.Red;
            Log.Error("[{0:O}] - Offline - Outage Count: {1}, Current Outage Duration: {2}",
                DateTime.Now, offlineCount, currentOutageDuration);
        }

        await Task.Delay(settings.PingFrequencyMs!.Value);
    }
}

static async Task BeepAsync(bool amOnline)
{
    if (!amOnline)
    {
        Console.Beep(800, 100);
        Console.Beep(400, 100);
    }
    else
    {
        Console.Beep(400, 100);
        Console.Beep(800, 100);
    }
    await Task.Delay(25);
}

void PrintOutageReport()
{
    var tempFailures = failures
        .OrderBy(failure => failure.OutageEnd)
        .ToArray();

    if (tempFailures.Length == 0)
    {
        AnsiConsole.WriteLine("No outages have been observed.");
        return;
    }

    var table = new Table
    {
        Title = new TableTitle("Outage Report"),

        Caption = new TableTitle($"Target {settings.PingTargetHost} pinged at a rate of {settings.PingFrequencyMs}ms with a timeout of {settings.PingTimeoutMs}ms.")
    };
    table.AddColumns(
            "Outage Start",
            "Outage End",
            "Outage Duration",
            "Ping Failures");

    var barChart = new BarChart();

    foreach (var (OutageStart, OutageEnd, FailuresDuringOutage) in tempFailures)
    {
        var rowStyle = Style.Plain;

        // Ongoing outage
        if (OutageEnd == DateTime.MaxValue)
        {
            rowStyle = new Style(foreground: Color.Orange1, background: rowStyle.Background, decoration: rowStyle.Decoration, link: rowStyle.Link);
        }

        barChart.AddItem(new BarChartItem(string.Empty, FailuresDuringOutage, rowStyle.Foreground));
    }

    for (int i = 0; i < tempFailures.Length; i++)
    {
        var (OutageStart, OutageEnd, FailuresDuringOutage) = tempFailures[i];
        var rowStyle = Style.Plain;
        var formattedOutageEnd = $"{OutageEnd}";
        var formattedOutageDuration = $"{OutageEnd - OutageStart}";

        // Ongoing outage
        if (OutageEnd == DateTime.MaxValue)
        {
            rowStyle = new Style(foreground: Color.Orange1, background: rowStyle.Background, decoration: rowStyle.Decoration, link: rowStyle.Link);
            formattedOutageEnd = "Ongoing";
            formattedOutageDuration = $"{DateTime.Now - OutageStart}";
        }

        var largestOutage = barChart.Data.Max(row => row.Value);
        var localChart = new BarChart
        {
            Width = (int)(FailuresDuringOutage * 100 / largestOutage)
        };

        table.AddRow(
            new Text($"{OutageStart}", rowStyle),
            new Text(formattedOutageEnd, rowStyle),
            new Text(formattedOutageDuration, rowStyle),
            localChart.AddItem(barChart.Data[i]));
    }

    AnsiConsole.Write(table);
}

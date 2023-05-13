using System.Net.NetworkInformation;

class Program
{
    static List<int> _failedPingCounts = new List<int>();

    static void Main()
    {
        _ = Task.Run(UptimeJob);

        while (true)
        {
            Console.ReadKey(true);
            DrawLineGraph(); // Draw the line graph
        }
    }

    private static void UptimeJob()
    {
        bool offline = false;
        int offlineCount = 0;
        DateTime lastOutageStart = DateTime.MinValue;
        TimeSpan outageDuration = default;

        Beep();

        while (true)
        {
            bool pingSuccess;
            long pingResponseTime = default;

            try
            {
                var pingReply = new Ping().Send("1.1.1.1", 1000);
                pingResponseTime = pingReply.RoundtripTime;
                pingSuccess = pingReply.Status == IPStatus.Success;
            }
            catch
            {
                pingSuccess = false;
            }

            if (pingSuccess)
            {
                // Online
                if (offline)
                {
                    offline = false;
                    outageDuration = DateTime.Now - lastOutageStart;
                    lastOutageStart = DateTime.MinValue;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(
                        "[{0:O}] - Back Online - Outage Count: {1}, Last Outage Duration: {2}, Response time: {3}ms",
                        DateTime.Now, offlineCount, outageDuration, pingResponseTime);

                    _failedPingCounts.Add(0); // Add a zero count for successful ping

                    Beep();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(
                        "[{0:O}] - Online - Outage Count: {1}, Last Outage Duration: {2}, Response time: {3}ms",
                        DateTime.Now, offlineCount, outageDuration, pingResponseTime);
                }
            }
            else
            {
                // Offline
                if (!offline)
                {
                    offline = true;
                    offlineCount++;
                    lastOutageStart = DateTime.Now;
                    _failedPingCounts.Add(1); // Add a one count for failed ping
                    Beep();
                }
                else
                {
                    _failedPingCounts[^1]++; // Increment the count for the current outage
                }

                var currentOutageDuration = DateTime.Now - lastOutageStart;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0:O}] - Offline - Outage Count: {1}, Current Outage Duration: {2}",
                    DateTime.Now, offlineCount, currentOutageDuration);
            }

            Thread.Sleep(1000);
        }
    }

    private static void Beep()
    {
        for (int i = 0; i < 4; i++)
        {
            Console.Beep(800, 100);
            Thread.Sleep(25);
        }
    }

    private static void DrawLineGraph()
    {
        Console.WriteLine("Failed Ping Counts Line Graph:");
        foreach (int count in _failedPingCounts)
        {
            Console.Write(count + " ");
            for (int i = 0; i < count; i++)
            {
                Console.Write("#");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}

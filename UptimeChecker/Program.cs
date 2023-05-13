using System.Net.NetworkInformation;

class Program
{
    static void Main()
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
                var pingReply = new Ping().Send("google.com", 1000);
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
                    Console.WriteLine("[{0:O}] - Back Online - Outage Count: {1}, Last Outage Duration: {2}, Response time: {3}",
                        DateTime.Now, offlineCount, outageDuration, pingResponseTime);

                    Beep();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("[{0:O}] - Online - Outage Count: {1}, Last Outage Duration: {2}, Response time: {3}",
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
                }
                var currentOutageDuration = DateTime.Now - lastOutageStart;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[{0:O}] - Offline - Outage Count: {1}, Current Outage Duration: {2}",
                    DateTime.Now, offlineCount, currentOutageDuration);

                Beep();
            }

            Thread.Sleep(1000);
        }
    }

    private static void Beep()
    {
        for (int i = 0; i < 4; i++)
        {
            Console.Beep(800, 100);
        }
    }
}

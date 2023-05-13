using System.Net.NetworkInformation;

class Program
{
    private static TimeSpan outageDuration;

    static void Main()
    {
        bool offline = false;
        int offlineCount = 0;
        DateTime lastOutageStart = DateTime.MinValue;

        while (true)
        {
            bool pingResult = false;
            try
            {
                pingResult = new Ping().Send("google.com", 1000).Status == IPStatus.Success;
            }
            catch
            {
                pingResult = false;
            }

            if (pingResult)
            {

                // Online
                if (offline)
                {
                    offline = false;
                    outageDuration = DateTime.Now - lastOutageStart;
                    lastOutageStart = DateTime.MinValue;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[{0:O}] - Back Online - Outage Count: {1}, Last Outage Duration: {2}",
                        DateTime.Now, offlineCount, outageDuration);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("[{0:O}] - Online - Outage Count: {1}, Last Outage Duration: {2}",
                        DateTime.Now, offlineCount, outageDuration);
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
            }

            Thread.Sleep(1000);
        }
    }
}

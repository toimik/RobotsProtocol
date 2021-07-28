namespace Toimik.RobotsProtocol.Samples
{
    using System;
    using System.Collections.Generic;

    public class TxtProgram
    {
        public static void Main()
        {
            var content = @"
                # This is a sample content of a simple robots.txt

                User-agent: my-bot
                User-agent: *       # This value is the catch-all for all other bots
                Allow: /path        # This is a supported non-standard directive

                User-agent: my-bot
                User-agent: your-bot
                Dissalow: /         # This field is intentionally misspelled

                User-agent: your-bot
                Crawl-delay: 5      # This is another supported non-standard directive

                Sitemap: http://www.example.com/sitemap.xml
                Sitemap: http://www.example.com/sitemap2.xml

                Host: example.com   # This is yet another supported non-standard directive

                Useragent: *        # This field is also intentionally misspelled
                Crawl-delay: 2";

            var robotsTxt = new RobotsTxt();

            // Since 'Allow' is a non standard directive, it is ignored unless we tell it to take
            // the values into consideration when determining if a path may be crawled. This value
            // is false by default but is shown here for demo purposes.
            var isAllowDirectiveIgnored = false;

            // Since 'Host' is a non standard directive, it is ignored unless we tell it to capture
            // the values
            var customFields = new HashSet<string>
            {
                "host"
            };

            // If we want to be lenient with misspellings, we specify the mappings here. Otherwise,
            // lines with misspelled fields are ignored.
            var misspelledFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // NOTE: Here are two but all the fields are supported
                { "Dissalow", "Disallow" },
                { "Useragent", "User-agent" },
            };
            _ = robotsTxt.Load(
                content,
                isAllowDirectiveIgnored,
                customFields,
                misspelledFields);

            List("Host", robotsTxt.GetCustom("host"));

            Console.WriteLine();

            var userAgents = new HashSet<string>() { "my-bot", "your-bot", "other-bot" };
            foreach (string userAgent in userAgents)
            {
                Console.WriteLine($"Crawl-delay ({userAgent}): {robotsTxt.GetCrawlDelay(userAgent)}");
            }

            Console.WriteLine();

            var paths = new HashSet<string>() { "/", "/path", "/file.html" };
            foreach (string path in paths)
            {
                foreach (string bot in userAgents)
                {
                    Console.WriteLine($"'{path}' allowed for '{bot}'? {robotsTxt.IsAllowed(bot, path)}");
                }

                Console.WriteLine();
            }

            List("Sitemap", robotsTxt.Sitemaps);
        }

        private static void List(string field, IEnumerator<string> enumerator)
        {
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                Console.WriteLine($"{field}: {value}");
            }
        }
    }
}
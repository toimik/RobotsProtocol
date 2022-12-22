namespace Toimik.RobotsProtocol.Samples;

using System;
using System.Collections.Generic;

public class TagProgram
{
    public static void Main()
    {
        var robotsTag = new RobotsTag();

        // This data is either retrieved from Robots Meta Tag (e.g. <meta name="badbot"
        // content="none"> or X-Robots-Tag HTTP response header (e.g. X-Robots-Tag: otherbot:
        // index, nofollow). But here, we hard code them.
        var data = new string[]
        {
            // To some crawlers, this means noindex, nofollow
            "badbot: none",

            // This is as good as not explicitly stating them
            "goodbot: index, follow",

             // This implicitly refers to the default user agent ("robots") that means "all
             // other crawlers"
            "max-snippet: 100, noindex, nofollow",
        };

        // According to the standard, colon (':') is used to identify either a user agent or a
        // directive. By default, if a colon is found in a line of data, the text to the left of
        // the colon refers to the user-agent.

        // However, the first line starts with "max-snippet", which is actually a directive.
        // Therefore, it is identified as a special word so that the parser does not mistake it
        // as a user agent.
        var specialWords = new HashSet<string>
        {
            "max-snippet",

            // ... Add accordingly when used in your program
        };

        // Load the data to parse. This extracts every directive into their own Tag class
        _ = robotsTag.Load(data, specialWords);

        // There should be three tags for the default user agent.
        var tagCount = robotsTag.GetTagCount("robots");
        Console.WriteLine($"There are {tagCount} tags for 'robots' user agent: ");

        // List the tags
        var tags = robotsTag.GetTags("robots");
        while (tags.MoveNext())
        {
            Console.WriteLine($"  {tags.Current}");
        }

        Console.WriteLine();

        // This condition is required if 'none' is a directive that your program supports
        var hasNone = robotsTag.HasTag("robots", "none");

        var hasNoIndex = robotsTag.HasTag("robots", "noindex");
        var isIndexable = !hasNone && !hasNoIndex;
        Console.WriteLine($"Is indexable for 'robots'? {isIndexable}");

        var hasNoFollow = robotsTag.HasTag("robots", "nofollow");
        var isFollowable = !hasNone && !hasNoFollow;
        Console.WriteLine($"Is followable for 'robots'? {isFollowable}");
    }
}
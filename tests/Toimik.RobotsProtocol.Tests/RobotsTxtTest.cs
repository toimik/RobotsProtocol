namespace Toimik.RobotsProtocol.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public class RobotsTxtTest
    {
        [Fact]
        public async Task AllowDefinedAtValidLocation()
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                {new Directive(isAllowed: true, path: string.Empty)}"; // allow:

            var errors = await robotsTxt.Load(data);

            Assert.Empty(errors);
        }

        [Fact]
        public async Task AllowDefinedAtValidLocationButIsIgnored()
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                {new Directive(isAllowed: true, path: string.Empty)}"; // allow:

            await robotsTxt.Load(data, isAllowDirectiveIgnored: true);

            // An ignored allow has the same meaning of disallow nothing
            Assert.True(robotsTxt.IsAllowed("bot", "/"));
        }

        [Fact]
        public async Task AllowDefinedWithoutColonAtValidLocationButIsIgnored()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow";

            var errors = await robotsTxt.Load(data, isAllowDirectiveIgnored: true);

            Assert.Empty(errors);
        }

        [Fact]
        public async Task BlankLines()
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                {Environment.NewLine}
                {Environment.NewLine}";

            var errors = await robotsTxt.Load(data);

            Assert.Empty(errors);
        }

        [Fact]
        public async Task CasingAndInsignificantSpacesDoNotMatter()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                uSeR-aGent:bot
                USER-AGENT : *   ### Test multiple comment indicators
                 ALLOW  : /       # #
                 CRAWL-DELAY   : 11
                 DISALLOW   : /

                SITEMAP : http://www.example.com/sitemap.xml.gz # Test non XML extension
                sIteMap:http://www.example.com/sitemap.xml

                #";

            var errors = await robotsTxt.Load(data);

            Assert.Empty(errors);
        }

        [Fact]
        public async Task Comments()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                #Comment
                # Comment ";

            var errors = await robotsTxt.Load(data);

            Assert.Empty(errors);
        }

        [Fact]
        public async Task CrawlDelay()
        {
            var robotsTxt = new RobotsTxt();
            const int ExpectedCrawlDelay = 1;
            var data = @$"
                user-agent: *
                crawl-delay: {ExpectedCrawlDelay} # This takes effect because it is the latest entry with a value that can be parsed into a number
                crawl-delay: b";

            await robotsTxt.Load(data);

            Assert.Equal(ExpectedCrawlDelay, robotsTxt.GetCrawlDelay("bot"));
        }

        [Fact]
        public async Task CrawlDelayDefinedAtInvalidLocation()
        {
            var robotsTxt = new RobotsTxt();
            var data = "crawl-delay: 1";

            var errors = await robotsTxt.Load(data);

            Assert.Single(errors);

            var expectedError = new Error<TxtErrorCode>(new Line(1, data.TrimEnd()), TxtErrorCode.RuleFoundBeforeUserAgent);
            var error = Utils.GetOnlyItem(errors);
            var actualError = error.ToString();
            Assert.Equal(expectedError.ToString(), actualError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DirectiveDefinedAtInvalidLocation(bool flag)
        {
            var robotsTxt = new RobotsTxt();
            var data = new Directive(isAllowed: flag, path: string.Empty).ToString();

            var errors = await robotsTxt.Load(data);

            Assert.Single(errors);

            var expectedError = new Error<TxtErrorCode>(new Line(1, data.TrimEnd()), TxtErrorCode.RuleFoundBeforeUserAgent);
            var error = Utils.GetOnlyItem(errors);
            var actualError = error.ToString();
            Assert.Equal(expectedError.ToString(), actualError);
        }

        [Fact]
        public async Task HostCaptured()
        {
            var robotsTxt = new RobotsTxt();
            const string Field = "Host";
            const string ExpectedValue = "example.com";
            var data = $"{Field}: {ExpectedValue}";
            var customFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Field,
            };

            await robotsTxt.Load(data, customFields: customFields);

            var values = robotsTxt.GetCustom(Field);
            var actualValue = Utils.GetOnlyItem(values);
            Assert.Equal(ExpectedValue, actualValue);
        }

        [Fact]
        public async Task HostCapturedOnce()
        {
            var robotsTxt = new RobotsTxt();
            const string Field = "Host";
            const string ExpectedValue = "example.com";
            var data = @$"
                {Field}: {ExpectedValue}
                {Field}: {ExpectedValue}";
            var customFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Field,
            };

            await robotsTxt.Load(data, customFields: customFields);

            var actualCount = robotsTxt.GetCustomCount(Field);
            Assert.Equal(1, actualCount);
        }

        [Fact]
        public async Task HostUncaptured()
        {
            var robotsTxt = new RobotsTxt();
            const string Field = "Host";
            var data = @$"
                Sitemap: http://www.example.com/sitemap.xml.gz
                {Field}: example.com";

            await robotsTxt.Load(data);

            Assert.Equal(0, robotsTxt.GetCustomCount(Field));
            Assert.False(robotsTxt.GetCustom(Field).MoveNext());
        }

        [Fact]
        public async Task InitialState()
        {
            var robotsTxt = new RobotsTxt();

            await robotsTxt.Load("");

            Assert.Null(robotsTxt.GetCrawlDelay("not found"));
            Assert.False(robotsTxt.GetCustom("not found").MoveNext());
            Assert.Equal(0, robotsTxt.GetCustomCount("not found"));
            Assert.Null(robotsTxt.GetRuleGroup("not found"));
            Assert.Null(robotsTxt.GetSpecificUserAgent("not found"));

            Assert.True(robotsTxt.IsAllowed("not found", "not found"));
        }

        [Fact]
        public async Task MatchByImplicitAllow()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: bot
                disallow: /";

            await robotsTxt.Load(data);

            var matchResult = robotsTxt.Match("bottle", "/file");
            Assert.Null(matchResult.UserAgent);
        }

        [Fact]
        public async Task MatchByMostSpecific()
        {
            var robotsTxt = new RobotsTxt();
            const string ExpectedPath = "/folder/p";
            var data = @$"
                user-agent: *
                allow: /folder
                allow: {ExpectedPath}";

            await robotsTxt.Load(data);

            var matchResult = robotsTxt.Match("bot", "/folder/page");
            Assert.Equal(ExpectedPath, matchResult.Directive.Path);
        }

        [Theory]
        [InlineData("ALLOW")]
        [InlineData("Allow # With spaces")]
        [InlineData("DISALLOW")]
        [InlineData("Disallow # With spaces")]

        // No need to test the above two with ':' because empty value is allowed for them
        [InlineData("USER-AGENT")]
        [InlineData("User-agent:")]
        [InlineData(" user-agent : # With spaces ")]
        [InlineData("SITEMAP")]
        [InlineData("Sitemap:")]
        [InlineData(" sitemap : # With spaces ")]
        [InlineData("CRAWL-DELAY")]
        [InlineData("Crawl-delay:")]
        [InlineData(" crawl-delay : # With spaces ")]
        public async Task MissingValue(string field)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                {Environment.NewLine}
                user-agent: * # This enables testing for 'allow' and 'disallow' fields
                {field}";

            var errors = await robotsTxt.Load(data);

            Assert.Single(errors);

            var expectedError = new Error<TxtErrorCode>(new Line(2, field.Trim()), TxtErrorCode.MissingValue);
            var error = Utils.GetOnlyItem(errors);
            var actualError = error.ToString();
            Assert.Equal(expectedError.ToString(), actualError);
        }

        [Fact]
        public async Task Misspelling()
        {
            var robotsTxt = new RobotsTxt();
            const string Path = "/path";
            const string MisspelledDirective = "Dissalow";
            var data = @$"
                User-agent: *
                {MisspelledDirective}: {Path}";

            var misspelledFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { MisspelledDirective, "Disallow" }
            };
            await robotsTxt.Load(data, misspelledFields: misspelledFields);

            Assert.False(robotsTxt.IsAllowed("bot", Path));
        }

        [Theory]
        [InlineData("allow")]
        [InlineData("disallow")]
        public async Task PathWithoutSlashPrefix(string directive)
        {
            var robotsTxt = new RobotsTxt();
            var problematicText = $"{directive}: path";
            var data = @$"
                user-agent: *
                {problematicText}";

            var errors = await robotsTxt.Load(data);

            Assert.Single(errors);

            var expectedError = new Error<TxtErrorCode>(new Line(2, problematicText), TxtErrorCode.InvalidPathFormat);
            var error = Utils.GetOnlyItem(errors);
            var actiaErrpr = error.ToString();
            Assert.Equal(expectedError.ToString(), actiaErrpr);
        }

        [Theory]
        [InlineData(false, "/fish/")]
        [InlineData(false, "/animals/fish/")]
        [InlineData(false, "/fish/?id=anything")]
        [InlineData(false, "/fish/salmon.htm")]
        [InlineData(true, "/fish")]
        [InlineData(true, "/fish.html")]
        [InlineData(true, "/Fish/Salmon.asp")]
        public async Task RestrictByFolder(bool isMatch, string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                disallow: /fish/";

            await robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", path));
        }

        [Theory]
        [InlineData("/*.php", false, "/index.php")]
        [InlineData("/*.php", false, "/filename.php")]
        [InlineData("/*.php", false, "/folder/filename.php")]
        [InlineData("/*.php", false, "/folder/filename.php?parameters")]
        [InlineData("/*.php", false, "/folder/any.php.file.html")]
        [InlineData("/*.php", false, "/filename.php/")]
        [InlineData("/*.php", true, "/")]
        [InlineData("/*.php", true, "/windows.PHP")]
        [InlineData("/fish*.php", false, "/fish.php")]
        [InlineData("/fish*.php", false, "/fish-heads/catfish.php?parameters")]
        [InlineData("/fish*.php", true, "/Fish.PHP")]
        public async Task RestrictByInfix(
            string disallowedPath,
            bool isMatch,
            string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                disallow: {disallowedPath}";

            await robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", path));
        }

        [Theory]
        [InlineData("allow", false)]
        [InlineData("disallow", true)]
        public async Task RestrictByInverse(string disallowedPath, bool isMatch)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                {disallowedPath}: ";

            await robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", "/"));
        }

        [Fact]
        public async Task RestrictByLeastRestrictiveWhenBothMatch()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /$
                disallow: /";

            await robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/"));
        }

        [Fact]
        public async Task RestrictByLeastRestrictiveWhenMatchesAreIdentical()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /folder
                disallow: /folder";

            await robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/folder/page"));
        }

        [Fact]
        public async Task RestrictByLeastRestrictiveWhenMatchesHaveSamePathLength()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /page
                disallow: /*.ph";

            await robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/page.php5"));
        }

        [Fact]
        public async Task RestrictByLongerPathWhenBothMatch()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /page
                disallow: /*.htm";

            await robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed("bot", "/page.htm"));
        }

        [Fact]
        public async Task RestrictByLongerPathWhenMatchesHaveDifferentPathLength()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /p
                disallow: /";

            await robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/page"));
        }

        [Theory]
        [InlineData("/fish", false, "/fish")]
        [InlineData("/fish", false, "/fish.html")]
        [InlineData("/fish", false, "/fish/salmon.html")]
        [InlineData("/fish", false, "/fish-heads")]
        [InlineData("/fish", false, "/fish-heads/yummy.html")]
        [InlineData("/fish", false, "/fish.php?id=anything")]
        [InlineData("/fish", true, "/Fish.asp")]
        [InlineData("/fish", true, "/catfish")]
        [InlineData("/fish", true, "/?id=fish")]
        [InlineData("/fish", true, "/desert/fish")]
        [InlineData("/fish*", false, "/fish")]
        [InlineData("/fish*", false, "/fish.html")]
        [InlineData("/fish*", false, "/fish/salmon.html")]
        [InlineData("/fish*", false, "/fish-heads")]
        [InlineData("/fish*", false, "/fish-heads/yummy.html")]
        [InlineData("/fish*", false, "/fish.php?id=anything")]
        [InlineData("/fish*", true, "/Fish.asp")]
        [InlineData("/fish*", true, "/catfish")]
        [InlineData("/fish*", true, "/?id=fish")]
        [InlineData("/fish*", true, "/desert/fish")]
        public async Task RestrictByPrefix(
            string disallowedPath,
            bool isMatch,
            string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                disallow: {disallowedPath}";

            await robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", path));
        }

        [Theory]
        [InlineData("/", "/")]
        [InlineData("/", "/path")]
        [InlineData("/*", "/")]
        [InlineData("/*", "/path")]
        public async Task RestrictBySlash(string disallowedPath, string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                disallow: {disallowedPath}";

            await robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed("bot", path));
        }

        [Theory]
        [InlineData(false, "/filename.php")]
        [InlineData(false, "/folder/filename.php")]
        [InlineData(true, "/filename.php?parameters")]
        [InlineData(true, "/filename.php/")]
        [InlineData(true, "/filename.php5")]
        [InlineData(true, "/windows.PHP")]
        public async Task RestrictBySuffix(bool isMatch, string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                disallow: /*.php$";

            await robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", path));
        }

        [Fact]
        public async Task RuleGroup()
        {
            const string UserAgent = "bot";

            var robotsTxt1 = new RobotsTxt();
            var robotsTxt2 = new RobotsTxt();
            robotsTxt1.AddDirective(UserAgent, new Directive(false, "/"));
            robotsTxt1.AddDirective(UserAgent, new Directive(true, "/path"));
            robotsTxt1.SetCrawlDelay(UserAgent, 1);

            var data = @$"
                User-agent: {UserAgent}
                Allow: /path
                Crawl-delay: 1
                Disallow: /";
            await robotsTxt2.Load(data);

            var ruleGroup = robotsTxt1.GetRuleGroup(UserAgent);
            var ruleGroup2 = robotsTxt2.GetRuleGroup(UserAgent);
            Assert.Equal(ruleGroup.ToString(), ruleGroup2.ToString());
        }

        [Fact]
        public async Task Sitemaps()
        {
            var robotsTxt = new RobotsTxt();
            const string Host = "http://www.example.com";
            const string Filename1 = "sitemap.xml";
            const string Filename2 = "SITEMAP2.XML"; // Will not be lowercased
            var url1 = $"{Host.ToUpper()}/{Filename1}";
            var url2 = $"{Host}/{Filename2}";
            var expectedSitemaps = new HashSet<string>()
            {
                $"{Host}/{Filename1}",
                url2
            };
            var data = $@"
                sitemap: {url1}
                sitemap: {url2}
                sitemap: {url2} # Not added due to duplication
                sitemap: {Host} # Not added due to absence of filename
                sitemap: {Host}/ # Not added due to short filename
                sitemap: example.com/{Filename1} # Not added due to absence of scheme";

            await robotsTxt.Load(data);

            Assert.Equal(2, robotsTxt.SitemapCount);

            var sitemaps = robotsTxt.Sitemaps;
            while (sitemaps.MoveNext())
            {
                Assert.Contains(sitemaps.Current, expectedSitemaps);
            }
        }

        [Fact]
        public async Task UserAgentDispersed()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent = "a";
            const string Path1 = "/c";
            const string Path2 = "/e";
            var data = @$"
                user-agent: {UserAgent}
                disallow: {Path1}

                user-agent: b
                disallow: /d

                user-agent: {UserAgent}
                disallow: {Path2}";

            await robotsTxt.Load(data);

            var isAllowed = robotsTxt.IsAllowed(UserAgent, Path1);
            Assert.False(isAllowed);

            isAllowed = robotsTxt.IsAllowed(UserAgent, Path2);
            Assert.False(isAllowed);
        }

        [Fact]
        public async Task UserAgentMatchNone()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: bottle
                disallow: /";

            await robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/path"));
        }

        [Fact]
        public async Task UserAgentMatchWildcard()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent = "bot";
            var data = @$"
                user-agent: {UserAgent}*
                disallow: /";

            await robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed($"{UserAgent}y", "/path"));
        }

        [Fact]
        public async Task UserAgentMatcWhole()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent = "bot";
            var data = @$"
                user-agent: {UserAgent.ToUpper()}
                disallow: /";

            await robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed(UserAgent, "/path"));
        }

        [Fact]
        public async Task UserAgentRepeated()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent1 = "c";
            const string UserAgent2 = "d";
            const string Path = "/e";
            var data = @$"
                user-agent: a
                disallow: /b

                user-agent: {UserAgent1}
                user-agent: {UserAgent2}
                disallow: {Path}

                user-agent: f";

            await robotsTxt.Load(data);

            var isAllowed = robotsTxt.IsAllowed(UserAgent1, Path);
            Assert.False(isAllowed);

            isAllowed = robotsTxt.IsAllowed(UserAgent2, Path);
            Assert.False(isAllowed);
        }

        [Fact]
        public async Task UserAgentWithVersion()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent = "bot";
            var data = @$"
                user-agent: {UserAgent.ToUpper()} /1.23
                disallow: /";

            await robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed(UserAgent, "/path"));
        }
    }
}
namespace Toimik.RobotsProtocol.Tests
{
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class RobotsTxtTest
    {
        [Fact]
        public void AllowDefinedAtValidLocation()
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                {new Directive(isAllowed: true, path: string.Empty)}"; // allow:

            var errors = robotsTxt.Load(data);

            Assert.Empty(errors);
        }

        [Fact]
        public void AllowDefinedAtValidLocationButIsIgnored()
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                {new Directive(isAllowed: true, path: string.Empty)}"; // allow:

            robotsTxt.Load(data, isAllowDirectiveIgnored: true);

            // An ignored allow has the same meaning of disallow nothing
            Assert.True(robotsTxt.IsAllowed("bot", "/"));
        }

        [Fact]
        public void AllowDefinedWithoutColonAtValidLocationButIsIgnored()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow";

            var errors = robotsTxt.Load(data, isAllowDirectiveIgnored: true);

            Assert.Empty(errors);
        }

        [Fact]
        public void BlankLines()
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                {Environment.NewLine}
                {Environment.NewLine}";

            var errors = robotsTxt.Load(data);

            Assert.Empty(errors);
        }

        [Fact]
        public void CasingAndInsignificantSpacesDoNotMatter()
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

            var errors = robotsTxt.Load(data);

            Assert.Empty(errors);
        }

        [Fact]
        public void Comments()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                #Comment
                # Comment ";

            var errors = robotsTxt.Load(data);

            Assert.Empty(errors);
        }

        [Fact]
        public void CrawlDelay()
        {
            var robotsTxt = new RobotsTxt();
            const int ExpectedCrawlDelay = 1;
            var data = @$"
                user-agent: *
                crawl-delay: {ExpectedCrawlDelay} # This takes effect because it is the latest entry with a value that can be parsed into a number
                crawl-delay: b";

            robotsTxt.Load(data);

            Assert.Equal(ExpectedCrawlDelay, robotsTxt.GetCrawlDelay("bot"));
        }

        [Fact]
        public void CrawlDelayDefinedAtInvalidLocation()
        {
            var robotsTxt = new RobotsTxt();
            var data = "crawl-delay: 1";

            var errors = robotsTxt.Load(data);

            Assert.Single(errors);

            var expectedError = new Error<TxtErrorCode>(new Line(1, data.TrimEnd()), TxtErrorCode.RuleFoundBeforeUserAgent);
            var error = Utils.GetOnlyItem(errors);
            var actualError = error.ToString();
            Assert.Equal(expectedError.ToString(), actualError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DirectiveDefinedAtInvalidLocation(bool flag)
        {
            var robotsTxt = new RobotsTxt();
            var data = new Directive(isAllowed: flag, path: string.Empty).ToString();

            var errors = robotsTxt.Load(data);

            Assert.Single(errors);

            var expectedError = new Error<TxtErrorCode>(new Line(1, data.TrimEnd()), TxtErrorCode.RuleFoundBeforeUserAgent);
            var error = Utils.GetOnlyItem(errors);
            var actualError = error.ToString();
            Assert.Equal(expectedError.ToString(), actualError);
        }

        [Fact]
        public void HostCaptured()
        {
            var robotsTxt = new RobotsTxt();
            const string Field = "Host";
            const string ExpectedValue = "example.com";
            var data = $"{Field}: {ExpectedValue}";
            var customFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Field,
            };

            robotsTxt.Load(data, customFields: customFields);

            var values = robotsTxt.GetCustom(Field);
            var actualValue = Utils.GetOnlyItem(values);
            Assert.Equal(ExpectedValue, actualValue);
        }

        [Fact]
        public void HostCapturedOnce()
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

            robotsTxt.Load(data, customFields: customFields);

            var actualCount = robotsTxt.GetCustomCount(Field);
            Assert.Equal(1, actualCount);
        }

        [Fact]
        public void HostUncaptured()
        {
            var robotsTxt = new RobotsTxt();
            const string Field = "Host";
            var data = @$"
                Sitemap: http://www.example.com/sitemap.xml.gz
                {Field}: example.com";

            robotsTxt.Load(data);

            Assert.Equal(0, robotsTxt.GetCustomCount(Field));
            Assert.False(robotsTxt.GetCustom(Field).MoveNext());
        }

        [Fact]
        public void InitialState()
        {
            var robotsTxt = new RobotsTxt();

            robotsTxt.Load("");

            Assert.Null(robotsTxt.GetCrawlDelay("not found"));
            Assert.False(robotsTxt.GetCustom("not found").MoveNext());
            Assert.Equal(0, robotsTxt.GetCustomCount("not found"));
            Assert.Null(robotsTxt.GetRuleGroup("not found"));
            Assert.Null(robotsTxt.GetSpecificUserAgent("not found"));

            Assert.True(robotsTxt.IsAllowed("not found", "not found"));
        }

        [Fact]
        public void LoadStartsAfresh()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent1 = "bot";
            const string CustomField = "host";
            var data = @$"
                user-agent: {UserAgent1}
                allow: /
                crawl-delay: 2

                sitemap: http://www.example.com/sitemap.xml.gz

                {CustomField}: example.com";
            var customFields = new HashSet<string>
            {
                CustomField,
            };
            robotsTxt.Load(data, customFields: customFields);
            const string UserAgent2 = "otherbot";
            data = @$"
                user-agent: {UserAgent2}
                disallow: /
                crawl-delay: 1

                sitemap: http://www.example.com/sitemap.xml

                {CustomField}: www.example.com";
            robotsTxt.Load(data, customFields: customFields);

            Assert.Equal(1, robotsTxt.SitemapCount);
            Assert.Equal(1, robotsTxt.GetCustomCount(CustomField));

            Assert.Null(robotsTxt.GetCrawlDelay(UserAgent1));
            Assert.Null(robotsTxt.GetRuleGroup(UserAgent1));

            Assert.Equal(1, robotsTxt.GetCrawlDelay(UserAgent2));
            var ruleGroup = robotsTxt.GetRuleGroup(UserAgent2);
            var directives = ruleGroup.Directives;
            Assert.True(directives.MoveNext());
            Assert.False(directives.MoveNext());
        }

        [Fact]
        public void MatchByImplicitAllow()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: bot
                disallow: /";

            robotsTxt.Load(data);

            var matchResult = robotsTxt.Match("bottle", "/file");
            Assert.Null(matchResult.UserAgent);
        }

        [Fact]
        public void MatchByMostSpecific()
        {
            var robotsTxt = new RobotsTxt();
            const string ExpectedPath = "/folder/p";
            var data = @$"
                user-agent: *
                allow: /folder
                allow: {ExpectedPath}";

            robotsTxt.Load(data);

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
        public void MissingValue(string field)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                {Environment.NewLine}
                user-agent: * # This enables testing for 'allow' and 'disallow' fields
                {field}";

            var errors = robotsTxt.Load(data);

            Assert.Single(errors);

            var expectedError = new Error<TxtErrorCode>(new Line(2, field.Trim()), TxtErrorCode.MissingValue);
            var error = Utils.GetOnlyItem(errors);
            var actualError = error.ToString();
            Assert.Equal(expectedError.ToString(), actualError);
        }

        [Fact]
        public void Misspelling()
        {
            var robotsTxt = new RobotsTxt();
            const string Path = "/path";
            const string MisspelledDirective = "Dissalow";
            var data = @$"
                User-agent: *
                {MisspelledDirective}: {Path}";

            var misspelledFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { MisspelledDirective, "Disallow" },
            };
            robotsTxt.Load(data, misspelledFields: misspelledFields);

            Assert.False(robotsTxt.IsAllowed("bot", Path));
        }

        [Theory]
        [InlineData("allow")]
        [InlineData("disallow")]
        public void PathWithoutSlashPrefix(string directive)
        {
            var robotsTxt = new RobotsTxt();
            var problematicText = $"{directive}: path";
            var data = @$"
                user-agent: *
                {problematicText}";

            var errors = robotsTxt.Load(data);

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
        public void RestrictByFolder(bool isMatch, string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                disallow: /fish/";

            robotsTxt.Load(data);

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
        public void RestrictByInfix(
            string disallowedPath,
            bool isMatch,
            string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                disallow: {disallowedPath}";

            robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", path));
        }

        [Theory]
        [InlineData("allow", false)]
        [InlineData("disallow", true)]
        public void RestrictByInverse(string disallowedPath, bool isMatch)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                {disallowedPath}: ";

            robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", "/"));
        }

        [Fact]
        public void RestrictByLeastRestrictiveWhenBothMatch()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /$
                disallow: /";

            robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/"));
        }

        [Fact]
        public void RestrictByLeastRestrictiveWhenMatchesAreIdentical()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /folder
                disallow: /folder";

            robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/folder/page"));
        }

        [Fact]
        public void RestrictByLeastRestrictiveWhenMatchesHaveSamePathLength()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /page
                disallow: /*.ph";

            robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/page.php5"));
        }

        [Fact]
        public void RestrictByLongerPathWhenBothMatch()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /page
                disallow: /*.htm";

            robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed("bot", "/page.htm"));
        }

        [Fact]
        public void RestrictByLongerPathWhenMatchesHaveDifferentPathLength()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                allow: /p
                disallow: /";

            robotsTxt.Load(data);

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
        public void RestrictByPrefix(
            string disallowedPath,
            bool isMatch,
            string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                disallow: {disallowedPath}";

            robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", path));
        }

        [Theory]
        [InlineData("/", "/")]
        [InlineData("/", "/path")]
        [InlineData("/*", "/")]
        [InlineData("/*", "/path")]
        public void RestrictBySlash(string disallowedPath, string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @$"
                user-agent: *
                disallow: {disallowedPath}";

            robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed("bot", path));
        }

        [Theory]
        [InlineData(false, "/filename.php")]
        [InlineData(false, "/folder/filename.php")]
        [InlineData(true, "/filename.php?parameters")]
        [InlineData(true, "/filename.php/")]
        [InlineData(true, "/filename.php5")]
        [InlineData(true, "/windows.PHP")]
        public void RestrictBySuffix(bool isMatch, string path)
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: *
                disallow: /*.php$";

            robotsTxt.Load(data);

            Assert.Equal(isMatch, robotsTxt.IsAllowed("bot", path));
        }

        [Fact]
        public void RuleGroup()
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
            robotsTxt2.Load(data);

            var ruleGroup = robotsTxt1.GetRuleGroup(UserAgent);
            var ruleGroup2 = robotsTxt2.GetRuleGroup(UserAgent);
            Assert.Equal(ruleGroup.ToString(), ruleGroup2.ToString());
        }

        [Fact]
        public void Sitemaps()
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
                url2,
            };
            var data = $@"
                sitemap: {url1}
                sitemap: {url2}
                sitemap: {url2} # Not added due to duplication
                sitemap: {Host} # Not added due to absence of filename
                sitemap: {Host}/ # Not added due to short filename
                sitemap: example.com/{Filename1} # Not added due to absence of scheme";

            robotsTxt.Load(data);

            Assert.Equal(2, robotsTxt.SitemapCount);

            var sitemaps = robotsTxt.Sitemaps;
            while (sitemaps.MoveNext())
            {
                Assert.Contains(sitemaps.Current, expectedSitemaps);
            }
        }

        [Fact]
        public void UserAgentDispersed()
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

            robotsTxt.Load(data);

            var isAllowed = robotsTxt.IsAllowed(UserAgent, Path1);
            Assert.False(isAllowed);

            isAllowed = robotsTxt.IsAllowed(UserAgent, Path2);
            Assert.False(isAllowed);
        }

        [Fact]
        public void UserAgentMatchNone()
        {
            var robotsTxt = new RobotsTxt();
            var data = @"
                user-agent: bottle
                disallow: /";

            robotsTxt.Load(data);

            Assert.True(robotsTxt.IsAllowed("bot", "/path"));
        }

        [Fact]
        public void UserAgentMatchWildcard()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent = "bot";
            var data = @$"
                user-agent: {UserAgent}*
                disallow: /";

            robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed($"{UserAgent}y", "/path"));
        }

        [Fact]
        public void UserAgentMatcWhole()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent = "bot";
            var data = @$"
                user-agent: {UserAgent.ToUpper()}
                disallow: /";

            robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed(UserAgent, "/path"));
        }

        [Fact]
        public void UserAgentRepeated()
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

            robotsTxt.Load(data);

            var isAllowed = robotsTxt.IsAllowed(UserAgent1, Path);
            Assert.False(isAllowed);

            isAllowed = robotsTxt.IsAllowed(UserAgent2, Path);
            Assert.False(isAllowed);
        }

        [Fact]
        public void UserAgentWithVersion()
        {
            var robotsTxt = new RobotsTxt();
            const string UserAgent = "bot";
            var data = @$"
                user-agent: {UserAgent.ToUpper()} /1.23
                disallow: /";

            robotsTxt.Load(data);

            Assert.False(robotsTxt.IsAllowed(UserAgent, "/path"));
        }
    }
}
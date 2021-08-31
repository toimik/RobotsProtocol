namespace Toimik.RobotsProtocol.Tests
{
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class RobotsTagTest
    {
        [Theory]
        [InlineData("", 1)]
        [InlineData(" ", 1)]
        [InlineData(" ,  ,", 3)]
        public void Empty(string datum, int errorCount)
        {
            var robotsTag = new RobotsTag();
            var data = new List<string>
            {
                datum,
            };
            var errors = robotsTag.Load(data);

            Assert.Equal(errorCount, errors.Count);
            var error = Utils.GetOnlyItem(errors.GetEnumerator());
            var line = error.Line;
            Assert.Equal(1, line.Number);
            Assert.Equal(datum.Trim(), line.Text);
            Assert.Equal(TagErrorCode.MissingValue, error.Code);
        }

        [Fact]
        public void ErrorAtOtherLineNumber()
        {
            const string Datum = " MAX-SNIPPET: ";
            var robotsTag = new RobotsTag();
            var data = new List<string>
            {
                "all",
                Datum,
            };
            var errors = robotsTag.Load(data);
            var error = Utils.GetOnlyItem(errors.GetEnumerator());
            var line = error.Line;
            Assert.Equal(2, line.Number);
            Assert.Equal(Datum.Trim(), line.Text);
            Assert.Equal(TagErrorCode.MissingValue, error.Code);
        }

        [Theory]

        // With implicit default user agent
        [InlineData("all", "robots", "all", 1)]
        [InlineData("all", "robots", "not found", 0)]
        [InlineData("all", "bot", "not found", 0)]
        [InlineData("max-snippet: 100", "robots", "max-snippet", 1)]
        [InlineData("all, max-snippet: 100, max-image-preview: none", "robots", "max-snippet", 1)]
        [InlineData("all, max-snippet: 100, max-image-preview: none", "robots", null, 3)]

        // With explicit default user agent
        [InlineData("robots: all", "robots", "all", 1)]
        [InlineData("robots: all", "robots", "not found", 0)]
        [InlineData("robots: all", "bot", "not found", 0)]
        [InlineData("robots: max-snippet: 100", "robots", "max-snippet", 1)]
        [InlineData("robots: all, max-snippet: 100, max-image-preview: none", "robots", "max-snippet", 1)]
        [InlineData("robots: all, max-snippet: 100, max-image-preview: none", "robots", null, 3)]

        // With specific user agent
        [InlineData("bot: all", "robots", "all", 0)]
        [InlineData("bot: all", "robots", "not found", 0)]
        [InlineData("bot: all", "bot", "not found", 0)]
        [InlineData("bot: all, max-snippet: 100, max-image-preview: none", "bot", "max-snippet", 1)]
        [InlineData("bot: all, max-snippet: 100, max-image-preview: none", "bot", null, 3)]
        public void GetTagCount(
             string datum,
             string userAgent,
             string directive,
             int tagCount)
        {
            var robotsTag = new RobotsTag();
            var data = new List<string>
            {
                datum.ToUpper(),
            };
            var specialWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "max-snippet",
                "max-image-preview",
            };
            robotsTag.Load(data, specialWords);

            Assert.Equal(tagCount, robotsTag.GetTagCount(userAgent, directive));
        }

        [Theory]

        // With implicit default user agent
        [InlineData("all", "robots", "all", true)]
        [InlineData("all", "robots", "not found", false)]
        [InlineData("all", "bot", "not found", false)]
        [InlineData("max-snippet: 100", "robots", "max-snippet", true)]
        [InlineData("all, max-snippet: 100, max-image-preview: none", "robots", "max-snippet", true)]
        [InlineData("all, max-snippet: 100, max-image-preview: none", "robots", null, true)]

        // With explicit default user agent
        [InlineData("robots: all", "robots", "all", true)]
        [InlineData("robots: all", "robots", "not found", false)]
        [InlineData("robots: all", "bot", "not found", false)]
        [InlineData("robots: max-snippet: 100", "robots", "max-snippet", true)]
        [InlineData("robots: all, max-snippet: 100, max-image-preview: none", "robots", "max-snippet", true)]
        [InlineData("robots: all, max-snippet: 100, max-image-preview: none", "robots", null, true)]

        // With specific user agent
        [InlineData("bot: all", "robots", "all", false)]
        [InlineData("bot: all", "robots", "not found", false)]
        [InlineData("bot: all", "bot", "not found", false)]
        [InlineData("bot: all, max-snippet: 100, max-image-preview: none", "bot", "max-snippet", true)]
        [InlineData("bot: all, max-snippet: 100, max-image-preview: none", "bot", null, true)]
        public void HasTag(
            string datum,
            string userAgent,
            string directive,
            bool flag)
        {
            var robotsTag = new RobotsTag();
            var data = new List<string>
            {
                datum.ToUpper(),
            };
            var specialWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "max-snippet",
                "max-image-preview",
            };
            robotsTag.Load(data, specialWords);

            Assert.Equal(flag, robotsTag.HasTag(userAgent, directive));
        }

        [Fact]
        public void LoadStartsAfresh()
        {
            var robotsTag = new RobotsTag();
            const string UserAgent1 = "bot";
            var data = new List<string>
            {
                $"{UserAgent1}: all",
            };
            robotsTag.Load(data);
            const string UserAgent2 = "otherbot";
            data = new List<string>
            {
                $"{UserAgent2}: none",
            };
            robotsTag.Load(data);

            Assert.Equal(0, robotsTag.GetTagCount(UserAgent1));
            Assert.Equal(1, robotsTag.GetTagCount(UserAgent2));
        }

        [Fact]
        public void SpacesDoNotMatter()
        {
            var robotsTag = new RobotsTag();
            var data = new List<string>
            {
                $"  robots  :  max-snippet  :  100  ".ToUpper(),
            };
            robotsTag.Load(data);

            var tags = robotsTag.GetTags(RobotsTag.UserAgentForCatchAll);
            tags.MoveNext();
            var tag = tags.Current;
            Assert.Equal($"{RobotsTag.UserAgentForCatchAll}: max-snippet: 100", tag.ToString());
        }

        [Fact]
        public void SpecialWordUncaptured()
        {
            const string UserAgent = "max-snippet";
            var robotsTag = new RobotsTag();
            var data = new List<string>
            {
                $"{UserAgent}: 100, max-image-preview: none".ToUpper(),
            };
            robotsTag.Load(data);

            Assert.False(robotsTag.HasTag(RobotsTag.UserAgentForCatchAll));
            Assert.True(robotsTag.HasTag(UserAgent));
            Assert.Equal(2, robotsTag.GetTagCount(UserAgent));
            var tags = robotsTag.GetTags(UserAgent);
            tags.MoveNext();
            var tag = tags.Current;
            Assert.Equal("100", tag.Directive);
            Assert.Null(tag.Value);
        }

        [Fact]
        public void TagsReturnedIgnoringDuplicates()
        {
            const string First = "all";
            const string Second = "max-snippet: 100";
            const string Third = "max-image-preview: none";
            var robotsTag = new RobotsTag();
            var data = new List<string>
            {
                $"{First},{Second},{Second},{First},{Third}".ToUpper(),
            };
            robotsTag.Load(data);

            Assert.Equal(3, robotsTag.GetTagCount(RobotsTag.UserAgentForCatchAll));
        }

        [Fact]
        public void UnavailableAfter()
        {
            var robotsTag = new RobotsTag();
            const string Directive = "unavailable_after";
            var date = "4 Jul 2000 16:30:00 GMT";
            var data = new List<string>
            {
                $"{Directive}: {date}",
            };
            var specialWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "unavailable_after",
            };
            robotsTag.Load(data, specialWords);

            var tags = robotsTag.GetTags(RobotsTag.UserAgentForCatchAll, Directive);
            tags.MoveNext();
            var tag = tags.Current;
            Assert.Equal(DateTime.Parse(date), DateTime.Parse(tag.Value));
        }
    }
}
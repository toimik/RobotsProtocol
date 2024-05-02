/*
 * Copyright 2021-2022 nurhafiz@hotmail.sg
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Toimik.RobotsProtocol;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Represents a robots.txt.
/// </summary>
/// <remarks>
/// By default, values of the following case-insensitive fields are parsed by <see
/// cref="Load(string, bool, ISet{string}, IDictionary{string, string})"/> and <see
/// cref="Load(Stream, bool, ISet{string}, IDictionary{string, string})"/>:
/// <list type="bullet">
/// <item>
/// <description>User-agent</description>
/// </item>
/// <item>
/// <description>Allow</description>
/// </item>
/// <item>
/// <description>Disallow</description>
/// </item>
/// <item>
/// <description>Sitemap</description>
/// </item>
/// <item>
/// <description>Crawl-delay</description>
/// </item>
/// </list>
/// <para>
/// Value(s) of custom field(s) (e.g: <c>Host</c>) can be extracted by specifying their name to the
/// corresponding parameter.
/// </para>
/// <para>
/// <see cref="IsAllowed(string, string)"/> and <see cref="Match(string, string)"/> interpret this
/// instance according to Google's Robots Exclusion Protocol Specification
/// - which Google has submitted to be recognized as an official standard - but with additional
/// support for <c>Crawl-delay</c> directive.
/// </para>
/// </remarks>
/// <seealso cref="https://developers.google.com/search/blog/2019/07/rep-id"/>
/// <seealso cref="http://www.robotstxt.org/"/>
public sealed class RobotsTxt
{
    private const double DefaultMatchTimeoutInSeconds = 5;

    private const string UserAgentForCatchAll = "*";

    private readonly Dictionary<string, ISet<string>> customFieldToValues = new(StringComparer.OrdinalIgnoreCase);

    private readonly ISet<string> sitemaps = new HashSet<string>();

    private readonly Dictionary<string, RuleGroup> userAgentToRuleGroup = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="RobotsTxt"/> class.
    /// </summary>
    public RobotsTxt()
    {
    }

    public int SitemapCount => sitemaps.Count;

    public IEnumerator<string> Sitemaps => sitemaps.GetEnumerator();

    [ExcludeFromCodeCoverage]
    public static bool IsMatch(
       string pattern,
       string pathWithOptionalQuery,
       double matchTimeout = DefaultMatchTimeoutInSeconds)
    {
        bool isMatch;
        try
        {
            isMatch = Regex.IsMatch(
                pathWithOptionalQuery,
                pattern,
                RegexOptions.None,
                TimeSpan.FromSeconds(matchTimeout));
        }
        catch (RegexMatchTimeoutException)
        {
            isMatch = false;
        }

        return isMatch;
    }

    public void AddCustom(string field, string value)
    {
        customFieldToValues.TryGetValue(field, out ISet<string>? values);
        if (values == null)
        {
            values = new HashSet<string>();
            customFieldToValues.Add(field, values);
        }

        values.Add(value);
    }

    public void AddDirective(string userAgent, Directive? directive)
    {
        userAgentToRuleGroup.TryGetValue(userAgent, out RuleGroup? ruleGroup);
        if (ruleGroup == null)
        {
            ruleGroup = new(userAgent);
            userAgentToRuleGroup.Add(userAgent, ruleGroup);
        }

        ruleGroup.AddDirective(directive);
    }

    /// <summary>
    /// Adds a sitemap to this instance.
    /// </summary>
    /// <param name="sitemap">
    /// An absolute URL to a sitemap. The URL must include a filename because without it, the URL is
    /// pointing to the home page of a web site. Invalid values are ignored.
    /// </param>
    public void AddSitemap(string sitemap)
    {
        var normalizedSitemap = NormalizeSitemap(sitemap);
        if (normalizedSitemap != null)
        {
            sitemaps.Add(normalizedSitemap);
        }
    }

    public int? GetCrawlDelay(string userAgent)
    {
        var agent = GetSpecificUserAgent(userAgent);
        return agent == null
            ? null
            : userAgentToRuleGroup[agent].CrawlDelay;
    }

    public IEnumerator<string> GetCustom(string field)
    {
        customFieldToValues.TryGetValue(field, out ISet<string>? values);
        values ??= new HashSet<string>(0);
        return values.GetEnumerator();
    }

    public int GetCustomCount(string field)
    {
        customFieldToValues.TryGetValue(field, out ISet<string>? values);
        var count = values == null
            ? 0
            : values.Count;
        return count;
    }

    public RuleGroup? GetRuleGroup(string userAgent)
    {
        var agent = GetSpecificUserAgent(userAgent);
        if (agent == null)
        {
            return null;
        }

        var ruleGroup = userAgentToRuleGroup[agent];
        return ruleGroup;
    }

    /// <summary>
    /// Gets, for this instance, the most specific <c>User-agent</c> that matches the specified user agent.
    /// </summary>
    /// <param name="userAgent">A user agent to match against.</param>
    /// <returns>
    /// <c>null</c> (if no match) or a case-insensitive <c>User-agent</c> that is:
    /// <list type="bullet">
    /// <item>
    /// <description>exactly named as <paramref name="agent"/></description>
    /// </item>
    /// <item>
    /// <description>
    /// starts with the same characters as <paramref name="agent"/> and suffixed with a '*'
    /// </description>
    /// </item>
    /// <item>
    /// <description>the catch-all (*)</description>
    /// </item>
    /// </list>
    /// </returns>
    public string? GetSpecificUserAgent(string userAgent)
    {
        var agent = userAgent;
        var hasUserAgent = userAgentToRuleGroup.ContainsKey(agent);

        // If an exact case-insensitive match is not found, try to find one that uses a wild card
        // (e.g. bot*)
        if (!hasUserAgent)
        {
            // For every failed attempt, remove the last character of the name and suffix the name
            // with an asterisk. Repeat until the name becomes '*', which is the catch-all token.
            string tempName = agent;
            while (!userAgentToRuleGroup.ContainsKey(agent))
            {
                if (agent.Equals(UserAgentForCatchAll))
                {
                    agent = null;
                    break;
                }

                tempName = tempName.Remove(tempName.Length - 1);
                agent = $"{tempName}*";
            }
        }

        return agent;
    }

    public bool IsAllowed(
        string userAgent,
        string pathWithOptionalQuery,
        double matchTimeoutInSeconds = DefaultMatchTimeoutInSeconds)
    {
        var matchResult = Match(
            userAgent,
            pathWithOptionalQuery,
            matchTimeoutInSeconds);
        return matchResult.Directive.IsAllowed;
    }

    /// <summary>
    /// Loads, for this instance, the data of a robots.txt from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">
    /// A stream containing the data of a robots.txt. This is left opened after processing.
    /// </param>
    /// <param name="isAllowDirectiveIgnored">
    /// Optional indication of whether the <c>Allow</c> directive is ignored. (This option is made
    /// available because the directive is not a standard.) The default is <c>false</c>.
    /// </param>
    /// <param name="customFields">
    /// Optional set of <b>case-insensitive</b> fields whose value(s) must be extracted. The values
    /// are left as-is but with leading and trailing spaces removed.
    /// </param>
    /// <param name="misspelledFields">
    /// Optional mapping of <b>case-insensitive</b> misspelled fields to their corresponding field.
    /// This is useful if leniency is required. e.g. A misspelled <c>dissalow</c> is mapped to <c>disallow</c>.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> containing every <see cref="Error"/>, if any, found when parsing the
    /// data. This is never <c>null</c>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when <paramref name="stream"/> is manually closed.
    /// </exception>
    /// <remarks>
    /// All existing entries, if any, are cleared when this method is called.
    /// <para>Call <see cref="Stream.Close()"/> on <paramref name="stream"/> to cancel loading.</para>
    /// </remarks>
    public async Task<IEnumerable<Error<TxtErrorCode>>> Load(
        Stream stream,
        bool isAllowDirectiveIgnored = false,
        ISet<string>? customFields = null,
        IDictionary<string, string>? misspelledFields = null)
    {
        customFieldToValues.Clear();
        sitemaps.Clear();
        userAgentToRuleGroup.Clear();

        using var reader = new StreamReader(stream, leaveOpen: true);

        // This is done outside of the loop below so that the line numbering starts from the first
        // non-empty line
        var text = await ReadUntilNonEmptyLine(reader).ConfigureAwait(false);
        if (text == null)
        {
            return new LinkedList<Error<TxtErrorCode>>();
        }

        var errors = new LinkedList<Error<TxtErrorCode>>();
        var userAgents = new HashSet<string>();
        customFields ??= new HashSet<string>(0);
        misspelledFields ??= new Dictionary<string, string>(0);
        var hasEncounteredDirective = false;
        var lineNumber = 0;
        var skippedLineCount = 0;
        do
        {
            lineNumber++;
            text = text.Trim();

            // '#' denotes the start of a comment
            var hashIndex = text.IndexOf('#');

            var entry = hashIndex != -1
                ? text[..hashIndex].TrimEnd()
                : text;
            if (entry == string.Empty)
            {
                skippedLineCount++;
                continue;
            }

            var line = new Line(lineNumber, text);

            // ':' delimits a field and its value. A pair must exist in a non-empty line. Since all
            // non-comment lines are in the form of 'field: value', a missing colon implies that a
            // value is unintentionally left out.
            var colonIndex = entry.IndexOf(':');
            if (colonIndex == -1)
            {
                // Although an empty value for a directive is allowed, it does not make sense to
                // record an error if the allow directive is ignored. Hence, the line is skipped.
                var isAllowDirective = entry.Equals("allow", StringComparison.OrdinalIgnoreCase);
                if (isAllowDirective
                    && isAllowDirectiveIgnored)
                {
                    skippedLineCount++;
                }
                else
                {
                    errors.AddLast(new Error<TxtErrorCode>(line, TxtErrorCode.MissingValue));
                }
            }
            else
            {
                var field = entry[..colonIndex].Trim();
                var hasMispelledField = misspelledFields.TryGetValue(field, out string? misspelledField);
                if (misspelledField != null)
                {
                    field = misspelledField;
                }

                field = field.ToLower();

                var value = entry[(colonIndex + 1)..].Trim();
                switch (field)
                {
                    case "allow":
                    case "disallow":

                        // Checking the first character suffices
                        var isAllowDirective = field[0].Equals('a');

                        if (isAllowDirective
                            && isAllowDirectiveIgnored)
                        {
                            skippedLineCount++;
                            break;
                        }

                        if (userAgents.Count == 0)
                        {
                            errors.AddLast(new Error<TxtErrorCode>(line, TxtErrorCode.RuleFoundBeforeUserAgent));
                        }
                        else
                        {
                            if (value != string.Empty
                                && value[0] != '/')
                            {
                                errors.AddLast(new Error<TxtErrorCode>(line, TxtErrorCode.InvalidPathFormat));
                            }
                            else
                            {
                                foreach (string userAgent in userAgents)
                                {
                                    AddDirective(userAgent, new Directive(isAllowDirective, value.ToLower()));
                                }
                            }

                            hasEncounteredDirective = true;
                        }

                        break;

                    case "crawl-delay":
                        if (userAgents.Count == 0)
                        {
                            errors.AddLast(new Error<TxtErrorCode>(line, TxtErrorCode.RuleFoundBeforeUserAgent));
                        }
                        else
                        {
                            if (value == string.Empty)
                            {
                                errors.AddLast(new Error<TxtErrorCode>(line, TxtErrorCode.MissingValue));
                            }
                            else
                            {
                                // Since there may be more than one, the last valid value takes effect
                                try
                                {
                                    var number = int.Parse(value);
                                    foreach (string userAgent in userAgents)
                                    {
                                        SetCrawlDelay(userAgent, number);
                                    }

                                    hasEncounteredDirective = true;
                                }
                                catch
                                {
                                    // If it is not a valid number, ignore it
                                }
                            }
                        }

                        break;

                    case "sitemap":
                        if (value == string.Empty)
                        {
                            errors.AddLast(new Error<TxtErrorCode>(line, TxtErrorCode.MissingValue));
                        }
                        else
                        {
                            AddSitemap(value);
                        }

                        break;

                    case "user-agent":
                        if (value == string.Empty)
                        {
                            errors.AddLast(new Error<TxtErrorCode>(line, TxtErrorCode.MissingValue));
                        }
                        else
                        {
                            value = NormalizeUserAgent(value);

                            // User-agents can appear one after another. If any directive is
                            // encountered after a user-agent, the names are cleared.
                            if (hasEncounteredDirective)
                            {
                                userAgents.Clear();
                            }

                            userAgents.Add(value);
                        }

                        hasEncounteredDirective = false;
                        break;

                    default:
                        var hasCustomField = customFields.Contains(field);
                        if (!hasCustomField)
                        {
                            skippedLineCount++;
                        }
                        else
                        {
                            AddCustom(field, value.ToLower());
                        }

                        break;
                }
            }
        }
        while ((text = await reader.ReadLineAsync().ConfigureAwait(false)) != null);

        // If data is made up of comment(s) only, treat the robots.txt as if it was empty
        if (skippedLineCount == lineNumber)
        {
            return new LinkedList<Error<TxtErrorCode>>();
        }

        // User-agent(s) may be found at the end of a robots.txt without any corresponding directive
        // defined for them. That is equivalent to not defining those user-agent(s) in the first
        // place. However, they are still added - but with an empty directive - for the sake of
        // keeping the original data intact.
        if (!hasEncounteredDirective)
        {
            foreach (string userAgent in userAgents)
            {
                AddDirective(userAgent, directive: null);
            }
        }

        return errors;
    }

    /// <summary>
    /// Loads, for this instance, the data of a robots.txt from a <see cref="string"/>.
    /// </summary>
    /// <param name="data">Data of a robots.txt.</param>
    /// <param name="isAllowDirectiveIgnored">
    /// Optional indication of whether the <c>Allow</c> directive is ignored. (This option is made
    /// available because the directive is not a standard.) The default is <c>false</c>.
    /// </param>
    /// <param name="customFields">
    /// Optional set of <b>case-insensitive</b> fields whose value(s) must be extracted. The values
    /// are left as-is but with leading and trailing spaces removed.
    /// </param>
    /// <param name="misspelledFields">
    /// Optional mapping of <b>case-insensitive</b> misspelled fields to their corresponding field.
    /// This is useful if leniency is required. e.g. A misspelled <c>dissalow</c> is mapped to <c>disallow</c>.
    /// </param>
    /// <returns>
    /// Every <see cref="Error"/>, if any, found when parsing the data. This is never <c>null</c>.
    /// </returns>
    /// <remarks>All existing entries, if any, are cleared when this method is called.</remarks>
    public IEnumerable<Error<TxtErrorCode>> Load(
        string data,
        bool isAllowDirectiveIgnored = false,
        ISet<string>? customFields = null,
        IDictionary<string, string>? misspelledFields = null)
    {
        var byteArray = Encoding.UTF8.GetBytes(data);
        using var stream = new MemoryStream(byteArray);
        var errors = Load(
            stream,
            isAllowDirectiveIgnored,
            customFields,
            misspelledFields).Result;
        return errors;
    }

    public MatchResult Match(
        string userAgent,
        string pathWithOptionalQuery,
        double matchTimeoutInSeconds = DefaultMatchTimeoutInSeconds)
    {
        var agent = GetSpecificUserAgent(userAgent);
        if (agent == null)
        {
            return new MatchResult(new Directive(isAllowed: true, path: "/"), null);
        }

        var ruleGroup = userAgentToRuleGroup[agent];
        var matchResult = ruleGroup.Match(pathWithOptionalQuery, matchTimeoutInSeconds);
        return matchResult;
    }

    public void SetCrawlDelay(string userAgent, int crawlDelay)
    {
        userAgentToRuleGroup.TryGetValue(userAgent, out RuleGroup? ruleGroup);
        if (ruleGroup == null)
        {
            ruleGroup = new(userAgent);
            userAgentToRuleGroup.Add(userAgent, ruleGroup);
        }

        ruleGroup.CrawlDelay = crawlDelay;
    }

    private static string? NormalizeSitemap(string sitemap)
    {
        const string ColonSlashSlash = "://";
        var index = sitemap.IndexOf(ColonSlashSlash);
        if (index == -1)
        {
            return null;
        }

        index = sitemap.IndexOf('/', index + ColonSlashSlash.Length);
        if (index == -1)
        {
            // Sitemap must contain a filename
            return null;
        }

        var path = sitemap[(index + 1)..];
        if (path == string.Empty)
        {
            // Sitemap's filename can be of any length. It can even point to the root itself in case
            // there are websites that organize their contents into different directories without
            // putting any content at the homepage.
            return null;
        }

        // URL's path and query are case-sensitive. Other parts are lowercased.
        var host = sitemap[..index].ToLower();
        sitemap = $"{host}/{path}";
        return sitemap;
    }

    private static string NormalizeUserAgent(string userAgent)
    {
        // If there is a version number, ignore it
        var index = userAgent.IndexOf('/');
        userAgent = index == -1
            ? userAgent
            : userAgent[..index].TrimEnd();
        return userAgent.ToLower();
    }

    private static async Task<string> ReadUntilNonEmptyLine(StreamReader reader)
    {
        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            line = line.Trim();
            if (line != string.Empty)
            {
                break;
            }
        }

        return line!;
    }
}
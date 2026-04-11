/*
 * Copyright 2021-2026 nurhafiz@hotmail.sg
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Represents the group of rules for a user-agent.
/// </summary>
public sealed class RuleGroup(string userAgent)
{
    private readonly ISet<Directive?> directives = new SortedSet<Directive?>(new DirectiveComparer());

    public int? CrawlDelay { get; set; }

    public IEnumerator<Directive?> Directives => directives.GetEnumerator();

    public string UserAgent { get; } = userAgent;

    public void AddDirective(Directive? directive)
    {
        directives.Add(directive);
    }

    public MatchResult Match(string pathWithOptionalQuery, double matchTimeout)
    {
        var effectedAllows = new ConcurrentBag<string>();
        var effectedDisallows = new ConcurrentBag<string>();
        foreach (Directive? directive in directives)
        {
            bool isAllowed;
            string path;

            // A directive is null when it is automatically added for User-agent(s) that is/are
            // defined - usually at the end - without any corresponding directive
            if (directive == null)
            {
                isAllowed = true;
                path = "/";
            }
            else
            {
                isAllowed = directive.IsAllowed;
                path = directive.Path;

                // If the path is empty, invert the directive such that 'Disallow: ' becomes allow
                // everything and vice versa
                if (path == string.Empty)
                {
                    isAllowed = !isAllowed;
                    path = "/";
                }
            }

                // If the original path ends with a '$', it denotes suffix matching. Detect and
                // remove it before escaping so we avoid introducing duplicate '$' characters in
                // the resulting regex.
                var isSuffixMatch = path.EndsWith('$');
                if (isSuffixMatch)
                {
                    path = path[..^1];
                }

                // Determine whether the original path ends with a slash. For folder-style
                // directives (ending with '/'), perform a prefix match against the request
                // path rather than searching for the escaped pattern anywhere within it. This
                // avoids accidental matches of the folder segment occurring later in the path
                // (e.g. '/animals/fish/' should not match '/fish/').
                var isEndsWithSlash = path.EndsWith('/');

                var escaped = EscapePath(path);

                bool isMatch;
                if (isSuffixMatch)
                {
                    // Match the suffix: append a single '$' to the escaped pattern.
                    isMatch = RobotsTxt.IsMatch($"{escaped}$", pathWithOptionalQuery, matchTimeout);
                }
                else
                {
                    if (isEndsWithSlash)
                    {
                        // For folder directives, the path must start with the folder pattern.
                        // Preserve the original case-sensitive behavior by using the current
                        // culture comparison (which matches the repository's tests).
                        isMatch = pathWithOptionalQuery.StartsWith(path, System.StringComparison.CurrentCulture);
                    }
                    else
                    {
                        isMatch = RobotsTxt.IsMatch($"^{escaped}", pathWithOptionalQuery, matchTimeout);
                    }
                }

                if (isMatch)
                {
                    // Store the escaped pattern so GetEffectedPath can unescape it when
                    // determining the most specific match.
                    if (isAllowed)
                    {
                        effectedAllows.Add(escaped);
                    }
                    else
                    {
                        effectedDisallows.Add(escaped);
                    }
                }
        }

        var effectedAllow = GetEffectedPath(effectedAllows);
        var effectedDisallow = GetEffectedPath(effectedDisallows);

        MatchResult matchResult;
        if (effectedAllow == string.Empty)
        {
            matchResult = effectedDisallow == string.Empty
                ? new MatchResult(new Directive(isAllowed: true, path: "/"))
                : new MatchResult(new Directive(isAllowed: false, path: effectedDisallow), UserAgent);
        }
        else
        {
            if (effectedDisallow == string.Empty)
            {
                matchResult = new(new Directive(isAllowed: true, path: effectedAllow), UserAgent);
            }
            else
            {
                var allowLength = effectedAllow.Length;
                var disallowLength = effectedDisallow.Length;
                if (allowLength > disallowLength)
                {
                    // The most specific takes effect
                    matchResult = new(new Directive(isAllowed: true, path: effectedAllow), UserAgent);
                }
                else if (allowLength < disallowLength)
                {
                    // The most specific takes effect
                    matchResult = new(new Directive(isAllowed: false, path: effectedDisallow), UserAgent);
                }
                else
                {
                    // The least restrictive takes effect
                    matchResult = new(new Directive(isAllowed: true, path: effectedAllow), UserAgent);
                }
            }
        }

        return matchResult;
    }

    public override string ToString()
    {
        var builder = new StringBuilder($"User-agent: {UserAgent}")
            .AppendLine();
        foreach (Directive? directive in directives)
        {
            builder.Append(directive == null ? string.Empty : directive);
            builder.AppendLine();
        }

        if (CrawlDelay != null)
        {
            builder.AppendLine();
            builder.Append($"Crawl-delay: {CrawlDelay.Value}");
        }

        var text = builder.ToString();
        return text;
    }

    private static string EscapePath(string path)
    {
        // Replace wildcard '*' with a placeholder, escape the remainder using Regex.Escape,
        // then restore the wildcard as '.*'. This ensures other regex metacharacters such as
        // '?', '+', '(', ')', '[', ']', '{', '}', '|', '^', '$' are escaped.
        const string WildcardPlaceholder = "\uE000";
        path = path.Replace("*", WildcardPlaceholder);
        path = Regex.Escape(path);
        path = path.Replace(WildcardPlaceholder, ".*");
        return path;
    }

    private static string GetEffectedPath(ConcurrentBag<string> paths)
    {
        var effected = string.Empty;
        foreach (string path in paths)
        {
            // The longest path takes precedence because it is the most specific. This applies
            // regardless of whether the path consists of a wild card (*).
            var tempPath = UnescapePath(path);
            if (tempPath.Length > effected.Length)
            {
                effected = tempPath;
            }
        }

        return effected;
    }

    private static string UnescapePath(string path)
    {
        path = path.Replace("\\.", ".")
            .Replace(".*", "*");
        return path;
    }

    private class DirectiveComparer : IComparer<Directive?>
    {
        public int Compare(Directive? directive, Directive? otherDirective)
        {
            var text = directive == null
                ? string.Empty
                : directive.ToString();
            var otherText = otherDirective == null
                ? string.Empty
                : otherDirective.ToString();
            var result = text.CompareTo(otherText);
            return result;
        }
    }
}
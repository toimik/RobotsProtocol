/*
 * Copyright 2021 nurhafiz@hotmail.sg
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

namespace Toimik.RobotsProtocol
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the group of rules for a user-agent.
    /// </summary>
    public sealed class RuleGroup
    {
        private readonly ISet<Directive> directives = new SortedSet<Directive>(new DirectiveComparer());

        public RuleGroup(string userAgent)
        {
            UserAgent = userAgent;
        }

        public int? CrawlDelay { get; set; }

        public IEnumerator<Directive> Directives => directives.GetEnumerator();

        public string UserAgent { get; }

        public void AddDirective(Directive directive)
        {
            directives.Add(directive);
        }

        public MatchResult Match(string pathWithOptionalQuery, double matchTimeout)
        {
            var effectedAllows = new ConcurrentBag<string>();
            var effectedDisallows = new ConcurrentBag<string>();
            Parallel.ForEach(directives, directive =>
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

                    // If the path is empty, invert the directive such that 'Disallow: ' becomes
                    // allow everything and vice versa
                    if (path == string.Empty)
                    {
                        isAllowed = !isAllowed;
                        path = "/";
                    }
                }

                path = EscapePath(path);

                // '$' at the end of a path denotes that the match must match the suffix. Otherwise,
                // the match must match the head.
                var isMatchBySuffix = path.EndsWith('$');

                bool isMatch;
                if (isMatchBySuffix)
                {
                    isMatch = RobotsTxt.IsMatch(
                        $"{path}$",
                        pathWithOptionalQuery,
                        matchTimeout);
                }
                else
                {
                    var isEndsWithSlash = path.EndsWith('/');
                    if (isEndsWithSlash)
                    {
                        isMatch = pathWithOptionalQuery.IndexOf(path) != -1;
                    }
                    else
                    {
                        isMatch = RobotsTxt.IsMatch(
                            $"^{path}",
                            pathWithOptionalQuery,
                            matchTimeout);
                    }
                }

                if (isMatch)
                {
                    if (isAllowed)
                    {
                        effectedAllows.Add(path);
                    }
                    else
                    {
                        effectedDisallows.Add(path);
                    }
                }
            });

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
                    var allowLength = effectedAllow.Length; ;
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
            foreach (Directive directive in directives)
            {
                builder.Append(directive);
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
            // Make the period a literal. This must be done before replacing the wild card below to
            // prevent the substituted period from getting replaced.
            path = path.Replace(".", "\\.");

            // '*' (wild card) refers to any character. Prefix it with a period to use in pattern
            // matching.
            path = path.Replace("*", ".*");

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

        private class DirectiveComparer : IComparer<Directive>
        {
            public int Compare(Directive directive, Directive otherDirective)
            {
                var text = directive.ToString();
                var otherText = otherDirective.ToString();
                var result = text.CompareTo(otherText);
                return result;
            }
        }
    }
}
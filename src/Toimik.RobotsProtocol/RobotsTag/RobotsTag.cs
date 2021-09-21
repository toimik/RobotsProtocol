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
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the counterpart of a robots.txt but for page-level settings.
    /// </summary>
    /// <remarks>
    /// Use this to load and query values of Robots Meta Tags and X-Robots-Tag HTTP headers.
    /// <para>
    /// Unlike robots.txt, there is no standard directives to adhere to. As such, no convenience
    /// methods are provided for direct access to the respective values.
    /// </para>
    /// <para>
    /// <em>NB: If enabled, the user agent parameter passed to methods falls back directly to
    /// <c>robots</c> if there is no exact case-insensitive match.</em>
    /// </para>
    /// </remarks>
    public sealed class RobotsTag
    {
        public const string UserAgentForCatchAll = "robots";

        private readonly IDictionary<string, IDictionary<string, ISet<Tag>>> userAgentToDirectiveToTags = new Dictionary<string, IDictionary<string, ISet<Tag>>>(StringComparer.OrdinalIgnoreCase);

        public RobotsTag()
        {
        }

        /// <summary>
        /// Gets, for this instance, the number of <see cref="Tag"/>(s) whose respective values
        /// match the specified directive and user agent.
        /// </summary>
        /// <param name="userAgent">
        /// User agent that must match <see cref="Tag.UserAgent"/>.
        /// </param>
        /// <param name="directive">
        /// Optional directive that must match <see cref="Tag.Directive"/>.
        /// </param>
        /// <remarks>
        /// The number of matching <see cref="Tag"/>(s).
        /// </remarks>
        public int GetTagCount(string userAgent, string directive = null)
        {
            var tags = DoGetTags(userAgent, directive);
            return tags.Count;
        }

        /// <summary>
        /// Gets, for this instance, every <see cref="Tag"/> whose respective values match the
        /// specified directive and user agent.
        /// </summary>
        /// <param name="userAgent">
        /// User agent that must match <see cref="Tag.UserAgent"/>.
        /// </param>
        /// <param name="directive">
        /// Optional directive that must match <see cref="Tag.Directive"/>.
        /// </param>
        /// <remarks>
        /// An enumerator of matching <see cref="Tag"/> in no particular order. This is never
        /// <c>null</c>.
        /// </remarks>
        public IEnumerator<Tag> GetTags(string userAgent, string directive = null)
        {
            var tags = DoGetTags(userAgent, directive);
            return tags.GetEnumerator();
        }

        /// <summary>
        /// Determines, for this instance, whether there is any <see cref="Tag"/> whose respective
        /// values match the specified directive and user agent.
        /// </summary>
        /// <param name="userAgent">
        /// User agent that must match <see cref="Tag.UserAgent"/>.
        /// </param>
        /// <param name="directive">
        /// Optional directive that must match <see cref="Tag.Directive"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if there is any matching <see cref="Tag"/>; <c>false</c> otherwise.
        /// </returns>
        public bool HasTag(string userAgent, string directive = null)
        {
            var tags = GetTags(userAgent, directive);
            var hasDirecitve = tags.MoveNext();
            return hasDirecitve;
        }

        /// <summary>
        /// Loads, for this instance, the data.
        /// </summary>
        /// <param name="data">
        /// Every line, each in the form of
        /// <c>[user-agent:]directive[:value][,directive[:value]][,...]</c> where bracketed fields
        /// are optional.
        /// </param>
        /// <param name="specialWords">
        /// Optional set of case-insensitive words treated as the name of directives with values
        /// (e.g. max-snippet is the name of this valued directive: <em>max-snippet: 10</em>).
        /// <para>
        /// All directives in the form of <c>directive: value</c> appearing as the prefix of a datum
        /// are treated as the name of a targeted user agent unless the directive is specified in
        /// this set.
        /// </para>
        /// </param>
        /// <returns>
        /// Every <see cref="Error{TagErrorCode}"/>, if any.
        /// </returns>
        /// <remarks>
        /// All existing entries, if any, are cleared when this method is called.
        /// <para>
        /// Each directive and value, if any, is extracted into individual <see cref="Tag"/> that is
        /// associated with the corresponding user agent.
        /// </para>
        /// </remarks>
        public IEnumerable<Error<TagErrorCode>> Load(IEnumerable<string> data, ISet<string> specialWords = null)
        {
            userAgentToDirectiveToTags.Clear();
            var errors = new LinkedList<Error<TagErrorCode>>();
            if (specialWords == null)
            {
                specialWords = new HashSet<string>(0);
            }

            var lineNumber = 1;
            foreach (string datum in data)
            {
                // e.g. all, max-snippet: 100

                // e.g. robots: max-snippet: 100

                // e.g. max-snippet: 100, all

                var text = datum.Trim();

                // The prefix of each datum can either have an implicit user agent or an explicit
                // one. The default user agent is "robots" but specific ones can be anything. There
                // is also a need to differentiate between a user agent and a directive (that has a
                // value). This is because both of them use colon as a separator.
                var tokens = text.Split(",");
                var firstToken = tokens[0];
                string userAgent;
                var colonIndex = firstToken.IndexOf(':');
                if (colonIndex == -1)
                {
                    // e.g. all
                    userAgent = UserAgentForCatchAll;
                }
                else
                {
                    var prefix = firstToken.Substring(0, colonIndex).TrimEnd();
                    var isSpecialWord = specialWords.Contains(prefix);
                    if (isSpecialWord)
                    {
                        // e.g. max-snippet: 100
                        userAgent = UserAgentForCatchAll;
                    }
                    else
                    {
                        // e.g. bot: max-snippet: 100
                        userAgent = prefix.ToLower();
                        tokens = text[(colonIndex + 1)..].Split(",");
                    }
                }

                // Then process the rest of the tokens
                for (int i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i].Trim();
                    if (token == string.Empty)
                    {
                        var line = new Line(lineNumber, text);
                        errors.AddLast(new Error<TagErrorCode>(line, TagErrorCode.MissingValue));
                        continue;
                    }

                    var tag = CreateTag(userAgent, token);
                    AddTag(tag);
                }

                lineNumber++;
            }

            return errors;
        }

        private static Tag CreateTag(string userAgent, string content)
        {
            // NOTE: content is in the form of [directive:] value

            Tag tag;
            var index = content.IndexOf(':');
            if (index == -1)
            {
                // e.g. all
                tag = new Tag(content, userAgent: userAgent);
            }
            else
            {
                // e.g. max-snippet : 100
                var directive = content.Substring(0, index).TrimEnd();
                var value = content[(index + 1)..].TrimStart();
                tag = new Tag(
                    directive,
                    value,
                    userAgent);
            }

            return tag;
        }

        private static void MapTag(IDictionary<string, ISet<Tag>> directiveToTags, Tag tag)
        {
            var tags = new HashSet<Tag>(new TagComparer())
            {
                tag,
            };
            directiveToTags.Add(tag.Directive, tags);
        }

        private void AddTag(Tag tag)
        {
            var userAgent = tag.UserAgent;
            var hasUserAgent = userAgentToDirectiveToTags.ContainsKey(userAgent);
            if (!hasUserAgent)
            {
                var directiveToTags = new Dictionary<string, ISet<Tag>>(StringComparer.OrdinalIgnoreCase);
                MapTag(directiveToTags, tag);
                userAgentToDirectiveToTags.Add(userAgent, directiveToTags);
            }
            else
            {
                var directiveToTags = userAgentToDirectiveToTags[userAgent];
                var directive = tag.Directive;
                var hasDirective = directiveToTags.ContainsKey(directive);
                if (!hasDirective)
                {
                    MapTag(directiveToTags, tag);
                }
                else
                {
                    var tags = directiveToTags[directive];
                    tags.Add(tag);
                }
            }
        }

        private ISet<Tag> DoGetTags(string userAgent, string directive)
        {
            IDictionary<string, ISet<Tag>> directiveToTags = null;
            if (userAgent != null)
            {
                var hasUserAgent = userAgentToDirectiveToTags.ContainsKey(userAgent);
                if (hasUserAgent)
                {
                    directiveToTags = userAgentToDirectiveToTags[userAgent];
                }
            }

            ISet<Tag> tags;
            if (directiveToTags == null)
            {
                tags = new HashSet<Tag>(0);
            }
            else
            {
                if (directive == null)
                {
                    tags = new HashSet<Tag>();
                    foreach (ISet<Tag> tempTags in directiveToTags.Values)
                    {
                        tags.UnionWith(tempTags);
                    }
                }
                else
                {
                    var hasDirective = directiveToTags.ContainsKey(directive);
                    tags = hasDirective
                        ? (ISet<Tag>)directiveToTags[directive]
                        : new HashSet<Tag>(0);
                }
            }

            return tags;
        }

        private class TagComparer : IEqualityComparer<Tag>
        {
            public bool Equals(Tag tag, Tag otherTag)
            {
                var text = ToString(tag);
                var otherText = ToString(otherTag);
                var isEquals = text.Equals(otherText, StringComparison.OrdinalIgnoreCase);
                return isEquals;
            }

            public int GetHashCode(Tag tag)
            {
                var hashCode = (tag.UserAgent, tag.Directive, tag.Value).GetHashCode();
                return hashCode;
            }

            private static string ToString(Tag tag)
            {
                var text = tag.Value == null
                    ? tag.Directive
                    : $"{tag.Directive}: {tag.Value}";
                text = tag.UserAgent.Equals(UserAgentForCatchAll)
                    ? text
                    : $"{tag.UserAgent}: {text}";
                return text;
            }
        }
    }
}
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
    /// <summary>
    /// Represents an atomic Robots meta tag or its equivalent - X-Robots-Tag.
    /// </summary>
    /// <remarks>
    /// A robots meta tag is in the form of: &lt;meta name="user-agent"
    /// content="[directive:]value"&gt; whereas its <c>X-Robots-Tag</c> equivalent is
    /// <c>X-Robots-Tag: [user-agent:] [directive:] value</c>.
    /// <para>
    /// Although more than one <c>[directive:] value</c> can be specified in the <c>content</c>,
    /// this class stores one only.
    /// </para>
    /// <para>Examples of values for the content are:
    /// <list type="bullet">
    /// <item>
    /// <description>all</description>
    /// </item>
    /// <item>
    /// <description>nofollow</description>
    /// </item>
    /// <item>
    /// <description>max-snippet: 100</description>
    /// </item>
    /// <item>
    /// <description>unavailable_after: 1 Jan 2000 12:34:56 UTC</description>
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class Tag
    {
        public Tag(
            string directive,
            string value = null,
            string userAgent = null)
        {
            Directive = directive.ToLower();
            Value = value?.ToLower();
            UserAgent = userAgent?.ToLower();
        }

        public string Directive { get; }

        public string UserAgent { get; }

        public string Value { get; }

        public override string ToString()
        {
            var text = Value == null
                ? Directive
                : $"{Directive}: {Value}";
            text = UserAgent == null
                ? text
                : $"{UserAgent}: {text}";
            return text;
        }
    }
}
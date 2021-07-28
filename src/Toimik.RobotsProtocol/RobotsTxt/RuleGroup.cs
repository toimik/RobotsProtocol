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
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents the group of rules for a user-agent.
    /// </summary>
    public sealed class RuleGroup
    {
        private readonly ISet<Directive> directives = new SortedSet<Directive>();

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
    }
}
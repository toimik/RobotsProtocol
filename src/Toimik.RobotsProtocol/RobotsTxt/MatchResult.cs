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
    /// Represents the result of matching /robots.txt directives to a URL.
    /// </summary>
    /// <remarks>
    /// If <see cref="UserAgent"/> is <c>null</c>, the match is an implicit allow due to the absence
    /// of any named or catch-all <c>user-agent</c>.
    /// </remarks>
    public struct MatchResult
    {
        public MatchResult(Directive directive, string userAgent = null)
        {
            Directive = directive;
            UserAgent = userAgent;
        }

        public Directive Directive { get; }

        public string UserAgent { get; }
    }
}
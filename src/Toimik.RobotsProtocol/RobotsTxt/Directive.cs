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
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a directive ( <c>Disallow: &lt;path&gt;</c> or <c>Allow: &lt;path&gt;</c>) in a
    /// robots.txt.
    /// </summary>
    /// <remarks>
    /// According to Google's specs, only the <c>allow</c> and <c>disallow</c> fields are called
    /// directives.
    /// </remarks>
    public sealed class Directive : IComparable
    {
        public Directive(bool isAllowed, string path)
        {
            IsAllowed = isAllowed;
            Path = path;
        }

        public bool IsAllowed { get; }

        public string Path { get; }

        [ExcludeFromCodeCoverage]
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (obj is not Directive otherDirective)
            {
                throw new ArgumentException($"Object is not a {nameof(Directive)}");
            }

            var result = ToString().CompareTo(otherDirective.ToString());
            return result;
        }

        public override string ToString()
        {
            var name = IsAllowed
                ? "Allow"
                : "Disallow";
            var text = $"{name}: {Path}";
            return text;
        }
    }
}
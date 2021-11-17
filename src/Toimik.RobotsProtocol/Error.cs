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
    /// Represents an error encountered when loading values into <see cref="RobotsTxt"/> or
    /// <see cref="RobotsTag"/>.
    /// </summary>
    /// <typeparam name="T">
    /// <see cref="TxtErrorCode"/> or <see cref="TagErrorCode"/>.
    /// </typeparam>
    public sealed class Error<T>
    {
        public Error(Line line, T code)
        {
            Line = line;
            Code = code;
        }

        public T Code { get; }

        public Line Line { get; }

        public override string ToString()
        {
            var text = $"{Line} ; {Code}";
            return text;
        }
    }
}
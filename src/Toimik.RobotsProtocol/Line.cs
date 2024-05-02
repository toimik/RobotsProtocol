/*
 * Copyright 2021-2024 nurhafiz@hotmail.sg
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

/// <summary>
/// Represents a line in a robots.txt or a list of HTTP Meta tags / X-Robots-Tag values.
/// </summary>
public sealed class Line(int number, string text)
{
    public int Number { get; } = number;

    public string Text { get; } = text;

    public override string ToString()
    {
        var text = $"{Number}: {Text}";
        return text;
    }
}
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

using System.ComponentModel;

public enum TxtErrorCode
{
    [Description("Missing value.")]
    MissingValue,

    [Description("Path must be empty or start with '/'.")]
    InvalidPathFormat,

    [Description("Rule found before any User-agent field.")]
    RuleFoundBeforeUserAgent,
}
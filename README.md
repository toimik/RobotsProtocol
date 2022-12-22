![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/nurhafiz/c331314aee27fa8f1d49a3870142f8b4/raw/RobotsProtocol-coverage.json)
![Nuget](https://img.shields.io/nuget/v/Toimik.RobotsProtocol)

# Toimik.RobotsProtocol

.NET 6 C# [robots.txt](https://en.wikipedia.org/wiki/Robots_exclusion_standard) parser and a C# [Robots Meta Tag / X-Robots-Tag](https://developers.google.com/search/docs/advanced/robots/robots_meta_tag) parser.

## Features

### RobotsTxt.cs
- Creates instance via string or stream
- Parses standard, extended, and custom fields:
  - User-agent
  - Disallow
  - Crawl-delay
  - Sitemap
  - Allow (Toggle-able; Can be ignored if needed)
  - Others (e.g. Host)
- Supports misspellings of fields
- Matches wild cards in paths (* and $)

### RobotsTag.cs

- Parses custom fields

## Quick Start

### Installation

#### Package Manager

```command
PM> Install-Package Toimik.RobotsProtocol
```

#### .NET CLI

```command
> dotnet add package Toimik.RobotsProtocol
```

### Usage

Snippets are shown below. 

Refer to demo programs in `samples` folder for complete source code.

#### RobotsTxt.cs (for parsing robots.txt)
```c# 
var robotsTxt = new RobotsTxt();

// Load content of a robots.txt from a String
var content = "...";
_ = robotsTxt.Load(content);

// Load content of a robots.txt from a Stream
// var stream = "...";
// _ = await robotsTxt.Load(stream);

var isAllowed = robotsTxt.IsAllowed("autobot", "/folder/file.htm"};
```

#### RobotsTag.cs (for parsing robots meta tag / x-robots-tag)
```c# 
var robotsTag = new RobotsTag();

// This data is either retrieved from Robots Meta Tag (e.g. <meta name="badbot"
// content="none"> or X-Robots-Tag HTTP response header (e.g. X-Robots-Tag: otherbot:
// index, nofollow). 
var data = ...;

// Words treated as the name of directives with values (e.g. max-snippet: 10).
var specialWords = new HashSet<string>
{
    "max-snippet",
    "max-image-preview",

    // ... Add accordingly
};

// Load the data to parse. This will extract every directive into their own Tag class
_ = robotsTag.Load(data, specialWords);

var hasNone = robotsTag.HasTag("autobot", "none");
var hasNoIndex = robotsTag.HasTag("autobot", "noindex");
var isIndexable = !hasNone && !hasNoIndex;
```
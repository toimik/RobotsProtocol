# Toimik.RobotsProtocol

Parsers for [Robots Exclusion Standard](https://en.wikipedia.org/wiki/Robots_exclusion_standard) (aka /robots.txt), [Robots Meta Tag, and X-Robots-Tag](https://developers.google.com/search/docs/advanced/robots/robots_meta_tag) written in C# (.NET 5).

*Please star this project*

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
PM> Install-Package Toimik.RobotsProtocol -Version 0.1.1
```

#### .NET CLI

```command
> dotnet add package Toimik.RobotsProtocol --version 0.1.1
```

#### PackageReference

```command
<PackageReference Include="Toimik.RobotsProtocol" Version="0.1.1" />
```

#### Scripts & Interactive

```command
> #r "nuget: Toimik.RobotsProtocol, 0.1.1"
```

#### Paket CLI

```command
> paket add Toimik.RobotsProtocol --version 0.1.1
```

#### Cake

```command
// Install Toimik.RobotsProtocol as a Cake Addin
#addin nuget:?package=Toimik.RobotsProtocol&version=0.1.1

// Install Toimik.RobotsProtocol as a Cake Tool
#tool nuget:?package=Toimik.RobotsProtocol&version=0.1.1
```

### Usage

Snippets are shown below. 

Refer to demo programs in `samples` folder for complete source code.

#### RobotsTxt.cs (for parsing /robots.txt)
```c# 
var robotsTxt = new RobotsTxt();

// Content of the /robots.txt as a String or Stream
var content = "...";

_ = await robotsTxt.Load(content);

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
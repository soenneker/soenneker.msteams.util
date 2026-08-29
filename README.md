[![](https://img.shields.io/nuget/v/soenneker.msteams.util.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.msteams.util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.msteams.util/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.msteams.util/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.msteams.util.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.msteams.util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.msteams.util/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.msteams.util/actions/workflows/codeql.yml)

# Soenneker.MsTeams.Util

A centralized utility for sending rich, configurable Adaptive Card messages to Microsoft Teams channels via a service bus, with environment-aware filtering and dynamic content generation.

## Install

```bash
dotnet add package Soenneker.MsTeams.Util
```

## Quick start

```csharp
using Soenneker.MsTeams.Util.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddMsTeamsUtilAsSingleton();
```

Adds `IMsTeamsUtil` as a singleton service.

## What you get

- `IMsTeamsUtil` — A centralized utility for sending rich, configurable Adaptive Card messages to Microsoft Teams channels via a service bus, with environment-aware filtering and dynamic content generation.
- `MsTeamsUtilRegistrar` — A centralized utility for sending rich, configurable Adaptive Card messages to Microsoft Teams channels via a service bus, with environment-aware filtering and dynamic content generation.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IMsTeamsUtil.SendMessage(title, channel, summary, facts, e, additionalBody, skipLocal, cancellationToken)` | Sends an Adaptive Card message with a title and optional details to a specified Teams channel. | A task that completes when the message has been sent. |
| `IMsTeamsUtil.SendMessage(e, title, channel, summary, facts, skipLocal, cancellationToken)` | Sends an exception as an Adaptive Card message to a Teams channel. | A task that completes when the message has been sent. |
| `IMsTeamsUtil.SendMessage(title, summary, items, channel, skipLocal, cancellationToken)` | Sends a list of items as a table-based Adaptive Card to a specified Teams channel. | A task that completes when the message has been sent. |
| `IMsTeamsUtil.SendMessageCard(card, channel, skipLocal, cancellationToken)` | Sends a fully-formed Adaptive Card directly to a specified Teams channel. | A task that completes when the message card has been sent. |
| `MsTeamsUtilRegistrar.AddMsTeamsUtilAsSingleton(services)` | Adds `IMsTeamsUtil` as a singleton service. | The same service collection, so additional registrations can be chained. |
| `MsTeamsUtilRegistrar.AddMsTeamsUtilAsScoped(services)` | Adds `IMsTeamsUtil` as a scoped service. | The same service collection, so additional registrations can be chained. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
- Dispose instances you own when their scope ends so held resources can be released.

# Soenneker.MsTeams.Util
[![](https://img.shields.io/nuget/v/soenneker.msteams.util.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.msteams.util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.msteams.util/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.msteams.util/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.msteams.util.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.msteams.util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.msteams.util/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.msteams.util/actions/workflows/codeql.yml)

Builds Adaptive Cards for common notifications and either sends them directly to Teams or places a `MsTeamsMessage` on Service Bus.

## Installation

```bash
dotnet add package Soenneker.MsTeams.Util
```

## Configuration

```json
{
  "Environment": "Local",
  "MsTeams": {
    "UseQueue": false,
    "Enabled": true,
    "Errors": {
      "Enabled": true,
      "WebhookUrl": "https://example.webhook.office.com/webhookb2/..."
    }
  }
}
```

Each channel needs an `Enabled` setting. Direct sending also needs the sender's global `MsTeams:Enabled` switch and an absolute HTTPS `WebhookUrl`. Channel names cannot contain `:`. Keep webhook URLs in a secret provider.

`MsTeams:UseQueue` and per-channel `Enabled` values respond to configuration reloads. `Environment` is read when the utility is constructed; `skipLocal` suppresses a send only when that value is `Local`.

## Registration

```csharp
using Soenneker.MsTeams.Util.Registrars;

builder.Services.AddMsTeamsUtilAsScoped();
// or: builder.Services.AddMsTeamsUtilAsSingleton();
```

The scoped registration keeps the Teams sender and its underlying HTTP client infrastructure singleton while allowing the higher-level utility and its other scoped dependencies to be disposed with the scope.

## Send a notification

```csharp
using Soenneker.MsTeams.Util.Abstract;

await teams.SendMessage(
    title: "Import completed",
    channel: "Errors",
    summary: "Customer import finished with warnings",
    facts: new Dictionary<string, string?>
    {
        ["Imported"] = importedCount.ToString(),
        ["Rejected"] = rejectedCount.ToString()
    },
    skipLocal: true,
    cancellationToken: cancellationToken);
```

The exception overload defaults to the `Errors` channel and the title `Exception thrown`. `SendMessage<T>` builds a table from a list, while `SendMessageCard` accepts an already-built `AdaptiveCard`.

When `UseQueue` is `false`, successful completion means the Teams webhook accepted the card; a rejected send throws `InvalidOperationException`. When `UseQueue` is `true`, successful completion means Service Bus accepted the message, not that Teams delivered it. Disabled channels and `skipLocal` return without sending.

Exception details, facts, table values, summaries, and additional body text become card content. Remove credentials and personal data before calling these APIs, and account for duplicate notifications if queue delivery is retried.

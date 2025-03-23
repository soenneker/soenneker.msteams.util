using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.MsTeams.Util.Abstract;

/// <summary>
/// A centralized utility for sending rich, configurable Adaptive Card messages to Microsoft Teams channels via a service bus, with environment-aware filtering and dynamic content generation.
/// </summary>
public interface IMsTeamsUtil
{
    /// <summary>
    /// Sends an Adaptive Card message with a title and optional details to a specified Teams channel.
    /// </summary>
    /// <param name="title">The title of the message.</param>
    /// <param name="channel">The target Teams channel name.</param>
    /// <param name="summary">An optional summary for the message.</param>
    /// <param name="facts">An optional list of facts (key-value pairs) to include.</param>
    /// <param name="e">An optional exception to include in the message.</param>
    /// <param name="additionalBody">Additional string content to include in the card body.</param>
    /// <param name="skipLocal">If true, skips sending in a local environment.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    ValueTask SendMessage(string title, string channel, string? summary = null, Dictionary<string, string?>? facts = null, Exception? e = null, string? additionalBody = null,
        bool skipLocal = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an exception as an Adaptive Card message to a Teams channel.
    /// </summary>
    /// <param name="e">The exception to include in the message.</param>
    /// <param name="title">An optional title for the message. Defaults to "Exception thrown".</param>
    /// <param name="channel">The target channel. Defaults to "errors".</param>
    /// <param name="summary">An optional summary for the message.</param>
    /// <param name="facts">Optional facts to display in the message.</param>
    /// <param name="skipLocal">If true, skips sending in a local environment.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    ValueTask SendMessage(Exception e, string? title = null, string? channel = null, string? summary = null, Dictionary<string, string?>? facts = null,
        bool skipLocal = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a list of items as a table-based Adaptive Card to a specified Teams channel.
    /// </summary>
    /// <typeparam name="T">The type of items to include in the table.</typeparam>
    /// <param name="title">The title of the message.</param>
    /// <param name="summary">An optional summary for the card.</param>
    /// <param name="items">The list of items to render as a table.</param>
    /// <param name="channel">The target Teams channel name.</param>
    /// <param name="skipLocal">If true, skips sending in a local environment.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    ValueTask SendMessage<T>(string title, string? summary, List<T> items, string channel, bool skipLocal = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a fully-formed Adaptive Card directly to a specified Teams channel.
    /// </summary>
    /// <param name="card">The Adaptive Card to send.</param>
    /// <param name="channel">The target Teams channel name.</param>
    /// <param name="skipLocal">If true, skips sending in a local environment.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    ValueTask SendMessageCard(AdaptiveCards.AdaptiveCard card, string channel, bool skipLocal = false, CancellationToken cancellationToken = default);
}
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;

namespace EquiApi.Core.Services;

/// <summary>
///     A push notification
/// </summary>
/// <param name="Title">The title shown on the device</param>
/// <param name="Body">The text shown on the device</param>
/// <param name="Data">The values the app receives when the notification is opened</param>
public sealed record PushMessage(string Title, string Body, IReadOnlyDictionary<string, string> Data);

/// <summary>
///     Delivers push notifications to devices
/// </summary>
public interface IPushSender
{
    /// <summary>
    ///     Sends a notification to devices
    /// </summary>
    /// <param name="tokens">The push tokens of the devices</param>
    /// <param name="message">The notification</param>
    /// <returns>The tokens that are no longer valid, e.g. because the app was removed</returns>
    public ValueTask<IReadOnlyCollection<string>> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message);
}

/// <summary>
///     Used if no push service is configured, e.g. in the local development
/// </summary>
internal sealed class NoPushSender : IPushSender
{
    public ValueTask<IReadOnlyCollection<string>> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message) =>
        ValueTask.FromResult<IReadOnlyCollection<string>>([]);
}

public interface IChatNotifier
{
    /// <summary>
    ///     Notifies the devices of the recipient about a new message. A failure is only logged, the message itself
    ///     was already sent.
    /// </summary>
    /// <param name="message">The new message including its sender</param>
    public ValueTask NotifyNewMessageAsync(ChatMessage message);
}

internal sealed class ChatNotifier(IUnitOfWork uow, IPushSender pushSender, ILogger<ChatNotifier> logger)
    : IChatNotifier
{
    public async ValueTask NotifyNewMessageAsync(ChatMessage message)
    {
        try
        {
            var tokens = await uow.PersonRepository.GetPushTokensOfPersonAsync(message.RecipientId);
            if (tokens.Count == 0)
            {
                return;
            }

            string sender = $"{message.Sender.FirstName} {message.Sender.LastName}";
            var data = new Dictionary<string, string>
            {
                ["contactId"] = message.SenderId.ToString(),
                ["contactName"] = sender
            };

            var invalidTokens = await pushSender.SendAsync(tokens, new PushMessage(sender, BodyOf(message), data));
            if (invalidTokens.Count > 0)
            {
                await uow.PersonRepository.RemovePushTokensAsync(invalidTokens);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not send a push notification for message {MessageId}", message.Id);
        }
    }

    private static string BodyOf(ChatMessage message) =>
        message.Type switch
        {
            ChatMessageType.Horse => "Hat ein Pferd geteilt",
            ChatMessageType.Measurement => "Hat eine Messung geteilt",
            ChatMessageType.File when message.FileContentType?.StartsWith("image/", StringComparison.Ordinal) == true
                => "Hat ein Bild geschickt",
            ChatMessageType.File => "Hat eine Datei geschickt",
            _ => message.Text ?? string.Empty
        };
}
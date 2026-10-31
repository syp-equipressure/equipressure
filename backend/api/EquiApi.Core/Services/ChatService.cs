using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf;
using OneOf.Types;

namespace EquiApi.Core.Services;

/// <summary>
///     The values of a new chat message
/// </summary>
public sealed record NewChatMessageData(
    int SenderId,
    int RecipientId,
    ChatMessageType Type,
    string? Text,
    int? HorseId,
    int? MeasurementGroupId);

/// <summary>
///     A file (e.g. an image) sent in a chat message
/// </summary>
/// <param name="FileName">The original name of the file</param>
/// <param name="ContentType">The media type of the file, e.g. image/png</param>
/// <param name="Data">The content of the file</param>
public sealed record ChatFileData(string FileName, string ContentType, byte[] Data);

/// <summary>
///     A conversation of a person with another person
/// </summary>
/// <param name="Contact">The other person</param>
/// <param name="LastMessage">The newest message of the conversation</param>
/// <param name="UnreadCount">The number of messages the person did not read yet</param>
public sealed record ConversationData(Person Contact, ChatMessage LastMessage, int UnreadCount);

public interface IChatService
{
    /// <summary>
    ///     Gets all conversations of a person, the newest first
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The conversations (may be empty) or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<ConversationData>>, NotFound>> GetConversationsAsync(
        int personId);

    /// <summary>
    ///     Gets all messages between two persons, the oldest first
    /// </summary>
    /// <param name="personId">The id of the first person</param>
    /// <param name="otherPersonId">The id of the second person</param>
    /// <returns>The messages (may be empty) or <see cref="NotFound" /> if one of the persons does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<ChatMessage>>, NotFound>> GetConversationAsync(
        int personId, int otherPersonId);

    /// <summary>
    ///     Sends a message. A shared horse has to be a horse of the sender. A shared measurement can be sent by the
    ///     persons of the horse, the rider, the owner of the measurement device (e.g. the saddler who measured) and
    ///     the persons it was released to - but only if the measured horse belongs to the sender or the recipient,
    ///     so a saddler cannot send the measurements of one customer to another one.
    /// </summary>
    /// <param name="data">The values of the message</param>
    /// <returns>
    ///     The new message, <see cref="NotFound" /> if one of the persons, the horse or the measurement group does not
    ///     exist or <see cref="InvalidMessage" /> if the message cannot be sent
    /// </returns>
    public ValueTask<OneOf<Success<ChatMessage>, NotFound, InvalidMessage>> SendMessageAsync(NewChatMessageData data);

    /// <summary>
    ///     Sends a file, e.g. an image, with an optional text
    /// </summary>
    /// <param name="senderId">The id of the sender</param>
    /// <param name="recipientId">The id of the recipient</param>
    /// <param name="text">An optional text</param>
    /// <param name="file">The file</param>
    /// <returns>
    ///     The new message, <see cref="NotFound" /> if one of the persons does not exist
    ///     or <see cref="InvalidMessage" /> if the message cannot be sent
    /// </returns>
    public ValueTask<OneOf<Success<ChatMessage>, NotFound, InvalidMessage>> SendFileAsync(int senderId,
        int recipientId, string? text, ChatFileData file);

    /// <summary>
    ///     Gets the file of a message, only the sender and the recipient can get it
    /// </summary>
    /// <param name="personId">The id of the sender or the recipient</param>
    /// <param name="messageId">The id of the message</param>
    /// <returns>The file or <see cref="NotFound" /> if the person cannot see the message or it has no file</returns>
    public ValueTask<OneOf<Success<ChatFileData>, NotFound>> GetFileAsync(int personId, int messageId);

    /// <summary>
    ///     Marks all messages a person received from another person as read
    /// </summary>
    /// <param name="personId">The id of the recipient</param>
    /// <param name="otherPersonId">The id of the sender</param>
    /// <returns>The number of newly read messages or <see cref="NotFound" /> if one of the persons does not exist</returns>
    public ValueTask<OneOf<Success<int>, NotFound>> MarkAsReadAsync(int personId, int otherPersonId);

    /// <summary>
    ///     Marks all messages a person received as delivered, the apps call this as soon as they are online and
    ///     whenever a new message arrives. Read messages are always delivered as well.
    /// </summary>
    /// <param name="personId">The id of the recipient</param>
    /// <returns>
    ///     The ids of the senders whose messages were newly delivered (may be empty)
    ///     or <see cref="NotFound" /> if the person does not exist
    /// </returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<int>>, NotFound>> MarkAsDeliveredAsync(int personId);

    /// <summary>
    ///     Adds a shared horse or measurement to the recipient of the message: the sender becomes a contact of the
    ///     recipient and the measurement groups are released to the recipient. Accepting a message twice has no effect.
    /// </summary>
    /// <param name="personId">The id of the recipient</param>
    /// <param name="messageId">The id of the message</param>
    /// <returns>
    ///     The accepted message, <see cref="NotFound" /> if the person did not receive the message
    ///     or <see cref="InvalidMessage" /> if the message does not share anything or the persons cannot be contacts
    /// </returns>
    public ValueTask<OneOf<Success<ChatMessage>, NotFound, InvalidMessage>> AcceptShareAsync(int personId,
        int messageId);

    /// <summary>
    ///     Returned if a message cannot be sent or accepted
    /// </summary>
    /// <param name="Reason">Why the message is not possible</param>
    public readonly record struct InvalidMessage(string Reason);
}

internal sealed class ChatService(IUnitOfWork uow, IClock clock, ILogger<ChatService> logger) : IChatService
{
    public async ValueTask<OneOf<Success<IReadOnlyCollection<ConversationData>>, NotFound>> GetConversationsAsync(
        int personId)
    {
        if (!await uow.PersonRepository.PersonExistsAsync(personId))
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        // the messages are ordered newest first, so the first message of each group is the last one of the conversation
        var messages = await uow.ChatRepository.GetMessagesOfPersonAsync(personId);
        var conversations = messages.GroupBy(m => m.SenderId == personId ? m.RecipientId : m.SenderId)
                                    .Select(group =>
                                    {
                                        var last = group.First();
                                        var contact = last.SenderId == personId ? last.Recipient : last.Sender;
                                        int unread = group.Count(m => m.RecipientId == personId && m.ReadAt is null);

                                        return new ConversationData(contact, last, unread);
                                    })
                                    .ToList();

        return new Success<IReadOnlyCollection<ConversationData>>(conversations);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<ChatMessage>>, NotFound>> GetConversationAsync(
        int personId, int otherPersonId)
    {
        if (!await BothPersonsExistAsync(personId, otherPersonId))
        {
            return new NotFound();
        }

        var messages = await uow.ChatRepository.GetConversationAsync(personId, otherPersonId);

        return new Success<IReadOnlyCollection<ChatMessage>>(messages);
    }

    public async ValueTask<OneOf<Success<ChatMessage>, NotFound, IChatService.InvalidMessage>> SendMessageAsync(
        NewChatMessageData data)
    {
        if (data.SenderId == data.RecipientId)
        {
            return new IChatService.InvalidMessage("A message cannot be sent to oneself");
        }

        if (data.Type == ChatMessageType.File)
        {
            return new IChatService.InvalidMessage("Files have to be uploaded");
        }

        if (!await BothPersonsExistAsync(data.SenderId, data.RecipientId))
        {
            return new NotFound();
        }

        int? horseId = null;
        int? groupId = null;
        switch (data.Type)
        {
            case ChatMessageType.Horse:
                if (!await uow.HorseRepository.HorseExistsAsync(data.HorseId ?? 0))
                {
                    logger.LogInformation("Cannot share horse {HorseId}, it does not exist", data.HorseId);

                    return new NotFound();
                }

                horseId = data.HorseId;

                break;
            case ChatMessageType.Measurement:
                var group = await uow.MeasurementRepository.GetGroupByIdAsync(data.MeasurementGroupId ?? 0);
                if (group is null)
                {
                    logger.LogInformation("Cannot share measurement group {GroupId}, it does not exist",
                                          data.MeasurementGroupId);

                    return new NotFound();
                }

                if (!await uow.MeasurementRepository.CanShareGroupAsync(data.SenderId, group.Id))
                {
                    logger.LogWarning("Person {PersonId} cannot share measurement group {GroupId}", data.SenderId,
                                      group.Id);

                    return new IChatService.InvalidMessage(
                        "Only persons of the horse, the measuring person or persons it was released to can share a measurement");
                }

                // a saddler may only send the measurements of the customer they are writing to,
                // an equestrian shares the measurements of their own horses
                if (!await uow.HorseRepository.IsPersonOfHorseAsync(data.RecipientId, group.HorseId)
                    && !await uow.HorseRepository.IsPersonOfHorseAsync(data.SenderId, group.HorseId))
                {
                    logger.LogWarning("Person {PersonId} cannot send measurement group {GroupId} to person {RecipientId}",
                                      data.SenderId, group.Id, data.RecipientId);

                    return new IChatService.InvalidMessage(
                        "A measurement can only be sent to the owner or a rider of the measured horse");
                }

                groupId = group.Id;

                break;
        }

        if (horseId is not null && !await uow.HorseRepository.IsPersonOfHorseAsync(data.SenderId, horseId.Value))
        {
            logger.LogWarning("Person {PersonId} cannot share horse {HorseId}, it is not one of their horses",
                              data.SenderId, horseId);

            return new IChatService.InvalidMessage("Only the owner or a rider of a horse can share it");
        }

        var message = new ChatMessage
        {
            Type = data.Type,
            Text = string.IsNullOrWhiteSpace(data.Text) ? null : data.Text.Trim(),
            SentAt = clock.GetCurrentInstant(),
            SenderId = data.SenderId,
            RecipientId = data.RecipientId,
            // a shared measurement only references its group, the horse is reachable through the group
            HorseId = data.Type == ChatMessageType.Horse ? horseId : null,
            MeasurementGroupId = groupId
        };

        return new Success<ChatMessage>(await SaveMessageAsync(message));
    }

    public async ValueTask<OneOf<Success<ChatMessage>, NotFound, IChatService.InvalidMessage>> SendFileAsync(
        int senderId, int recipientId, string? text, ChatFileData file)
    {
        if (senderId == recipientId)
        {
            return new IChatService.InvalidMessage("A message cannot be sent to oneself");
        }

        if (file.Data.Length == 0)
        {
            return new IChatService.InvalidMessage("The file is empty");
        }

        if (!await BothPersonsExistAsync(senderId, recipientId))
        {
            return new NotFound();
        }

        var message = new ChatMessage
        {
            Type = ChatMessageType.File,
            Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
            SentAt = clock.GetCurrentInstant(),
            SenderId = senderId,
            RecipientId = recipientId,
            FileName = Path.GetFileName(file.FileName),
            FileContentType = file.ContentType,
            FileSize = file.Data.Length,
            File = new ChatFile { Data = file.Data }
        };

        return new Success<ChatMessage>(await SaveMessageAsync(message));
    }

    public async ValueTask<OneOf<Success<ChatFileData>, NotFound>> GetFileAsync(int personId, int messageId)
    {
        var message = await uow.ChatRepository.GetMessageByIdAsync(messageId);
        if (message is null || (message.SenderId != personId && message.RecipientId != personId))
        {
            logger.LogInformation("Person {PersonId} cannot see message {MessageId}", personId, messageId);

            return new NotFound();
        }

        var file = await uow.ChatRepository.GetFileAsync(messageId);
        if (file is null)
        {
            return new NotFound();
        }

        return new Success<ChatFileData>(new ChatFileData(message.FileName ?? "datei",
                                                          message.FileContentType ?? "application/octet-stream",
                                                          file.Data));
    }

    private async ValueTask<ChatMessage> SaveMessageAsync(ChatMessage message)
    {
        uow.ChatRepository.AddMessage(message);
        await uow.SaveChangesAsync();
        logger.LogInformation("Person {SenderId} sent message {MessageId} to person {RecipientId}",
                              message.SenderId, message.Id, message.RecipientId);

        // reload to get the persons and the shared horse or measurement group
        var saved = await uow.ChatRepository.GetMessageByIdAsync(message.Id);

        return saved ?? message;
    }

    public async ValueTask<OneOf<Success<int>, NotFound>> MarkAsReadAsync(int personId, int otherPersonId)
    {
        if (!await BothPersonsExistAsync(personId, otherPersonId))
        {
            return new NotFound();
        }

        var unread = await uow.ChatRepository.GetUnreadMessagesAsync(personId, otherPersonId);
        if (unread.Count == 0)
        {
            return new Success<int>(0);
        }

        var now = clock.GetCurrentInstant();
        foreach (var message in unread)
        {
            message.ReadAt = now;
            message.DeliveredAt ??= now;
        }

        await uow.SaveChangesAsync();

        return new Success<int>(unread.Count);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<int>>, NotFound>> MarkAsDeliveredAsync(int personId)
    {
        if (!await uow.PersonRepository.PersonExistsAsync(personId))
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        var undelivered = await uow.ChatRepository.GetUndeliveredMessagesAsync(personId);
        if (undelivered.Count == 0)
        {
            return new Success<IReadOnlyCollection<int>>([]);
        }

        var now = clock.GetCurrentInstant();
        foreach (var message in undelivered)
        {
            message.DeliveredAt = now;
        }

        await uow.SaveChangesAsync();

        return new Success<IReadOnlyCollection<int>>(undelivered.Select(m => m.SenderId).Distinct().ToList());
    }

    public async ValueTask<OneOf<Success<ChatMessage>, NotFound, IChatService.InvalidMessage>> AcceptShareAsync(
        int personId, int messageId)
    {
        var message = await uow.ChatRepository.GetMessageByIdAsync(messageId);
        if (message is null || message.RecipientId != personId)
        {
            logger.LogInformation("Person {PersonId} did not receive message {MessageId}", personId, messageId);

            return new NotFound();
        }

        // texts and files cannot be added, only shared horses and measurements
        if (message.Type is not (ChatMessageType.Horse or ChatMessageType.Measurement))
        {
            return new IChatService.InvalidMessage("The message does not share a horse or a measurement");
        }

        if (message.AcceptedAt is not null)
        {
            return new Success<ChatMessage>(message);
        }

        // the horse or measurement group was deleted after it was shared
        if (message.HorseId is null && message.MeasurementGroupId is null)
        {
            return new IChatService.InvalidMessage("The shared horse or measurement does not exist anymore");
        }

        var equestrian = await uow.PersonRepository.GetPersonByIdAsync(message.SenderId);
        var saddler = await uow.PersonRepository.GetPersonByIdAsync(message.RecipientId);
        if (equestrian is null || saddler is null)
        {
            return new NotFound();
        }

        if (!PersonService.HasRole(equestrian, RoleName.Equestrian)
            || !PersonService.HasRole(saddler, RoleName.Saddler))
        {
            logger.LogWarning("Person {PersonId} cannot accept message {MessageId}, they are no saddler", personId,
                              messageId);

            return new IChatService.InvalidMessage("Only saddlers can add horses and measurements of equestrians");
        }

        await uow.PersonRepository.MarkAsContactAsync(equestrian, saddler);

        var now = clock.GetCurrentInstant();
        IReadOnlyCollection<int> groupIds = message.HorseId is { } horseId
            ? await uow.MeasurementRepository.GetGroupIdsOfHorseAsync(horseId)
            : [message.MeasurementGroupId!.Value];
        // a horse can be shared several times, every measurement is released only once
        var releasedIds = await uow.MeasurementRepository.GetReleasedGroupIdsAsync(saddler.Id);
        var newGroupIds = groupIds.Except(releasedIds).ToList();
        if (newGroupIds.Count > 0)
        {
            uow.MeasurementRepository.AddRelease(new Release
            {
                PersonId = saddler.Id,
                ReleaseTimestamp = now,
                MeasurementEligibilities = newGroupIds
                                           .Select(id => new MeasurementEligibility { MeasurementGroupId = id })
                                           .ToList()
            });
        }

        message.AcceptedAt = now;
        if (message.HorseId is { } sharedHorseId)
        {
            var shares = await uow.ChatRepository.GetSharesOfHorseAsync(message.SenderId, personId, sharedHorseId);
            foreach (var share in shares)
            {
                share.AcceptedAt ??= now;
            }
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Person {PersonId} accepted message {MessageId}", personId, messageId);

        return new Success<ChatMessage>(message);
    }

    private async ValueTask<bool> BothPersonsExistAsync(int personId, int otherPersonId)
    {
        foreach (int id in new[] { personId, otherPersonId })
        {
            if (!await uow.PersonRepository.PersonExistsAsync(id))
            {
                LogPersonNotFound(id);

                return false;
            }
        }

        return true;
    }

    private void LogPersonNotFound(int personId)
    {
        logger.LogInformation("Person {PersonId} not found", personId);
    }
}
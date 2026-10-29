using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IChatRepository
{
    /// <summary>
    ///     Gets all messages a person sent or received, including both persons and the shared horse or measurement group
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The messages, the newest first</returns>
    public ValueTask<IReadOnlyCollection<ChatMessage>> GetMessagesOfPersonAsync(int personId);

    /// <summary>
    ///     Gets the conversation between two persons, including both persons and the shared horses and measurement groups
    /// </summary>
    /// <param name="personId">The id of the first person</param>
    /// <param name="otherPersonId">The id of the second person</param>
    /// <returns>The messages, the oldest first</returns>
    public ValueTask<IReadOnlyCollection<ChatMessage>> GetConversationAsync(int personId, int otherPersonId);

    /// <summary>
    ///     Gets a message including both persons and the shared horse or measurement group
    /// </summary>
    /// <param name="messageId">The id of the message</param>
    /// <returns>The tracked message or null if it does not exist</returns>
    public ValueTask<ChatMessage?> GetMessageByIdAsync(int messageId);

    /// <summary>
    ///     Gets the unread messages a person received from another person
    /// </summary>
    /// <param name="recipientId">The id of the recipient</param>
    /// <param name="senderId">The id of the sender</param>
    /// <returns>The tracked unread messages</returns>
    public ValueTask<IReadOnlyCollection<ChatMessage>> GetUnreadMessagesAsync(int recipientId, int senderId);

    /// <summary>
    ///     Gets all messages a person received that are not delivered yet
    /// </summary>
    /// <param name="recipientId">The id of the recipient</param>
    /// <returns>The tracked undelivered messages</returns>
    public ValueTask<IReadOnlyCollection<ChatMessage>> GetUndeliveredMessagesAsync(int recipientId);

    /// <summary>
    ///     Gets the content of the file of a message
    /// </summary>
    /// <param name="messageId">The id of the message</param>
    /// <returns>The file or null if the message has no file</returns>
    public ValueTask<ChatFile?> GetFileAsync(int messageId);

    /// <summary>
    ///     Adds a new message, a file of the message is added as well
    /// </summary>
    /// <param name="message">The message to add</param>
    public void AddMessage(ChatMessage message);
}

internal sealed class ChatRepository(DbSet<ChatMessage> messageSet, DbSet<ChatFile> fileSet) : IChatRepository
{
    private IQueryable<ChatMessage> MessagesWithDetails =>
        messageSet.Include(m => m.Sender)
                  .Include(m => m.Recipient)
                  .Include(m => m.Horse)
                  .Include(m => m.MeasurementGroup)
                  .ThenInclude(mg => mg!.Horse);

    public async ValueTask<IReadOnlyCollection<ChatMessage>> GetMessagesOfPersonAsync(int personId) =>
        await MessagesWithDetails.AsNoTracking()
                                 .Where(m => m.SenderId == personId || m.RecipientId == personId)
                                 .OrderByDescending(m => m.SentAt)
                                 .ThenByDescending(m => m.Id)
                                 .ToListAsync();

    public async ValueTask<IReadOnlyCollection<ChatMessage>> GetConversationAsync(int personId, int otherPersonId) =>
        await MessagesWithDetails.AsNoTracking()
                                 .Where(m => (m.SenderId == personId && m.RecipientId == otherPersonId)
                                             || (m.SenderId == otherPersonId && m.RecipientId == personId))
                                 .OrderBy(m => m.SentAt)
                                 .ThenBy(m => m.Id)
                                 .ToListAsync();

    public async ValueTask<ChatMessage?> GetMessageByIdAsync(int messageId) =>
        await MessagesWithDetails.FirstOrDefaultAsync(m => m.Id == messageId);

    public async ValueTask<IReadOnlyCollection<ChatMessage>> GetUnreadMessagesAsync(int recipientId, int senderId) =>
        await messageSet.Where(m => m.RecipientId == recipientId && m.SenderId == senderId && m.ReadAt == null)
                        .ToListAsync();

    public async ValueTask<IReadOnlyCollection<ChatMessage>> GetUndeliveredMessagesAsync(int recipientId) =>
        await messageSet.Where(m => m.RecipientId == recipientId && m.DeliveredAt == null)
                        .ToListAsync();

    public async ValueTask<ChatFile?> GetFileAsync(int messageId) =>
        await fileSet.AsNoTracking().FirstOrDefaultAsync(f => f.MessageId == messageId);

    public void AddMessage(ChatMessage message)
    {
        messageSet.Add(message);
    }
}
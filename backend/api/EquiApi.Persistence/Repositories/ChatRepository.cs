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
    ///     Checks if a person is the owner or a rider of a horse
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>True if the person belongs to the horse, false otherwise</returns>
    public ValueTask<bool> IsPersonOfHorseAsync(int personId, int horseId);

    /// <summary>
    ///     Gets the ids of all measurement groups of a horse
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The ids of the measurement groups</returns>
    public ValueTask<IReadOnlyCollection<int>> GetGroupIdsOfHorseAsync(int horseId);

    /// <summary>
    ///     Adds a new message
    /// </summary>
    /// <param name="message">The message to add</param>
    public void AddMessage(ChatMessage message);

    /// <summary>
    ///     Adds a new release of measurement groups
    /// </summary>
    /// <param name="release">The release to add</param>
    public void AddRelease(Release release);
}

internal sealed class ChatRepository(
    DbSet<ChatMessage> messageSet,
    DbSet<PersonHorse> personHorseSet,
    DbSet<MeasurementGroup> groupSet,
    DbSet<Release> releaseSet) : IChatRepository
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

    public async ValueTask<bool> IsPersonOfHorseAsync(int personId, int horseId) =>
        await personHorseSet.AnyAsync(ph => ph.PersonId == personId && ph.HorseId == horseId);

    public async ValueTask<IReadOnlyCollection<int>> GetGroupIdsOfHorseAsync(int horseId) =>
        await groupSet.Where(mg => mg.HorseId == horseId)
                      .Select(mg => mg.Id)
                      .ToListAsync();

    public void AddMessage(ChatMessage message)
    {
        messageSet.Add(message);
    }

    public void AddRelease(Release release)
    {
        releaseSet.Add(release);
    }
}

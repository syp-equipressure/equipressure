using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using FluentValidation;

namespace EquiApi.Dtos;

/// <summary>
///     A horse shared in a chat message
/// </summary>
/// <param name="Id">The id of the horse</param>
/// <param name="Name">The name of the horse</param>
public sealed record SharedHorseDto(int Id, string Name);

/// <summary>
///     A measurement group shared in a chat message
/// </summary>
/// <param name="Id">The id of the measurement group</param>
/// <param name="Name">The name of the measurement group</param>
/// <param name="Date">The date of the measurement group</param>
/// <param name="HorseId">The id of the measured horse</param>
/// <param name="HorseName">The name of the measured horse</param>
public sealed record SharedMeasurementDto(int Id, string Name, LocalDate Date, int HorseId, string HorseName);

/// <summary>
///     A file sent in a chat message, the content is loaded separately
/// </summary>
/// <param name="FileName">The original name of the file</param>
/// <param name="ContentType">The media type of the file, images start with image/</param>
/// <param name="Size">The size of the file in bytes</param>
public sealed record ChatFileDto(string FileName, string ContentType, long Size);

/// <summary>
///     A chat message
/// </summary>
/// <param name="Id">The id of the message</param>
/// <param name="Type">Whether the message is a text, a shared horse or a shared measurement</param>
/// <param name="Text">The text, optional for shared horses and measurements</param>
/// <param name="SentAt">When the message was sent</param>
/// <param name="ReadAt">When the recipient read the message, null if not read yet</param>
/// <param name="AcceptedAt">When the recipient added the shared horse or measurement, null if not added yet</param>
/// <param name="SenderId">The id of the sender</param>
/// <param name="RecipientId">The id of the recipient</param>
/// <param name="Horse">The shared horse, only for shared horses</param>
/// <param name="Measurement">The shared measurement group, only for shared measurements</param>
/// <param name="File">The sent file, only for files and images</param>
public sealed record ChatMessageDto(
    int Id,
    ChatMessageType Type,
    string? Text,
    Instant SentAt,
    Instant? ReadAt,
    Instant? AcceptedAt,
    int SenderId,
    int RecipientId,
    SharedHorseDto? Horse,
    SharedMeasurementDto? Measurement,
    ChatFileDto? File)
{
    public static ChatMessageDto FromMessage(ChatMessage message) =>
        new(message.Id,
            message.Type,
            message.Text,
            message.SentAt,
            message.ReadAt,
            message.AcceptedAt,
            message.SenderId,
            message.RecipientId,
            message.Horse is null ? null : new SharedHorseDto(message.Horse.Id, message.Horse.Name),
            message.MeasurementGroup is null
                ? null
                : new SharedMeasurementDto(message.MeasurementGroup.Id, message.MeasurementGroup.Name,
                                           message.MeasurementGroup.Date, message.MeasurementGroup.HorseId,
                                           message.MeasurementGroup.Horse.Name),
            message.FileName is null
                ? null
                : new ChatFileDto(message.FileName, message.FileContentType ?? "application/octet-stream",
                                  message.FileSize ?? 0));
}

/// <summary>
///     The messages of a conversation
/// </summary>
/// <param name="Messages">The messages, the oldest first</param>
public sealed record ChatMessageListResponse(IReadOnlyList<ChatMessageDto> Messages)
{
    public static ChatMessageListResponse FromMessages(IEnumerable<ChatMessage> messages) =>
        new(messages.Select(ChatMessageDto.FromMessage).ToList());
}

/// <summary>
///     A conversation with another person
/// </summary>
/// <param name="Contact">The other person</param>
/// <param name="LastMessage">The newest message</param>
/// <param name="UnreadCount">The number of received messages that are not read yet</param>
public sealed record ConversationDto(PersonDto Contact, ChatMessageDto LastMessage, int UnreadCount)
{
    public static ConversationDto FromConversation(ConversationData conversation) =>
        new(PersonDto.FromPerson(conversation.Contact), ChatMessageDto.FromMessage(conversation.LastMessage),
            conversation.UnreadCount);
}

/// <summary>
///     All conversations of a person
/// </summary>
/// <param name="Conversations">The conversations, the newest first</param>
public sealed record ConversationListResponse(IReadOnlyList<ConversationDto> Conversations)
{
    public static ConversationListResponse FromConversations(IEnumerable<ConversationData> conversations) =>
        new(conversations.Select(ConversationDto.FromConversation).ToList());
}

/// <summary>
///     An event sent over the chat websocket
/// </summary>
/// <param name="Type">"message" for a new message, "read" if messages were read, "accepted" if a share was added</param>
/// <param name="Message">The new or accepted message</param>
/// <param name="ReaderId">The person who read the messages, only for "read"</param>
/// <param name="ContactId">The other person of the conversation whose messages were read, only for "read"</param>
public sealed record ChatEventDto(string Type, ChatMessageDto? Message, int? ReaderId, int? ContactId)
{
    public const string MessageType = "message";
    public const string ReadType = "read";
    public const string AcceptedType = "accepted";

    public static ChatEventDto NewMessage(ChatMessage message) =>
        new(MessageType, ChatMessageDto.FromMessage(message), null, null);

    public static ChatEventDto Accepted(ChatMessage message) =>
        new(AcceptedType, ChatMessageDto.FromMessage(message), null, null);

    public static ChatEventDto Read(int readerId, int contactId) => new(ReadType, null, readerId, contactId);
}

/// <summary>
///     Request to send a chat message
/// </summary>
/// <param name="Type">Whether the message is a text, a shared horse or a shared measurement</param>
/// <param name="Text">The text, required for text messages and optional otherwise</param>
/// <param name="HorseId">The id of the shared horse, required for shared horses</param>
/// <param name="MeasurementGroupId">The id of the shared measurement group, required for shared measurements</param>
public sealed record SendChatMessageRequest(
    ChatMessageType Type,
    string? Text,
    int? HorseId,
    int? MeasurementGroupId)
{
    public NewChatMessageData ToData(int senderId, int recipientId) =>
        new(senderId, recipientId, Type, Text, HorseId, MeasurementGroupId);

    public sealed class Validator : AbstractValidator<SendChatMessageRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Type).IsInEnum();
            // files are uploaded with their own endpoint
            RuleFor(x => x.Type).NotEqual(ChatMessageType.File);
            RuleFor(x => x.Text).MaximumLength(2000).When(x => x.Text is not null);
            RuleFor(x => x.Text).NotEmpty().When(x => x.Type == ChatMessageType.Text);
            RuleFor(x => x.HorseId).NotNull().GreaterThan(0).When(x => x.Type == ChatMessageType.Horse);
            RuleFor(x => x.MeasurementGroupId).NotNull().GreaterThan(0)
                                              .When(x => x.Type == ChatMessageType.Measurement);
        }
    }
}
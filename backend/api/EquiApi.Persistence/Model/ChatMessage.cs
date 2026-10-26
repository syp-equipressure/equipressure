namespace EquiApi.Persistence.Model;

public class ChatMessage
{
    public int Id { get; set; }
    public ChatMessageType Type { get; set; }

    // optional for shared horses and measurements, required for text messages
    public string? Text { get; set; }
    public Instant SentAt { get; set; }

    // set as soon as the recipient opened the conversation
    public Instant? ReadAt { get; set; }

    // set as soon as the recipient added the shared horse or measurement
    public Instant? AcceptedAt { get; set; }

    public int SenderId { get; set; }
    public Person Sender { get; set; } = null!;

    public int RecipientId { get; set; }
    public Person Recipient { get; set; } = null!;

    // only for shared horses
    public int? HorseId { get; set; }
    public Horse? Horse { get; set; }

    // only for shared measurements
    public int? MeasurementGroupId { get; set; }
    public MeasurementGroup? MeasurementGroup { get; set; }

    // only for files and images, the content is stored separately so that lists of messages stay small
    public string? FileName { get; set; }
    public string? FileContentType { get; set; }
    public long? FileSize { get; set; }
    public ChatFile? File { get; set; }
}

/// <summary>
///     The content of a file sent in a chat message
/// </summary>
public class ChatFile
{
    public int MessageId { get; set; }
    public ChatMessage Message { get; set; } = null!;

    public required byte[] Data { get; set; }
}

public enum ChatMessageType
{
    Text = 10,
    Horse = 20,
    Measurement = 30,
    File = 40
}
using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Test.Util;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OneOf.Types;

namespace EquiApi.Test;

public sealed class ChatServiceTests
{
    private const int EquestrianId = 1;
    private const int SaddlerId = 2;

    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = TestData.CreateClock();
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _sut = new ChatService(_uowMock, _clock, NullLogger<ChatService>.Instance);
        _uowMock.PersonRepository.PersonExistsAsync(Arg.Any<int>()).Returns(true);
    }

    [Fact]
    public async Task GetConversationsAsync_MessagesWithTwoPersons_ReturnsOneConversationPerPersonWithUnreadCount()
    {
        var equestrian = TestData.CreatePerson(EquestrianId);
        var saddler = TestData.CreatePerson(SaddlerId, "Sophie");
        var other = TestData.CreatePerson(3, "Anna");
        // newest first, like the repository returns them
        _uowMock.ChatRepository.GetMessagesOfPersonAsync(SaddlerId).Returns(
        [
            CreateMessage(3, other, saddler),
            CreateMessage(2, equestrian, saddler),
            CreateMessage(1, equestrian, saddler)
        ]);

        var result = await _sut.GetConversationsAsync(SaddlerId);

        var conversations = result.Value.Should().BeOfType<Success<IReadOnlyCollection<ConversationData>>>()
                                  .Which.Value;
        conversations.Select(c => c.Contact.Id).Should().Equal(3, EquestrianId);
        conversations.Select(c => c.LastMessage.Id).Should().Equal(3, 2);
        conversations.Select(c => c.UnreadCount).Should().Equal(1, 2);
    }

    [Fact]
    public async Task GetConversationsAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.PersonExistsAsync(EquestrianId).Returns(false);

        var result = await _sut.GetConversationsAsync(EquestrianId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task SendMessageAsync_ToOneself_ReturnsInvalidMessage()
    {
        var result = await _sut.SendMessageAsync(CreateData(EquestrianId, EquestrianId));

        result.Value.Should().BeOfType<IChatService.InvalidMessage>();
        _uowMock.ChatRepository.DidNotReceive().AddMessage(Arg.Any<ChatMessage>());
    }

    [Fact]
    public async Task SendMessageAsync_Text_AddsTrimmedMessage()
    {
        var result = await _sut.SendMessageAsync(CreateData(EquestrianId, SaddlerId, text: "  Hallo!  "));

        result.Value.Should().BeOfType<Success<ChatMessage>>();
        _uowMock.ChatRepository.Received(1).AddMessage(Arg.Is<ChatMessage>(m => m.Text == "Hallo!"
                                                                              && m.SenderId == EquestrianId
                                                                              && m.RecipientId == SaddlerId
                                                                              && m.SentAt
                                                                              == _clock.GetCurrentInstant()));
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task SendMessageAsync_HorseOfSomebodyElse_ReturnsInvalidMessage()
    {
        _uowMock.HorseRepository.HorseExistsAsync(1).Returns(true);
        _uowMock.HorseRepository.IsPersonOfHorseAsync(EquestrianId, 1).Returns(false);

        var result = await _sut.SendMessageAsync(CreateData(EquestrianId, SaddlerId, ChatMessageType.Horse,
                                                            horseId: 1));

        result.Value.Should().BeOfType<IChatService.InvalidMessage>();
        _uowMock.ChatRepository.DidNotReceive().AddMessage(Arg.Any<ChatMessage>());
    }

    [Fact]
    public async Task SendMessageAsync_MeasurementTheSenderMayShare_AddsMessageWithGroupOnly()
    {
        _uowMock.MeasurementRepository.GetGroupByIdAsync(1).Returns(TestData.CreateMeasurementGroup());
        _uowMock.MeasurementRepository.CanShareGroupAsync(SaddlerId, 1).Returns(true);
        // the recipient owns the measured horse
        _uowMock.HorseRepository.IsPersonOfHorseAsync(EquestrianId, 1).Returns(true);

        var result = await _sut.SendMessageAsync(CreateData(SaddlerId, EquestrianId, ChatMessageType.Measurement,
                                                            groupId: 1));

        result.Value.Should().BeOfType<Success<ChatMessage>>();
        _uowMock.ChatRepository.Received(1).AddMessage(Arg.Is<ChatMessage>(m => m.MeasurementGroupId == 1
                                                                              && m.HorseId == null));
    }

    [Fact]
    public async Task SendMessageAsync_MeasurementOfAnotherCustomer_ReturnsInvalidMessage()
    {
        // the saddler measured the horse, but it belongs neither to the saddler nor to the recipient
        _uowMock.MeasurementRepository.GetGroupByIdAsync(1).Returns(TestData.CreateMeasurementGroup());
        _uowMock.MeasurementRepository.CanShareGroupAsync(SaddlerId, 1).Returns(true);
        _uowMock.HorseRepository.IsPersonOfHorseAsync(Arg.Any<int>(), 1).Returns(false);

        var result = await _sut.SendMessageAsync(CreateData(SaddlerId, EquestrianId, ChatMessageType.Measurement,
                                                            groupId: 1));

        result.Value.Should().BeOfType<IChatService.InvalidMessage>();
        _uowMock.ChatRepository.DidNotReceive().AddMessage(Arg.Any<ChatMessage>());
    }

    [Fact]
    public async Task SendMessageAsync_MeasurementTheSenderMayNotShare_ReturnsInvalidMessage()
    {
        _uowMock.MeasurementRepository.GetGroupByIdAsync(1).Returns(TestData.CreateMeasurementGroup());
        _uowMock.MeasurementRepository.CanShareGroupAsync(SaddlerId, 1).Returns(false);

        var result = await _sut.SendMessageAsync(CreateData(SaddlerId, EquestrianId, ChatMessageType.Measurement,
                                                            groupId: 1));

        result.Value.Should().BeOfType<IChatService.InvalidMessage>();
        _uowMock.ChatRepository.DidNotReceive().AddMessage(Arg.Any<ChatMessage>());
    }

    [Fact]
    public async Task SendMessageAsync_MeasurementNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GetGroupByIdAsync(1).Returns((MeasurementGroup?) null);

        var result = await _sut.SendMessageAsync(CreateData(EquestrianId, SaddlerId, ChatMessageType.Measurement,
                                                            groupId: 1));

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task MarkAsReadAsync_UnreadMessages_SetsReadAtAndReturnsCount()
    {
        var messages = new List<ChatMessage>
        {
            CreateMessage(1, TestData.CreatePerson(EquestrianId), TestData.CreatePerson(SaddlerId)),
            CreateMessage(2, TestData.CreatePerson(EquestrianId), TestData.CreatePerson(SaddlerId))
        };
        _uowMock.ChatRepository.GetUnreadMessagesAsync(SaddlerId, EquestrianId).Returns(messages);

        var result = await _sut.MarkAsReadAsync(SaddlerId, EquestrianId);

        result.Value.Should().BeOfType<Success<int>>().Which.Value.Should().Be(2);
        messages.Should().OnlyContain(m => m.ReadAt == _clock.GetCurrentInstant()
                                           && m.DeliveredAt == _clock.GetCurrentInstant());
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task MarkAsDeliveredAsync_UndeliveredMessages_SetsDeliveredAtAndReturnsSenders()
    {
        var saddler = TestData.CreatePerson(SaddlerId);
        var messages = new List<ChatMessage>
        {
            CreateMessage(1, TestData.CreatePerson(EquestrianId), saddler),
            CreateMessage(2, TestData.CreatePerson(EquestrianId), saddler),
            CreateMessage(3, TestData.CreatePerson(3, "Anna"), saddler)
        };
        _uowMock.ChatRepository.GetUndeliveredMessagesAsync(SaddlerId).Returns(messages);

        var result = await _sut.MarkAsDeliveredAsync(SaddlerId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<int>>>()
              .Which.Value.Should().BeEquivalentTo([EquestrianId, 3]);
        messages.Should().OnlyContain(m => m.DeliveredAt == _clock.GetCurrentInstant() && m.ReadAt == null);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task MarkAsDeliveredAsync_NothingUndelivered_ReturnsNoSendersWithoutSaving()
    {
        _uowMock.ChatRepository.GetUndeliveredMessagesAsync(SaddlerId).Returns([]);

        var result = await _sut.MarkAsDeliveredAsync(SaddlerId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<int>>>().Which.Value.Should().BeEmpty();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task MarkAsDeliveredAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.PersonExistsAsync(SaddlerId).Returns(false);

        var result = await _sut.MarkAsDeliveredAsync(SaddlerId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task AcceptShareAsync_SharedMeasurement_AddsContactAndRelease()
    {
        var equestrian = TestData.CreatePerson(EquestrianId, role: RoleName.Equestrian);
        var saddler = TestData.CreatePerson(SaddlerId, "Sophie", role: RoleName.Saddler);
        var message = CreateMessage(1, equestrian, saddler, ChatMessageType.Measurement);
        message.MeasurementGroupId = 5;
        _uowMock.ChatRepository.GetMessageByIdAsync(1).Returns(message);
        _uowMock.PersonRepository.GetPersonByIdAsync(EquestrianId).Returns(equestrian);
        _uowMock.PersonRepository.GetPersonByIdAsync(SaddlerId).Returns(saddler);

        var result = await _sut.AcceptShareAsync(SaddlerId, 1);

        result.Value.Should().BeOfType<Success<ChatMessage>>();
        message.AcceptedAt.Should().Be(_clock.GetCurrentInstant());
        await _uowMock.PersonRepository.Received(1).MarkAsContactAsync(equestrian, saddler);
        _uowMock.MeasurementRepository.Received(1).AddRelease(Arg.Is<Release>(r =>
            r.PersonId == SaddlerId && r.MeasurementEligibilities.Single().MeasurementGroupId == 5));
    }

    [Fact]
    public async Task AcceptShareAsync_SharedHorseWasDeleted_ReturnsInvalidMessage()
    {
        // the reference is set to null when the horse is deleted
        var message = CreateMessage(1, TestData.CreatePerson(EquestrianId), TestData.CreatePerson(SaddlerId),
                                    ChatMessageType.Horse);
        _uowMock.ChatRepository.GetMessageByIdAsync(1).Returns(message);

        var result = await _sut.AcceptShareAsync(SaddlerId, 1);

        result.Value.Should().BeOfType<IChatService.InvalidMessage>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AcceptShareAsync_NotTheRecipient_ReturnsNotFound()
    {
        var message = CreateMessage(1, TestData.CreatePerson(EquestrianId), TestData.CreatePerson(SaddlerId),
                                    ChatMessageType.Horse);
        _uowMock.ChatRepository.GetMessageByIdAsync(1).Returns(message);

        var result = await _sut.AcceptShareAsync(EquestrianId, 1);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Theory]
    [InlineData(ChatMessageType.Text)]
    [InlineData(ChatMessageType.File)]
    public async Task AcceptShareAsync_NothingShared_ReturnsInvalidMessage(ChatMessageType type)
    {
        var message = CreateMessage(1, TestData.CreatePerson(EquestrianId), TestData.CreatePerson(SaddlerId), type);
        _uowMock.ChatRepository.GetMessageByIdAsync(1).Returns(message);

        var result = await _sut.AcceptShareAsync(SaddlerId, 1);

        result.Value.Should().BeOfType<IChatService.InvalidMessage>();
    }

    private static NewChatMessageData CreateData(int senderId, int recipientId,
                                                 ChatMessageType type = ChatMessageType.Text, string? text = "Hallo",
                                                 int? horseId = null, int? groupId = null) =>
        new(senderId, recipientId, type, text, horseId, groupId);

    private static ChatMessage CreateMessage(int id, Person sender, Person recipient,
                                             ChatMessageType type = ChatMessageType.Text) => new()
                                             {
                                                 Id = id,
                                                 Type = type,
                                                 Text = "Hallo",
                                                 SentAt = Instant.FromUtc(2026, 10, 20, 8, id),
                                                 Sender = sender,
                                                 SenderId = sender.Id,
                                                 Recipient = recipient,
                                                 RecipientId = recipient.Id
                                             };
}
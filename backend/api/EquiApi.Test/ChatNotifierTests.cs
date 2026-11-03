using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Test.Util;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace EquiApi.Test;

public sealed class ChatNotifierTests
{
    private const int RecipientId = 2;

    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly IPushSender _pushSender = Substitute.For<IPushSender>();
    private readonly ChatNotifier _sut;

    public ChatNotifierTests()
    {
        _sut = new ChatNotifier(_uowMock, _pushSender, NullLogger<ChatNotifier>.Instance);
    }

    private static ChatMessage CreateMessage(ChatMessageType type, string? text) => new()
    {
        Type = type,
        Text = text,
        SenderId = 1,
        Sender = TestData.CreatePerson(1, "Sophie", "Grüneis"),
        RecipientId = RecipientId
    };

    [Fact]
    public async Task NotifyNewMessageAsync_NoDevices_SendsNothing()
    {
        _uowMock.PersonRepository.GetPushTokensOfPersonAsync(RecipientId).Returns([]);

        await _sut.NotifyNewMessageAsync(CreateMessage(ChatMessageType.Text, "Hallo"));

        await _pushSender.DidNotReceiveWithAnyArgs().SendAsync(null!, null!);
    }

    [Fact]
    public async Task NotifyNewMessageAsync_TextMessage_SendsNameAndText()
    {
        _uowMock.PersonRepository.GetPushTokensOfPersonAsync(RecipientId).Returns(["token"]);
        _pushSender.SendAsync(default!, default!).ReturnsForAnyArgs([]);

        await _sut.NotifyNewMessageAsync(CreateMessage(ChatMessageType.Text, "Hallo"));

        await _pushSender.Received(1).SendAsync(
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Is<PushMessage>(m => m.Title == "Sophie Grüneis"
                                     && m.Body == "Hallo"
                                     && m.Data["contactId"] == "1"));
    }

    [Fact]
    public async Task NotifyNewMessageAsync_SharedHorse_DescribesTheMessage()
    {
        _uowMock.PersonRepository.GetPushTokensOfPersonAsync(RecipientId).Returns(["token"]);
        _pushSender.SendAsync(default!, default!).ReturnsForAnyArgs([]);

        await _sut.NotifyNewMessageAsync(CreateMessage(ChatMessageType.Horse, null));

        await _pushSender.Received(1).SendAsync(Arg.Any<IReadOnlyCollection<string>>(),
                                                Arg.Is<PushMessage>(m => m.Body == "Hat ein Pferd geteilt"));
    }

    [Fact]
    public async Task NotifyNewMessageAsync_DeviceIsGone_RemovesItsToken()
    {
        _uowMock.PersonRepository.GetPushTokensOfPersonAsync(RecipientId).Returns(["old", "new"]);
        _pushSender.SendAsync(default!, default!).ReturnsForAnyArgs(["old"]);

        await _sut.NotifyNewMessageAsync(CreateMessage(ChatMessageType.Text, "Hallo"));

        await _uowMock.PersonRepository.Received(1).RemovePushTokensAsync(
            Arg.Is<IReadOnlyCollection<string>>(tokens => tokens.Single() == "old"));
    }

    [Fact]
    public async Task NotifyNewMessageAsync_SendingFails_DoesNotThrow()
    {
        _uowMock.PersonRepository.GetPushTokensOfPersonAsync(RecipientId).Returns(["token"]);
        _pushSender.SendAsync(default!, default!).ThrowsAsyncForAnyArgs(new InvalidOperationException("offline"));

        var act = async () => await _sut.NotifyNewMessageAsync(CreateMessage(ChatMessageType.Text, "Hallo"));

        await act.Should().NotThrowAsync();
    }
}
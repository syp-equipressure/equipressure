using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.TestInt;

public sealed class ChatTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    [Fact]
    public async ValueTask SendMessage_Text_ReturnsCreatedMessageAndShowsUpInBothConversations()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();
        var request = new SendChatMessageRequest(ChatMessageType.Text, "Hallo!", null, null);

        var response = await ApiClient.PostAsJsonAsync($"api/persons/{equestrian.Id}/chats/{saddler.Id}", request,
                                                       JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var message = await ReadContentAsync<ChatMessageDto>(response);
        message.Text.Should().Be("Hallo!");
        message.SenderId.Should().Be(equestrian.Id);
        message.RecipientId.Should().Be(saddler.Id);
        message.ReadAt.Should().BeNull();

        var conversations = await ReadContentAsync<ConversationListResponse>(
            await ApiClient.GetAsync($"api/persons/{saddler.Id}/chats", TestCancellationToken));
        var conversation = conversations.Conversations.Should().ContainSingle().Which;
        conversation.Contact.Id.Should().Be(equestrian.Id);
        conversation.LastMessage.Id.Should().Be(message.Id);
        conversation.UnreadCount.Should().Be(1);

        var messages = await ReadContentAsync<ChatMessageListResponse>(
            await ApiClient.GetAsync($"api/persons/{equestrian.Id}/chats/{saddler.Id}", TestCancellationToken));
        messages.Messages.Select(m => m.Id).Should().Equal(message.Id);
    }

    [Fact]
    public async ValueTask SendMessage_EmptyText_ReturnsBadRequest()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();
        var request = new SendChatMessageRequest(ChatMessageType.Text, " ", null, null);

        var response = await ApiClient.PostAsJsonAsync($"api/persons/{equestrian.Id}/chats/{saddler.Id}", request,
                                                       JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask SendMessage_HorseOfSomebodyElse_ReturnsConflict()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();
        var horse = TestData.CreateHorse();
        await SeedAsync(horse);
        var request = new SendChatMessageRequest(ChatMessageType.Horse, null, horse.Id, null);

        var response = await ApiClient.PostAsJsonAsync($"api/persons/{equestrian.Id}/chats/{saddler.Id}", request,
                                                       JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask SendMessage_UnknownRecipient_ReturnsNotFound()
    {
        var (equestrian, _) = await SeedEquestrianAndSaddlerAsync();
        var request = new SendChatMessageRequest(ChatMessageType.Text, "Hallo!", null, null);

        var response = await ApiClient.PostAsJsonAsync($"api/persons/{equestrian.Id}/chats/{TestData.NonExistingId}",
                                                       request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask MarkAsRead_UnreadMessages_SetsReadAt()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();
        await SendTextAsync(equestrian.Id, saddler.Id, "Hallo!");

        var response = await ApiClient.PutAsync($"api/persons/{saddler.Id}/chats/{equestrian.Id}/read", null,
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var conversations = await ReadContentAsync<ConversationListResponse>(
            await ApiClient.GetAsync($"api/persons/{saddler.Id}/chats", TestCancellationToken));
        conversations.Conversations.Single().UnreadCount.Should().Be(0);
        conversations.Conversations.Single().LastMessage.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async ValueTask AcceptShare_SharedHorse_AddsContactAndReleasesAllMeasurements()
    {
        // everything in one seed, entities that are already saved would be added a second time
        var equestrian = TestData.CreatePerson("Lena", "Grassauer");
        var saddler = TestData.CreatePerson("Sophie", "Grüneis", role: RoleName.Saddler);
        var horse = TestData.CreateHorse();
        var saddle = TestData.CreateSaddle(horse);
        var device = TestData.CreateDevice("EP-CHAT", equestrian, TestData.CreateDeviceCategory());
        await SeedAsync(saddler, TestData.CreatePersonHorse(equestrian, horse, isOwner: true), saddle, device,
                        TestData.CreateMeasurementGroup(equestrian, horse, saddle, device),
                        TestData.CreateMeasurementGroup(equestrian, horse, saddle, device, "Springtraining"));
        var shared = await ReadContentAsync<ChatMessageDto>(
            await ApiClient.PostAsJsonAsync($"api/persons/{equestrian.Id}/chats/{saddler.Id}",
                                            new SendChatMessageRequest(ChatMessageType.Horse, null, horse.Id, null),
                                            JsonOptions, TestCancellationToken));
        shared.Horse.Should().Be(new SharedHorseDto(horse.Id, horse.Name));

        var response = await ApiClient.PutAsync($"api/persons/{saddler.Id}/chats/messages/{shared.Id}/accept", null,
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accepted = await ReadContentAsync<ChatMessageDto>(response);
        accepted.AcceptedAt.Should().NotBeNull();
        var contacts = await ReadContentAsync<ContactListResponse>(
            await ApiClient.GetAsync($"api/persons/{saddler.Id}/contacts", TestCancellationToken));
        contacts.Contacts.Select(c => c.Id).Should().Equal(equestrian.Id);
        await ModifyDatabaseContentAsync(async context =>
        {
            var release = await context.Releases.Include(r => r.MeasurementEligibilities)
                                       .SingleAsync(TestCancellationToken);
            release.PersonId.Should().Be(saddler.Id);
            release.MeasurementEligibilities.Should().HaveCount(2);
        });
    }

    [Fact]
    public async ValueTask SendFile_Image_CanBeDownloadedByBothPersonsOnly()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();
        var other = TestData.CreatePerson("Max", "Muster");
        await SeedAsync(other);
        byte[] image = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];
        using var form = new MultipartFormDataContent();
        // no media type, it is derived from the file name
        form.Add(new ByteArrayContent(image), "file", "sattel.png");
        form.Add(new StringContent("Hier das Foto"), "text");

        var response = await ApiClient.PostAsync($"api/persons/{equestrian.Id}/chats/{saddler.Id}/files", form,
                                                 TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var message = await ReadContentAsync<ChatMessageDto>(response);
        message.Type.Should().Be(ChatMessageType.File);
        message.Text.Should().Be("Hier das Foto");
        message.File.Should().Be(new ChatFileDto("sattel.png", "image/png", image.Length));

        var download = await ApiClient.GetAsync($"api/persons/{saddler.Id}/chats/messages/{message.Id}/file",
                                                TestCancellationToken);
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await download.Content.ReadAsByteArrayAsync(TestCancellationToken)).Should().Equal(image);

        var foreign = await ApiClient.GetAsync($"api/persons/{other.Id}/chats/messages/{message.Id}/file",
                                               TestCancellationToken);
        ShouldBeProblem(foreign, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask SendMessage_FileTypeWithoutUpload_ReturnsBadRequest()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();

        var response = await ApiClient.PostAsJsonAsync($"api/persons/{equestrian.Id}/chats/{saddler.Id}",
                                                       new SendChatMessageRequest(ChatMessageType.File, "x", null,
                                                                                  null),
                                                       JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask SendMessage_MeasurementOfTheSaddlersDevice_ReturnsCreatedMessage()
    {
        var equestrian = TestData.CreatePerson("Lena", "Grassauer");
        var saddler = TestData.CreatePerson("Sophie", "Grüneis", role: RoleName.Saddler);
        var horse = TestData.CreateHorse();
        var saddle = TestData.CreateSaddle(horse);
        // the saddler measured with her own device, she is no person of the horse
        var device = TestData.CreateDevice("EP-SADDLER", saddler, TestData.CreateDeviceCategory());
        var group = TestData.CreateMeasurementGroup(equestrian, horse, saddle, device);
        await SeedAsync(TestData.CreatePersonHorse(equestrian, horse, isOwner: true), saddle, group);

        var response = await ApiClient.PostAsJsonAsync($"api/persons/{saddler.Id}/chats/{equestrian.Id}",
                                                       new SendChatMessageRequest(ChatMessageType.Measurement,
                                                                                  "Deine Messung", null, group.Id),
                                                       JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var message = await ReadContentAsync<ChatMessageDto>(response);
        message.Measurement.Should().NotBeNull();
        message.Measurement.Id.Should().Be(group.Id);
        message.Measurement.HorseName.Should().Be(horse.Name);
    }

    [Fact]
    public async ValueTask SendMessage_MeasurementOfSomebodyElse_ReturnsConflict()
    {
        var equestrian = TestData.CreatePerson("Lena", "Grassauer");
        var saddler = TestData.CreatePerson("Sophie", "Grüneis", role: RoleName.Saddler);
        var horse = TestData.CreateHorse();
        var saddle = TestData.CreateSaddle(horse);
        var device = TestData.CreateDevice("EP-OTHER", equestrian, TestData.CreateDeviceCategory());
        var group = TestData.CreateMeasurementGroup(equestrian, horse, saddle, device);
        await SeedAsync(saddler, TestData.CreatePersonHorse(equestrian, horse, isOwner: true), saddle, group);

        var response = await ApiClient.PostAsJsonAsync($"api/persons/{saddler.Id}/chats/{equestrian.Id}",
                                                       new SendChatMessageRequest(ChatMessageType.Measurement, null,
                                                                                  null, group.Id),
                                                       JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask AcceptShare_MessageOfSomebodyElse_ReturnsNotFound()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();
        var message = await SendTextAsync(equestrian.Id, saddler.Id, "Hallo!");

        var response = await ApiClient.PutAsync($"api/persons/{equestrian.Id}/chats/messages/{message.Id}/accept",
                                                null, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AcceptShare_TextMessage_ReturnsConflict()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();
        var message = await SendTextAsync(equestrian.Id, saddler.Id, "Hallo!");

        var response = await ApiClient.PutAsync($"api/persons/{saddler.Id}/chats/messages/{message.Id}/accept", null,
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask Connect_NewMessage_PushesMessageToRecipient()
    {
        var (equestrian, saddler) = await SeedEquestrianAndSaddlerAsync();
        var socket = await CreateWebSocketClient()
                         .ConnectAsync(new Uri($"ws://localhost/api/persons/{saddler.Id}/chats/ws"),
                                       TestCancellationToken);

        var sent = await SendTextAsync(equestrian.Id, saddler.Id, "Hallo über den Websocket!");

        var buffer = new byte[8192];
        var result = await socket.ReceiveAsync(buffer, TestCancellationToken);
        var chatEvent = JsonSerializer.Deserialize<ChatEventDto>(buffer.AsSpan(0, result.Count), JsonOptions);
        chatEvent.Should().NotBeNull();
        chatEvent.Type.Should().Be(ChatEventDto.MessageType);
        chatEvent.Message.Should().NotBeNull();
        chatEvent.Message.Id.Should().Be(sent.Id);
        chatEvent.Message.Text.Should().Be("Hallo über den Websocket!");

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, TestCancellationToken);
    }

    [Fact]
    public async ValueTask Connect_NoWebSocketRequest_ReturnsBadRequest()
    {
        var (equestrian, _) = await SeedEquestrianAndSaddlerAsync();

        var response = await ApiClient.GetAsync($"api/persons/{equestrian.Id}/chats/ws", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    private async ValueTask<(Person Equestrian, Person Saddler)> SeedEquestrianAndSaddlerAsync()
    {
        var equestrian = TestData.CreatePerson("Lena", "Grassauer");
        var saddler = TestData.CreatePerson("Sophie", "Grüneis", role: RoleName.Saddler);
        await SeedAsync(equestrian, saddler);

        return (equestrian, saddler);
    }

    private async ValueTask<ChatMessageDto> SendTextAsync(int senderId, int recipientId, string text)
    {
        var response = await ApiClient.PostAsJsonAsync($"api/persons/{senderId}/chats/{recipientId}",
                                                       new SendChatMessageRequest(ChatMessageType.Text, text, null,
                                                                                  null),
                                                       JsonOptions, TestCancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return await ReadContentAsync<ChatMessageDto>(response);
    }
}

using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace EquiApi.Controllers;

[Route("api/persons/{personId:int}/chats")]
public sealed class ChatController(
    IChatService chatService,
    IPersonService personService,
    IChatSocketManager socketManager) : BaseController
{
    private const string UnknownContentType = "application/octet-stream";
    private static readonly FileExtensionContentTypeProvider contentTypes = new();

    // some clients (e.g. the app) do not send a media type, then it is derived from the file extension
    private static string ContentTypeOf(IFormFile file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType) && file.ContentType != UnknownContentType)
        {
            return file.ContentType;
        }

        return contentTypes.TryGetContentType(file.FileName, out string? contentType) ? contentType : UnknownContentType;
    }

    /// <summary>
    ///     Gets all conversations of a person, the newest first
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ConversationListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<ConversationListResponse>> GetConversations([FromRoute][ValidId] int personId)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        var result = await chatService.GetConversationsAsync(personId);

        return result.Match<ActionResult<ConversationListResponse>>(
            success => Ok(ConversationListResponse.FromConversations(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets all messages between a person and another person, the oldest first
    /// </summary>
    [HttpGet("{contactId:int}")]
    [ProducesResponseType<ChatMessageListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<ChatMessageListResponse>> GetMessages([FromRoute][ValidId] int personId,
                                                                              [FromRoute][ValidId] int contactId)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        var result = await chatService.GetConversationAsync(personId, contactId);

        return result.Match<ActionResult<ChatMessageListResponse>>(
            success => Ok(ChatMessageListResponse.FromMessages(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Sends a message (a text, a horse or a measurement) to another person.
    ///     Both persons are notified over the chat websocket.
    /// </summary>
    [HttpPost("{contactId:int}")]
    [ProducesResponseType<ChatMessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<ChatMessageDto>> SendMessage([FromRoute][ValidId] int personId,
                                                                     [FromRoute][ValidId] int contactId,
                                                                     [FromBody] SendChatMessageRequest request)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        if (!IsValid<SendChatMessageRequest.Validator, SendChatMessageRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await chatService.SendMessageAsync(request.ToData(personId, contactId));
        if (result.TryPickT0(out var success, out var failure))
        {
            await socketManager.SendAsync([personId, contactId], ChatEventDto.NewMessage(success.Value));

            return CreatedAtAction(nameof(GetMessages), new { personId, contactId },
                                   ChatMessageDto.FromMessage(success.Value));
        }

        return failure.Match<ActionResult<ChatMessageDto>>(
            _ => NotFound(),
            invalid => Problem(statusCode: StatusCodes.Status409Conflict, detail: invalid.Reason));
    }

    /// <summary>
    ///     Sends a file (e.g. an image) with an optional text as multipart form data, at most 10 MB.
    ///     Both persons are notified over the chat websocket.
    /// </summary>
    [HttpPost("{contactId:int}/files")]
    // a bit more than the file itself, for the text and the multipart boundaries
    [RequestSizeLimit(SendChatFileRequest.MaxFileSize + 64 * 1024)]
    [ProducesResponseType<ChatMessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<ChatMessageDto>> SendFile([FromRoute][ValidId] int personId,
                                                                  [FromRoute][ValidId] int contactId,
                                                                  [FromForm] SendChatFileRequest request)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        if (!IsValid<SendChatFileRequest.Validator, SendChatFileRequest>(request) || request.File is not { } file)
        {
            return ValidationProblem();
        }

        using var content = new MemoryStream();
        await file.CopyToAsync(content, HttpContext.RequestAborted);
        var data = new ChatFileData(file.FileName, ContentTypeOf(file), content.ToArray());

        var result = await chatService.SendFileAsync(personId, contactId, request.Text, data);
        if (result.TryPickT0(out var success, out var failure))
        {
            await socketManager.SendAsync([personId, contactId], ChatEventDto.NewMessage(success.Value));

            return CreatedAtAction(nameof(GetFile), new { personId, messageId = success.Value.Id },
                                   ChatMessageDto.FromMessage(success.Value));
        }

        return failure.Match<ActionResult<ChatMessageDto>>(
            _ => NotFound(),
            invalid => Problem(statusCode: StatusCodes.Status409Conflict, detail: invalid.Reason));
    }

    /// <summary>
    ///     Gets the content of a file sent in a message, only for the sender and the recipient.
    ///     The access token may be sent as query parameter <see cref="Setup.AccessTokenQueryKey" /> (e.g. for images).
    /// </summary>
    [HttpGet("messages/{messageId:int}/file")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> GetFile([FromRoute][ValidId] int personId,
                                                  [FromRoute][ValidId] int messageId)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        var result = await chatService.GetFileAsync(personId, messageId);

        return result.Match<IActionResult>(
            success => File(success.Value.Data, success.Value.ContentType, success.Value.FileName),
            _ => NotFound());
    }

    /// <summary>
    ///     Marks all messages the person received from the other person as read
    /// </summary>
    [HttpPut("{contactId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> MarkAsRead([FromRoute][ValidId] int personId,
                                                     [FromRoute][ValidId] int contactId)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        var result = await chatService.MarkAsReadAsync(personId, contactId);
        if (!result.TryPickT0(out var success, out _))
        {
            return NotFound();
        }

        if (success.Value > 0)
        {
            await socketManager.SendAsync([personId, contactId], ChatEventDto.Read(personId, contactId));
        }

        return NoContent();
    }

    /// <summary>
    ///     Marks all messages the person received as delivered (second tick). The apps call this as soon as they are
    ///     online and whenever a new message arrives, the senders are notified over the chat websocket.
    /// </summary>
    [HttpPut("delivered")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> MarkAsDelivered([FromRoute][ValidId] int personId)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        var result = await chatService.MarkAsDeliveredAsync(personId);
        if (!result.TryPickT0(out var success, out _))
        {
            return NotFound();
        }

        foreach (int senderId in success.Value)
        {
            await socketManager.SendAsync([personId, senderId], ChatEventDto.Delivered(personId, senderId));
        }

        return NoContent();
    }

    /// <summary>
    ///     Adds the horse or measurement shared in a received message: the sender becomes a contact
    ///     and the measurements are released to the person
    /// </summary>
    [HttpPut("messages/{messageId:int}/accept")]
    [ProducesResponseType<ChatMessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<ChatMessageDto>> AcceptShare([FromRoute][ValidId] int personId,
                                                                     [FromRoute][ValidId] int messageId)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        var result = await chatService.AcceptShareAsync(personId, messageId);
        if (result.TryPickT0(out var success, out var failure))
        {
            var message = success.Value;
            await socketManager.SendAsync([message.SenderId, message.RecipientId], ChatEventDto.Accepted(message));

            return Ok(ChatMessageDto.FromMessage(message));
        }

        return failure.Match<ActionResult<ChatMessageDto>>(
            _ => NotFound(),
            invalid => Problem(statusCode: StatusCodes.Status409Conflict, detail: invalid.Reason));
    }

    /// <summary>
    ///     Opens a websocket that receives the chat events (new, read and accepted messages) of the person.
    ///     Browsers cannot send a header for websockets, so the access token may be sent as query parameter
    ///     <see cref="Setup.AccessTokenQueryKey" />.
    /// </summary>
    [HttpGet("ws")]
    [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> Connect([FromRoute][ValidId] int personId)
    {
        if (!await IsCurrentPersonAsync(personId))
        {
            return AccessDenied();
        }

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Websocket request expected");
        }

        var person = await personService.GetPersonByIdAsync(personId);
        if (!person.IsT0)
        {
            return NotFound();
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await socketManager.HandleConnectionAsync(personId, socket, HttpContext.RequestAborted);

        return new EmptyResult();
    }
}
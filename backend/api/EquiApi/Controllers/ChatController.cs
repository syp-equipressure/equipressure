using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/persons/{personId:int}/chats")]
public sealed class ChatController(
    IChatService chatService,
    IPersonService personService,
    IChatSocketManager socketManager) : BaseController
{
    /// <summary>
    ///     Gets all conversations of a person, the newest first
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ConversationListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<ConversationListResponse>> GetConversations([FromRoute][ValidId] int personId)
    {
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
    ///     Marks all messages the person received from the other person as read
    /// </summary>
    [HttpPut("{contactId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> MarkAsRead([FromRoute][ValidId] int personId,
                                                     [FromRoute][ValidId] int contactId)
    {
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
    ///     There is no login yet, so the person is identified by the id in the route only.
    /// </summary>
    [HttpGet("ws")]
    [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> Connect([FromRoute][ValidId] int personId)
    {
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

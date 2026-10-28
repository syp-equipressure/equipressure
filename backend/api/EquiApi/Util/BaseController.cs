using EquiApi.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Util;

/// <summary>
///     Base controller class that provides common functionality (e.g. validation) for API controllers,
///     all endpoints require a user logged in over Keycloak
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    ///     Validates the given request with the specified validator and adds all validation errors to the model state,
    ///     so that they can be returned with <see cref="ControllerBase.ValidationProblem()" />
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <typeparam name="TValidator">
    ///     The validator class to use, its constructor dependencies (e.g. <see cref="IClock" />) are resolved from DI
    /// </typeparam>
    /// <typeparam name="TRequest">The request class</typeparam>
    /// <returns>True if the request passed validation; false otherwise</returns>
    protected bool IsValid<TValidator, TRequest>(TRequest request)
        where TValidator : IValidator<TRequest>
    {
        var validator = ActivatorUtilities.CreateInstance<TValidator>(HttpContext.RequestServices);
        var result = validator.Validate(request);

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return result.IsValid;
    }

    /// <summary>
    ///     Checks if the logged-in user may access the requested data, admins may access everything
    /// </summary>
    /// <param name="check">
    ///     The check to run with the id of the person of the logged-in user,
    ///     a user without a profile has no access
    /// </param>
    /// <returns>True if the user has access; false otherwise</returns>
    protected async ValueTask<bool> HasAccessAsync(Func<IAccessService, int, ValueTask<bool>> check)
    {
        if (User.IsAdmin)
        {
            return true;
        }

        var accessService = HttpContext.RequestServices.GetRequiredService<IAccessService>();
        var personId = await accessService.GetPersonIdAsync(User.ExternalId);

        return personId is not null && await check(accessService, personId.Value);
    }

    /// <summary>
    ///     Checks if the given person is the person of the logged-in user (or the user is an admin)
    /// </summary>
    protected ValueTask<bool> IsCurrentPersonAsync(int personId) =>
        HasAccessAsync((_, me) => ValueTask.FromResult(me == personId));

    /// <summary>
    ///     Response if the logged-in user may not access the requested data
    /// </summary>
    protected ObjectResult AccessDenied() =>
        Problem(statusCode: StatusCodes.Status403Forbidden, title: "No access to the requested data");
}
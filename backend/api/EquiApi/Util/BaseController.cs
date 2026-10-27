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
}
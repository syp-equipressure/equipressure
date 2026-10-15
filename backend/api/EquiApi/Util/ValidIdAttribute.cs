using System.ComponentModel.DataAnnotations;

namespace EquiApi.Util;

/// <summary>
///     Marks a parameter as database id, which has to be a positive number.
///     Invalid ids are rejected with 400 Bad Request by the <see cref="Microsoft.AspNetCore.Mvc.ApiControllerAttribute" />.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class ValidIdAttribute() : RangeAttribute(1, int.MaxValue)
{
    public override string FormatErrorMessage(string name) => $"{name} has to be a positive number";
}
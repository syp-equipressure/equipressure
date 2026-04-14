namespace EquiPressure.Core.Service;

public interface IBaseService
{
    public readonly record struct InvalidData;

    public readonly record struct Conflict;
}

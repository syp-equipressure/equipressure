namespace EquiApi.Core.Util;

public class Helper
{
    public static void UpdateIfNotNull<T>(T? value, Action<T> setter)
    {
        if (value is not null)
        {
            setter(value);
        }
    }
    
    public static void UpdateIfHasValue<T>(T? value, Action<T> setter) where T : struct
    {
        if (value.HasValue)
        {
            setter(value.Value);
        }
    }
}

namespace EquiApi.Core.Util;

public sealed class Settings
{
    public const string SectionKey = "General";
    public required string ClientOrigin { get; init; }

    /// <summary>
    ///     If set, the database is migrated and filled with test data on startup (only in Development)
    /// </summary>
    public bool SeedDevelopmentData { get; init; }
}
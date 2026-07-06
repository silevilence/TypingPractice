using TypingCore.Models;
using TypingCore.Persistence;

namespace TypingCore.Tests.Persistence;

public sealed class JsonUserPreferencesRepositoryTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"TypingCore.Preferences.{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsync_returns_defaults_when_file_does_not_exist()
    {
        JsonUserPreferencesRepository repository = CreateRepository();

        UserPreferences preferences = await repository.LoadAsync();

        Assert.Equal(UserPreferences.Default, preferences);
    }

    [Fact]
    public async Task SaveAsync_round_trips_preferences()
    {
        JsonUserPreferencesRepository repository = CreateRepository();
        UserPreferences expected = new(
            UserTheme.Dark,
            "Consolas",
            24d,
            "Ctrl+Space",
            "F5",
            "Ctrl+Tab");

        await repository.SaveAsync(expected);
        UserPreferences actual = await repository.LoadAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task LoadAsync_reports_malformed_json()
    {
        Directory.CreateDirectory(tempDirectory);
        string filePath = Path.Combine(tempDirectory, "preferences.json");
        await File.WriteAllTextAsync(filePath, "{ broken");
        JsonUserPreferencesRepository repository = new(filePath);

        FormatException exception = await Assert.ThrowsAsync<FormatException>(
            () => repository.LoadAsync());

        Assert.Contains("格式无效", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private JsonUserPreferencesRepository CreateRepository()
        => new(Path.Combine(tempDirectory, "preferences.json"));
}

using System.Text.Json;
using System.Text.Json.Serialization;
using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Persistence;

/// <summary>
/// Persists user preferences in an application-owned JSON file.
/// </summary>
/// <remarks>
/// Instances are intended for serialized application-level access and are not thread-safe.
/// </remarks>
public sealed class JsonUserPreferencesRepository : IUserPreferencesRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonUserPreferencesRepository"/> class.
    /// </summary>
    /// <param name="filePath">The complete path of the preferences JSON file.</param>
    public JsonUserPreferencesRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = Path.GetFullPath(filePath);
    }

    /// <inheritdoc />
    public async Task<UserPreferences> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return UserPreferences.Default;
        }

        await using FileStream stream = File.OpenRead(filePath);
        try
        {
            return await JsonSerializer
                    .DeserializeAsync<UserPreferences>(
                        stream,
                        SerializerOptions,
                        cancellationToken)
                    .ConfigureAwait(false)
                ?? throw new FormatException("用户偏好配置为空。");
        }
        catch (JsonException ex)
        {
            throw new FormatException("用户偏好配置格式无效。", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (FileStream stream = new(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await JsonSerializer
                    .SerializeAsync(
                        stream,
                        preferences,
                        SerializerOptions,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}

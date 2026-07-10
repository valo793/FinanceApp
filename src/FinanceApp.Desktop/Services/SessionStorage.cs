using System.IO;
using System.Text.Json;

namespace FinanceApp.Desktop.Services;

/// <summary>
/// Persists the user session (tokens) to a local JSON file in AppData so the user
/// stays logged in across app restarts.
/// </summary>
public sealed class SessionStorage
{
    private static readonly string StorageDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FinanceApp");

    private static readonly string FilePath = Path.Combine(StorageDir, "session.json");

    public record SessionData(string AccessToken, string RefreshToken, int ExpiresIn);

    public async Task SaveAsync(string accessToken, string refreshToken, int expiresIn)
    {
        Directory.CreateDirectory(StorageDir);
        var data = new SessionData(accessToken, refreshToken, expiresIn);
        var json = JsonSerializer.Serialize(data);
        await File.WriteAllTextAsync(FilePath, json);
    }

    public async Task<SessionData?> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<SessionData>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Clear()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}

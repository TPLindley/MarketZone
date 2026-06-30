using System.Text.Json;
using mzConfigure.Models;

namespace mzConfigure.Services;

/// <summary>
/// Manages a persistent JSON library of all specials ever used.
/// Automatically filters out duplicates based on text (case-insensitive).
/// </summary>
public class SpecialsLibraryService
{
    private const string LibraryFileName = "specials_library.json";
    private readonly string _libraryFilePath;

    public SpecialsLibraryService()
    {
        _libraryFilePath = Path.Combine(FileSystem.AppDataDirectory, LibraryFileName);
    }

    /// <summary>
    /// Load all specials from the library file
    /// </summary>
    public async Task<List<Special>> LoadLibraryAsync()
    {
        try
        {
            Log.Debug($"SpecialsLibraryService: Loading library from {_libraryFilePath}");

            if (!File.Exists(_libraryFilePath))
            {
                Log.Info("SpecialsLibraryService: Library file does not exist yet");
                return new List<Special>();
            }

            var json = await File.ReadAllTextAsync(_libraryFilePath);
            Log.Debug($"SpecialsLibraryService: Read {json.Length} characters from library file");

            var specials = JsonSerializer.Deserialize<List<Special>>(json);
            var count = specials?.Count ?? 0;
            Log.Info($"SpecialsLibraryService: Loaded {count} specials from library");

            return specials ?? new List<Special>();
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "SpecialsLibraryService: Failed to load library");
            return new List<Special>();
        }
    }

    /// <summary>
    /// Add new specials to the library, filtering out duplicates.
    /// Duplicates are detected by Text property (case-insensitive).
    /// If a duplicate exists with different color, the existing entry is kept.
    /// </summary>
    public async Task AddToLibraryAsync(IEnumerable<Special> newSpecials)
    {
        // Load existing library
        var library = await LoadLibraryAsync();
        var initialCount = library.Count;

        // Create a dictionary for fast duplicate lookup (case-insensitive)
        var libraryDict = library.ToDictionary(
            s => s.Text.Trim().ToLowerInvariant(),
            s => s,
            StringComparer.OrdinalIgnoreCase);

        // Add only non-duplicates
        int addedCount = 0;
        foreach (var special in newSpecials)
        {
            var key = special.Text.Trim().ToLowerInvariant();
            if (!libraryDict.ContainsKey(key))
            {
                library.Add(special);
                libraryDict[key] = special;
                addedCount++;
            }
            // If duplicate with different color, ignore the new one (keep existing)
        }

        Log.Info($"SpecialsLibraryService: Added {addedCount} new specials (library now has {library.Count} items, was {initialCount})");

        // Save updated library
        await SaveLibraryAsync(library);
    }

    /// <summary>
    /// Save the entire library to JSON file
    /// </summary>
    private async Task SaveLibraryAsync(List<Special> library)
    {
        try
        {
            var json = JsonSerializer.Serialize(library, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_libraryFilePath, json);
            Log.Info($"SpecialsLibraryService: Saved {library.Count} specials to {_libraryFilePath}");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "SpecialsLibraryService: Failed to save library");
        }
    }

    /// <summary>
    /// Clear the entire library (for testing or user request)
    /// </summary>
    public async Task ClearLibraryAsync()
    {
        try
        {
            if (File.Exists(_libraryFilePath))
            {
                File.Delete(_libraryFilePath);
            }
        }
        catch
        {
            // Silently fail
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Replace the local library with specials from the server.
    /// This is called when connecting to sync with the server's library.
    /// </summary>
    public async Task SyncLibraryFromServerAsync(List<Special> serverLibrary)
    {
        try
        {
            await SaveLibraryAsync(serverLibrary);
            Log.Info($"SpecialsLibraryService: Synced library with {serverLibrary.Count} items from server");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "SpecialsLibraryService: Failed to sync library from server");
            throw;
        }
    }

    /// <summary>
    /// Get the count of items in the library
    /// </summary>
    public async Task<int> GetLibraryCountAsync()
    {
        var library = await LoadLibraryAsync();
        return library.Count;
    }
}


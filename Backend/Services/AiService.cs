using System.Net;
using System.Text;
using System.Text.Json;
using TACT.DTOs;

namespace TACT.Services;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;

    public AiService(HttpClient httpClient, ILogger<AiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _apiKey = _configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key is missing!");
    }

    public async Task<List<ClientAppsDto>> GetBlockedAppsAsync(string taskTitle, List<ClientAppsDto> apps)
    {
        if (apps == null || !apps.Any()) 
            return new List<ClientAppsDto>();

        var appsDataForPrompt = apps.Select(a => new { a.PackageName, a.AppName });

        var prompt = $@"
Analyze the following work task titled: '{taskTitle}'.
Given this list of installed mobile apps with their PackageName and AppName:
{JsonSerializer.Serialize(appsDataForPrompt)}

Return ONLY a JSON array containing the packageNames of the apps from the list above that MUST BE BLOCKED during this task to maintain focus.
Example output format:
[""com.instagram.android"", ""com.facebook.katana""]";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.1
            }
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);

        var model = _configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

        int maxRetries = 2; 

        for (int retry = 0; retry <= maxRetries; retry++)
        {
            try
            {
                var jsonContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (retry < maxRetries)
                    {
                        _logger.LogWarning("Gemini API rate limited (429). Retrying attempt {Attempt} in 5 seconds...", retry + 1);
                        await Task.Delay(5000); 
                        continue;
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API Error: {StatusCode} - {Error}", response.StatusCode, errorMsg);
                    return new List<ClientAppsDto>();
                }

                var responseJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseJson);
                var textResult = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrEmpty(textResult))
                    return new List<ClientAppsDto>();

                var blockedPackageNames = JsonSerializer.Deserialize<List<string>>(textResult, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<string>();

                return apps
                    .Where(app => blockedPackageNames.Contains(app.PackageName, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze apps with Gemini AI on attempt {Attempt}.", retry + 1);
                if (retry == maxRetries) return new List<ClientAppsDto>();
            }
        }

        return new List<ClientAppsDto>();
    }
    
    
    public async Task<List<ExtractedTaskAiDto>> ExtractTasksAndBlockedAppsAsync(string voiceText, List<ClientAppsDto> userApps)
{
    if (string.IsNullOrWhiteSpace(voiceText))
        return new List<ExtractedTaskAiDto>();

    userApps ??= new List<ClientAppsDto>();

    var appsDataForPrompt = userApps.Select(a => new { a.PackageName, a.AppName });
    
    var currentUtcTime = DateTime.UtcNow.ToString("o"); 

    var prompt = $@"
You are an AI task assistant. Analyze the following transcribed user voice message:
""{voiceText}""

Current Reference UTC Time is: {currentUtcTime}

Tasks Context & Instructions:
1. Extract all separate work, study, or daily tasks mentioned in the text.
2. Estimate or extract the StartTime and EndTime for EACH task in ISO 8601 UTC format.
   - If a duration is mentioned (e.g., 'for 2 hours'), calculate EndTime = StartTime + duration.
   - If specific times are mentioned (e.g., 'from 5 PM to 7 PM'), calculate the exact UTC dates based on current reference time.
   - If no time is mentioned, default StartTime to current reference time and EndTime to 1 hour later.
3. For EACH task, analyze the installed apps list and decide which apps MUST BE BLOCKED to prevent distraction during that specific task.

Installed Apps List (PackageName & AppName):
{JsonSerializer.Serialize(appsDataForPrompt)}

Return ONLY a JSON array of task objects matching EXACTLY this structure:
[
  {{
    ""title"": ""Task title here"",
    ""startTime"": ""2026-07-23T15:00:00Z"",
    ""endTime"": ""2026-07-23T17:00:00Z"",
    ""blockedPackageNames"": [""com.instagram.android"", ""com.facebook.katana""]
  }}
]";

    var requestBody = new
    {
        contents = new[]
        {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        },
        generationConfig = new
        {
            response_mime_type = "application/json",
            temperature = 0.1 
        }
    };

    var jsonPayload = JsonSerializer.Serialize(requestBody);
    var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";
    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

    int maxRetries = 2;

    for (int retry = 0; retry <= maxRetries; retry++)
    {
        try
        {
            var jsonContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (retry < maxRetries)
                {
                    _logger.LogWarning("Gemini API rate limited (429). Retrying in 5 seconds...");
                    await Task.Delay(5000);
                    continue;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API Error: {StatusCode} - {Error}", response.StatusCode, errorMsg);
                return new List<ExtractedTaskAiDto>();
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            var textResult = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(textResult))
                return new List<ExtractedTaskAiDto>();

            var rawAiTasks = JsonSerializer.Deserialize<List<RawTaskResponseDto>>(textResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<RawTaskResponseDto>();

            var finalResult = rawAiTasks.Select(rawTask =>
            {
                DateTime parsedStartTime = DateTime.TryParse(rawTask.StartTime, out var sTime) ? sTime.ToUniversalTime() : DateTime.UtcNow;
                DateTime parsedEndTime = DateTime.TryParse(rawTask.EndTime, out var eTime) ? eTime.ToUniversalTime() : parsedStartTime.AddHours(1);

                return new ExtractedTaskAiDto
                {
                    Title = rawTask.Title,
                    StartTime = parsedStartTime,
                    EndTime = parsedEndTime,
                    BlockedApps = userApps
                        .Where(app => rawTask.BlockedPackageNames.Contains(app.PackageName, StringComparer.OrdinalIgnoreCase))
                        .ToList()
                };
            }).ToList();

            return finalResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract tasks with Gemini AI. Attempt {Attempt}", retry + 1);
            if (retry == maxRetries) return new List<ExtractedTaskAiDto>();
        }
    }

    return new List<ExtractedTaskAiDto>();
}
    
}
using System.Text;
using System.Text.Json;
using FolioForge.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FolioForge.Infrastructure.Services;

public class GeminiAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiAiService> _logger;

    // UPDATED: Using the latest stable model
    private const string ModelId = "gemini-2.0-flash";

    public GeminiAiService(HttpClient httpClient, IConfiguration config, ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"];
        _logger = logger;
    }

    public async Task<string> GeneratePortfolioDataAsync(string resumeText)
    {
        // 1. Check if Key is valid (Basic check)
        if (string.IsNullOrEmpty(_apiKey) || !_apiKey.StartsWith("AIza"))
        {
            _logger.LogError("Invalid Gemini API Key. It should start with 'AIza'.");
            throw new Exception("Invalid API Key configuration.");
        }

        // 2. Build URL with the new Model ID
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelId}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = BuildPrompt(resumeText) }
                    }
                }
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(url, jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Gemini API Error ({response.StatusCode}): {error}");
            throw new Exception($"Gemini API Failed: {response.StatusCode}");
        }

        var responseString = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(responseString);
            var textResult = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return CleanJson(textResult);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to parse Gemini response: {responseString}");
            throw;
        }
    }

    private string BuildPrompt(string resumeText)
    {
        return $@"
        You are a professional resume parser. Analyze the text below and extract structured data.
        
        CRITICAL RULES:
        1. For 'Experience' and 'Projects', do NOT write paragraphs.
        2. Extract distinct achievements/responsibilities as a LIST of strings called 'points'.
        3. If the resume has bullet points, preserve them. If it has paragraphs, split them into logical bullet points.
        4. Keep descriptions professional, concise, and impact-oriented.

        REQUIRED JSON STRUCTURE:
        {{
          ""summary"": ""Professional summary string"",
          ""skills"": [""C#"", ""React"", ""Azure""],
          ""experience"": [ 
            {{ 
              ""company"": ""Company Name"", 
              ""role"": ""Job Title"", 
              ""points"": [
                ""Designed microservices architecture using .NET 8."",
                ""Reduced API latency by 40% via Redis caching.""
              ] 
            }} 
          ],
          ""projects"": [ 
            {{ 
              ""name"": ""Project Name"", 
              ""techStack"": ""React, Node.js"", 
              ""points"": [
                ""Built a real-time chat application using SignalR."",
                ""Implemented OAuth2 authentication for secure login.""
              ] 
            }} 
          ]
        }}

        RESUME TEXT:
        {resumeText}
        
        Do not include Markdown formatting (like ```json). Just the raw JSON string.";
    }

    private string CleanJson(string json)
    {
        return json.Replace("```json", "").Replace("```", "").Trim();
    }
}
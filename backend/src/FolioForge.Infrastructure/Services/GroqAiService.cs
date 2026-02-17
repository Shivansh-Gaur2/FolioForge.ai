using System.Text;
using System.Text.Json;
using FolioForge.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FolioForge.Infrastructure.Services;

public class GroqAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GroqAiService> _logger;

    public GroqAiService(HttpClient httpClient, IConfiguration config, ILogger<GroqAiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["Groq:ApiKey"];
        _logger = logger;
    }

    public async Task<string> GeneratePortfolioDataAsync(string resumeText)
    {
        var url = "https://api.groq.com/openai/v1/chat/completions";

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a resume parser. Output ONLY valid JSON. No markdown, no explanations."
                },
                new
                {
                    role = "user",
                    content = BuildPrompt(resumeText)
                }
            },
            temperature = 0.1 // Low temperature = more consistent JSON
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync(url, jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Groq API Error: {error}");
            throw new Exception($"Groq API Failed: {response.StatusCode}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseString);

        // Navigate: choices[0] -> message -> content
        var textResult = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return CleanJson(textResult);
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
        ";
    }

    private string CleanJson(string json)
    {
        var start = json.IndexOf("{");
        var end = json.LastIndexOf("}");
        if (start >= 0 && end > start)
        {
            return json.Substring(start, end - start + 1);
        }
        return json;
    }
}
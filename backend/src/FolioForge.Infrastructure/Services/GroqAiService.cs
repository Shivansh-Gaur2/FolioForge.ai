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
        Analyze this resume text and extract data into this exact JSON structure:
        {{
          ""summary"": ""Professional summary"",
          ""skills"": [""Skill1"", ""Skill2""],
          ""experience"": [ {{ ""company"": ""Name"", ""role"": ""Title"", ""description"": ""Details"" }} ],
          ""projects"": [ {{ ""name"": ""Title"", ""techStack"": ""Technologies"", ""description"": ""Details"" }} ]
        }}

        Resume Text:
        {resumeText}
        ";
    }

    private string CleanJson(string json)
    {
        // Llama 3 sometimes adds "Here is the JSON:" preamble. We remove it.
        var start = json.IndexOf("{");
        var end = json.LastIndexOf("}");
        if (start >= 0 && end > start)
        {
            return json.Substring(start, end - start + 1);
        }
        return json;
    }
}
using System.Text;
using System.Diagnostics;
using System.Text.Json;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FolioForge.Infrastructure.Services;

public class GroqAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint;
    private readonly ILogger<GroqAiService> _logger;

    public GroqAiService(HttpClient httpClient, IConfiguration config, ILogger<GroqAiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey configuration is missing");
        _model = config["Groq:Model"] ?? "llama-3.3-70b-versatile";
        _endpoint = config["Groq:Endpoint"] ?? "https://api.groq.com/openai/v1/chat/completions";
        _logger = logger;
    }

    public async Task<string> GeneratePortfolioDataAsync(string resumeText)
    {
        // create a business-level span to group the AI call
        using var activity = FolioForgeDiagnostics.ActivitySource.StartActivity(
            FolioForgeDiagnostics.GeneratePortfolio,
            ActivityKind.Internal);
        activity?.SetTag("ai.model", _model);
        activity?.SetTag("prompt.length", resumeText.Length);

        var requestBody = new
        {
            model = _model,
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

        // Use per-request headers instead of mutating the shared HttpClient.DefaultRequestHeaders
        // DefaultRequestHeaders is NOT thread-safe — concurrent requests would corrupt headers.
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = jsonContent
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);

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

        return CleanJson(textResult ?? throw new InvalidOperationException("AI response text is null"));
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
        5. Extract ALL URLs/links found in the resume (GitHub, LinkedIn, portfolio, project demos, etc.).
        6. For projects, extract any associated URL (GitHub repo, live demo, etc.) into the 'url' field.
        7. For experience, extract the duration/period (e.g. 'Jan 2023 - Present') into the 'duration' field.
        8. Extract education details if present.
        9. If no URL is found for a field, use an empty string.

        REQUIRED JSON STRUCTURE:
        {{
          ""summary"": ""Professional summary string"",
          ""skills"": [""C#"", ""React"", ""Azure""],
          ""experience"": [ 
            {{ 
              ""company"": ""Company Name"", 
              ""role"": ""Job Title"",
              ""duration"": ""Jan 2023 - Present"",
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
              ""url"": ""https://github.com/user/project"",
              ""points"": [
                ""Built a real-time chat application using SignalR."",
                ""Implemented OAuth2 authentication for secure login.""
              ] 
            }} 
          ],
          ""education"": [
            {{
              ""degree"": ""B.Tech in Computer Science"",
              ""institution"": ""University Name"",
              ""year"": ""2020 - 2024"",
              ""gpa"": ""3.8/4.0""
            }}
          ],
          ""links"": {{
            ""github"": ""https://github.com/username"",
            ""linkedin"": ""https://linkedin.com/in/username"",
            ""portfolio"": ""https://example.com"",
            ""email"": ""user@example.com"",
            ""twitter"": """"
          }}
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
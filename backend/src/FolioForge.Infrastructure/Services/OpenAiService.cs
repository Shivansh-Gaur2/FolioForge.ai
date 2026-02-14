using FolioForge.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat; // The new namespace for Chat Completions
using System.ClientModel; // For ApiKeyCredential

namespace FolioForge.Infrastructure.Services;

public class OpenAiService : IAiService
{
    private readonly string _apiKey;
    private readonly string _model = "gpt-4o-mini"; // Cheap, fast, smart enough

    public OpenAiService(IConfiguration configuration)
    {
        _apiKey = configuration["OpenAi:ApiKey"]
                  ?? throw new ArgumentNullException("OpenAi:ApiKey is missing");
    }

    public async Task<string> GeneratePortfolioDataAsync(string resumeText)
    {
        ChatClient client = new ChatClient(_model, new ApiKeyCredential(_apiKey));

        // THE PROMPT: The most important part of this entire feature
        var systemPrompt = @"
You are a professional resume parser. Convert the provided resume text into a STRICT JSON format for a developer portfolio.
The JSON must have this exact structure:
{
  ""summary"": ""A professional summary (max 300 chars)"",
  ""skills"": [""C#"", "".NET"", ""React"", ""SQL""],
  ""experience"": [
    {
      ""company"": ""Company Name"",
      ""role"": ""Job Title"",
      ""duration"": ""Jan 2022 - Present"",
      ""description"": ""Key achievement or responsibility.""
    }
  ],
  ""projects"": [
    {
      ""name"": ""Project Name"",
      ""techStack"": ""React, Node.js"",
      ""description"": ""What it does.""
    }
  ]
}
Do NOT wrap the response in markdown code blocks (like ```json). Just return the raw JSON string.";

        ChatCompletion completion = await client.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Here is the resume text:\n\n{resumeText}")
            ]);

        // Return the clean JSON content
        return completion.Content[0].Text.Trim();
    }
}
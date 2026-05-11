using Microsoft.SemanticKernel;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using System.Text.Json;
using AgentTypes = NexusAI.Domain.Enums.AgentType;

namespace NexusAI.Core.Agents;

public class WebSearchAgent : AgentBase
{
    public override string AgentType => AgentTypes.WebSearch;

    private readonly string? _tavilyKey;
    private static readonly HttpClient _http = new();

    public WebSearchAgent(Kernel kernel) : base(kernel)
    {
        _tavilyKey = Environment.GetEnvironmentVariable("Tavily__ApiKey");
    }

    public override async Task<string> ExecuteAsync(
        AgentTask         task,
        AgentSession      session,
        IProgress<string> progress,
        CancellationToken ct)
    {
        progress.Report($"[WebSearch] Starting: {task.Title}");

        var searchResults = await SearchTavilyAsync(task.Description, ct);

        var system = """
            You are a web research specialist. Summarise the search results clearly.
            Be concise — maximum 300 words. Include key facts and cite sources.
            """;

        var user = $"""
            Research task: {task.Description}

            Search results:
            {searchResults}

            Summarise the key findings in 3-5 bullet points with source citations.
            """;

        var result = await StreamCompleteAsync(system, user, progress, ct);
        progress.Report($"[WebSearch] Complete");
        return result;
    }

    private async Task<string> SearchTavilyAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_tavilyKey))
        {
            return $"[Simulated search results for: {query}] " +
                   "No Tavily API key configured. Using LLM knowledge instead.";
        }

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                api_key         = _tavilyKey,
                query           = query,
                search_depth    = "basic",
                max_results     = 5,
                include_answer  = true
            });

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://api.tavily.com/search")
            {
                Content = new StringContent(payload,
                    System.Text.Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request, ct);
            var json     = await response.Content.ReadAsStringAsync(ct);
            var doc      = JsonDocument.Parse(json);

            var sb = new System.Text.StringBuilder();

            // Include Tavily's pre-computed answer if available
            if (doc.RootElement.TryGetProperty("answer", out var answer) &&
                answer.ValueKind != JsonValueKind.Null)
            {
                sb.AppendLine($"Summary: {answer.GetString()}");
                sb.AppendLine();
            }

            // Include top results
            if (doc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var r in results.EnumerateArray().Take(5))
                {
                    var title   = r.TryGetProperty("title",   out var t) ? t.GetString() : "";
                    var url     = r.TryGetProperty("url",     out var u) ? u.GetString() : "";
                    var content = r.TryGetProperty("content", out var c) ? c.GetString() : "";

                    sb.AppendLine($"Source: {title}");
                    sb.AppendLine($"URL: {url}");
                    sb.AppendLine($"Content: {Truncate(content ?? "", 300)}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Search error: {ex.Message}. Proceeding with LLM knowledge.";
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NexusAI.Core.Agents.Interfaces;
using NexusAI.Domain.Entities;

namespace NexusAI.Core.Agents;

public abstract class AgentBase : IAgent
{
    protected readonly Kernel _kernel;
    protected readonly IChatCompletionService _chat;

    protected AgentBase(Kernel kernel)
    {
        _kernel = kernel;
        _chat   = kernel.GetRequiredService<IChatCompletionService>();
    }

    public abstract string AgentType { get; }

    public abstract Task<string> ExecuteAsync(
        AgentTask         task,
        AgentSession      session,
        IProgress<string> progress,
        CancellationToken ct);

    protected async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        IProgress<string> progress,
        CancellationToken ct)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userMessage);

        // Use a long-lived CancellationToken independent of the request
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(14));

        var response = await _chat.GetChatMessageContentAsync(
            history, cancellationToken: cts.Token);

        var result = response.Content ?? string.Empty;
        progress.Report(result);
        return result;
    }

    protected async Task<string> StreamCompleteAsync(
        string systemPrompt,
        string userMessage,
        IProgress<string> progress,
        CancellationToken ct)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userMessage);

        var fullResponse = new System.Text.StringBuilder();

        // Use a long-lived CancellationToken independent of the request
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(14));

        await foreach (var chunk in _chat.GetStreamingChatMessageContentsAsync(
            history, cancellationToken: cts.Token))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                fullResponse.Append(chunk.Content);
                progress.Report(chunk.Content);
            }
        }

        return fullResponse.ToString();
    }
}

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

    // Helper — sends a prompt and returns the full response
    protected async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        IProgress<string> progress,
        CancellationToken ct)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userMessage);

        var response = await _chat.GetChatMessageContentAsync(
            history, cancellationToken: ct);

        var result = response.Content ?? string.Empty;
        progress.Report(result);
        return result;
    }

    // Helper — streams response token by token
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

        await foreach (var chunk in _chat.GetStreamingChatMessageContentsAsync(
            history, cancellationToken: ct))
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

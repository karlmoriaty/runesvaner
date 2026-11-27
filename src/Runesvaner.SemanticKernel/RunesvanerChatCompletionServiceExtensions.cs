using Microsoft.SemanticKernel.ChatCompletion;

namespace Runesvaner.SemanticKernel;

/// <summary>Extension methods for wrapping Semantic Kernel chat completion services.</summary>
public static class RunesvanerChatCompletionServiceExtensions
{
    /// <summary>Wraps a chat completion service with Runesvaner recording.</summary>
    public static IChatCompletionService AsRunesvaner(
        this IChatCompletionService inner,
        IRunesvanerRecorder recorder,
        string? provider = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(recorder);

        return new RunesvanerChatCompletionService(inner, recorder, provider);
    }
}
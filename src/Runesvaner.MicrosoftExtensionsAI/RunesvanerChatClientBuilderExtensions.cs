using Microsoft.Extensions.AI;

namespace Runesvaner.MicrosoftExtensionsAI;

public static class RunesvanerChatClientBuilderExtensions
{
    public static ChatClientBuilder UseRunesvaner(this ChatClientBuilder builder, IRunesvanerRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(recorder);

        return builder.Use(inner => new RunesvanerChatClient(inner, recorder));
    }
}
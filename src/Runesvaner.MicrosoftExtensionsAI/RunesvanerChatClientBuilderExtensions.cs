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

    public static ChatClientBuilder UseRunesvaner(this ChatClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Use((inner, services) =>
        {
            var recorder = services.GetService(typeof(IRunesvanerRecorder)) as IRunesvanerRecorder
                ?? throw new InvalidOperationException(
                    "IRunesvanerRecorder is not registered. Call AddRunesvaner() before building the chat client.");

            var contentCapture =
                (services.GetService(typeof(RunesvanerOptions)) as RunesvanerOptions)?.ContentCapture
                ?? ContentCaptureMode.None;

            return new RunesvanerChatClient(inner, recorder, contentCapture);
        });
    }
}
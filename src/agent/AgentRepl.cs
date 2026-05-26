using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Cadenza.Agent;

internal static class AgentRepl
{
    public static async Task RunAsync()
    {
        var history = new List<ChatMessage>
        {
            new(ChatRole.System, Agent.CurrentSystemPrompt),
        };
        var options = new ChatOptions { Tools = Agent.Tools };

        Console.WriteLine($"cadenza-agent REPL — model: {Agent.ServedModelName}");
        Console.WriteLine("Type a message and press Enter. Empty line or Ctrl+C to exit.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (input is null) break;
            input = input.Trim();
            if (input.Length == 0) break;

            history.Add(new ChatMessage(ChatRole.User, input));

            try
            {
                var first = true;
                await foreach (var update in Agent.ChatClient.GetStreamingResponseAsync(history, options).ConfigureAwait(false))
                {
                    if (first) { Console.WriteLine(); first = false; }
                    if (!string.IsNullOrEmpty(update.Text))
                        Console.Write(update.Text);
                }
                Console.WriteLine();
                Console.WriteLine();

                // We rely on UseFunctionInvocation to append assistant + tool
                // messages to its own internal flow, but we still need to keep
                // the visible turn in our history. Replay the final assistant
                // message by issuing a non-streaming call as a fallback when
                // the streaming path didn't materialize text.
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[error] {ex.Message}");
            }
        }
    }
}

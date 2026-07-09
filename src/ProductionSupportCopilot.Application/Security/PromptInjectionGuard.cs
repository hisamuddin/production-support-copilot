using System.Text.RegularExpressions;

namespace ProductionSupportCopilot.Application.Security;

public sealed record GuardResult(bool IsAllowed, string? Reason, string SanitizedText);

public interface IPromptInjectionGuard
{
    GuardResult InspectUserInput(string text);
    string MarkUntrustedDocument(string text);
}

public sealed partial class PromptInjectionGuard : IPromptInjectionGuard
{
    private static readonly string[] SuspiciousPhrases =
    [
        "ignore previous instructions", "ignore all instructions", "reveal system prompt",
        "developer message", "exfiltrate", "send secrets", "bypass authorization"
    ];

    public GuardResult InspectUserInput(string text)
    {
        var normalized = Whitespace().Replace(text ?? string.Empty, " ").Trim();
        var match = SuspiciousPhrases.FirstOrDefault(x => normalized.Contains(x, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? new GuardResult(true, null, normalized)
            : new GuardResult(false, $"Potential prompt injection phrase detected: '{match}'.", normalized);
    }

    public string MarkUntrustedDocument(string text) =>
        $"<untrusted_document>\n{text}\n</untrusted_document>";

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

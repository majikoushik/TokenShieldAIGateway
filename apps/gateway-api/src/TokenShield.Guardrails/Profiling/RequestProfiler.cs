using System.Text.RegularExpressions;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;
using TokenShield.Domain.Models;

namespace TokenShield.Guardrails.Profiling;

public class RequestProfiler : IRequestProfiler
{
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}", RegexOptions.Compiled);

    public RequestProfile ProfileRequest(ChatCompletionRequest request, int inputTokens)
    {
        var profile = new RequestProfile();

        // 1. Resolve from Metadata (priority settings)
        var metadata = request.Metadata ?? new Dictionary<string, string>();

        profile.RiskLevel = (GetMetadataValue(metadata, "riskLevel", "medium") ?? "medium").ToLowerInvariant();
        profile.Department = GetMetadataValue(metadata, "department", null);
        profile.Environment = GetMetadataValue(metadata, "environment", null);
        
        profile.RequiresReasoning = GetMetadataBool(metadata, "requiresReasoning", false);
        profile.RequiresStructuredOutput = GetMetadataBool(metadata, "requiresStructuredOutput", false);

        // 2. Token Counts
        profile.InputTokens = inputTokens;
        profile.EstimatedOutputTokens = request.MaxTokens ?? 150; // Mock standard limit fallback

        // 3. Scan for PII (Simple Phone/Email checks)
        profile.ContainsPii = ScanForPii(request.Messages);

        // 4. Infer TaskType if missing
        if (metadata.TryGetValue("taskType", out var taskTypeVal))
        {
            profile.TaskType = taskTypeVal.ToLowerInvariant();
        }
        else
        {
            profile.TaskType = InferTaskType(request.Messages);
        }

        // 5. Calculate Complexity Score (MVP Rules)
        profile.ComplexityScore = CalculateComplexity(profile);

        return profile;
    }

    private static string? GetMetadataValue(Dictionary<string, string> meta, string key, string? defaultValue)
    {
        return meta.TryGetValue(key, out var val) ? val : defaultValue;
    }

    private static bool GetMetadataBool(Dictionary<string, string> meta, string key, bool defaultValue)
    {
        if (meta.TryGetValue(key, out var val))
        {
            return bool.TryParse(val, out var result) ? result : defaultValue;
        }
        return defaultValue;
    }

    private static bool ScanForPii(List<ChatMessage> messages)
    {
        foreach (var msg in messages)
        {
            var content = msg.Content ?? "";
            if (EmailRegex.IsMatch(content) || PhoneRegex.IsMatch(content))
            {
                return true;
            }
        }
        return false;
    }

    private static string InferTaskType(List<ChatMessage> messages)
    {
        var combinedText = string.Join(" ", messages.Select(m => m.Content ?? "")).ToLowerInvariant();

        if (combinedText.Contains("summarize") || combinedText.Contains("summary") || combinedText.Contains("outline"))
        {
            return "summarization";
        }
        if (combinedText.Contains("translate") || combinedText.Contains("translation") || combinedText.Contains("language"))
        {
            return "translation";
        }
        if (combinedText.Contains("code") || combinedText.Contains("programming") || combinedText.Contains("function") || combinedText.Contains("develop") || combinedText.Contains("script") || combinedText.Contains("python"))
        {
            return "coding";
        }
        if (combinedText.Contains("reason") || combinedText.Contains("think") || combinedText.Contains("complex") || combinedText.Contains("logic") || combinedText.Contains("analyze"))
        {
            return "complex_reasoning";
        }

        return "general";
    }

    private static int CalculateComplexity(RequestProfile profile)
    {
        var score = 20; // base score

        if (profile.InputTokens > 4000)
        {
            score += 20;
        }

        if (profile.RequiresReasoning)
        {
            score += 30;
        }

        if (profile.TaskType == "complex_reasoning")
        {
            score += 20;
        }

        return Math.Min(score, 100); // capped at 100
    }
}

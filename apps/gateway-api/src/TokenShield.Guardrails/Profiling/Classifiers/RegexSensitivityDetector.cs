using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Common.Models.Profiling;
using TokenShield.Domain.Models;

namespace TokenShield.Guardrails.Profiling.Classifiers;

public class RegexSensitivityDetector : ISensitivityDetector
{
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}", RegexOptions.Compiled);
    
    // Basic heuristics
    private static readonly string[] FinancialTerms = {
        "account number", "credit card", "transaction amount", "invoice", "payment", "loan", "claim amount"
    };

    private static readonly string[] HealthTerms = {
        "diagnosis", "prescription", "patient", "medical record", "symptoms", "treatment"
    };

    private static readonly string[] LegalTerms = {
        "contract", "agreement", "legal notice", "litigation", "court", "compliance violation"
    };

    public SensitivityDetectionResult Detect(RequestClassificationInput input)
    {
        var normalized = input.NormalizedText;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new SensitivityDetectionResult();
        }

        var result = new SensitivityDetectionResult();
        var signals = new List<ProfileSignal>();

        // PII
        if (EmailRegex.IsMatch(normalized))
        {
            result.ContainsPii = true;
            signals.Add(new ProfileSignal { Name = "email_detected", Value = "true", Confidence = 0.9, Source = "regex" });
        }
        if (PhoneRegex.IsMatch(normalized))
        {
            result.ContainsPii = true;
            signals.Add(new ProfileSignal { Name = "phone_detected", Value = "true", Confidence = 0.9, Source = "regex" });
        }

        // Financial
        if (FinancialTerms.Any(t => normalized.Contains(t, System.StringComparison.OrdinalIgnoreCase)))
        {
            result.ContainsFinancialData = true;
            signals.Add(new ProfileSignal { Name = "financial_pattern_detected", Value = "true", Confidence = 0.8, Source = "regex" });
        }

        // Health
        if (HealthTerms.Any(t => normalized.Contains(t, System.StringComparison.OrdinalIgnoreCase)))
        {
            result.ContainsHealthData = true;
            signals.Add(new ProfileSignal { Name = "health_pattern_detected", Value = "true", Confidence = 0.8, Source = "regex" });
        }

        // Legal
        if (LegalTerms.Any(t => normalized.Contains(t, System.StringComparison.OrdinalIgnoreCase)))
        {
            result.ContainsLegalData = true;
            signals.Add(new ProfileSignal { Name = "legal_pattern_detected", Value = "true", Confidence = 0.8, Source = "regex" });
        }

        // Derive DataSensitivity
        if (result.ContainsHealthData || result.ContainsLegalData)
        {
            result.DataSensitivity = "restricted";
            result.Confidence = 0.8;
        }
        else if (result.ContainsFinancialData || result.ContainsPii)
        {
            result.DataSensitivity = "confidential";
            result.Confidence = 0.8;
        }

        result.Signals = signals;
        return result;
    }
}

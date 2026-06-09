using System;
using System.Collections.Generic;

namespace TokenShield.Application.Dto.Admin;

public class ProfilerRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetTaskType { get; set; } = string.Empty;
    public List<string> Phrases { get; set; } = new();
    public List<string> RegexPatterns { get; set; } = new();
    public double Confidence { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateProfilerRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string TargetTaskType { get; set; } = string.Empty;
    public List<string> Phrases { get; set; } = new();
    public List<string> RegexPatterns { get; set; } = new();
    public double Confidence { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateProfilerRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string TargetTaskType { get; set; } = string.Empty;
    public List<string> Phrases { get; set; } = new();
    public List<string> RegexPatterns { get; set; } = new();
    public double Confidence { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class TestProfilerRuleRequest
{
    public string Prompt { get; set; } = string.Empty;
    // We send rule details to test it without saving
    public string TargetTaskType { get; set; } = string.Empty;
    public List<string> Phrases { get; set; } = new();
    public List<string> RegexPatterns { get; set; } = new();
    public double Confidence { get; set; }
}

public class TestProfilerRuleResponse
{
    public bool IsMatch { get; set; }
    public string MatchReason { get; set; } = string.Empty;
    public string TargetTaskType { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

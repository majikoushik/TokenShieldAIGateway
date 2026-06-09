using System.Threading;
using System.Threading.Tasks;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Common.Models.Profiling;

namespace TokenShield.Guardrails.Profiling.Classifiers;

public class DisabledSemanticTaskClassifier : ISemanticTaskClassifier
{
    public Task<TaskClassificationResult> ClassifyAsync(RequestClassificationInput input, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TaskClassificationResult
        {
            TaskType = "general",
            Confidence = 0.0,
            ClassificationMethod = "disabled_semantic_classifier"
        });
    }
}

public class DisabledLlmRequestClassifier : ILlmRequestClassifier
{
    public Task<TaskClassificationResult> ClassifyAsync(RequestClassificationInput input, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TaskClassificationResult
        {
            TaskType = "general",
            Confidence = 0.0,
            ClassificationMethod = "disabled_llm_classifier"
        });
    }
}

using System.Threading;
using System.Threading.Tasks;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Common.Models.Profiling;
using TokenShield.Domain.Models;

namespace TokenShield.Application.Common.Interfaces.Profiling;

public interface IRequestProfilerFactory
{
    IRequestProfiler Create();
}

public interface IMetadataProfileResolver
{
    MetadataProfileResult Resolve(RequestClassificationInput input);
}

public interface ITaskClassifier
{
    Task<TaskClassificationResult> ClassifyAsync(
        RequestClassificationInput input,
        CancellationToken cancellationToken);
}

public interface ISemanticTaskClassifier
{
    Task<TaskClassificationResult> ClassifyAsync(
        RequestClassificationInput input,
        CancellationToken cancellationToken);
}

public interface ILlmRequestClassifier
{
    Task<TaskClassificationResult> ClassifyAsync(
        RequestClassificationInput input,
        CancellationToken cancellationToken);
}

public interface IRiskClassifier
{
    Task<RiskClassificationResult> ClassifyAsync(
        RequestClassificationInput input,
        CancellationToken cancellationToken);
}

public interface ISensitivityDetector
{
    SensitivityDetectionResult Detect(RequestClassificationInput input);
}

public interface IComplexityScorer
{
    ComplexityScoreResult Calculate(
        RequestClassificationInput input,
        TaskClassificationResult task,
        RiskClassificationResult risk,
        SensitivityDetectionResult sensitivity);
}

public interface IProfileResultMerger
{
    RequestProfile Merge(
        RequestClassificationInput input,
        MetadataProfileResult metadata,
        TaskClassificationResult task,
        RiskClassificationResult risk,
        SensitivityDetectionResult sensitivity,
        ComplexityScoreResult complexity);
}

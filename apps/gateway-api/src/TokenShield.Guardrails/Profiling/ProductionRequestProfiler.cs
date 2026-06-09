using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Common.Interfaces.Profiling;
using TokenShield.Application.Common.Models.Profiling;
using TokenShield.Application.Dto;
using TokenShield.Domain.Models;
using TokenShield.Guardrails.Profiling.Options;

namespace TokenShield.Guardrails.Profiling;

public class ProductionRequestProfiler : IRequestProfiler
{
    private readonly RequestProfilerOptions _options;
    private readonly IMetadataProfileResolver _metadataResolver;
    private readonly ITaskClassifier _taskClassifier;
    private readonly ISemanticTaskClassifier _semanticClassifier;
    private readonly ILlmRequestClassifier _llmClassifier;
    private readonly IRiskClassifier _riskClassifier;
    private readonly ISensitivityDetector _sensitivityDetector;
    private readonly IComplexityScorer _complexityScorer;
    private readonly IProfileResultMerger _resultMerger;

    public ProductionRequestProfiler(
        IOptions<RequestProfilerOptions> options,
        IMetadataProfileResolver metadataResolver,
        ITaskClassifier taskClassifier,
        ISemanticTaskClassifier semanticClassifier,
        ILlmRequestClassifier llmClassifier,
        IRiskClassifier riskClassifier,
        ISensitivityDetector sensitivityDetector,
        IComplexityScorer complexityScorer,
        IProfileResultMerger resultMerger)
    {
        _options = options.Value;
        _metadataResolver = metadataResolver;
        _taskClassifier = taskClassifier;
        _semanticClassifier = semanticClassifier;
        _llmClassifier = llmClassifier;
        _riskClassifier = riskClassifier;
        _sensitivityDetector = sensitivityDetector;
        _complexityScorer = complexityScorer;
        _resultMerger = resultMerger;
    }

    public async Task<RequestProfile> ProfileRequestAsync(ChatCompletionRequest request, int inputTokens, CancellationToken cancellationToken = default)
    {
        var input = new RequestClassificationInput
        {
            NormalizedText = string.Join(" ", request.Messages.Select(m => m.Content ?? "")).ToLowerInvariant(),
            Metadata = request.Metadata ?? new System.Collections.Generic.Dictionary<string, string>(),
            InputTokens = inputTokens,
            EstimatedOutputTokens = request.MaxTokens ?? 150,
            ModelRequested = request.Model,
            Messages = request.Messages
        };

        var metadataResult = _metadataResolver.Resolve(input);
        
        var sensitivityTask = Task.Run(() => _sensitivityDetector.Detect(input), cancellationToken);
        var taskClassificationTask = _taskClassifier.ClassifyAsync(input, cancellationToken);
        var riskClassificationTask = _riskClassifier.ClassifyAsync(input, cancellationToken);

        await Task.WhenAll(sensitivityTask, taskClassificationTask, riskClassificationTask);

        var sensitivity = sensitivityTask.Result;
        var taskClassification = taskClassificationTask.Result;
        var riskClassification = riskClassificationTask.Result;

        // Optionally override with semantic or LLM classifier if enabled
        if (_options.EnableSemanticClassifier)
        {
            var semanticResult = await _semanticClassifier.ClassifyAsync(input, cancellationToken);
            if (semanticResult.Confidence > taskClassification.Confidence)
            {
                taskClassification = semanticResult;
            }
        }

        if (_options.EnableLlmClassifier)
        {
            var llmResult = await _llmClassifier.ClassifyAsync(input, cancellationToken);
            if (llmResult.Confidence > taskClassification.Confidence)
            {
                taskClassification = llmResult;
            }
        }

        var complexity = _complexityScorer.Calculate(input, taskClassification, riskClassification, sensitivity);

        return _resultMerger.Merge(input, metadataResult, taskClassification, riskClassification, sensitivity, complexity);
    }
}

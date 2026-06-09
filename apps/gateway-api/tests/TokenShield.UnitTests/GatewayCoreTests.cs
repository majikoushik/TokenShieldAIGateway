using TokenShield.Application.Dto;
using TokenShield.Application.Services;
using TokenShield.Application.Validators;
using Xunit;

namespace TokenShield.UnitTests;

public class GatewayCoreTests
{
    private readonly ApiKeyService _apiKeyService = new();

    [Fact]
    public void ApiKeyService_GeneratesCorrectPrefixAndLengths()
    {
        // Act
        var prefix = "ts_dev_";
        var (rawKey, keyHash) = _apiKeyService.GenerateKey(prefix);

        // Assert
        Assert.StartsWith(prefix, rawKey);
        Assert.Equal(7 + 48, rawKey.Length); // prefix (7 chars) + 24 bytes converted to hex (48 chars) = 55 chars
        Assert.Equal(64, keyHash.Length); // SHA-256 hash length is 64 hex chars
    }

    [Fact]
    public void ApiKeyService_HashingIsDeterministic()
    {
        // Arrange
        var rawKey = "ts_dev_acmedeveloperkey12345";
        var expectedHash = "dead2ed75455a9d40cacc439bb04e85eeddef56b7966ce418d4a6b70851ac77b";

        // Act
        var resultHash = _apiKeyService.HashKey(rawKey);

        // Assert
        Assert.Equal(expectedHash, resultHash);
    }

    [Fact]
    public void ChatCompletionRequestValidator_ValidRequest_PassesValidation()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new()
            {
                new ChatMessage { Role = "system", Content = "You are a helpful assistant." },
                new ChatMessage { Role = "user", Content = "Say hello" }
            },
            Stream = false,
            Temperature = 1.0,
            MaxTokens = 100
        };
        var validator = new ChatCompletionRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ChatCompletionRequestValidator_InvalidRole_FailsValidation()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new()
            {
                new ChatMessage { Role = "invalid-role-name", Content = "Say hello" }
            },
            Stream = false
        };
        var validator = new ChatCompletionRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Message role must be either 'system', 'user', or 'assistant'"));
    }

    [Fact]
    public void ChatCompletionRequestValidator_StreamingEnabled_FailsValidation()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new()
            {
                new ChatMessage { Role = "user", Content = "Hello" }
            },
            Stream = true
        };
        var validator = new ChatCompletionRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Streaming responses are currently not supported"));
    }

    [Fact]
    public void ChatCompletionRequestValidator_EmptyMessages_FailsValidation()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new(),
            Stream = false
        };
        var validator = new ChatCompletionRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Messages list cannot be empty"));
    }
}

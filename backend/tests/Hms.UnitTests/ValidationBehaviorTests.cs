using FluentValidation;
using Hms.Application.Common;

namespace Hms.UnitTests;

/// <summary>Pipeline behavior must block invalid requests before the handler.</summary>
public sealed class ValidationBehaviorTests
{
    private sealed record TestRequest(string Value);

    [Fact]
    public async Task Invalid_request_never_reaches_handler()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(x => x.Value).NotEmpty();
        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var reached = false;

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            behavior.Handle(new TestRequest(""), () => { reached = true; return Task.FromResult("ok"); }, CancellationToken.None));

        Assert.False(reached);
        Assert.Contains("Value", exception.Errors.Keys);
    }

    [Fact]
    public async Task Valid_request_passes_to_handler()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(x => x.Value).NotEmpty();
        var behavior = new ValidationBehavior<TestRequest, string>([validator]);

        var result = await behavior.Handle(new TestRequest("ok"), () => Task.FromResult("handled"), CancellationToken.None);

        Assert.Equal("handled", result);
    }
}

using FluentAssertions;
using Looma.Domain.Core;

namespace Looma.Domain.Tests;

public class ResultTests
{
    [Fact]
    public void ShouldReturnSuccessWhenCreatingAnOkResult()
    {
        var result = Result.Ok();

        result.Succeeded.Should().BeTrue();
        result.Failed.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Success);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ShouldReturnNotFoundWhenCreatingANotFoundResult()
    {
        var result = Result.NotFound("missing");

        result.Succeeded.Should().BeFalse();
        result.Failed.Should().BeTrue();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be("missing");
    }

    [Fact]
    public void ShouldExposeTheValueWhenCreatingASuccessfulGenericResult()
    {
        var result = ResultT<int>.Ok(42);

        result.Succeeded.Should().BeTrue();
        result.Failed.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ShouldExposeAnErrorWithoutAValueWhenCreatingAFailedGenericResult()
    {
        var result = ResultT<string>.Failure("boom");

        result.Succeeded.Should().BeFalse();
        result.Failed.Should().BeTrue();
        result.Status.Should().Be(ResultStatus.Failure);
        result.Error.Should().Be("boom");
        result.Value.Should().BeNull();
    }
}

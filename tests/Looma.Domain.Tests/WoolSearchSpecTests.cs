using Looma.Domain.Entities;
using Looma.Domain.Search;
using FluentAssertions;

namespace Looma.Domain.Tests;

public class WoolSearchSpecTests
{
    private static readonly List<Wool> Wools =
    [
        new Wool { Id = 1, Name = "Alpaca Silk", Brand = "Drops", Material = "Alpaca", Color = "Beige", LengthToWeightRatio = 400, NeedleMinSize = 2.0, NeedleMaxSize = 5 },
        new Wool { Id = 2, Name = "Merino Extra Fine", Brand = "Drops", Material = "Mérinos", Color = "Rouge", LengthToWeightRatio = 200, NeedleMinSize = 2.5, NeedleMaxSize = 5 },
        new Wool { Id = 3, Name = "Cotton Light", Brand = "Paintbox", Material = "Coton", Color = "Bleu", LengthToWeightRatio = 350, NeedleMinSize = 2.5, NeedleMaxSize = 5 },
    ];

    [Fact]
    public void ShouldReturnAllWoolsWhenTheQueryIsEmpty()
    {
        WoolSearchSpec.Apply(Wools, "").Should().HaveCount(3);
    }

    [Fact]
    public void ShouldFilterByBrandWhenTheQueryContainsABrandName()
    {
        WoolSearchSpec.Apply(Wools, "drops").Should().HaveCount(2);
    }

    [Fact]
    public void ShouldMatchAllWordsWhenTheQueryContainsMultipleTerms()
    {
        WoolSearchSpec.Apply(Wools, "drops rouge").Should().HaveCount(1);
    }

    [Fact]
    public void ShouldIgnoreCaseWhenSearching()
    {
        WoolSearchSpec.Apply(Wools, "DROPS").Should().HaveCount(2);
    }

    [Fact]
    public void ShouldReturnNoResultWhenNothingMatches()
    {
        WoolSearchSpec.Apply(Wools, "inexistant").Should().BeEmpty();
    }
}

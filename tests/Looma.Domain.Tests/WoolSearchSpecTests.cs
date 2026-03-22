using Looma.Domain.Entities;
using Looma.Domain.Search;
using FluentAssertions;

namespace Looma.Domain.Tests;

public class WoolSearchSpecTests
{
    private static readonly List<Wool> Wools =
    [
        Wool.Create("Alpaca Silk", "Drops", "Alpaca", "Beige", 400, 2.0, 5),
        Wool.Create("Merino Extra Fine", "Drops", "Mérinos", "Rouge", 200, 2.5, 5),
        Wool.Create("Cotton Light", "Paintbox", "Coton", "Bleu", 350, 2.5, 5),
    ];

    [Fact]
    public void Query_vide_retourne_tout()
    {
        WoolSearchSpec.Apply(Wools, "").Should().HaveCount(3);
    }

    [Fact]
    public void Recherche_par_marque()
    {
        WoolSearchSpec.Apply(Wools, "drops").Should().HaveCount(2);
    }

    [Fact]
    public void Recherche_multi_mots()
    {
        WoolSearchSpec.Apply(Wools, "drops rouge").Should().HaveCount(1);
    }

    [Fact]
    public void Recherche_insensible_casse()
    {
        WoolSearchSpec.Apply(Wools, "DROPS").Should().HaveCount(2);
    }

    [Fact]
    public void Recherche_sans_resultat()
    {
        WoolSearchSpec.Apply(Wools, "inexistant").Should().BeEmpty();
    }
}
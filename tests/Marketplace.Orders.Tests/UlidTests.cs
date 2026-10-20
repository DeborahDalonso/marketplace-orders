using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Tests;

public class UlidTests
{
    [Fact]
    public void Gera_id_com_prefixo_e_26_caracteres_crockford()
    {
        var id = Ulid.New("ord_");

        Assert.Equal(30, id.Length);
        Assert.StartsWith("ord_", id);
        Assert.Matches("^ord_[0-9A-HJKMNP-TV-Z]{26}$", id);
    }

    [Fact]
    public void Gera_ids_diferentes_a_cada_chamada()
    {
        var ids = Enumerable.Range(0, 1000).Select(_ => Ulid.New("evt_")).ToHashSet();

        Assert.Equal(1000, ids.Count);
    }

    [Fact]
    public void Ordena_por_tempo_de_criacao()
    {
        var first = Ulid.New("ord_");
        Thread.Sleep(2);
        var second = Ulid.New("ord_");

        Assert.True(string.CompareOrdinal(first, second) < 0);
    }
}

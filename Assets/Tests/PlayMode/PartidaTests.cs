using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// O estado da partida, o placar e o recorde: o que decide quando o mundo se
/// mexe e quando ele para.
/// </summary>
public class PartidaTests
{
    [SetUp]
    public void Antes() => CenarioDeTeste.Preparar();

    [TearDown]
    public void Depois() => CenarioDeTeste.Limpar();

    [UnityTest]
    public IEnumerator Comeca_Esperando_ESoSaiDaiUmaVez()
    {
        var p = CenarioDeTeste.CriarPartida();
        yield return null;

        Assert.AreEqual(EstadoDaPartida.Esperando, p.Estado);

        p.Comecar();
        Assert.AreEqual(EstadoDaPartida.Voando, p.Estado);

        // chamar de novo não pode zerar o placar de quem já está voando
        p.Pontuar();
        p.Comecar();
        Assert.AreEqual(1, p.Pontos, "Comecar no meio do voo apagou o placar");
    }

    [UnityTest]
    public IEnumerator SoPontua_Voando()
    {
        var p = CenarioDeTeste.CriarPartida();
        yield return null;

        p.Pontuar();
        Assert.AreEqual(0, p.Pontos, "pontuou antes de a partida começar");

        p.Comecar();
        p.Pontuar();
        p.Pontuar();
        Assert.AreEqual(2, p.Pontos);

        p.Perder();
        p.Pontuar();
        Assert.AreEqual(2, p.Pontos, "pontuou depois de cair");
    }

    [UnityTest]
    public IEnumerator Recorde_GuardaOMaiorEAvisaQuandoBate()
    {
        var p = CenarioDeTeste.CriarPartida();
        yield return null;

        Assert.AreEqual(0, p.Recorde);

        p.Comecar();
        for (int i = 0; i < 5; i++) p.Pontuar();
        p.Perder();

        Assert.AreEqual(5, p.Recorde);
        Assert.IsTrue(p.BateuORecorde, "a primeira partida sempre bate o recorde");

        // uma partida pior não pode derrubar a marca
        p.Reiniciar();
        p.Comecar();
        p.Pontuar();
        p.Perder();

        Assert.AreEqual(5, p.Recorde, "uma partida pior apagou o recorde");
        Assert.IsFalse(p.BateuORecorde);
    }

    [UnityTest]
    public IEnumerator Perder_SoValeUmaVez()
    {
        var p = CenarioDeTeste.CriarPartida();
        yield return null;

        int mudancas = 0;
        p.Mudou += _ => mudancas++;

        p.Comecar();
        p.Perder();
        p.Perder();
        p.Perder();

        Assert.AreEqual(2, mudancas,
            "Perder chamado várias vezes avisou o resto do jogo mais de uma vez");
    }

    [UnityTest]
    public IEnumerator Reiniciar_ZeraOPlacarMasNaoORecorde()
    {
        var p = CenarioDeTeste.CriarPartida();
        yield return null;

        p.Comecar();
        for (int i = 0; i < 3; i++) p.Pontuar();
        p.Perder();
        p.Reiniciar();

        Assert.AreEqual(EstadoDaPartida.Esperando, p.Estado);
        Assert.AreEqual(0, p.Pontos);
        Assert.AreEqual(3, p.Recorde, "reiniciar não pode apagar a marca");
    }

    [UnityTest]
    public IEnumerator Medalha_SobeComOPlacar()
    {
        Assert.IsNull(Interface.MedalhaDe(0).nome, "sem medalha para quem não pontuou");
        Assert.IsNull(Interface.MedalhaDe(4).nome);
        Assert.AreEqual("bronze", Interface.MedalhaDe(5).nome);
        Assert.AreEqual("prata", Interface.MedalhaDe(15).nome);
        Assert.AreEqual("ouro", Interface.MedalhaDe(30).nome);
        Assert.AreEqual("ouro", Interface.MedalhaDe(999).nome);
        yield return null;
    }
}

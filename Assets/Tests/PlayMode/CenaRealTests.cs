using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Abre a cena que vai no executável e mexe nela.
///
/// Os outros testes montam um cenário de mentira e medem uma regra de cada
/// vez. Isso deixa passar a classe de defeito que só existe na cena montada:
/// referência não ligada, objeto fora da camada, peça faltando. Aqui a cena é
/// a mesma que o jogador abre.
/// </summary>
public class CenaRealTests
{
    [UnitySetUp]
    public IEnumerator Antes()
    {
        PlayerPrefs.DeleteKey("bicudo.recorde");
        PlayerPrefs.Save();
        yield return SceneManager.LoadSceneAsync("Jogo", LoadSceneMode.Single);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ACena_TemTudoQueOJogoPrecisa()
    {
        Assert.IsNotNull(Object.FindObjectOfType<Partida>(), "sem a partida");
        Assert.IsNotNull(Object.FindObjectOfType<Passaro>(), "sem o pássaro");
        Assert.IsNotNull(Object.FindObjectOfType<GeradorDeCanos>(), "sem os canos");
        Assert.IsNotNull(Object.FindObjectOfType<Interface>(), "sem a interface");
        Assert.IsNotNull(Object.FindObjectOfType<Controles>(), "sem os controles");
        Assert.IsNotNull(Object.FindObjectOfType<Sons>(), "sem os sons");
        Assert.IsNotNull(Camera.main, "sem câmera principal");
        yield return null;
    }

    [UnityTest]
    public IEnumerator AsReferencias_EstaoLigadas()
    {
        var inter = Object.FindObjectOfType<Interface>();
        Assert.IsNotNull(inter.passaro, "a interface não acha o pássaro e não sabe reiniciar");
        Assert.IsNotNull(inter.canos, "a interface não acha os canos e deixaria a tela suja");
        Assert.IsNotNull(inter.controles, "sem isto o toque que reinicia também bate as asas");

        var controles = Object.FindObjectOfType<Controles>();
        Assert.IsNotNull(controles.passaro, "os controles não chegam no pássaro");

        var gerador = Object.FindObjectOfType<GeradorDeCanos>();
        Assert.IsNotNull(gerador.modelo, "o gerador está sem modelo de cano");
        yield return null;
    }

    [UnityTest]
    public IEnumerator NadaEmCena_EstaSemDesenho()
    {
        var mudos = Object.FindObjectsOfType<SpriteRenderer>()
                          .Where(sr => sr.sprite == null && sr.enabled)
                          .Select(sr => sr.name)
                          .Distinct()
                          .ToList();

        Assert.IsEmpty(mudos, "objetos visíveis sem sprite: " + string.Join(", ", mudos.Take(8)));
        yield return null;
    }

    [UnityTest]
    public IEnumerator OJogo_ComecaParadoNaTelaInicial()
    {
        var partida = Object.FindObjectOfType<Partida>();
        var passaro = Object.FindObjectOfType<Passaro>();

        Assert.AreEqual(EstadoDaPartida.Esperando, partida.Estado);

        float altura = passaro.transform.position.y;
        for (float t = 0f; t < 1f; t += Time.deltaTime) yield return null;

        Assert.AreEqual(altura, passaro.transform.position.y, 0.6f,
            "o pássaro caiu antes de a partida começar");
    }

    [UnityTest]
    public IEnumerator UmToque_ComecaAPartidaEOMundoAnda()
    {
        var partida = Object.FindObjectOfType<Partida>();
        var passaro = Object.FindObjectOfType<Passaro>();
        var gerador = Object.FindObjectOfType<GeradorDeCanos>();

        var primeiro = gerador.Pares[0];
        float antes = primeiro.transform.localPosition.x;

        passaro.Bater();
        Assert.AreEqual(EstadoDaPartida.Voando, partida.Estado);

        for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;

        Assert.Less(primeiro.transform.localPosition.x, antes,
            "a partida começou e os canos não saíram do lugar");
    }

    [UnityTest]
    public IEnumerator OPassaro_MorreAoEncostarNoChao()
    {
        var partida = Object.FindObjectOfType<Partida>();
        var passaro = Object.FindObjectOfType<Passaro>();

        passaro.Bater();
        Assert.AreEqual(EstadoDaPartida.Voando, partida.Estado);

        // deixa cair sem bater mais asa nenhuma
        for (float t = 0f; t < 6f && partida.Estado == EstadoDaPartida.Voando; t += Time.deltaTime)
            yield return null;

        Assert.AreEqual(EstadoDaPartida.Caiu, partida.Estado,
            "o pássaro atravessou o chão em vez de morrer nele");
    }

    [UnityTest]
    public IEnumerator OChao_EstaNaCamadaQueMata()
    {
        var chao = GameObject.Find("Chao");
        Assert.IsNotNull(chao, "a cena está sem chão");
        Assert.AreEqual(PrefabsDoBicudo_Camada, chao.layer,
            "o chão fora da camada consultada pelo pássaro: dá para cair para fora do mundo");
        Assert.IsNotNull(chao.GetComponent<Collider2D>(), "o chão está sem colisor");
        yield return null;
    }

    /// <summary>
    /// A camada do cenário, repetida aqui de propósito.
    ///
    /// PrefabsDoBicudo vive na assembly de editor e não existe no executável.
    /// Este teste roda nos dois lados, então o número entra direto.
    /// </summary>
    const int PrefabsDoBicudo_Camada = 8;
}

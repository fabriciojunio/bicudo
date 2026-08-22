using System.Collections;
using System.Collections.Generic;
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
        Assert.IsNotNull(gerador.passaro,
            "o gerador não sabe onde está o pássaro: o placar nunca sairia do zero");
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

    [UnityTest]
    public IEnumerator OPlacar_ContaAoPassarOsCanos()
    {
        var partida = Object.FindObjectOfType<Partida>();
        var passaro = Object.FindObjectOfType<Passaro>();
        var gerador = Object.FindObjectOfType<GeradorDeCanos>();
        gerador.velocidade = 14f;      // adianta a chegada dos canos

        // O que se mede aqui é o placar, não a colisão. Sem desligar o que
        // mata, o pássaro morre antes de o primeiro cano chegar e os canos
        // param junto com a partida: o teste falharia por outro motivo.
        passaro.camadasQueMatam = 0;
        passaro.Bater();

        for (float t = 0f; t < 5f && partida.Pontos == 0; t += Time.deltaTime)
            yield return null;

        Assert.Greater(partida.Pontos, 0,
            "os canos passaram e o placar não contou nada na cena de verdade");
    }

    [UnityTest]
    public IEnumerator OCenario_NuncaAbreVazioNasBordas()
    {
        // Relato do jogador: o chão sumia enquanto ele voava. A causa era a
        // fila de peças começar a cobrir só a partir do zero, deixando toda a
        // metade esquerda da tela mostrando céu de tempos em tempos.
        var passaro = Object.FindObjectOfType<Passaro>();
        passaro.camadasQueMatam = 0;      // o que se mede é o cenário, não a queda
        passaro.Bater();

        var chao = GameObject.Find("Chao").GetComponent<FaixaQueRola>();
        var fundo = GameObject.Find("Fundo").GetComponent<FaixaQueRola>();
        Assert.IsNotNull(chao, "o chão não rola");
        Assert.IsNotNull(fundo, "o céu não rola");

        chao.velocidade = 40f;            // apressa as voltas da fila
        fundo.velocidade = 40f;
        yield return null;

        float meia = Enquadramento.MeiaLargura();

        for (float t = 0f; t < 6f; t += Time.deltaTime)
        {
            yield return null;
            Assert.LessOrEqual(chao.BordaEsquerda(), -meia + 0.01f, "abriu céu à esquerda do chão");
            Assert.GreaterOrEqual(chao.BordaDireita(), meia - 0.01f, "abriu céu à direita do chão");
            Assert.LessOrEqual(fundo.BordaEsquerda(), -meia + 0.01f, "faltou fundo à esquerda");
            Assert.GreaterOrEqual(fundo.BordaDireita(), meia - 0.01f, "faltou fundo à direita");
        }
    }

    [UnityTest]
    public IEnumerator OsCanos_NaTelaInicial_EsperamAVistaELonge()
    {
        // Empurrar a entrada dos canos para fora da tela resolveu o cano
        // aparecendo do nada no meio do voo, mas criou o mesmo defeito na
        // largada: o primeiro par saía do nada assim que a partida começava.
        // Por isso eles esperam à vista, parados ao longe.
        var partida = Object.FindObjectOfType<Partida>();
        var passaro = Object.FindObjectOfType<Passaro>();
        var gerador = Object.FindObjectOfType<GeradorDeCanos>();

        Assert.AreEqual(EstadoDaPartida.Esperando, partida.Estado);
        yield return null;

        foreach (var c in gerador.Pares)
        {
            Assert.IsTrue(c.gameObject.activeInHierarchy,
                "cano escondido na espera: apareceria do nada ao começar a partida");
            Assert.Greater(c.transform.position.x - passaro.transform.position.x, 6f,
                "cano parado perto demais do pássaro na largada");
        }
    }

    [UnityTest]
    public IEnumerator OsCanos_SoTrocamDeLugarForaDaTela()
    {
        // Relato do jogador: ao passar do cano, ele sumia atrás e reaparecia na
        // frente. O par nunca é destruído, é reaproveitado, e a troca estava
        // acontecendo em x fixo, dentro do campo de visão.
        var passaro = Object.FindObjectOfType<Passaro>();
        var gerador = Object.FindObjectOfType<GeradorDeCanos>();

        passaro.camadasQueMatam = 0;      // o que se mede é a troca, não a morte
        gerador.velocidade = 20f;         // força várias reposições em poucos segundos
        passaro.Bater();
        yield return null;

        float meia = Enquadramento.MeiaLargura();
        Assert.Less(gerador.xDeSaida, -meia, "a saída dos canos caiu dentro da tela");
        Assert.Greater(gerador.xDeEntrada, meia, "a entrada dos canos caiu dentro da tela");

        var anterior = new Dictionary<Cano, float>();
        foreach (var c in gerador.Pares) anterior[c] = c.transform.position.x;

        int trocas = 0;
        for (float t = 0f; t < 6f; t += Time.deltaTime)
        {
            yield return null;

            // Um quadro longo deixa o cano passar da saída antes de ser pego.
            // Isso é da máquina, não do enquadramento, então entra na conta.
            float folgaDeQuadro = gerador.velocidade * Time.deltaTime + 0.01f;

            foreach (var c in gerador.Pares)
            {
                float agora = c.transform.position.x;
                float antes = anterior[c];

                if (agora > antes)        // andou para a direita: foi reposto
                {
                    trocas++;
                    Assert.LessOrEqual(antes, gerador.xDeSaida + folgaDeQuadro,
                        "o cano sumiu antes da saída, com o jogador olhando");
                    Assert.Greater(agora, meia,
                        "o cano reapareceu dentro da tela em vez de fora dela");
                }

                anterior[c] = agora;
            }
        }

        Assert.Greater(trocas, 0, "nenhum cano foi reposto: o teste não mediu nada");
    }

    /// <summary>
    /// A camada do cenário, repetida aqui de propósito.
    ///
    /// PrefabsDoBicudo vive na assembly de editor e não existe no executável.
    /// Este teste roda nos dois lados, então o número entra direto.
    /// </summary>
    const int PrefabsDoBicudo_Camada = 8;
}

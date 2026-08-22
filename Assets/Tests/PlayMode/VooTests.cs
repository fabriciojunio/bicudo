using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// O voo: gravidade, impulso, colisão e a contagem de pontos.
///
/// É o que o jogador sente a cada segundo, e o que mais dói quando quebra sem
/// ninguém perceber.
/// </summary>
public class VooTests
{
    [SetUp]
    public void Antes() => CenarioDeTeste.Preparar();

    [TearDown]
    public void Depois() => CenarioDeTeste.Limpar();

    [UnityTest]
    public IEnumerator Esperando_OPassaroNaoCai()
    {
        CenarioDeTeste.CriarPartida();
        var p = CenarioDeTeste.CriarPassaro(new Vector2(0f, 1f));
        yield return null;

        float alturaInicial = p.transform.position.y;
        for (float t = 0f; t < 1f; t += Time.deltaTime) yield return null;

        Assert.AreEqual(alturaInicial, p.transform.position.y, 0.5f,
            "na tela inicial o pássaro balança no lugar, não despenca");
    }

    [UnityTest]
    public IEnumerator Voando_AGravidadePuxaParaBaixo()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var p = CenarioDeTeste.CriarPassaro(new Vector2(0f, 3f));
        yield return null;

        partida.Comecar();
        float alturaInicial = p.transform.position.y;

        for (float t = 0f; t < 0.5f; t += Time.deltaTime) yield return null;

        Assert.Less(p.transform.position.y, alturaInicial - 0.3f,
            "solto no ar, ele tem que cair");
    }

    [UnityTest]
    public IEnumerator Bater_SobeSempreOMesmoTanto()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var p = CenarioDeTeste.CriarPassaro(new Vector2(0f, 3f));
        yield return null;
        partida.Comecar();

        // O impulso troca a velocidade em vez de somar a ela. Se somasse, dois
        // toques seguidos mandariam o pássaro para fora da tela, e o jogo
        // deixaria de ser sobre ritmo.
        p.Bater();
        float primeira = p.VelocidadeVertical;

        p.Bater();
        float segunda = p.VelocidadeVertical;

        Assert.AreEqual(primeira, segunda, 0.001f,
            "dois toques seguidos somaram velocidade em vez de trocá-la");
        Assert.AreEqual(p.impulso, primeira, 0.001f);
    }

    [UnityTest]
    public IEnumerator Bater_NaTelaInicial_ComecaAPartida()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var p = CenarioDeTeste.CriarPassaro(new Vector2(0f, 1f));
        yield return null;

        Assert.AreEqual(EstadoDaPartida.Esperando, partida.Estado);

        p.Bater();
        Assert.AreEqual(EstadoDaPartida.Voando, partida.Estado,
            "o primeiro toque tem que largar o pássaro, sem tela intermediária");
    }

    [UnityTest]
    public IEnumerator Encostar_NoCenario_Encerra()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var p = CenarioDeTeste.CriarPassaro(new Vector2(0f, 3f));
        CenarioDeTeste.CriarParede(new Vector2(0f, 0f), new Vector2(10f, 2f));
        yield return null;

        partida.Comecar();

        for (float t = 0f; t < 3f && partida.Estado == EstadoDaPartida.Voando; t += Time.deltaTime)
            yield return null;

        Assert.AreEqual(EstadoDaPartida.Caiu, partida.Estado,
            "caiu em cima da parede e a partida continuou");
    }

    [UnityTest]
    public IEnumerator QuedaMaxima_LimitaAVelocidade()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var p = CenarioDeTeste.CriarPassaro(new Vector2(0f, 50f));
        yield return null;
        partida.Comecar();

        for (float t = 0f; t < 3f; t += Time.deltaTime) yield return null;

        Assert.GreaterOrEqual(p.VelocidadeVertical, -p.quedaMaxima - 0.01f,
            "sem teto de queda não dá tempo de reagir ao chão");
    }

    [UnityTest]
    public IEnumerator Recolocar_DevolveOPassaroAoBerco()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var p = CenarioDeTeste.CriarPassaro(new Vector2(0f, 2f));
        yield return null;

        var berco = p.Berco;
        partida.Comecar();
        for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;

        p.Recolocar();

        Assert.AreEqual(berco, p.transform.position);
        Assert.AreEqual(0f, p.VelocidadeVertical, 0.001f);
        Assert.AreEqual(Quaternion.identity, p.transform.rotation);
    }

    // ------------------------------------------------------------------ canos

    [UnityTest]
    public IEnumerator Cano_ContaOPontoUmaVezSo()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var cano = CenarioDeTeste.CriarCano(new Vector2(0f, 0f));
        yield return null;
        partida.Comecar();

        // A trava existe porque o gerador confere posição a cada quadro: sem
        // ela, o mesmo cano contaria ponto em todos os quadros em que estiver
        // atrás do pássaro.
        Assert.IsTrue(cano.TentarPontuar(), "a primeira passagem vale ponto");
        Assert.IsFalse(cano.TentarPontuar(), "a segunda no mesmo cano não vale");
        Assert.IsFalse(cano.TentarPontuar());
    }

    [UnityTest]
    public IEnumerator Cano_Armar_AbreAPassagemNaAlturaPedida()
    {
        var cano = CenarioDeTeste.CriarCano(new Vector2(0f, 0f), meiaAbertura: 1.5f);
        yield return null;

        cano.Armar(2f);

        Assert.AreEqual(2f, cano.AlturaDaPassagem, 0.001f);
        Assert.AreEqual(3.5f, cano.canoDeCima.localPosition.y, 0.001f);
        Assert.AreEqual(0.5f, cano.canoDeBaixo.localPosition.y, 0.001f);
        Assert.IsFalse(cano.JaPontuou, "armar de novo devolve o cano ao estado limpo");
    }

    [UnityTest]
    public IEnumerator Gerador_MantemOsCanosEmCirculacao()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var modelo = CenarioDeTeste.CriarCano(new Vector2(100f, 0f));
        var g = CenarioDeTeste.CriarGerador(modelo);
        yield return null;

        int quantos = g.Pares.Count;
        Assert.AreEqual(g.quantidade, quantos, "o gerador não montou a fila");

        partida.Comecar();
        for (float t = 0f; t < 6f; t += Time.deltaTime) yield return null;

        Assert.AreEqual(quantos, g.Pares.Count,
            "a quantidade de canos mudou: alguém está criando ou destruindo no meio do jogo");

        foreach (var p in g.Pares)
            Assert.GreaterOrEqual(p.transform.localPosition.x, g.xDeSaida - 1f,
                "um cano ficou para trás em vez de voltar para o fim da fila");
    }

    [UnityTest]
    public IEnumerator Gerador_LimpaOCaminhoAoVoltarParaAEspera()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var modelo = CenarioDeTeste.CriarCano(new Vector2(100f, 0f));
        var g = CenarioDeTeste.CriarGerador(modelo);
        yield return null;

        partida.Comecar();
        for (float t = 0f; t < 2f; t += Time.deltaTime) yield return null;

        partida.Perder();
        partida.Reiniciar();
        yield return null;

        // Um cano parado na frente do pássaro no instante em que a partida
        // recomeça é uma morte que o jogador não teve como evitar.
        foreach (var p in g.Pares)
            Assert.GreaterOrEqual(p.transform.localPosition.x, g.xDeEntrada - 0.01f,
                "sobrou cano na tela depois de reiniciar");
    }

    [UnityTest]
    public IEnumerator Gerador_ContaOPontoQuandoOCanoPassaOPassaro()
    {
        // O teste que faltava. A primeira versão contava ponto por gatilho e
        // nunca disparou, porque o pássaro não tem colisor. Os testes de então
        // chamavam TentarPontuar direto e passavam, enquanto o jogo montado
        // ficava a partida inteira em zero.
        var partida = CenarioDeTeste.CriarPartida();
        var passaro = CenarioDeTeste.CriarPassaro(new Vector2(0f, 1f));
        var modelo = CenarioDeTeste.CriarCano(new Vector2(100f, 0f));
        var g = CenarioDeTeste.CriarGerador(modelo, passaro.transform);
        g.velocidade = 12f;
        yield return null;

        partida.Comecar();
        Assert.AreEqual(0, partida.Pontos);

        for (float t = 0f; t < 3f && partida.Pontos == 0; t += Time.deltaTime)
            yield return null;

        Assert.Greater(partida.Pontos, 0,
            "os canos passaram pelo pássaro e o placar continuou zerado");
    }

    [UnityTest]
    public IEnumerator Gerador_NaoContaOMesmoCanoDuasVezes()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var passaro = CenarioDeTeste.CriarPassaro(new Vector2(0f, 1f));
        var modelo = CenarioDeTeste.CriarCano(new Vector2(100f, 0f));
        var g = CenarioDeTeste.CriarGerador(modelo, passaro.transform);
        g.quantidade = 1;
        g.velocidade = 6f;
        yield return null;

        partida.Comecar();
        for (float t = 0f; t < 2f; t += Time.deltaTime) yield return null;

        // A conferência roda a cada quadro. Sem a trava, o mesmo cano contaria
        // ponto em todos os quadros em que estivesse atrás do pássaro.
        Assert.LessOrEqual(partida.Pontos, 2,
            $"um cano só rendeu {partida.Pontos} pontos: a trava não segurou");
    }

    [UnityTest]
    public IEnumerator Gerador_Enquadrado_TrocaOCanoDeLugarForaDaTela()
    {
        // O outro lado do mesmo defeito: em tela larga o cano sumia poucos
        // passos atrás do pássaro e reaparecia do nada à frente dele, porque a
        // entrada e a saída estavam presas na cena, em unidades que davam certo
        // só em 16:9.
        CenarioDeTeste.CriarPartida();
        CenarioDeTeste.CriarCamera(meiaAltura: 7f, proporcao: 21f / 9f);

        var modelo = CenarioDeTeste.CriarCano(new Vector2(100f, 0f));
        var g = CenarioDeTeste.CriarGerador(modelo);
        g.ajustarPelaCamera = true;
        yield return null;

        float meia = Enquadramento.MeiaLargura();

        Assert.Less(g.xDeSaida, -meia, "o cano sairia de cena com o jogador olhando");
        Assert.Greater(g.xDeEntrada, meia, "o cano apareceria já dentro da tela");
        Assert.GreaterOrEqual(g.quantidade * g.espacamento, g.xDeEntrada - g.xDeSaida,
            "a fila é curta: o cano reposto cairia dentro da tela mesmo assim");
    }

    [UnityTest]
    public IEnumerator Gerador_NaEspera_PoeOPrimeiroCanoNaDistanciaDeLargada()
    {
        CenarioDeTeste.CriarPartida();
        var modelo = CenarioDeTeste.CriarCano(new Vector2(100f, 0f));
        var g = CenarioDeTeste.CriarGerador(modelo);
        g.xDeEntrada = 30f;                 // como fica numa tela bem larga
        g.distanciaDaLargada = 9f;
        yield return null;

        float menor = float.MaxValue;
        foreach (var p in g.Pares) menor = Mathf.Min(menor, p.transform.localPosition.x);

        // Sem isto, quanto mais larga a tela, mais tempo de céu vazio antes do
        // primeiro cano: a espera passaria de cinco segundos em 21:9.
        Assert.AreEqual(9f, menor, 0.01f,
            "o primeiro cano ficou esperando na borda de entrada");
    }

    // ----------------------------------------------------------------- cenário

    [UnityTest]
    public IEnumerator Faixa_RolaSemAbrirVao()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var f = CenarioDeTeste.CriarFaixa(quantasPecas: 3, largura: 6f);
        yield return null;
        partida.Comecar();

        for (float t = 0f; t < 5f; t += Time.deltaTime)
        {
            yield return null;
            Assert.LessOrEqual(f.MaiorVaoEntrePecas(), 6f + 0.5f,
                "abriu um vão entre as peças: a reposição saiu de múltiplo da largura");
        }
    }

    [UnityTest]
    public IEnumerator Faixa_Enquadrada_CobreATelaDePontaAPonta()
    {
        // O defeito que este teste guarda: o chão sumia pela borda esquerda e
        // voltava sozinho enquanto o jogador voava. A peça mais à esquerda
        // nunca passava de menos uma largura, então a fila começava a cobrir só
        // a partir do zero, e tudo à esquerda disso ficava mostrando céu.
        var partida = CenarioDeTeste.CriarPartida();
        CenarioDeTeste.CriarCamera(meiaAltura: 7f, proporcao: 21f / 9f);

        var f = CenarioDeTeste.CriarFaixa(quantasPecas: 4, largura: 40f);
        f.ajustarPelaCamera = true;
        f.velocidade = 40f;               // apressa as voltas da fila
        yield return null;

        float meia = Enquadramento.MeiaLargura();
        Assert.Greater(meia, 16f, "a câmera de teste saiu estreita: não mediria nada");

        partida.Comecar();

        for (float t = 0f; t < 5f; t += Time.deltaTime)
        {
            yield return null;
            Assert.LessOrEqual(f.BordaEsquerda(), -meia + 0.01f,
                "abriu vazio na borda esquerda da tela");
            Assert.GreaterOrEqual(f.BordaDireita(), meia - 0.01f,
                "abriu vazio na borda direita da tela");
        }
    }

    [UnityTest]
    public IEnumerator Faixa_CurtaDemais_Avisa()
    {
        // Falhar calado aqui custa caro: o sintoma é o cenário piscando na
        // borda, e nada no console apontando para a causa.
        CenarioDeTeste.CriarCamera(meiaAltura: 7f, proporcao: 21f / 9f);
        var f = CenarioDeTeste.CriarFaixa(quantasPecas: 2, largura: 4f);
        f.ajustarPelaCamera = true;

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*não cobre.*"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator Faixa_ParadaEnquantoNaoComeca()
    {
        var partida = CenarioDeTeste.CriarPartida();
        var f = CenarioDeTeste.CriarFaixa(quantasPecas: 2, largura: 6f);
        f.rolaSempre = false;
        yield return null;

        float antes = f.pecas[0].localPosition.x;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime) yield return null;

        Assert.AreEqual(antes, f.pecas[0].localPosition.x, 0.001f,
            "o chão andou antes de a partida começar");
    }
}

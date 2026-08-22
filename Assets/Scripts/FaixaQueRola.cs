using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uma faixa que anda para a esquerda sem fim: chão e fundo usam a mesma peça.
///
/// Carrega cópias enfileiradas e devolve para a ponta direita a que sai de
/// vista, sempre em múltiplos exatos da largura. Em qualquer outro passo
/// aparece uma emenda, e é o tipo de falha que só se enxerga depois de um
/// tempo olhando.
/// </summary>
public class FaixaQueRola : MonoBehaviour
{
    [Tooltip("Velocidade em unidades por segundo. O fundo usa menos que o chão.")]
    public float velocidade = 3.2f;

    [Tooltip("Se continua rolando com o jogo parado. O fundo da tela inicial usa isto.")]
    public bool rolaSempre = false;

    [Tooltip("As cópias enfileiradas. Precisam sobrar para além das duas bordas.")]
    public Transform[] pecas;

    [Tooltip("Largura de uma cópia, em unidades.")]
    public float largura = 8f;

    [Tooltip("Se calcula sozinha onde repor a peça, pela largura real da tela.")]
    public bool ajustarPelaCamera = true;

    [Tooltip("X em que a peça volta para o fim da fila. Tem que estar fora da tela.")]
    public float limiteEsquerdo = -8f;

    [Tooltip("Sobra além da borda da tela, para a troca não encostar no canto.")]
    public float folgaForaDaTela = 2f;

    Partida partida;
    float meiaConhecida;

    void Start()
    {
        partida = Partida.Atual;
        meiaConhecida = Enquadramento.MeiaLargura();
        if (ajustarPelaCamera) Enquadrar();
    }

    /// <summary>
    /// Escolhe onde repor a peça e alinha a fila a partir daí.
    ///
    /// A peça mais à esquerda nunca fica além do limite, então a fila só passa
    /// a cobrir de limite mais uma largura em diante. Com o limite valendo
    /// menos uma largura, que era o valor de antes, essa borda caía em zero: a
    /// metade esquerda da tela ficava sem chão e ele voltava sozinho, quadro a
    /// quadro, como se o cenário piscasse. O limite tem que descontar também a
    /// meia tela.
    /// </summary>
    public void Enquadrar()
    {
        if (pecas == null || pecas.Length == 0 || largura <= 0.01f) return;

        float meia = Enquadramento.MeiaLargura();
        float alcance = largura * pecas.Length;

        limiteEsquerdo = -(meia + largura + folgaForaDaTela);

        // Do limite para a direita a fila cobre o alcance dela. Se for curta
        // demais para a tela, empurrar o limite só troca o buraco de lado.
        // Prende no que dá e avisa, em vez de falhar calado.
        float limiteMinimo = meia - alcance;
        if (limiteEsquerdo < limiteMinimo)
        {
            limiteEsquerdo = limiteMinimo;
            Debug.LogWarning($"[Bicudo] a faixa {name} tem {pecas.Length} peças de {largura} " +
                             $"e não cobre os {meia * 2f:0.0} de tela: vai faltar borda.");
        }

        Reenfileirar();
    }

    /// <summary>
    /// Põe as peças em fila a partir do limite, sem esperar a rolagem acertar.
    ///
    /// Sem isto os primeiros segundos rodam com o arranjo que veio da cena, e o
    /// jogador vê o cenário se ajeitando sozinho na abertura.
    /// </summary>
    void Reenfileirar()
    {
        var ordem = new List<Transform>();
        foreach (var p in pecas) if (p != null) ordem.Add(p);
        ordem.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

        for (int i = 0; i < ordem.Count; i++)
        {
            var pos = ordem[i].localPosition;
            pos.x = limiteEsquerdo + largura * (i + 1);
            ordem[i].localPosition = pos;
        }
    }

    void Update()
    {
        // O mesmo executável abre num monitor ultrawide e num notebook, e a
        // janela muda de tamanho no meio da partida. Refazer a conta aqui é o
        // que faz o cenário se arrumar sozinho em vez de ficar preso na
        // proporção que existia na abertura. Realinhar não aparece: as peças
        // são iguais entre si.
        if (ajustarPelaCamera && Enquadramento.Mudou(ref meiaConhecida)) Enquadrar();

        if (!PodeRolar()) return;
        if (pecas == null || pecas.Length == 0 || largura <= 0.01f) return;

        float passo = velocidade * Time.deltaTime;
        float volta = largura * pecas.Length;

        foreach (var p in pecas)
        {
            if (p == null) continue;

            p.localPosition += Vector3.left * passo;

            // enquanto, e não se: com quadro longo a peça pode passar do
            // limite mais de uma largura de uma vez
            while (p.localPosition.x <= limiteEsquerdo)
                p.localPosition += Vector3.right * volta;
        }
    }

    bool PodeRolar()
    {
        if (rolaSempre) return true;
        if (partida == null) return true;
        return partida.Estado == EstadoDaPartida.Voando;
    }

    /// <summary>Maior vão entre duas cópias vizinhas. Usado pelos testes.</summary>
    public float MaiorVaoEntrePecas()
    {
        if (pecas == null || pecas.Length < 2) return 0f;

        var xs = new List<float>();
        foreach (var p in pecas) if (p != null) xs.Add(p.localPosition.x);
        xs.Sort();

        float maior = 0f;
        for (int i = 1; i < xs.Count; i++) maior = Mathf.Max(maior, xs[i] - xs[i - 1]);
        return maior;
    }

    /// <summary>Ponto mais à esquerda que a fila desenha, em coordenada de mundo.</summary>
    public float BordaEsquerda()
    {
        float menor = float.MaxValue;
        foreach (var p in pecas) if (p != null) menor = Mathf.Min(menor, p.position.x);
        return menor;
    }

    /// <summary>Ponto mais à direita que a fila desenha, em coordenada de mundo.</summary>
    public float BordaDireita()
    {
        float maior = float.MinValue;
        foreach (var p in pecas) if (p != null) maior = Mathf.Max(maior, p.position.x);
        return maior + largura;
    }
}

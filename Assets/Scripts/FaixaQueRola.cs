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

    [Tooltip("As cópias enfileiradas. Duas bastam se cada uma cobrir a tela.")]
    public Transform[] pecas;

    [Tooltip("Largura de uma cópia, em unidades.")]
    public float largura = 8f;

    Partida partida;

    void Start() { partida = Partida.Atual; }

    void Update()
    {
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
            while (p.localPosition.x <= -largura)
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

        var xs = new System.Collections.Generic.List<float>();
        foreach (var p in pecas) if (p != null) xs.Add(p.localPosition.x);
        xs.Sort();

        float maior = 0f;
        for (int i = 1; i < xs.Count; i++) maior = Mathf.Max(maior, xs[i] - xs[i - 1]);
        return maior;
    }
}

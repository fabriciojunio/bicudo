using UnityEngine;

/// <summary>
/// Quanto de mundo a câmera enxerga para cada lado do centro.
///
/// A altura visível é escolhida por nós, mas a largura sai da proporção da
/// tela, e proporção só existe quando o jogo roda. Deixar largura chutada na
/// cena foi o que fez cano e chão sumirem na frente do jogador: em tela larga,
/// o ponto onde a peça volta para o fim da fila caía dentro do campo de visão.
///
/// Medir uma vez no Start também não basta. O mesmo executável abre num
/// monitor ultrawide e num notebook, e a janela pode ser redimensionada ou
/// jogada em tela cheia no meio da partida. Quem depende desta conta precisa
/// refazê-la quando a tela muda, e é para isso que serve o Mudou.
/// </summary>
public static class Enquadramento
{
    /// <summary>
    /// Usado quando não há câmera, como nos testes de bancada.
    ///
    /// Equivale a uma tela 16:9 com a altura deste jogo. Não serve para o
    /// executável, e sim para o cálculo não virar zero e esconder o erro.
    /// </summary>
    public const float MeiaLarguraDeReserva = 12.5f;

    public static float MeiaLargura()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return MeiaLarguraDeReserva;

        float meia = cam.orthographicSize * cam.aspect;
        return meia > 0.01f ? meia : MeiaLarguraDeReserva;
    }

    /// <summary>
    /// Diz se a medida mudou desde a última vez que se perguntou, e já anota a
    /// nova.
    ///
    /// Vigia a própria meia largura, e não a janela em pixels: é este o número
    /// de que o cenário depende, e uma janela mais alta na mesma proporção não
    /// muda nada. Uma conta por quadro é barata, e é o preço de o cenário se
    /// arrumar sozinho ao trocar de máquina, de janela ou de tela cheia em vez
    /// de ficar preso na proporção que existia na abertura.
    /// </summary>
    public static bool Mudou(ref float conhecida)
    {
        float agora = MeiaLargura();
        if (Mathf.Abs(agora - conhecida) < 0.001f) return false;

        conhecida = agora;
        return true;
    }
}

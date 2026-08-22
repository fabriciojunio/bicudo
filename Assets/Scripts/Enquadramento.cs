using UnityEngine;

/// <summary>
/// Quanto de mundo a câmera enxerga para cada lado do centro.
///
/// A altura visível é escolhida por nós, mas a largura sai da proporção da
/// tela, e proporção só existe quando o jogo roda. Deixar largura chutada na
/// cena foi o que fez cano e chão sumirem na frente do jogador: em tela larga,
/// o ponto onde a peça volta para o fim da fila caía dentro do campo de visão.
/// </summary>
public static class Enquadramento
{
    /// <summary>
    /// Usado quando não há câmera, como nos testes de bancada.
    ///
    /// Equivale a uma tela 16:9 com a altura do jogo. Não serve para o
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
}

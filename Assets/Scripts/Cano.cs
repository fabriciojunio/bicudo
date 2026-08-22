using UnityEngine;

/// <summary>
/// Um par de canos com a passagem no meio.
///
/// O par anda para a esquerda por conta própria e se anuncia ao gerador
/// quando sai da tela, para ser reaproveitado. Criar e destruir um par a cada
/// dois segundos funcionaria, mas enche o coletor de lixo de trabalho durante
/// a partida, que é justamente quando um engasgo custa caro.
/// </summary>
public class Cano : MonoBehaviour
{
    [Tooltip("Metade da abertura, em unidades. A passagem inteira é o dobro disto.")]
    public float meiaAbertura = 1.35f;

    [Tooltip("O cano de cima, que desce do teto.")]
    public Transform canoDeCima;

    [Tooltip("O cano de baixo, que sobe do chão.")]
    public Transform canoDeBaixo;

    [Tooltip("Gatilho que conta o ponto quando o pássaro atravessa.")]
    public Collider2D portaria;

    /// <summary>Verdadeiro depois que o pássaro já passou e o ponto foi contado.</summary>
    public bool JaPontuou { get; private set; }

    /// <summary>Onde fica o centro da passagem, em altura de mundo.</summary>
    public float AlturaDaPassagem { get; private set; }

    /// <summary>Posiciona a abertura e devolve o par ao estado de não pontuado.</summary>
    public void Armar(float alturaDaPassagem)
    {
        AlturaDaPassagem = alturaDaPassagem;
        JaPontuou = false;

        if (canoDeCima != null)
            canoDeCima.localPosition = new Vector3(0f, alturaDaPassagem + meiaAbertura, 0f);

        if (canoDeBaixo != null)
            canoDeBaixo.localPosition = new Vector3(0f, alturaDaPassagem - meiaAbertura, 0f);
    }

    /// <summary>
    /// Marca o ponto uma vez só.
    ///
    /// O gatilho da portaria fica dentro da passagem, e o pássaro pode entrar
    /// e sair dele mais de uma vez subindo e descendo no mesmo vão. Sem esta
    /// trava, um voo trêmulo no meio do cano valeria vários pontos.
    /// </summary>
    public bool TentarPontuar()
    {
        if (JaPontuou) return false;
        JaPontuou = true;
        return true;
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.GetComponent<Passaro>() == null) return;
        if (!TentarPontuar()) return;

        Partida.Atual?.Pontuar();
        Sons.Instancia?.Pontuou();
    }
}

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
    public float meiaAbertura = 2.5f;

    [Tooltip("O cano de cima, que desce do teto.")]
    public Transform canoDeCima;

    [Tooltip("O cano de baixo, que sobe do chão.")]
    public Transform canoDeBaixo;

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
    /// Quem chama é o gerador, comparando a posição do cano com a do pássaro.
    /// A primeira versão usava um gatilho no meio da passagem, e ele nunca
    /// disparou: o pássaro não tem colisor, porque detecta batida por consulta
    /// de círculo. Gatilho precisa de colisor dos dois lados.
    /// </summary>
    public bool TentarPontuar()
    {
        if (JaPontuou) return false;
        JaPontuou = true;
        return true;
    }
}

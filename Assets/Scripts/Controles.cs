using UnityEngine;

/// <summary>
/// Lê o comando de bater as asas e repassa ao pássaro.
///
/// Fica separado do Passaro de propósito: assim a entrada pode ser desligada
/// pela interface (com a tela de fim aberta, por exemplo) sem mexer na física,
/// e os testes conseguem chamar Bater direto sem simular teclado.
/// </summary>
public class Controles : MonoBehaviour
{
    public Passaro passaro;

    [Tooltip("Quando falso, nada do que o jogador apertar chega ao pássaro.")]
    public bool aceitandoComandos = true;

    void Update()
    {
        if (!aceitandoComandos || passaro == null) return;
        if (Mandou()) passaro.Bater();
    }

    /// <summary>
    /// Espaço, seta para cima, W ou clique.
    ///
    /// São todas as teclas que alguém tenta sem ler instrução nenhuma. O
    /// clique entra porque o jogo é de um botão só e mouse é o primeiro
    /// reflexo de quem senta na frente da tela.
    /// </summary>
    public static bool Mandou()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.UpArrow)
            || Input.GetKeyDown(KeyCode.W)
            || Input.GetMouseButtonDown(0);
    }
}

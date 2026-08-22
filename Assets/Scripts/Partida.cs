using System;
using UnityEngine;

/// <summary>Em que ponto a partida está.</summary>
public enum EstadoDaPartida
{
    /// <summary>Tela inicial: o pássaro flutua parado e nada se move.</summary>
    Esperando,

    /// <summary>Jogando de verdade: gravidade ligada, canos chegando.</summary>
    Voando,

    /// <summary>Bateu. O mundo para e a tela de fim aparece.</summary>
    Caiu,
}

/// <summary>
/// O estado da partida, o placar e o recorde.
///
/// Tudo que precisa saber "o jogo começou?" pergunta aqui, em vez de cada
/// peça manter a própria cópia do estado. Chão, fundo, canos e pássaro leem
/// o mesmo lugar, então não existe a situação de o cenário continuar rolando
/// depois que o pássaro já caiu.
/// </summary>
public class Partida : MonoBehaviour
{
    public static Partida Atual { get; private set; }

    const string ChaveRecorde = "bicudo.recorde";

    public EstadoDaPartida Estado { get; private set; } = EstadoDaPartida.Esperando;
    public int Pontos { get; private set; }

    /// <summary>Maior pontuação já feita nesta máquina.</summary>
    public int Recorde
    {
        get => PlayerPrefs.GetInt(ChaveRecorde, 0);
        private set { PlayerPrefs.SetInt(ChaveRecorde, value); PlayerPrefs.Save(); }
    }

    /// <summary>Verdadeiro quando a última partida bateu o recorde anterior.</summary>
    public bool BateuORecorde { get; private set; }

    public event Action<EstadoDaPartida> Mudou;
    public event Action<int> Pontuou;

    void Awake()
    {
        // Uma partida por cena. Duas instâncias significariam dois placares e
        // dois estados discordando sobre se o jogo começou.
        if (Atual != null && Atual != this) { Destroy(gameObject); return; }
        Atual = this;
    }

    void OnDestroy() { if (Atual == this) Atual = null; }

    /// <summary>Sai da tela inicial e larga o pássaro no ar.</summary>
    public void Comecar()
    {
        if (Estado != EstadoDaPartida.Esperando) return;
        Pontos = 0;
        BateuORecorde = false;
        Trocar(EstadoDaPartida.Voando);
    }

    public void Pontuar()
    {
        if (Estado != EstadoDaPartida.Voando) return;
        Pontos++;
        Pontuou?.Invoke(Pontos);
    }

    /// <summary>
    /// Encerra a partida.
    ///
    /// O recorde é gravado aqui e não na tela de fim: se ficasse na interface,
    /// uma partida encerrada com a tela ainda não montada perderia a marca.
    /// </summary>
    public void Perder()
    {
        if (Estado != EstadoDaPartida.Voando) return;

        if (Pontos > Recorde)
        {
            Recorde = Pontos;
            BateuORecorde = true;
        }
        Trocar(EstadoDaPartida.Caiu);
    }

    /// <summary>Volta ao estado de espera, com o mundo limpo.</summary>
    public void Reiniciar()
    {
        Pontos = 0;
        BateuORecorde = false;
        Trocar(EstadoDaPartida.Esperando);
    }

    void Trocar(EstadoDaPartida novo)
    {
        Estado = novo;
        Mudou?.Invoke(novo);
    }

    /// <summary>Apaga o recorde. Existe para os testes e para o menu.</summary>
    public void ApagarRecorde()
    {
        PlayerPrefs.DeleteKey(ChaveRecorde);
        PlayerPrefs.Save();
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Solta os pares de canos e os reaproveita.
///
/// Mantém uma fila fixa de pares: quando um sai pela esquerda, volta para a
/// direita com uma abertura nova em vez de ser destruído. Com o espaçamento e
/// a velocidade do jogo, meia dúzia cobre a tela inteira com folga.
/// </summary>
public class GeradorDeCanos : MonoBehaviour
{
    [Header("Ritmo")]
    [Tooltip("Distância entre um par e o seguinte, em unidades.")]
    public float espacamento = 3.6f;

    [Tooltip("Velocidade com que os canos vêm na direção do pássaro.")]
    public float velocidade = 3.2f;

    [Header("Abertura")]
    [Tooltip("Altura mais baixa em que o centro da passagem pode cair.")]
    public float alturaMinima = -1.6f;

    [Tooltip("Altura mais alta em que o centro da passagem pode cair.")]
    public float alturaMaxima = 2.2f;

    [Header("Cena")]
    [Tooltip("Onde os pares nascem, à direita da tela.")]
    public float xDeEntrada = 7f;

    [Tooltip("Passando disto para a esquerda, o par volta para o fim da fila.")]
    public float xDeSaida = -7f;

    public Cano modelo;

    [Tooltip("Quantos pares circulam. Precisa cobrir a tela mais um de folga.")]
    public int quantidade = 6;

    readonly List<Cano> pares = new List<Cano>();
    Partida partida;

    /// <summary>Os pares em circulação. Usado pelos testes.</summary>
    public IReadOnlyList<Cano> Pares => pares;

    void Start()
    {
        partida = Partida.Atual;
        if (partida != null) partida.Mudou += AoMudarEstado;

        Criar();
        Recolher();
    }

    void OnDestroy()
    {
        if (partida != null) partida.Mudou -= AoMudarEstado;
    }

    void AoMudarEstado(EstadoDaPartida estado)
    {
        if (estado == EstadoDaPartida.Esperando) Recolher();
    }

    void Criar()
    {
        if (modelo == null) return;

        for (int i = pares.Count; i < quantidade; i++)
        {
            var novo = Instantiate(modelo, transform);
            novo.name = $"Cano_{i}";
            pares.Add(novo);
        }
    }

    /// <summary>
    /// Tira todos os canos da tela.
    ///
    /// Na tela inicial o caminho precisa estar limpo: um par parado na frente
    /// do pássaro no instante em que a partida começa é uma morte que o
    /// jogador não teve como evitar.
    /// </summary>
    public void Recolher()
    {
        for (int i = 0; i < pares.Count; i++)
        {
            var p = pares[i];
            p.gameObject.SetActive(false);
            p.transform.localPosition = new Vector3(xDeEntrada + espacamento * i, 0f, 0f);
            p.Armar(AlturaSorteada());
        }
    }

    void Update()
    {
        if (partida == null || partida.Estado != EstadoDaPartida.Voando) return;

        foreach (var p in pares)
        {
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);

            p.transform.localPosition += Vector3.left * velocidade * Time.deltaTime;

            if (p.transform.localPosition.x < xDeSaida) Reciclar(p);
        }
    }

    /// <summary>Manda o par para trás do último da fila, com abertura nova.</summary>
    void Reciclar(Cano p)
    {
        float maiorX = float.MinValue;
        foreach (var outro in pares)
            if (outro != p) maiorX = Mathf.Max(maiorX, outro.transform.localPosition.x);

        p.transform.localPosition = new Vector3(maiorX + espacamento, 0f, 0f);
        p.Armar(AlturaSorteada());
    }

    float AlturaSorteada() => Random.Range(alturaMinima, alturaMaxima);
}

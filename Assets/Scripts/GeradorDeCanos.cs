using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Solta os pares de canos e os reaproveita.
///
/// Mantém uma fila fixa de pares: quando um sai pela esquerda, volta para a
/// direita com uma abertura nova em vez de ser destruído. Quantos pares a fila
/// precisa ter depende da largura da tela, e por isso é decidido ao rodar.
/// </summary>
public class GeradorDeCanos : MonoBehaviour
{
    [Header("Ritmo")]
    [Tooltip("Distância entre um par e o seguinte, em unidades.")]
    public float espacamento = 4.2f;

    [Tooltip("Velocidade com que os canos vêm na direção do pássaro.")]
    public float velocidade = 3.2f;

    [Header("Abertura")]
    [Tooltip("Altura mais baixa em que o centro da passagem pode cair.")]
    public float alturaMinima = -1f;

    [Tooltip("Altura mais alta em que o centro da passagem pode cair.")]
    public float alturaMaxima = 2.2f;

    [Header("Cena")]
    [Tooltip("Se calcula sozinho onde os canos entram e saem, pela largura real da tela.")]
    public bool ajustarPelaCamera = true;

    [Tooltip("Sobra além da borda da tela: meia largura do cano mais uma folga.")]
    public float folgaForaDaTela = 2f;

    [Tooltip("Onde os pares nascem, à direita da tela.")]
    public float xDeEntrada = 7f;

    [Tooltip("Passando disto para a esquerda, o par volta para o fim da fila.")]
    public float xDeSaida = -7f;

    [Tooltip("A que distância o primeiro par espera, na tela inicial.")]
    public float distanciaDaLargada = 9f;

    public Cano modelo;

    [Tooltip("Quem atravessa. O ponto conta quando o cano passa dele.")]
    public Transform passaro;

    [Tooltip("Quantos pares circulam. Precisa cobrir a tela mais um de folga.")]
    public int quantidade = 6;

    readonly List<Cano> pares = new List<Cano>();
    Partida partida;
    float meiaConhecida;

    /// <summary>Os pares em circulação. Usado pelos testes.</summary>
    public IReadOnlyList<Cano> Pares => pares;

    void Start()
    {
        partida = Partida.Atual;
        if (partida != null) partida.Mudou += AoMudarEstado;

        meiaConhecida = Enquadramento.MeiaLargura();

        // antes de criar a fila: o enquadramento é que diz de quantos pares
        // ela precisa
        if (ajustarPelaCamera) Enquadrar();

        Criar();
        Recolher();
    }

    void OnDestroy()
    {
        if (partida != null) partida.Mudou -= AoMudarEstado;
    }

    /// <summary>
    /// Põe a entrada e a saída dos canos fora do campo de visão.
    ///
    /// Com os dois presos na cena, em tela larga o cano sumia poucos passos
    /// atrás do pássaro e reaparecia do nada à frente dele. A troca continua a
    /// mesma; o que muda é ela acontecer onde ninguém vê.
    /// </summary>
    public void Enquadrar()
    {
        float borda = Enquadramento.MeiaLargura() + folgaForaDaTela;
        xDeSaida = -borda;
        xDeEntrada = borda;

        // A fila anda em bloco: o par que sai vai parar atrás do último. Se ela
        // for curta, esse lugar cai dentro da tela e o defeito volta por outro
        // caminho, agora do lado direito.
        int precisa = Mathf.CeilToInt((xDeEntrada - xDeSaida) / espacamento) + 1;
        quantidade = Mathf.Max(quantidade, precisa);
    }

    void AoMudarEstado(EstadoDaPartida estado)
    {
        if (estado == EstadoDaPartida.Esperando) Recolher();
    }

    /// <summary>
    /// Completa a fila até a quantidade pedida.
    ///
    /// Roda também no meio da partida, quando a janela cresce e passa a caber
    /// mais cano na tela. Por isso o par novo nasce atrás do último da fila, e
    /// não na origem: nascendo em zero ele apareceria em cima do pássaro.
    /// </summary>
    void Criar()
    {
        if (modelo == null) return;

        for (int i = pares.Count; i < quantidade; i++)
        {
            var novo = Instantiate(modelo, transform);
            novo.name = $"Cano_{i}";
            novo.transform.localPosition = new Vector3(FimDaFila() + espacamento, 0f, 0f);
            novo.Armar(AlturaSorteada());
            pares.Add(novo);
        }
    }

    /// <summary>X do último par da fila, ou a entrada se a fila estiver vazia.</summary>
    float FimDaFila()
    {
        float maior = float.MinValue;
        foreach (var p in pares) maior = Mathf.Max(maior, p.transform.localPosition.x);
        return maior > float.MinValue ? maior : xDeEntrada - espacamento;
    }

    /// <summary>
    /// Recua todos os canos para a posição de largada.
    ///
    /// Na tela inicial o caminho precisa estar limpo: um par parado na frente
    /// do pássaro no instante em que a partida começa é uma morte que o jogador
    /// não teve como evitar. Mas os pares ficam à vista, parados ao longe, como
    /// no jogo em que este se inspira. Escondê-los custava caro dos dois lados:
    /// ou eles apareciam do nada no meio da tela ao começar a partida, ou
    /// esperavam fora dela e a partida abria com segundos de céu vazio.
    /// </summary>
    public void Recolher()
    {
        float inicio = Mathf.Min(xDeEntrada, distanciaDaLargada);

        for (int i = 0; i < pares.Count; i++)
        {
            var p = pares[i];
            p.transform.localPosition = new Vector3(inicio + espacamento * i, 0f, 0f);
            p.Armar(AlturaSorteada());
        }
    }

    void Update()
    {
        // A tela pode mudar no meio da partida, por troca de janela ou de tela
        // cheia. Refazer a conta mantém a entrada e a saída dos canos fora do
        // campo de visão, que é a única coisa que o jogador enxergaria se
        // ficasse errada.
        if (ajustarPelaCamera && Enquadramento.Mudou(ref meiaConhecida))
        {
            Enquadrar();
            Criar();
        }

        if (partida == null || partida.Estado != EstadoDaPartida.Voando) return;

        foreach (var p in pares)
        {
            p.transform.localPosition += Vector3.left * velocidade * Time.deltaTime;

            ConferirPonto(p);

            if (p.transform.localPosition.x < xDeSaida) Reciclar(p);
        }
    }

    /// <summary>
    /// Conta o ponto quando o cano fica para trás do pássaro.
    ///
    /// A primeira versão punha um gatilho no meio da passagem e esperava o
    /// pássaro entrar nele. Nunca disparou: o pássaro não tem colisor, porque
    /// detecta batida por consulta de círculo, e gatilho exige colisor dos dois
    /// lados. Comparar posição não depende de física nenhuma e é o mesmo
    /// critério que o jogador enxerga - o cano passou, o ponto veio.
    /// </summary>
    void ConferirPonto(Cano p)
    {
        if (passaro == null || p.JaPontuou) return;
        if (p.transform.position.x >= passaro.position.x) return;
        if (!p.TentarPontuar()) return;

        Partida.Atual?.Pontuar();
        Sons.Instancia?.Pontuou();
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

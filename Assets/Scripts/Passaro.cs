using UnityEngine;

/// <summary>
/// O pássaro: cai sozinho, sobe quando o jogador manda, e morre ao encostar
/// em qualquer coisa.
///
/// A física é escrita na mão, sem Rigidbody2D. O impulso do Flappy Bird é
/// uma troca seca de velocidade, não uma força: com corpo rígido, o valor que
/// se aplica depende da massa, do arrasto e da gravidade do projeto, e ajustar
/// o pulo vira tentativa e erro. Aqui a altura do pulo é um número em unidades
/// e a queda é uma aceleração, ambos legíveis no Inspector.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Passaro : MonoBehaviour
{
    [Header("Voo")]
    [Tooltip("Quanto o impulso sobe, em unidades por segundo.")]
    public float impulso = 6.2f;

    [Tooltip("Aceleração da queda, em unidades por segundo ao quadrado.")]
    public float gravidade = 18f;

    [Tooltip("Velocidade máxima de queda. Sem isto ele despenca rápido demais para reagir.")]
    public float quedaMaxima = 10f;

    [Header("Inclinação")]
    [Tooltip("Ângulo com o bico para cima, logo após bater as asas.")]
    public float anguloSubindo = 24f;

    [Tooltip("Ângulo com o bico para baixo, no fim da queda.")]
    public float anguloCaindo = -70f;

    [Tooltip("Quão rápido o bico acompanha a velocidade.")]
    public float rapidezDaInclinacao = 7f;

    [Header("Espera")]
    [Tooltip("Altura do balanço na tela inicial.")]
    public float balanco = 0.18f;
    public float ritmoDoBalanco = 2.2f;

    [Header("Corpo")]
    [Tooltip("Raio usado para detectar colisão com cano e chão.")]
    public float raio = 0.28f;
    public LayerMask camadasQueMatam;

    /// <summary>Velocidade vertical atual. Exposta para a inclinação e os testes.</summary>
    public float VelocidadeVertical { get; private set; }

    /// <summary>Onde ele nasce, e para onde volta ao reiniciar.</summary>
    public Vector3 Berco { get; private set; }

    Partida partida;
    Vector3 posicaoDeEspera;

    void Awake()
    {
        Berco = transform.position;
        posicaoDeEspera = Berco;
    }

    void Start()
    {
        partida = Partida.Atual;
        if (partida != null) partida.Mudou += AoMudarEstado;
        Recolocar();
    }

    void OnDestroy()
    {
        if (partida != null) partida.Mudou -= AoMudarEstado;
    }

    void AoMudarEstado(EstadoDaPartida estado)
    {
        if (estado == EstadoDaPartida.Esperando) Recolocar();
        // ao começar a voar, o primeiro impulso sai de quem apertou a tecla
    }

    /// <summary>Devolve o pássaro ao berço, parado e na horizontal.</summary>
    public void Recolocar()
    {
        transform.position = Berco;
        transform.rotation = Quaternion.identity;
        VelocidadeVertical = 0f;
    }

    void Update()
    {
        if (partida == null) return;

        switch (partida.Estado)
        {
            case EstadoDaPartida.Esperando: Boiar(); break;
            case EstadoDaPartida.Voando:    Voar();  break;
            case EstadoDaPartida.Caiu:      Tombar(); break;
        }
    }

    /// <summary>Balanço parado da tela inicial, só para não parecer congelado.</summary>
    void Boiar()
    {
        float y = Berco.y + Mathf.Sin(Time.time * ritmoDoBalanco) * balanco;
        transform.position = new Vector3(Berco.x, y, Berco.z);
        transform.rotation = Quaternion.identity;
        posicaoDeEspera = transform.position;
    }

    void Voar()
    {
        VelocidadeVertical -= gravidade * Time.deltaTime;
        VelocidadeVertical = Mathf.Max(VelocidadeVertical, -quedaMaxima);
        transform.position += Vector3.up * VelocidadeVertical * Time.deltaTime;

        Inclinar();
        ConferirBatida();
    }

    /// <summary>Depois de morto ele ainda cai, até encostar no chão.</summary>
    void Tombar()
    {
        VelocidadeVertical -= gravidade * Time.deltaTime;
        VelocidadeVertical = Mathf.Max(VelocidadeVertical, -quedaMaxima);
        transform.position += Vector3.up * VelocidadeVertical * Time.deltaTime;

        transform.rotation = Quaternion.Lerp(transform.rotation,
            Quaternion.Euler(0f, 0f, anguloCaindo), Time.deltaTime * rapidezDaInclinacao * 1.6f);
    }

    /// <summary>Bate as asas: troca a velocidade, não soma a ela.</summary>
    public void Bater()
    {
        if (partida == null) return;

        if (partida.Estado == EstadoDaPartida.Esperando)
        {
            transform.position = posicaoDeEspera;
            partida.Comecar();
        }

        if (partida.Estado != EstadoDaPartida.Voando) return;

        // troca seca: subir sempre a mesma altura, venha de onde vier
        VelocidadeVertical = impulso;
        Sons.Instancia?.Bateu();
    }

    void Inclinar()
    {
        float alvo = VelocidadeVertical > 0f
            ? anguloSubindo
            : Mathf.Lerp(anguloSubindo, anguloCaindo,
                         Mathf.InverseLerp(0f, -quedaMaxima, VelocidadeVertical));

        transform.rotation = Quaternion.Lerp(transform.rotation,
            Quaternion.Euler(0f, 0f, alvo), Time.deltaTime * rapidezDaInclinacao);
    }

    /// <summary>
    /// Colisão por consulta de círculo, e não por trigger.
    ///
    /// O pássaro é movido pelo transform, sem corpo rígido, e nesse caso os
    /// eventos de colisão do Unity não são confiáveis: o objeto atravessa o
    /// cano entre dois quadros sem disparar nada. Perguntar a cada quadro se
    /// há algo sólido no lugar dele resolve, e é barato para um círculo só.
    /// </summary>
    void ConferirBatida()
    {
        if (Physics2D.OverlapCircle(transform.position, raio, camadasQueMatam) != null)
            Morrer();
    }

    public void Morrer()
    {
        if (partida == null || partida.Estado != EstadoDaPartida.Voando) return;
        VelocidadeVertical = 0f;
        Sons.Instancia?.BateuNoCano();
        partida.Perder();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raio);
    }
}

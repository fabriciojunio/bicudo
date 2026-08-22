using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toda a interface do jogo numa peça só: tela inicial, placar durante o voo
/// e tela de fim.
///
/// São três estados da mesma tela, não três telas: o placar do topo continua
/// o mesmo objeto do começo ao fim, e o que muda é quem está visível. Separar
/// em três componentes obrigaria os três a assinar o mesmo evento de estado e
/// a concordar sobre quem esconde quem.
/// </summary>
public class Interface : MonoBehaviour
{
    [Tooltip("Desligado enquanto a tela de fim está aberta, para o clique de reiniciar não virar impulso.")]
    public Controles controles;

    public GeradorDeCanos canos;
    public Passaro passaro;

    Partida partida;

    Text placar, titulo, dica, pontosDoFim, recordeDoFim, aviso, medalha;
    RectTransform painelInicial, painelFim;

    float quandoCaiu;

    /// <summary>
    /// Espera antes de aceitar o toque que reinicia.
    ///
    /// Quem morre está batendo asa, e o mesmo toque que matou reiniciaria a
    /// partida antes de a pessoa ver o próprio placar.
    /// </summary>
    const float EsperaAntesDeReiniciar = 0.8f;

    void Start()
    {
        partida = Partida.Atual;
        Montar();

        if (partida != null)
        {
            partida.Mudou += AoMudarEstado;
            partida.Pontuou += AoPontuar;
            AoMudarEstado(partida.Estado);
        }
    }

    void OnDestroy()
    {
        if (partida == null) return;
        partida.Mudou -= AoMudarEstado;
        partida.Pontuou -= AoPontuar;
    }

    void Update()
    {
        if (partida == null || partida.Estado != EstadoDaPartida.Caiu) return;
        if (Time.time - quandoCaiu < EsperaAntesDeReiniciar) return;
        if (!Controles.Mandou()) return;

        Reiniciar();
    }

    public void Reiniciar()
    {
        if (passaro != null) passaro.Recolocar();
        if (canos != null) canos.Recolher();
        partida?.Reiniciar();
    }

    void AoPontuar(int pontos)
    {
        if (placar != null) placar.text = pontos.ToString();
    }

    void AoMudarEstado(EstadoDaPartida estado)
    {
        bool esperando = estado == EstadoDaPartida.Esperando;
        bool caiu = estado == EstadoDaPartida.Caiu;

        if (painelInicial != null) painelInicial.gameObject.SetActive(esperando);
        if (painelFim != null) painelFim.gameObject.SetActive(caiu);
        if (placar != null) placar.gameObject.SetActive(!esperando);

        if (controles != null) controles.aceitandoComandos = !caiu;

        if (esperando && placar != null) placar.text = "0";

        if (caiu)
        {
            quandoCaiu = Time.time;
            Sons.Instancia?.Caiu();
            PreencherFim();
        }
    }

    void PreencherFim()
    {
        if (partida == null) return;

        if (pontosDoFim != null) pontosDoFim.text = partida.Pontos.ToString();
        if (recordeDoFim != null) recordeDoFim.text = $"melhor: {partida.Recorde}";

        if (aviso != null)
        {
            aviso.gameObject.SetActive(partida.BateuORecorde);
            aviso.text = "recorde novo";
        }

        if (medalha != null)
        {
            var (nome, cor) = MedalhaDe(partida.Pontos);
            medalha.gameObject.SetActive(nome != null);
            if (nome != null) { medalha.text = nome; medalha.color = cor; }
        }
    }

    /// <summary>
    /// A medalha do fim de partida.
    ///
    /// Os cortes são baixos de propósito. Num jogo em que a primeira dezena de
    /// tentativas termina com dois ou três pontos, medalha que só aparece aos
    /// cinquenta nunca aparece.
    /// </summary>
    public static (string nome, Color cor) MedalhaDe(int pontos)
    {
        if (pontos >= 30) return ("ouro", new Color(1f, 0.84f, 0.35f));
        if (pontos >= 15) return ("prata", new Color(0.83f, 0.86f, 0.90f));
        if (pontos >= 5)  return ("bronze", new Color(0.85f, 0.58f, 0.35f));
        return (null, Color.white);
    }

    void Montar()
    {
        Kit.Canvas(gameObject, 100, comCliques: false);
        var t = transform;

        placar = Kit.Texto(t, "Placar", "0", 88, new Vector2(0f, 340f),
                           new Vector2(400f, 120f), Kit.Creme);

        // ------------------------------------------------------ tela inicial
        painelInicial = Kit.Painel(t, "TelaInicial", new Vector2(0f, 40f), new Vector2(460f, 300f));
        titulo = Kit.Texto(painelInicial, "Titulo", "BICUDO", 72,
                           new Vector2(0f, 80f), new Vector2(440f, 100f), Kit.Destaque);
        Kit.Texto(painelInicial, "Subtitulo", "um toque, uma asa", 22,
                  new Vector2(0f, 20f), new Vector2(440f, 40f), Kit.Apagado);
        dica = Kit.Texto(painelInicial, "Dica",
                         "espaço, seta para cima ou clique\npara bater as asas",
                         24, new Vector2(0f, -60f), new Vector2(440f, 90f), Kit.Creme);

        // ---------------------------------------------------------- tela de fim
        painelFim = Kit.Painel(t, "TelaDeFim", new Vector2(0f, 40f), new Vector2(460f, 380f));
        Kit.Texto(painelFim, "Fim", "CAIU", 56, new Vector2(0f, 140f),
                  new Vector2(440f, 80f), Kit.Destaque);

        pontosDoFim = Kit.Texto(painelFim, "Pontos", "0", 78, new Vector2(0f, 50f),
                                new Vector2(440f, 100f), Kit.Creme);
        medalha = Kit.Texto(painelFim, "Medalha", "", 30, new Vector2(0f, -10f),
                            new Vector2(440f, 50f), Kit.Destaque);
        recordeDoFim = Kit.Texto(painelFim, "Recorde", "melhor: 0", 26,
                                 new Vector2(0f, -60f), new Vector2(440f, 40f), Kit.Apagado);
        aviso = Kit.Texto(painelFim, "Aviso", "recorde novo", 24,
                          new Vector2(0f, -100f), new Vector2(440f, 40f), Kit.Destaque);
        Kit.Texto(painelFim, "Reinicia", "toque para tentar de novo", 22,
                  new Vector2(0f, -150f), new Vector2(440f, 40f), Kit.Creme);

        painelFim.gameObject.SetActive(false);
        if (aviso != null) aviso.gameObject.SetActive(false);
        if (medalha != null) medalha.gameObject.SetActive(false);
    }
}

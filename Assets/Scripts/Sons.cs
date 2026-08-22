using UnityEngine;

/// <summary>
/// Os efeitos do jogo, gerados por síntese na hora de acordar.
///
/// Nenhum arquivo de áudio de terceiros entra no projeto. Os dois pacotes de
/// arte usados aqui são só de imagem, e um pacote de som traria uma terceira
/// licença para conferir. Onda quadrada com envelope resolve um jogo deste
/// tamanho e mantém a entrega com uma origem a menos para explicar.
/// </summary>
public class Sons : MonoBehaviour
{
    public static Sons Instancia { get; private set; }

    const int Taxa = 44100;
    const string ChaveVolume = "bicudo.volume";

    AudioSource fonte;
    AudioClip bater, ponto, batida, queda;

    /// <summary>Volume dos efeitos, de 0 a 1. Fica gravado entre partidas.</summary>
    public static float Volume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveVolume, 0.5f));
        set { PlayerPrefs.SetFloat(ChaveVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;

        fonte = GetComponent<AudioSource>();
        if (fonte == null) fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;

        bater  = Gerar("bater",  0.10f, 620f, 340f, 0.35f, quadrada: true);
        ponto  = Gerar("ponto",  0.13f, 700f, 1180f, 0.30f, quadrada: true);
        batida = Gerar("batida", 0.18f, 200f, 90f, 0.45f, quadrada: false, ruido: 0.5f);
        queda  = Gerar("queda",  0.45f, 420f, 70f, 0.40f, quadrada: true);
    }

    void OnDestroy() { if (Instancia == this) Instancia = null; }

    public void Bateu()       => Tocar(bater);
    public void Pontuou()     => Tocar(ponto);
    public void BateuNoCano() => Tocar(batida);
    public void Caiu()        => Tocar(queda);

    void Tocar(AudioClip clipe)
    {
        if (clipe == null || fonte == null) return;
        fonte.PlayOneShot(clipe, Volume);
    }

    /// <summary>
    /// Um efeito curto: a frequência escorrega de um valor a outro enquanto o
    /// volume decai.
    ///
    /// O decaimento é o que separa um som de jogo de um bipe de alarme. Sem
    /// ele a onda corta seca no fim e estala no alto-falante.
    /// </summary>
    static AudioClip Gerar(string nome, float duracao, float deHz, float paraHz,
                           float volume, bool quadrada, float ruido = 0f)
    {
        int amostras = Mathf.Max(1, Mathf.RoundToInt(Taxa * duracao));
        var dados = new float[amostras];

        float fase = 0f;
        var sorteio = new System.Random(nome.GetHashCode());

        for (int i = 0; i < amostras; i++)
        {
            float t = i / (float)amostras;
            float hz = Mathf.Lerp(deHz, paraHz, t);

            fase += hz / Taxa;
            if (fase > 1f) fase -= 1f;

            float onda = quadrada ? (fase < 0.5f ? 1f : -1f) : Mathf.Sin(fase * Mathf.PI * 2f);
            if (ruido > 0f) onda += ((float)sorteio.NextDouble() * 2f - 1f) * ruido;

            // decaimento exponencial, mais parecido com o que o ouvido espera
            float envelope = Mathf.Exp(-4.5f * t);
            dados[i] = Mathf.Clamp(onda * envelope * volume, -1f, 1f);
        }

        var clipe = AudioClip.Create(nome, amostras, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }
}

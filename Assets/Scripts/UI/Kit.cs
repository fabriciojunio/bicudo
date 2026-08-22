using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Peças de interface montadas por código, na identidade do jogo.
///
/// Nada de prefab de UI: assim qualquer cena, inclusive as de teste, sobe com
/// a interface inteira sem depender de alguém ter arrastado objeto na mão.
/// </summary>
public static class Kit
{
    public static readonly Color Creme    = new Color(0.98f, 0.96f, 0.88f);
    public static readonly Color Apagado  = new Color(0.75f, 0.72f, 0.64f);
    public static readonly Color Destaque = new Color(1f, 0.82f, 0.30f);
    public static readonly Color Sombra   = new Color(0.10f, 0.09f, 0.13f, 0.75f);

    static Font fonteEmCache;

    /// <summary>
    /// A fonte da interface.
    ///
    /// Monoespaçada: o placar troca de número a cada cano, e em fonte
    /// proporcional o "1" é bem mais estreito que o "8", então o número dança
    /// de lado a cada ponto. Com largura fixa ele fica parado.
    ///
    /// Fonte de sistema não vai dentro do executável, por isso a lista é
    /// conferida contra o que a máquina tem e sobra a embutida no fim.
    /// </summary>
    public static Font Fonte
    {
        get
        {
            if (fonteEmCache == null) fonteEmCache = Escolher();
            return fonteEmCache;
        }
    }

    static Font Escolher()
    {
        string[] preferidas = { "Cascadia Mono SemiBold", "Cascadia Mono", "Consolas", "Courier New" };
        var instaladas = new System.Collections.Generic.HashSet<string>(Font.GetOSInstalledFontNames());

        foreach (var nome in preferidas)
        {
            if (!instaladas.Contains(nome)) continue;
            var f = Font.CreateDynamicFontFromOSFont(nome, 32);
            if (f != null) return f;
        }
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>
    /// Canvas que cabe em qualquer tela.
    ///
    /// Acompanha a altura e não a largura: num monitor mais largo que 16:9 o
    /// casamento por largura estica a interface até ela sair por cima e por
    /// baixo. Pela altura, sobra espaço nas laterais, que é inofensivo.
    /// </summary>
    public static Canvas Canvas(GameObject alvo, int ordem, bool comCliques = true)
    {
        var canvas = alvo.GetComponent<Canvas>() ?? alvo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordem;

        var escala = alvo.GetComponent<CanvasScaler>() ?? alvo.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(540, 960);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escala.matchWidthOrHeight = 1f;

        if (comCliques)
        {
            if (alvo.GetComponent<GraphicRaycaster>() == null) alvo.AddComponent<GraphicRaycaster>();
            GarantirEventSystem();
        }
        return canvas;
    }

    public static void GarantirEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    public static Text Texto(Transform pai, string nome, string conteudo, int tamanho,
                             Vector2 posicao, Vector2 medida, Color cor,
                             TextAnchor alinhamento = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        var t = go.AddComponent<Text>();
        t.font = Fonte;
        t.fontSize = tamanho;
        t.alignment = alinhamento;
        t.color = cor;
        t.text = conteudo;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;

        // Contorno escuro, sempre.
        //
        // O texto fica sobre o céu, que muda de cor conforme o fundo escolhido.
        // Sem contorno, existe combinação em que o placar simplesmente some.
        var contorno = go.AddComponent<Outline>();
        contorno.effectColor = Sombra;
        contorno.effectDistance = new Vector2(2f, -2f);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = medida;
        return t;
    }

    /// <summary>Painel escuro translúcido, para dar contraste a um bloco de texto.</summary>
    public static RectTransform Painel(Transform pai, string nome, Vector2 posicao, Vector2 medida)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.10f, 0.16f, 0.82f);
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = medida;
        return rt;
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Monta a cena do jogo inteira.
///
/// Uma cena só: o Bicudo não tem menu separado. A tela inicial, o voo e a tela
/// de fim são três estados da mesma cena, e trocar de cena entre eles só
/// acrescentaria carregamento no meio de um jogo que se reinicia em um toque.
/// </summary>
public static class CenaDoBicudo
{
    public const string PastaCenas = "Assets/Cenas";
    public const string CenaJogo = PastaCenas + "/Jogo.unity";

    /// <summary>Altura visível da câmera, em unidades. Define todo o resto.</summary>
    const float MeiaAltura = 7f;

    /// <summary>Onde fica o chão, contado do centro da tela.</summary>
    const float AlturaDoChao = -5.6f;

    /// <summary>
    /// A tela mais larga que o cenário se compromete a cobrir, em meia largura.
    ///
    /// A largura de verdade só existe quando o jogo roda, e quem a mede é o
    /// Enquadramento. Aqui ela serve para decidir quantas peças recortar, que é
    /// coisa de montagem e não dá para refazer depois. Vinte e cinco unidades
    /// dão conta de 32:9 com a altura deste jogo.
    /// </summary>
    const float MaiorMeiaLargura = 25f;

    [MenuItem("Bicudo/3. Gerar cena", false, 2)]
    public static void Gerar()
    {
        System.IO.Directory.CreateDirectory(PastaCenas);
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MontarCamera();
        MontarFundo();
        MontarChao();
        var passaro = MontarPassaro();
        var canos = MontarCanos();
        // sem isto o gerador não sabe quando o cano ficou para trás, e o
        // placar nunca sai do zero
        if (canos != null && passaro != null) canos.passaro = passaro.transform;
        MontarSistemas(passaro, canos);

        EditorSceneManager.SaveScene(cena, CenaJogo);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CenaJogo, true) };

        AssetDatabase.SaveAssets();
        Debug.Log("[Bicudo] cena gerada e registrada em Build Settings.");
    }

    static void MontarCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = MeiaAltura;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x7A, 0xC5, 0xD8, 0xFF);
        go.transform.position = new Vector3(0f, 0f, -10f);
        go.AddComponent<AudioListener>();
    }

    /// <summary>
    /// O céu ao fundo, rolando devagar.
    ///
    /// Vai mais lento que o chão de propósito: com os dois na mesma velocidade
    /// a tela vira uma folha só deslizando, e some a sensação de profundidade.
    /// </summary>
    static void MontarFundo()
    {
        var sprite = ArteDoBicudo.AcharFundo();
        if (sprite == null) { Debug.LogWarning("[Bicudo] fundo ausente"); return; }

        float largura = sprite.rect.width / ArteDoBicudo.PixelsPorUnidade;
        float altura = sprite.rect.height / ArteDoBicudo.PixelsPorUnidade;
        float escala = (MeiaAltura * 2f) / altura;
        float larguraNaTela = largura * escala;

        var raiz = new GameObject("Fundo");

        // A fila precisa cobrir a tela inteira mais uma peça de cada lado, que
        // é a folga onde a reposição acontece sem ninguém ver.
        int quantas = Mathf.Max(3,
            Mathf.CeilToInt((MaiorMeiaLargura * 2f + larguraNaTela) / larguraNaTela) + 1);
        var pecas = new Transform[quantas];

        for (int i = 0; i < pecas.Length; i++)
        {
            var go = new GameObject("Ceu_" + i);
            go.transform.SetParent(raiz.transform, false);
            go.transform.localPosition = new Vector3(i * larguraNaTela, 0f, 0f);
            go.transform.localScale = new Vector3(escala, escala, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -20;
            pecas[i] = go.transform;
        }

        // O pai fica na origem: quem alinha a fila é o Enquadrar da faixa, ao
        // rodar, e ele trabalha em coordenada local. Deslocar o pai aqui só
        // faria a conta dele sair torta pelo mesmo tanto.
        raiz.transform.position = Vector3.zero;

        var rola = raiz.AddComponent<FaixaQueRola>();
        rola.velocidade = 0.6f;
        rola.rolaSempre = true;      // o céu anda mesmo na tela inicial
        rola.pecas = pecas;
        rola.largura = larguraNaTela;
    }

    /// <summary>
    /// O chão: uma faixa que rola e mata quem encosta.
    ///
    /// O colisor é um só, largo e parado. Os tiles rolam por baixo dele apenas
    /// como desenho: se o colisor rolasse junto, ele sairia de cena e abriria
    /// um buraco no fim do mundo.
    /// </summary>
    static void MontarChao()
    {
        var sprite = ArteDoBicudo.Achar("Chao");
        var raiz = new GameObject("Chao") { layer = PrefabsDoBicudo.CamadaCenario };
        raiz.transform.position = new Vector3(0f, AlturaDoChao, 0f);

        if (sprite != null)
        {
            // Três peças, não duas.
            //
            // A fila cobre, no pior instante, da borda de reposição para a
            // direita menos uma largura. Para sobrar tela dos dois lados ela
            // precisa medir mais que a tela inteira mais uma peça. Com duas de
            // quarenta a conta não fechava e o chão sumia na borda esquerda a
            // cada volta, que foi o defeito relatado.
            const int PorPeca = 40;
            int quantas = Mathf.Max(3,
                Mathf.CeilToInt((MaiorMeiaLargura * 2f + PorPeca) / PorPeca) + 1);
            var pecas = new Transform[quantas];

            for (int p = 0; p < pecas.Length; p++)
            {
                var peca = new GameObject("Faixa_" + p);
                peca.transform.SetParent(raiz.transform, false);
                peca.transform.localPosition = new Vector3(p * PorPeca, 0f, 0f);

                for (int i = 0; i < PorPeca; i++)
                {
                    var t = new GameObject("Tile_" + i);
                    t.transform.SetParent(peca.transform, false);
                    // meio tile adiante: o pivô do desenho é o centro, e assim
                    // a peça ocupa exatamente de 0 até a largura dela
                    t.transform.localPosition = new Vector3(i + 0.5f, 0f, 0f);

                    var sr = t.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = 6;
                }
                pecas[p] = peca.transform;
            }

            var rola = raiz.AddComponent<FaixaQueRola>();
            rola.velocidade = 3.2f;
            rola.pecas = pecas;
            rola.largura = PorPeca;

            // Terra sólida embaixo da fileira que rola, parada.
            //
            // Só a fileira de tiles deixaria o vão entre ela e a borda de baixo
            // aparecendo o céu. Isto é um retângulo, não rola, e some por baixo
            // da tela em qualquer proporção.
            MontarTerra(raiz.transform, sprite);
        }

        var col = raiz.AddComponent<BoxCollider2D>();
        col.size = new Vector2(MaiorMeiaLargura * 3f, 4f);
        col.offset = new Vector2(0f, -1.6f);
    }

    /// <summary>Bloco de terra parado, do chão para baixo, sem emenda.</summary>
    static void MontarTerra(Transform pai, Sprite sprite)
    {
        var go = new GameObject("Terra");
        go.transform.SetParent(pai, false);
        go.transform.localPosition = new Vector3(0f, -6f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(MaiorMeiaLargura * 3f, 11f);
        sr.color = new Color(0.55f, 0.42f, 0.30f);
        sr.sortingOrder = 5;
    }

    static Passaro MontarPassaro()
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDoBicudo.CaminhoPassaro);
        if (modelo == null)
        {
            Debug.LogError("[Bicudo] prefab do pássaro não encontrado");
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        go.transform.position = new Vector3(-2.2f, 1f, 0f);
        return go.GetComponent<Passaro>();
    }

    static GeradorDeCanos MontarCanos()
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDoBicudo.CaminhoCano);

        var go = new GameObject("Canos");
        var g = go.AddComponent<GeradorDeCanos>();

        if (modelo != null) g.modelo = modelo.GetComponent<Cano>();
        else Debug.LogError("[Bicudo] prefab do cano não encontrado");

        // Valores de partida apenas. Quem manda são o Enquadrar do gerador e a
        // proporção real da tela, que aqui no editor ainda não existe.
        g.xDeEntrada = 10f;
        g.xDeSaida = -11f;
        g.distanciaDaLargada = 9f;
        return g;
    }

    static void MontarSistemas(Passaro passaro, GeradorDeCanos canos)
    {
        var go = new GameObject("_Sistemas");
        go.AddComponent<Partida>();

        go.AddComponent<Sons>();
        go.AddComponent<AudioSource>().playOnAwake = false;

        var controles = go.AddComponent<Controles>();
        controles.passaro = passaro;

        // Nasce já com Canvas e RectTransform.
        //
        // Montado como GameObject comum, ele entra na cena com um Transform
        // simples, e os componentes de UI que exigem Canvas reclamam antes de
        // o Start ter chance de adicioná-lo.
        var ui = new GameObject("Interface",
            typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        var inter = ui.AddComponent<Interface>();
        inter.controles = controles;
        inter.passaro = passaro;
        inter.canos = canos;
    }
}

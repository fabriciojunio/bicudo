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

        int quantas = Mathf.Max(3, Mathf.CeilToInt((MeiaAltura * 4f) / larguraNaTela) + 1);
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

        // começa uma peça à esquerda, senão sobra céu vazio na borda
        raiz.transform.position = new Vector3(-larguraNaTela, 0f, 0f);

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
            const int PorPeca = 24;
            var pecas = new Transform[2];

            for (int p = 0; p < pecas.Length; p++)
            {
                var peca = new GameObject("Faixa_" + p);
                peca.transform.SetParent(raiz.transform, false);
                peca.transform.localPosition = new Vector3(p * PorPeca - PorPeca * 0.5f, 0f, 0f);

                for (int i = 0; i < PorPeca; i++)
                {
                    for (int camada = 0; camada < 3; camada++)
                    {
                        var t = new GameObject("Tile_" + i + "_" + camada);
                        t.transform.SetParent(peca.transform, false);
                        t.transform.localPosition = new Vector3(i, -camada, 0f);

                        var sr = t.AddComponent<SpriteRenderer>();
                        sr.sprite = sprite;
                        sr.sortingOrder = 6;
                    }
                }
                pecas[p] = peca.transform;
            }

            var rola = raiz.AddComponent<FaixaQueRola>();
            rola.velocidade = 3.2f;
            rola.pecas = pecas;
            rola.largura = PorPeca;
        }

        var col = raiz.AddComponent<BoxCollider2D>();
        col.size = new Vector2(80f, 4f);
        col.offset = new Vector2(0f, -1.6f);
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

        g.xDeEntrada = 8f;
        g.xDeSaida = -9f;
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

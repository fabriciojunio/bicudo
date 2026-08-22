using UnityEditor;
using UnityEngine;

/// <summary>
/// Monta os prefabs do jogo a partir da arte já recortada.
///
/// Tudo por código, e não montado à mão no editor: o projeto inteiro se
/// reconstrói de um menu só, e revisar uma mudança de prefab vira ler um
/// diff de C# em vez de um YAML de mil linhas.
/// </summary>
public static class PrefabsDoBicudo
{
    public const int CamadaCenario = 8;
    public static LayerMask MascaraDoCenario => 1 << CamadaCenario;

    const string Pasta = "Assets/Prefabs";
    public const string CaminhoPassaro = Pasta + "/Passaro.prefab";
    public const string CaminhoCano    = Pasta + "/Cano.prefab";

    /// <summary>Altura do cano montado, em unidades. Cobre a tela com folga.</summary>
    const int PedacosDeCorpo = 12;

    [MenuItem("Bicudo/2. Montar prefabs", false, 1)]
    public static void Montar()
    {
        System.IO.Directory.CreateDirectory(Pasta);
        MontarPassaro();
        MontarCano();
        AssetDatabase.SaveAssets();
        Debug.Log("[Bicudo] prefabs montados.");
    }

    static void MontarPassaro()
    {
        var go = new GameObject("Passaro");

        var sr = go.AddComponent<SpriteRenderer>();
        var quadros = ArteDoBicudo.QuadrosDoPassaro();
        if (quadros.Length > 0) sr.sprite = quadros[0];
        sr.sortingOrder = 10;

        var p = go.AddComponent<Passaro>();
        p.camadasQueMatam = MascaraDoCenario;

        // os quadros entram aqui: sem eles o pássaro fica congelado no
        // primeiro desenho e parece um adesivo colado na tela
        go.AddComponent<AsaBatendo>().quadros = quadros;

        PrefabUtility.SaveAsPrefabAsset(go, CaminhoPassaro);
        Object.DestroyImmediate(go);
    }

    /// <summary>
    /// O par de canos.
    ///
    /// Cada lado é uma boca mais uma pilha de corpo que sai da tela. Empilhar
    /// em vez de esticar um sprite só é o que mantém o pixel quadrado: um
    /// corpo esticado em 12 unidades deforma o desenho e destoa do resto.
    /// </summary>
    static void MontarCano()
    {
        var boca = ArteDoBicudo.Achar("CanoBoca");
        var corpo = ArteDoBicudo.Achar("CanoCorpo");

        var raiz = new GameObject("Cano");
        var cano = raiz.AddComponent<Cano>();

        cano.canoDeCima = Lado(raiz.transform, "CanoDeCima", boca, corpo, paraCima: false);
        cano.canoDeBaixo = Lado(raiz.transform, "CanoDeBaixo", boca, corpo, paraCima: true);

        // A portaria fica no meio da abertura, alta o bastante para o pássaro
        // não passar por fora dela nem subindo nem descendo.
        var portariaGO = new GameObject("Portaria");
        portariaGO.transform.SetParent(raiz.transform, false);
        var portaria = portariaGO.AddComponent<BoxCollider2D>();
        portaria.isTrigger = true;
        portaria.size = new Vector2(0.4f, 40f);
        cano.portaria = portaria;

        PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoCano);
        Object.DestroyImmediate(raiz);
    }

    /// <summary>
    /// Um lado do par: a boca virada para a abertura e o corpo indo embora.
    /// </summary>
    static Transform Lado(Transform pai, string nome, Sprite boca, Sprite corpo, bool paraCima)
    {
        var go = new GameObject(nome) { layer = CamadaCenario };
        go.transform.SetParent(pai, false);

        // sentido: o cano de baixo cresce para baixo, o de cima para cima
        float sentido = paraCima ? -1f : 1f;

        var bocaGO = new GameObject("Boca") { layer = CamadaCenario };
        bocaGO.transform.SetParent(go.transform, false);
        bocaGO.transform.localPosition = Vector3.zero;
        var bsr = bocaGO.AddComponent<SpriteRenderer>();
        bsr.sprite = boca;
        bsr.sortingOrder = 5;
        // a boca do cano de baixo é a mesma arte de cabeça para baixo
        bocaGO.transform.localScale = new Vector3(1f, paraCima ? -1f : 1f, 1f);

        for (int i = 0; i < PedacosDeCorpo; i++)
        {
            var pedaco = new GameObject($"Corpo_{i}") { layer = CamadaCenario };
            pedaco.transform.SetParent(go.transform, false);
            pedaco.transform.localPosition = new Vector3(0f, sentido * (1f + i), 0f);

            var csr = pedaco.AddComponent<SpriteRenderer>();
            csr.sprite = corpo;
            csr.sortingOrder = 4;
        }

        // Um colisor só para o lado inteiro, e não um por pedaço.
        //
        // Doze colisores empilhados por cano, vezes seis canos, é peso à toa
        // na física e ainda cria costuras onde o pássaro engancha entre dois.
        var col = go.AddComponent<BoxCollider2D>();
        float altura = PedacosDeCorpo + 1f;
        col.size = new Vector2(1.75f, altura);
        col.offset = new Vector2(0f, sentido * (altura * 0.5f - 0.5f));

        return go.transform;
    }
}

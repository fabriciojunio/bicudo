using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Traz a arte dos dois pacotes e marca os recortes.
///
/// Os PNG entram intactos e o recorte vive só nos metadados de importação. A
/// licença do pacote da Gufiao Studios proíbe redistribuir os arquivos como
/// peças soltas, mesmo modificados; mantendo os originais e o recorte fora
/// deles, o que vai para o repositório é o pacote como veio, dentro de um
/// projeto, que é o que a licença permite.
/// </summary>
public static class ArteDoBicudo
{
    const string Origem = "../_assets_flappy";
    public const string Pasta = "Assets/Arte";

    public const string FolhaPassaro = Pasta + "/Passaro.png";
    public const string FolhaCano    = Pasta + "/Cano.png";
    public const string FolhaChao    = Pasta + "/Chao.png";
    public const string Fundo        = Pasta + "/Fundo.png";

    /// <summary>16 pixels por unidade: um tile do pacote ocupa uma unidade de mundo.</summary>
    public const float PixelsPorUnidade = 16f;

    /// <summary>Um pedaço da folha, em pixels, com o nome que o jogo usa.</summary>
    public struct Recorte
    {
        public string nome;
        public int x, y, largura, altura;
        public float pivoY;

        public Recorte(string nome, int x, int y, int largura, int altura, float pivoY = 0.5f)
        {
            this.nome = nome; this.x = x; this.y = y;
            this.largura = largura; this.altura = altura; this.pivoY = pivoY;
        }
    }

    /// <summary>
    /// O que copiar de onde.
    ///
    /// Só o necessário. Os pacotes trazem dezenas de variações de cor e
    /// estilo, e arrastar tudo para dentro do projeto engorda a entrega com
    /// arquivo que nunca é carregado.
    /// </summary>
    static readonly (string de, string para)[] Copias =
    {
        ("Flappy Bird Assets/Player/StyleBird1/Bird1-3.png", FolhaPassaro),
        ("Flappy Bird Assets/Tiles/Style 1/PipeStyle1.png",  FolhaCano),
        ("Flappy Bird Assets/Tiles/Style 1/SimpleStyle1.png", FolhaChao),
        ("Flappy Bird Assets/Background/Background4.png",     Fundo),
    };

    [MenuItem("Bicudo/1. Preparar arte", false, 0)]
    public static void Preparar()
    {
        string raiz = Path.GetFullPath(Path.Combine(Application.dataPath, "..", Origem));
        if (!Directory.Exists(raiz))
        {
            Debug.LogError($"[Bicudo] pacotes de arte não encontrados em {raiz}");
            return;
        }

        Directory.CreateDirectory(Pasta);

        foreach (var (de, para) in Copias)
        {
            string caminho = Path.Combine(raiz, de);
            if (!File.Exists(caminho)) { Debug.LogWarning($"[Bicudo] sem {caminho}"); continue; }
            File.Copy(caminho, para, overwrite: true);
        }
        AssetDatabase.Refresh();

        // O pássaro é uma tira de 64x16: quatro quadros de 16 lado a lado.
        Fatiar(FolhaPassaro, Enumerable.Range(0, 4)
            .Select(i => new Recorte($"Passaro_{i}", i * 16, 0, 16, 16)).ToArray());

        // Do cano só saem duas peças. A boca e um pedaço de corpo repetível:
        // o cano é montado empilhando corpo até a borda da tela, então não
        // adianta recortar o desenho inteiro, que tem altura fixa.
        Fatiar(FolhaCano, new[]
        {
            new Recorte("CanoBoca",  0, 144, 32, 16),   // topo da folha, contado de baixo
            new Recorte("CanoCorpo", 0, 100, 32, 16),   // miolo uniforme do corpo
        });

        Fatiar(FolhaChao, new[] { new Recorte("Chao", 0, 16, 16, 16) });

        ConfigurarInteira(Fundo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bicudo] arte preparada.");
    }

    /// <summary>
    /// Marca os recortes pelos metadados de importação.
    ///
    /// Vai pelo ISpriteEditorDataProvider e não por TextureImporter.spritesheet:
    /// o segundo foi descontinuado, compila, não avisa nada e não faz efeito.
    /// A folha inteira vira um sprite só e o pássaro aparece com os quatro
    /// quadros lado a lado.
    /// </summary>
    static void Fatiar(string caminho, Recorte[] recortes)
    {
        var imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (imp == null) { Debug.LogWarning($"[Bicudo] não importou {caminho}"); return; }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = PixelsPorUnidade;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();

        var fabrica = new SpriteDataProviderFactories();
        fabrica.Init();
        var dados = fabrica.GetSpriteEditorDataProviderFromObject(imp);
        dados.InitSpriteEditorDataProvider();

        var lista = new List<SpriteRect>();
        foreach (var r in recortes)
        {
            lista.Add(new SpriteRect
            {
                name = r.nome,
                spriteID = GUID.Generate(),
                rect = new Rect(r.x, r.y, r.largura, r.altura),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, r.pivoY),
            });
        }

        dados.SetSpriteRects(lista.ToArray());
        dados.Apply();
        imp.SaveAndReimport();

        Debug.Log($"[Bicudo] {Path.GetFileName(caminho)}: {lista.Count} recortes.");
    }

    /// <summary>Imagem que entra inteira, sem recorte. É o caso do fundo.</summary>
    static void ConfigurarInteira(string caminho)
    {
        var imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = PixelsPorUnidade;
        imp.filterMode = FilterMode.Point;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
    }

    /// <summary>Acha um recorte pelo nome, em qualquer das folhas.</summary>
    public static Sprite Achar(string nome)
    {
        foreach (var folha in new[] { FolhaPassaro, FolhaCano, FolhaChao })
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(folha))
                if (obj is Sprite s && s.name == nome) return s;
        }
        return null;
    }

    public static Sprite AcharFundo() => AssetDatabase.LoadAssetAtPath<Sprite>(Fundo);

    /// <summary>Os quatro quadros do pássaro, em ordem.</summary>
    public static Sprite[] QuadrosDoPassaro()
    {
        return AssetDatabase.LoadAllAssetsAtPath(FolhaPassaro)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();
    }
}

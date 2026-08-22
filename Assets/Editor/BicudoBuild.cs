using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Monta o jogo inteiro numa ordem só e gera o executável.
///
/// A ordem importa: os prefabs dependem dos recortes, e a cena depende dos
/// prefabs. Fora de ordem sai cena vazia sem erro nenhum, que é o pior tipo
/// de falha para achar depois.
/// </summary>
public static class BicudoBuild
{
    [MenuItem("Bicudo/MONTAR TUDO", false, -10)]
    public static void MontarTudo()
    {
        Debug.Log("[Bicudo] 1/3 preparando a arte");
        ArteDoBicudo.Preparar();

        Debug.Log("[Bicudo] 2/3 montando prefabs");
        PrefabsDoBicudo.Montar();

        Debug.Log("[Bicudo] 3/3 gerando a cena");
        CenaDoBicudo.Gerar();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bicudo] PRONTO - jogo montado.");
    }

    [MenuItem("Bicudo/Gerar executável", false, 20)]
    public static void GerarExecutavel()
    {
        var opcoes = new BuildPlayerOptions
        {
            scenes = new[] { CenaDoBicudo.CenaJogo },
            locationPathName = "Build/Bicudo.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var relatorio = BuildPipeline.BuildPlayer(opcoes);
        var resumo = relatorio.summary;

        if (resumo.result == BuildResult.Succeeded)
            Debug.Log($"[Bicudo] executável gerado: {resumo.totalSize / 1048576} MB em Build/");
        else
            Debug.LogError($"[Bicudo] build falhou: {resumo.result}");
    }
}

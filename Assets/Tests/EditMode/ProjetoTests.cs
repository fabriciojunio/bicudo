using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Confere que o projeto montado tem tudo no lugar.
///
/// O jogo se constrói a partir de scripts de editor, e um passo que falha em
/// silêncio deixa a cena de pé mas vazia. Estes testes olham o resultado da
/// montagem, não a intenção dela.
/// </summary>
public class ProjetoTests
{
    [Test]
    public void AArte_FoiRecortada()
    {
        var quadros = ArteDoBicudo.QuadrosDoPassaro();
        Assert.AreEqual(4, quadros.Length,
            "o pássaro precisa dos quatro quadros do bater de asas");

        Assert.IsNotNull(ArteDoBicudo.Achar("CanoBoca"), "sem a boca do cano");
        Assert.IsNotNull(ArteDoBicudo.Achar("CanoCorpo"), "sem o corpo do cano");
        Assert.IsNotNull(ArteDoBicudo.Achar("Chao"), "sem o tile do chão");
        Assert.IsNotNull(ArteDoBicudo.AcharFundo(), "sem o fundo");
    }

    [Test]
    public void OsQuadrosDoPassaro_SaoTodosDoMesmoTamanho()
    {
        var quadros = ArteDoBicudo.QuadrosDoPassaro();
        Assert.IsNotEmpty(quadros);

        var primeiro = quadros[0].rect.size;
        foreach (var q in quadros)
            Assert.AreEqual(primeiro, q.rect.size,
                $"o quadro {q.name} saiu de tamanho diferente: a animação vai pular");
    }

    [Test]
    public void OsPrefabs_Existem()
    {
        foreach (var caminho in new[] { PrefabsDoBicudo.CaminhoPassaro, PrefabsDoBicudo.CaminhoCano })
            Assert.IsTrue(File.Exists(caminho), $"prefab ausente: {caminho}");
    }

    [Test]
    public void OPassaro_TemDesenhoEAnimacao()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDoBicudo.CaminhoPassaro);
        Assert.IsNotNull(prefab);

        var sr = prefab.GetComponent<SpriteRenderer>();
        Assert.IsNotNull(sr, "sem SpriteRenderer");
        Assert.IsNotNull(sr.sprite, "o pássaro ficaria invisível");

        var asa = prefab.GetComponent<AsaBatendo>();
        Assert.IsNotNull(asa, "sem o componente que troca os quadros");
        Assert.IsNotNull(asa.quadros, "os quadros não foram ligados");
        Assert.AreEqual(4, asa.quadros.Length,
            "sem os quadros ligados o pássaro fica congelado no primeiro desenho");
    }

    [Test]
    public void OCano_TemOsDoisLados()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDoBicudo.CaminhoCano);
        Assert.IsNotNull(prefab);

        var cano = prefab.GetComponent<Cano>();
        Assert.IsNotNull(cano);
        Assert.IsNotNull(cano.canoDeCima, "sem o cano de cima");
        Assert.IsNotNull(cano.canoDeBaixo, "sem o cano de baixo");
        // Não há gatilho de pontuação: quem conta é o gerador, comparando
        // posições. Gatilho exigiria colisor no pássaro, que não tem.
    }

    [Test]
    public void OsLadosDoCano_SaoSolidosENaCamadaQueMata()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDoBicudo.CaminhoCano);
        var cano = prefab.GetComponent<Cano>();

        foreach (var lado in new[] { cano.canoDeCima, cano.canoDeBaixo })
        {
            var col = lado.GetComponent<Collider2D>();
            Assert.IsNotNull(col, $"{lado.name} está sem colisor: dá para atravessar");
            Assert.IsFalse(col.isTrigger, $"{lado.name} é gatilho e não barra nada");
            Assert.AreEqual(PrefabsDoBicudo.CamadaCenario, lado.gameObject.layer,
                $"{lado.name} fora da camada que o pássaro consulta");
        }
    }

    [Test]
    public void NenhumDesenho_FicouSemSprite()
    {
        // Um SpriteRenderer sem sprite é invisível e não reclama. Foi assim que
        // já saiu build com o cenário inteiro faltando.
        foreach (var caminho in new[] { PrefabsDoBicudo.CaminhoPassaro, PrefabsDoBicudo.CaminhoCano })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            var mudos = prefab.GetComponentsInChildren<SpriteRenderer>(true)
                              .Where(sr => sr.sprite == null)
                              .Select(sr => sr.name)
                              .ToList();

            Assert.IsEmpty(mudos,
                $"em {Path.GetFileName(caminho)} há desenho sem sprite: " + string.Join(", ", mudos));
        }
    }

    [Test]
    public void ACena_EstaRegistradaNoBuild()
    {
        Assert.IsTrue(File.Exists(CenaDoBicudo.CenaJogo), "a cena não foi gerada");

        var registradas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        CollectionAssert.Contains(registradas, CenaDoBicudo.CenaJogo,
            "a cena existe mas ficou de fora do build: o executável abriria vazio");
    }
}

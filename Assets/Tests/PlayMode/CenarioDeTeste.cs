using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monta as peças mínimas para um teste rodar sem a cena do jogo.
///
/// Cada teste levanta o que precisa e derruba tudo depois. Compartilhar
/// objetos entre testes faria um passar ou falhar conforme a ordem em que a
/// suíte rodou, que é o tipo de falha que ninguém consegue reproduzir.
/// </summary>
public static class CenarioDeTeste
{
    public const int CamadaCenario = 8;
    public static LayerMask MascaraDoCenario => 1 << CamadaCenario;

    static readonly List<GameObject> criados = new List<GameObject>();

    /// <summary>
    /// Zera o que sobrevive entre testes: o recorde fica em PlayerPrefs, e sem
    /// isto um teste mediria a marca deixada pelo anterior.
    /// </summary>
    public static void Preparar()
    {
        PlayerPrefs.DeleteKey("bicudo.recorde");
        PlayerPrefs.Save();
        Physics2D.queriesHitTriggers = true;
        VarrerSobras();
    }

    /// <summary>
    /// Tira de cena o que ficou do teste anterior.
    ///
    /// Um teste que carrega a cena do jogo deixa ela carregada para os
    /// seguintes. Como só pode existir uma Partida, a que o cenário de teste
    /// cria depois se autodestrói, e o teste passa a medir a partida da cena
    /// antiga, no estado em que ela tiver parado. O sintoma é o pássaro caindo
    /// numa tela que deveria estar parada, sem nada no código explicando.
    /// </summary>
    static void VarrerSobras()
    {
        foreach (var p in Object.FindObjectsOfType<Partida>()) Object.DestroyImmediate(p.gameObject);
        foreach (var p in Object.FindObjectsOfType<Passaro>()) Object.DestroyImmediate(p.gameObject);
        foreach (var g in Object.FindObjectsOfType<GeradorDeCanos>()) Object.DestroyImmediate(g.gameObject);
        foreach (var f in Object.FindObjectsOfType<FaixaQueRola>()) Object.DestroyImmediate(f.gameObject);
        foreach (var i in Object.FindObjectsOfType<Interface>()) Object.DestroyImmediate(i.gameObject);
    }

    public static Partida CriarPartida()
    {
        var go = Novo("_Partida");
        return go.AddComponent<Partida>();
    }

    public static Passaro CriarPassaro(Vector2 onde)
    {
        var go = Novo("Passaro");
        go.SetActive(false);              // segura o Awake até terminar de montar
        go.transform.position = onde;
        go.AddComponent<SpriteRenderer>();

        var p = go.AddComponent<Passaro>();
        p.camadasQueMatam = MascaraDoCenario;

        go.SetActive(true);
        return p;
    }

    /// <summary>Um obstáculo sólido na camada que mata.</summary>
    public static GameObject CriarParede(Vector2 centro, Vector2 tamanho)
    {
        var go = Novo("Parede");
        go.layer = CamadaCenario;
        go.transform.position = centro;
        go.AddComponent<BoxCollider2D>().size = tamanho;
        return go;
    }

    public static Cano CriarCano(Vector2 onde, float meiaAbertura = 1.35f)
    {
        var go = Novo("Cano");
        go.SetActive(false);
        go.transform.position = onde;

        var cano = go.AddComponent<Cano>();
        cano.meiaAbertura = meiaAbertura;
        cano.canoDeCima = Filho(go, "CanoDeCima");
        cano.canoDeBaixo = Filho(go, "CanoDeBaixo");

        var portariaGO = new GameObject("Portaria");
        portariaGO.transform.SetParent(go.transform, false);
        var portaria = portariaGO.AddComponent<BoxCollider2D>();
        portaria.isTrigger = true;
        portaria.size = new Vector2(0.4f, 40f);
        cano.portaria = portaria;

        go.SetActive(true);
        return cano;
    }

    public static GeradorDeCanos CriarGerador(Cano modelo)
    {
        var go = Novo("Canos");
        go.SetActive(false);

        var g = go.AddComponent<GeradorDeCanos>();
        g.modelo = modelo;
        g.quantidade = 4;
        g.xDeEntrada = 8f;
        g.xDeSaida = -9f;

        go.SetActive(true);
        return g;
    }

    public static FaixaQueRola CriarFaixa(int quantasPecas, float largura)
    {
        var go = Novo("Faixa");
        go.SetActive(false);

        var pecas = new Transform[quantasPecas];
        for (int i = 0; i < quantasPecas; i++)
        {
            var p = new GameObject("Peca_" + i);
            p.transform.SetParent(go.transform, false);
            p.transform.localPosition = new Vector3(i * largura, 0f, 0f);
            pecas[i] = p.transform;
        }

        var f = go.AddComponent<FaixaQueRola>();
        f.pecas = pecas;
        f.largura = largura;
        f.velocidade = 3f;

        go.SetActive(true);
        return f;
    }

    static Transform Filho(GameObject pai, string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        return go.transform;
    }

    static GameObject Novo(string nome)
    {
        var go = new GameObject(nome);
        criados.Add(go);
        return go;
    }

    public static void Limpar()
    {
        foreach (var go in criados) if (go != null) Object.DestroyImmediate(go);
        criados.Clear();

        PlayerPrefs.DeleteKey("bicudo.recorde");
        PlayerPrefs.Save();
    }
}

using UnityEngine;

/// <summary>
/// Troca os quadros do pássaro.
///
/// Sem Animator: são quatro quadros num laço, e um controlador de animação
/// para isso significa dois assets a mais no projeto e um lugar a mais onde a
/// referência pode se perder. O bater fica mais rápido enquanto ele sobe, que
/// é o que dá a sensação de esforço.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AsaBatendo : MonoBehaviour
{
    public Sprite[] quadros;

    [Tooltip("Quadros por segundo no voo plano.")]
    public float ritmo = 10f;

    [Tooltip("Quantas vezes mais rápido logo depois do impulso.")]
    public float pressaSubindo = 1.8f;

    SpriteRenderer sr;
    Passaro passaro;
    float relogio;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        passaro = GetComponent<Passaro>();
    }

    void Update()
    {
        if (quadros == null || quadros.Length == 0) return;

        float velocidade = ritmo;
        if (passaro != null && passaro.VelocidadeVertical > 0.5f) velocidade *= pressaSubindo;

        relogio += Time.deltaTime * velocidade;
        int i = Mathf.FloorToInt(relogio) % quadros.Length;
        sr.sprite = quadros[i];
    }
}

using UnityEngine;

public class ConveyorBeltAnimation : MonoBehaviour
{
    public Vector2 direction = new Vector2(0, -1);

    private Material mat;
    private ConveyorBelt standardBelt;
    private OverheadConveyor overheadBelt;
    private static readonly int MainTexOffset = Shader.PropertyToID("_MainTex");

    void Start()
    {
        // Próbujemy pobraæ jeden lub drugi komponent
        standardBelt = GetComponent<ConveyorBelt>();
        overheadBelt = GetComponent<OverheadConveyor>();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            mat = spriteRenderer.material;
        }

        if (standardBelt == null && overheadBelt == null)
        {
            Debug.LogWarning($"[ConveyorAnim] Brak skryptu pasa na {gameObject.name}!");
        }
    }

    void Update()
    {
        if (mat == null) return;

        float currentSpeed = 0f;

        // Pobieramy prêdkoœæ z dostêpnego komponentu
        if (standardBelt != null)
            currentSpeed = standardBelt.CurrentBeltSpeed;
        else if (overheadBelt != null)
            currentSpeed = overheadBelt.CurrentBeltSpeed; // Upewnij siê, ¿e OverheadConveyor te¿ ma tê w³aœciwoœæ
        else
            return;

        Vector2 globalOffset = direction * (Time.time * currentSpeed);
        mat.SetTextureOffset(MainTexOffset, globalOffset);
    }
}
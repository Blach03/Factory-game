using UnityEngine;

public class ConveyorBeltAnimation : MonoBehaviour
{
    public Vector2 direction = new Vector2(0, -1);

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private ConveyorBelt standardBelt;
    private OverheadConveyor overheadBelt;
    private bool isVisible = true;
    private bool isRegisteredInManager = false;
    private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");
    private float cachedSpeed;

    void Start()
    {
        // Pr�bujemy pobra� jeden lub drugi komponent
        standardBelt = GetComponent<ConveyorBelt>();
        overheadBelt = GetComponent<OverheadConveyor>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        if (standardBelt == null && overheadBelt == null)
        {
            Debug.LogWarning($"[ConveyorAnim] Brak skryptu pasa na {gameObject.name}!");
        }

        cachedSpeed = GetCurrentSpeed();

        if (spriteRenderer != null)
        {
            SetVisibility(spriteRenderer.isVisible);
        }
    }

    void OnEnable()
    {
        if (spriteRenderer != null)
        {
            SetVisibility(spriteRenderer.isVisible);
        }
        else
        {
            SetVisibility(false);
        }
    }

    void OnDisable()
    {
        SetVisibility(false);
    }

    public void ApplyScrollOffset()
    {
        if (!isVisible || spriteRenderer == null || propertyBlock == null) return;

        float currentSpeed = GetCurrentSpeed();
        if (!Mathf.Approximately(currentSpeed, cachedSpeed))
        {
            cachedSpeed = currentSpeed;
        }

        Vector2 globalOffset = direction * (Time.time * cachedSpeed);

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector(MainTexST, new Vector4(1f, 1f, globalOffset.x, globalOffset.y));
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private float GetCurrentSpeed()
    {
        if (standardBelt != null)
        {
            return standardBelt.CurrentBeltSpeed;
        }

        if (overheadBelt != null)
        {
            return overheadBelt.CurrentBeltSpeed;
        }

        return 0f;
    }

    void OnBecameVisible()
    {
        SetVisibility(true);
    }

    void OnBecameInvisible()
    {
        SetVisibility(false);
    }

    private void SetVisibility(bool visible)
    {
        isVisible = visible;

        if (visible)
        {
            if (!isRegisteredInManager)
            {
                ConveyorBeltAnimationManager.Register(this);
                isRegisteredInManager = true;
            }
        }
        else
        {
            if (isRegisteredInManager)
            {
                ConveyorBeltAnimationManager.Unregister(this);
                isRegisteredInManager = false;
            }
        }
    }
}
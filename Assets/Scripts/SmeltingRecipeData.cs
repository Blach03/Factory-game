using UnityEngine;
using UnityEngine.Serialization; // Wymagane dla FormerlySerializedAs

[CreateAssetMenu(fileName = "SmeltingRecipe", menuName = "Resource/Smelting Recipe")]
public class SmeltingRecipeData : ScriptableObject, IBuildingRecipe
{
    [Header("Basic Info")]
    [FormerlySerializedAs("recipeName")]
    public string _recipeName;
    public string recipeName => _recipeName;

    [Header("Requirements")]
    [SerializeField]
    [FormerlySerializedAs("techRequirementId")]
    private string _techRequirementId;
    public string techRequirementId => _techRequirementId;

    [Header("Input")]
    [FormerlySerializedAs("primaryInput")]
    public ResourceData _primaryInput;
    public ResourceData primaryInput => _primaryInput;

    [FormerlySerializedAs("primaryInputAmount")]
    public int _primaryInputAmount = 1;
    public int primaryInputAmount => _primaryInputAmount;

    [FormerlySerializedAs("secondaryInput")]
    public ResourceData _secondaryInput;
    public ResourceData secondaryInput => _secondaryInput;

    [FormerlySerializedAs("secondaryInputAmount")]
    public int _secondaryInputAmount = 0;
    public int secondaryInputAmount => _secondaryInputAmount;

    [Header("Output")]
    [FormerlySerializedAs("outputItem")]
    public ResourceData _outputItem;
    public ResourceData outputItem => _outputItem;

    [FormerlySerializedAs("outputAmount")]
    public int _outputAmount = 1;
    public int outputAmount => _outputAmount;

    public float smeltingTime = 3.0f;

    public float powerRequirement = 0f;
    float IBuildingRecipe.powerRequirement => powerRequirement;
}
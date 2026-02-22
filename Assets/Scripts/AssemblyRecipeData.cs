using UnityEngine;
using UnityEngine.Serialization; // Wymagane dla FormerlySerializedAs

[CreateAssetMenu(fileName = "AssemblyRecipe", menuName = "Resource/Assembly Recipe")]
public class AssemblyRecipeData : ScriptableObject, IBuildingRecipe
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

    // Tertiary nie potrzebuje zmiany, jeœli nie by³o go w interfejsie i nazwa zostaje ta sama
    public ResourceData tertiaryInput;
    public int tertiaryInputAmount = 0;

    [Header("Output")]
    [FormerlySerializedAs("outputItem")]
    public ResourceData _outputItem;
    public ResourceData outputItem => _outputItem;

    [FormerlySerializedAs("outputAmount")]
    public int _outputAmount = 1;
    public int outputAmount => _outputAmount;

    public float assemblyTime = 5.0f;

    public float powerRequirement = 0f;
    float IBuildingRecipe.powerRequirement => powerRequirement;
}
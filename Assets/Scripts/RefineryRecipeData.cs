using UnityEngine;

[CreateAssetMenu(fileName = "NewRefineryRecipe", menuName = "Factory/Refinery Recipe")]
public class RefineryRecipeData : ScriptableObject, IBuildingRecipe
{
    [Header("Główne Ustawienia")]
    public string recipeName;
    public string techRequirementId;
    public float processTime = 3.0f;

    [Header("Składnik Przedmiotowy (Primary)")]
    public ResourceData inputItem;
    public int inputItemAmount;

    public ResourceData tetriaryinputItem;
    public int tetriaryinputItemAmount;

    [Header("Składnik Płynny (Secondary)")]
    public ResourceData fluidResource; // Twoja ropa (ResourceData)
    public int fluidAmount; // Ilość płynu (jako float dla logiki rafinerii)

    [Header("Wynik")]
    public ResourceData outputResource;
    public int outputResultAmount;

    // --- IMPLEMENTACJA INTERFEJSU IBuildingRecipe ---
    // Mapujemy nasze pola na te wymagane przez interfejs, by Tooltip działał bez zmian.

    string IBuildingRecipe.recipeName => recipeName;
    string IBuildingRecipe.techRequirementId => techRequirementId;

    // Przedmiot wejściowy jako Primary
    ResourceData IBuildingRecipe.primaryInput => inputItem;
    int IBuildingRecipe.primaryInputAmount => inputItemAmount;

    // Płyn jako Secondary (dlatego Tooltip go wyświetli!)
    ResourceData IBuildingRecipe.secondaryInput => fluidResource;
    int IBuildingRecipe.secondaryInputAmount => fluidAmount; 

    // Rezultat
    ResourceData IBuildingRecipe.outputItem => outputResource;
    int IBuildingRecipe.outputAmount => outputResultAmount;

    public float powerRequirement = 0f;
    float IBuildingRecipe.powerRequirement => powerRequirement;
}
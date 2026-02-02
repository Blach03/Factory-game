
public interface IBuildingRecipe
{
    string recipeName { get; }
    string techRequirementId { get; }

    ResourceData primaryInput { get; }
    int primaryInputAmount { get; }

    ResourceData secondaryInput { get; }
    int secondaryInputAmount { get; }

    ResourceData outputItem { get; }
    int outputAmount { get; }
}
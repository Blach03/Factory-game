public interface IProductionBuilding
{
    IBuildingRecipe GetCurrentRecipe();
    int GetInputCount(int slotIndex);
    int GetCurrentOutputAmount();
    float GetProgressTimer();
    int inputCapacity { get; }
    int outputCapacity { get; }
}
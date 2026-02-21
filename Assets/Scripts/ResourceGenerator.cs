using UnityEngine;

public class ResourceGenerator : MonoBehaviour
{
    public GameObject ironOreDepositPrefab;
    public GameObject coalOreDepositPrefab;
    public GameObject copperOreDepositPrefab;
    public GameObject OilDepositPrefab;
    public GameObject WaterDepositPrefab;
    public GameObject SulfurDepositPrefab;

    public int minDeposits = 100;
    public int maxDeposits = 200;

    public Transform depositsContainer;

    private int minGridX = -20;
    private int maxGridX = 20;
    private int minGridY = -20;
    private int maxGridY = 20;

    public void InitializeGenerator()
    {
        if (depositsContainer == null)
        {
            GameObject containerGO = GameObject.Find("--DEPOSITS--");
            if (containerGO == null)
            {
                containerGO = new GameObject("--DEPOSITS--");
                containerGO.transform.position = Vector3.zero;
            }
            depositsContainer = containerGO.transform;
        }

        GenerateDeposits();
    }

    private void GenerateDeposits()
    {
        if (ironOreDepositPrefab == null)
        {
            Debug.LogError("Brak przypisanego Prefabu z³o¿a rudy ¿elaza!");
            return;
        }
        if (coalOreDepositPrefab == null)
        {
            Debug.LogError("Brak przypisanego Prefabu z³o¿a wêgla!");
            return;
        }

        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager nie jest dostêpny!");
            return;
        }

        int depositsToGenerate = Random.Range(minDeposits, maxDeposits + 1);
        int generatedCount = 0;
        int maxAttempts = depositsToGenerate * 5;

        int ironCount = 0;
        int coalCount = 0;
        int copperCount = 0;

        for (int i = 0; i < maxAttempts && generatedCount < depositsToGenerate; i++)
        {
            int randomX = Random.Range(minGridX, maxGridX + 1);
            int randomY = Random.Range(minGridY, maxGridY + 1);
            Vector2Int gridPosition = new Vector2Int(randomX, randomY);

            if (GridManager.Instance.GetResourceDeposit(gridPosition) == null)
            {
                GameObject depositPrefabToUse;
                float RandomValue = Random.value;
                bool isCoal = RandomValue < 0.2f;
                bool isOil = RandomValue > 0.2f && RandomValue < 0.27f;
                bool isWater = RandomValue > 0.27f && RandomValue < 0.34f;
                bool isSulfur = RandomValue > 0.34f && RandomValue < 0.4f;
                bool isCopper = RandomValue > 0.4f && RandomValue < 0.7f;

                if (isCoal)
                {
                    depositPrefabToUse = coalOreDepositPrefab;
                    coalCount++;
                }
                else if (isCopper)
                {
                    depositPrefabToUse = copperOreDepositPrefab;
                    copperCount++;
                }
                else if (isSulfur)
                {
                    depositPrefabToUse = SulfurDepositPrefab;
                    copperCount++;
                }
                else if (isOil)
                {
                    depositPrefabToUse = OilDepositPrefab;
                    copperCount++;
                }
                else if (isWater)
                {
                    depositPrefabToUse = WaterDepositPrefab;
                    copperCount++;
                }
                else
                {
                    depositPrefabToUse = ironOreDepositPrefab;
                    ironCount++;
                }

                GameObject newDepositObject = Instantiate(depositPrefabToUse, depositsContainer);
                ResourceDeposit deposit = newDepositObject.GetComponent<ResourceDeposit>();

                if (deposit != null)
                {
                    deposit.Initialize(gridPosition);
                    generatedCount++;
                }
                else
                {
                    Destroy(newDepositObject);
                    Debug.LogError($"Prefab {depositPrefabToUse.name} nie zawiera skryptu ResourceDeposit!");
                    return;
                }
            }
        }

        Debug.Log($"Generowanie surowców zakoñczone. Wygenerowano: {generatedCount}/{depositsToGenerate} z³ó¿.");
    }
}
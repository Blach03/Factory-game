using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnlockManager : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private GameObject assemblerButton;
    [SerializeField] private GameObject overheadConveyorButton; // Nowe pole dla t2

    void Start()
    {
        StartCoroutine(InitialRefreshRoutine());
    }

    private IEnumerator InitialRefreshRoutine()
    {
        float timer = 0;
        while (TechTreeManager.Instance == null && timer < 5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        RefreshUnlocks();
    }

    public void RefreshUnlocks()
    {
        TechTreeManager tree = TechTreeManager.Instance;

        if (tree == null)
        {
            tree = Object.FindObjectsByType<TechTreeManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        }

        if (tree == null) return;

        // --- Logika Odblokowywania ---

        // Technologia t1 -> Assembler
        if (assemblerButton != null)
        {
            assemblerButton.SetActive(tree.IsResearched("t1"));
        }

        // Technologia t2 -> Overhead Conveyor
        if (overheadConveyorButton != null)
        {
            overheadConveyorButton.SetActive(tree.IsResearched("t2"));
        }

        Debug.Log("<color=green>[UnlockManager]</color> Stan przycisków odœwie¿ony.");
    }
}
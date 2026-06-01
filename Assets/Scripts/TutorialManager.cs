using UnityEngine;
using System.Collections.Generic;
using System;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private TutorialPanel tutorialPanel;
    private bool isTutorialActive = false;
    private int currentStepIndex = 0;
    private bool finalSummaryTimerElapsed = false;

    private List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        [TextArea(3, 5)]
        public string description;
        public Action onStepStart;   // Callback gdy krok się zaczyna
        public Func<bool> onCheckComplete; // Callback do weryfikacji ukończenia
        public bool isCompleted = false;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeTutorialSteps();

        TryResolveTutorialPanel();
    }

    private void InitializeTutorialSteps()
    {
        tutorialSteps.Clear();

        // STEP 1: Postaw minera na węglu
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 1: Mine Coal",
            description = "Welcome to your factory! You can move the camera with WASD or the arrow keys, and zoom in or out with the mouse wheel. Your first task is to set up a coal mine. Click on the Miner button in the buildings menu, then place it on a Coal deposit. Look for the dark coal deposits on the map.",
            onStepStart = () => 
            {
                Debug.Log("[Tutorial] Step 1 started - Highlight Miner in UI");
                TutorialUIHighlight.Instance?.HighlightButton("MinerButton");
            },
            onCheckComplete = () => CheckMinerOnCoal()
        });

        // STEP 2: Postaw taśmociąg
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 2: Build a Conveyor Belt",
            description = "Now connect your miner to a storage area using a Conveyor Belt. Select the Conveyor Belt and drag from the miner to create a line. Tip: press R to rotate selected buildable objects before placing. The build cost of the currently selected object is shown at the top of the screen.",
            onStepStart = () => 
            {
                Debug.Log("[Tutorial] Step 2 started - Highlight Conveyor Belt");
                TutorialItemTracker.Reset();
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "ConveyorButton",
                    "ConveyorBeltButton",
                    "BeltButton",
                    "Conveyor",
                    "MainCanvas/BottomBar/ConveyorButton"
                );
            },
            onCheckComplete = () => TutorialItemTracker.AnyItemMovedByConveyor
        });

        // STEP 3: Postaw magazyn
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 3: Build a Storage Container",
            description = "Place a Storage Container at the end of your conveyor belt to collect coal. Tip: when you click Storage, you can set an item limit. Storage will stop collecting that item if your inventory is already at or above the limit. You can also pick up non-moving items from the map by pressing Z on them.",
            onStepStart = () => 
            {
                Debug.Log("[Tutorial] Step 3 started");
                TutorialItemTracker.ResetStorageToInventory();
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "StorageButton",
                    "StorageContainerButton",
                    "StoregeButton",
                    "StoreButton",
                    "MainCanvas/BottomBar/StorageButton"
                );
            },
            onCheckComplete = () => TutorialItemTracker.AnyItemMovedByStorageToInventory
        });

        // STEP 4: Inventory crafting (Iron Gear x10)
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 4: Open Inventory and Craft",
            description = "Press E to open your inventory. You can craft items directly there. Select Iron Gear and click Craft x10. The crafting job is added to a queue and continues processing in the background, even if you close the inventory.",
            onStepStart = () => 
            {
                Debug.Log("[Tutorial] Step 4 started - Inventory Crafting");
                TutorialItemTracker.ResetCraftX10IronGear();
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "InventoryButton",
                    "InventoryPanel",
                    "IronGear",
                    "IronGearRecipe",
                    "MainCanvas/InventoryPanel"
                );
            },
            onCheckComplete = () => TutorialItemTracker.PressedCraftX10IronGear
        });

        // STEP 5: Furnace + Copper Bar recipe
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 5: Build a Furnace and Set Recipe",
            description = "Build a Furnace and select the Copper Bar recipe. This is your first smelting setup.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 5 started - Furnace recipe selection");
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "FurnaceButton",
                    "Furnace",
                    "SmelterButton",
                    "MainCanvas/BottomBar/FurnaceButton"
                );
            },
            onCheckComplete = () => CheckAnyFurnaceHasCopperBarRecipe()
        });

        // STEP 6: Miners on copper + belt guidance
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 6: Feed the Furnace",
            description = "Place miners on Copper Ore (orange deposits), and route both Copper Ore and Coal to the furnace using conveyor belts. Tip: you can destroy buildings with X, and it fully refunds the resources used to build them.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 6 started - Miners on Copper Ore");
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "MinerButton",
                    "MainCanvas/BottomBar/MinerButton"
                );
            },
            onCheckComplete = () => CheckMinerOnCopper()
        });

        // STEP 7: Move copper bars from furnace to storage/inventory
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 7: Collect Copper Bars in Storage",
            description = "Transport items from the furnace and place a new Storage Container to collect Copper Bars into your inventory. Tip: if you ever get lost, you can press H to teleport to the middle of the map. You can check your production progress by opening the furnace's UI.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 7 started - Waiting for Copper Bar in storage");
                TutorialItemTracker.ResetStorageCopperBarToInventory();
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "StorageButton",
                    "StorageContainerButton",
                    "StoregeButton",
                    "StoreButton",
                    "MainCanvas/BottomBar/StorageButton"
                );
            },
            onCheckComplete = () => TutorialItemTracker.CopperBarMovedByStorageToInventory
        });

        // STEP 8: Craft Copper Wire + Basic Science Packs
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 8: Craft Science Packs in Inventory",
            description = "Open inventory again with E. First craft Copper Wires, then craft 10 Basic Science Packs.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 8 started - Craft science packs in inventory");
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "InventoryButton",
                    "InventoryPanel",
                    "BasicResearchPack",
                    "BasicResearchPackRecipe",
                    "MainCanvas/InventoryPanel"
                );
            },
            onCheckComplete = () => CheckBasicSciencePackCraftedInInventory(10)
        });

        // STEP 9: Open tech tree and research Assembler
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 9: Research Assembler",
            description = "Press T to open the technology tree, select the first technology (Assembler), and research it.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 9 started - Research Assembler");
            },
            onCheckComplete = () => CheckAssemblerTechResearched()
        });

        // STEP 10: Build assembler and feed Copper Bar
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 10: Build Assembler for Copper Wire",
            description = "Place an Assembler, select the Copper Wire recipe, and deliver Copper Bars into the assembler.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 10 started - Assembler + Copper Wire");
                TutorialItemTracker.ResetCopperBarEnteredAssembler();
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "AssemblerButton",
                    "Assembler",
                    "MainCanvas/BottomBar/AssemblerButton"
                );
            },
            onCheckComplete = () => CheckAnyAssemblerHasCopperWireRecipe() && TutorialItemTracker.CopperBarEnteredAssembler
        });

        // STEP 11: Power shortage and pumpjack on water
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 11: Fix Power Shortage",
            description = "As you can notice in the top-right corner, we do not have enough power, so the assembler will not process this recipe until power is sufficient. Place a Pumpjack on a Water deposit to start extracting water.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 11 started - Pumpjack on Water");
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "PumpjackButton",
                    "Pumpjack",
                    "MainCanvas/BottomBar/PumpjackButton"
                );
            },
            onCheckComplete = () => CheckPumpjackOnWater()
        });

        // STEP 12: Build pipe near pumpjack and open pipe info
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 12: Connect a Pipe to the Pumpjack",
            description = "Build a Pipe next to your Pumpjack so the extracted water can enter a pipe network. Tip: you can inspect fluid info by clicking a pipe.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 12 started - Pipe near Pumpjack + open network info");
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "PipeButton",
                    "Pipe",
                    "MainCanvas/BottomBar/PipeButton"
                );
            },
            onCheckComplete = () => CheckPipeNetworkWindowOpen()
        });

        // STEP 13: Build refinery, select steam recipe and feed coal
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 13: Build a Refinery and Produce Steam",
            description = "Place a Refinery near your pipe network. Open its recipe selection and choose the Steam recipe, then connect Coal so the refinery can process and produce Steam.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 13 started - Refinery + Steam recipe");
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "RefineryButton",
                    "Refinery",
                    "MainCanvas/BottomBar/RefineryButton"
                );
            },
            onCheckComplete = () => CheckAnyRefineryProducedSteam()
        });

        // STEP 14: Build steam turbine and connect steam
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 14: Build a Steam Turbine",
            description = "Place a Steam Turbine. Refineries output fluids and gases to connections on the 3 tiles in front of their output side, so make sure your pipe is attached there. Connect the produced Steam to the turbine with pipe and start generating electricity.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 14 started - Steam Turbine");
                TutorialUIHighlight.Instance?.HighlightFirstExisting(
                    "SteamTurbineButton",
                    "TurbineButton",
                    "SteamTurbine",
                    "MainCanvas/BottomBar/SteamTurbineButton"
                );
            },
            onCheckComplete = () => CheckAnyPowerProduction()
        });

        // STEP 15: Keyboard shortcuts - copy blueprint + pipette
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 15: Copy Blueprint and Use Pipette",
            description = "Press Ctrl+C to enter area copy mode, then drag the mouse to select an area. You will get a blueprint of the whole selected area that you can rotate and place in one action. Also use pipette with Q: hover a machine and press Q to copy that machine with its configured recipe. Tip: if you make a mistake, you can always press Ctrl+Z to undo previous actions.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 15 started - Ctrl+C area copy and Q pipette");
                PlacementManager.Instance?.ResetTutorialShortcutMilestones();
            },
            onCheckComplete = () => CheckCopyAndPipetteShortcuts()
        });

        // STEP 16: Keyboard shortcuts - area delete
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 16: Delete an Area with Ctrl+X",
            description = "Press Ctrl+X to enter area delete mode. Drag with the mouse around a selected area to remove all machines and items inside that area.",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 16 started - Ctrl+X area delete");
            },
            onCheckComplete = () => CheckAreaDeleteShortcut()
        });

        // STEP 17: Final summary
        tutorialSteps.Add(new TutorialStep
        {
            title = "Step 17: Tutorial Complete",
            description = "That's all the basic stuff. Progress through the game by researching new technologies, and have fun!",
            onStepStart = () =>
            {
                Debug.Log("[Tutorial] Step 17 started - Final summary");
                finalSummaryTimerElapsed = false;
                StartCoroutine(CompleteFinalSummaryAfterDelay(10f));
            },
            onCheckComplete = () => finalSummaryTimerElapsed
        });
    }

    public void StartTutorial()
    {
        TryResolveTutorialPanel();

        if (tutorialPanel == null)
        {
            Debug.LogError("Nie można uruchomić tutoriala - brak TutorialPanel!");
            return;
        }

        isTutorialActive = true;
        currentStepIndex = 0;
        finalSummaryTimerElapsed = false;
        ShowCurrentStep();
    }

    public void SkipTutorial()
    {
        isTutorialActive = false;
        currentStepIndex = 0;
        finalSummaryTimerElapsed = false;
        if (tutorialPanel != null)
            tutorialPanel.HidePanel();
        TutorialUIHighlight.Instance?.ClearHighlight();
        Debug.Log("Tutorial pominięty.");
    }

    private void ShowCurrentStep()
    {
        if (currentStepIndex >= tutorialSteps.Count)
        {
            CompleteTutorial();
            return;
        }

        TutorialStep step = tutorialSteps[currentStepIndex];
        tutorialPanel.DisplayStep(step.title, step.description);

        // Wykonaj callback startu
        step.onStepStart?.Invoke();
    }

    private void Update()
    {
        if (!isTutorialActive || currentStepIndex >= tutorialSteps.Count) return;

        // Dla kroków inventory ukrywamy overlay tutoriala, gdy inventory jest otwarte,
        // żeby nie zasłaniać listy itemów i przycisków craftingu.
        bool isInventoryStep = currentStepIndex == 3 || currentStepIndex == 7;
        bool inventoryOpen = UIManager.Instance != null &&
                             UIManager.Instance.inventoryPanel != null &&
                             UIManager.Instance.inventoryPanel.activeSelf;

        // Dla kroków wyboru receptury ukrywamy overlay, gdy jest otwarte okno Choose Recipe.
        bool isFurnaceRecipeStep = currentStepIndex == 4;
        bool isAssemblerRecipeStep = currentStepIndex == 9;
        bool isRefineryRecipeStep = currentStepIndex == 12;
        bool chooseRecipeOpen = UIManager.Instance != null &&
                                UIManager.Instance.recipeSelectionPanel != null &&
                                UIManager.Instance.recipeSelectionPanel.gameObject.activeSelf;

        // Dla kroku technologii ukrywamy overlay, gdy drzewko jest otwarte.
        bool isTechTreeStep = currentStepIndex == 8;
        bool techTreeOpen = UIManager.Instance != null &&
                            UIManager.Instance.technologyPanel != null &&
                            UIManager.Instance.technologyPanel.activeSelf;

        if (tutorialPanel != null)
        {
            bool hideOverlay = (isInventoryStep && inventoryOpen) ||
                               ((isFurnaceRecipeStep || isAssemblerRecipeStep || isRefineryRecipeStep) && chooseRecipeOpen) ||
                               (isTechTreeStep && techTreeOpen);
            tutorialPanel.SetOverlayVisible(!hideOverlay);
        }

        TutorialStep step = tutorialSteps[currentStepIndex];
        if (step.onCheckComplete != null && step.onCheckComplete.Invoke())
        {
            if (!step.isCompleted)
            {
                step.isCompleted = true;
                Debug.Log($"[Tutorial] Krok {currentStepIndex + 1} ukończony!");
                TutorialUIHighlight.Instance?.ClearHighlight();
                
                // Przejdź do następnego kroku po 1 sekundzie (ostatni krok zamknij od razu)
                float completionDelay = currentStepIndex == tutorialSteps.Count - 1 ? 0f : 1f;
                StartCoroutine(NextStepAfterDelay(completionDelay));
            }
        }
    }

    private System.Collections.IEnumerator NextStepAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentStepIndex++;
        ShowCurrentStep();
    }

    private System.Collections.IEnumerator CompleteFinalSummaryAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isTutorialActive) yield break;
        if (currentStepIndex >= tutorialSteps.Count) yield break;

        finalSummaryTimerElapsed = true;
    }

    private bool CheckMinerOnCoal()
    {
        // Szukamy wszystkich postawionych MinerBuilding
        MinerBuilding[] miners = FindObjectsOfType<MinerBuilding>();
        
        foreach (MinerBuilding miner in miners)
        {
            if (miner == null) continue;
            
            List<GridObject> objectsAtPos = GridManager.Instance?.GetGridObjects(miner.GetGridPosition());
            if (objectsAtPos == null) continue;

            // Szukamy zasobu węgla na tym polu
            foreach (GridObject obj in objectsAtPos)
            {
                ResourceDeposit deposit = obj as ResourceDeposit;
                if (deposit != null && deposit.resourceData.resourceName == "Coal Ore")
                {
                    Debug.Log("[Tutorial] Miner postawiony na węglu - zadanie ukończone!");
                    return true;
                }
            }
        }

        return false;
    }

    private bool CheckAnyFurnaceHasCopperBarRecipe()
    {
        FurnaceBuilding[] furnaces = FindObjectsOfType<FurnaceBuilding>();
        foreach (FurnaceBuilding furnace in furnaces)
        {
            if (furnace == null) continue;
            SmeltingRecipeData recipe = furnace.GetCurrentRecipe();
            if (recipe == null) continue;

            bool isCopperBarByItem = recipe.outputItem != null && recipe.outputItem.resourceName == "Copper Bar";
            bool isCopperBarByName = !string.IsNullOrEmpty(recipe.recipeName) && recipe.recipeName.Contains("Copper Bar");

            if (isCopperBarByItem || isCopperBarByName)
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckMinerOnCopper()
    {
        MinerBuilding[] miners = FindObjectsOfType<MinerBuilding>();

        foreach (MinerBuilding miner in miners)
        {
            if (miner == null) continue;

            List<GridObject> objectsAtPos = GridManager.Instance?.GetGridObjects(miner.GetGridPosition());
            if (objectsAtPos == null) continue;

            foreach (GridObject obj in objectsAtPos)
            {
                ResourceDeposit deposit = obj as ResourceDeposit;
                if (deposit != null && deposit.resourceData.resourceName == "Copper Ore")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CheckBasicSciencePackCraftedInInventory(int minimumCount)
    {
        if (PlayerInventory.Instance == null) return false;

        ResourceData[] items = Resources.LoadAll<ResourceData>("Items");
        if (items == null || items.Length == 0) return false;

        ResourceData basicSciencePack = null;
        foreach (ResourceData item in items)
        {
            if (item == null) continue;

            if (item.name == "BasicResearchPack" ||
                item.resourceName == "Basic Science Pack" ||
                item.resourceName == "Basic Research Pack")
            {
                basicSciencePack = item;
                break;
            }
        }

        if (basicSciencePack == null) return false;

        return PlayerInventory.Instance.GetItemCount(basicSciencePack) >= minimumCount;
    }

    private bool CheckAssemblerTechResearched()
    {
        return TechTreeManager.Instance != null && TechTreeManager.Instance.IsResearched("t1");
    }

    private bool CheckAnyAssemblerHasCopperWireRecipe()
    {
        AssemblerBuilding[] assemblers = FindObjectsOfType<AssemblerBuilding>();
        foreach (AssemblerBuilding assembler in assemblers)
        {
            if (assembler == null) continue;

            AssemblyRecipeData recipe = assembler.currentRecipe;
            if (recipe == null) continue;

            bool isCopperWireByItem = recipe.outputItem != null && recipe.outputItem.resourceName == "Copper Wire";
            bool isCopperWireByName = !string.IsNullOrEmpty(recipe.recipeName) && recipe.recipeName.Contains("Copper Wire");

            if (isCopperWireByItem || isCopperWireByName)
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckPumpjackOnWater()
    {
        PumpjackBuilding[] pumpjacks = FindObjectsOfType<PumpjackBuilding>();
        foreach (PumpjackBuilding pumpjack in pumpjacks)
        {
            if (pumpjack == null) continue;
            if (pumpjack.currentExtractedResource != null && pumpjack.currentExtractedResource.resourceName == "Water")
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckPipeNetworkWindowOpen()
    {
        if (PipeNetworkUI.Instance == null || PipeNetworkUI.Instance.windowPanel == null)
            return false;

        return PipeNetworkUI.Instance.windowPanel.activeSelf;
    }

    private bool CheckAnyRefineryProducedSteam()
    {
        RefineryBuilding[] refineries = FindObjectsOfType<RefineryBuilding>();

        foreach (RefineryBuilding refinery in refineries)
        {
            if (refinery == null || refinery.currentRecipe == null) continue;

            bool hasSteamRecipeName = !string.IsNullOrEmpty(refinery.currentRecipe.recipeName) &&
                                      refinery.currentRecipe.recipeName.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasSteamOutput = refinery.currentRecipe.outputResource != null &&
                                  !string.IsNullOrEmpty(refinery.currentRecipe.outputResource.resourceName) &&
                                  refinery.currentRecipe.outputResource.resourceName.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!hasSteamRecipeName && !hasSteamOutput) continue;

            if (refinery.currentOutputAmount > 0)
                return true;

            if (IsSteamStoredInOutputNetwork(refinery))
                return true;
        }

        return false;
    }

    private bool IsSteamStoredInOutputNetwork(RefineryBuilding refinery)
    {
        if (refinery == null || GridManager.Instance == null) return false;

        for (int i = 0; i < 3; i++)
        {
            Vector2Int checkPos = refinery.occupiedPosition;
            switch (refinery.outputDirection)
            {
                case RefineryBuilding.Direction.Right:
                    checkPos += new Vector2Int(3, i);
                    break;
                case RefineryBuilding.Direction.Down:
                    checkPos += new Vector2Int(i, -1);
                    break;
                case RefineryBuilding.Direction.Left:
                    checkPos += new Vector2Int(-1, i);
                    break;
                case RefineryBuilding.Direction.Up:
                    checkPos += new Vector2Int(i, 3);
                    break;
            }

            var objectsAtPos = GridManager.Instance.GetGridObjects(checkPos);
            if (objectsAtPos == null) continue;

            for (int j = 0; j < objectsAtPos.Count; j++)
            {
                PipeBuilding pipe = objectsAtPos[j] as PipeBuilding;
                if (pipe == null || pipe.CurrentNetwork == null) continue;

                PipeNetwork network = pipe.CurrentNetwork;
                if (network.storedFluid <= 0f || network.FluidType == null) continue;

                string fluidName = network.FluidType.resourceName;
                if (!string.IsNullOrEmpty(fluidName) && fluidName.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }

    private bool CheckAnyPowerProduction()
    {
        return PowerManager.Instance != null && PowerManager.Instance.GlobalProduction > 0f;
    }

    private bool CheckCopyAndPipetteShortcuts()
    {
        if (PlacementManager.Instance == null) return false;

        return PlacementManager.Instance.HasTutorialMultiObjectCopyPasteDone() &&
               PlacementManager.Instance.HasTutorialPipetteBeenUsed();
    }

    private bool CheckAreaDeleteShortcut()
    {
        if (PlacementManager.Instance == null) return false;

        return PlacementManager.Instance.HasTutorialAreaDeleteBeenUsed();
    }

    private void CompleteTutorial()
    {
        isTutorialActive = false;
        finalSummaryTimerElapsed = false;
        if (tutorialPanel != null)
            tutorialPanel.HidePanel();
        TutorialUIHighlight.Instance?.ClearHighlight();
        Debug.Log("Tutorial ukończony!");
    }

    private void TryResolveTutorialPanel()
    {
        if (tutorialPanel != null) return;

        tutorialPanel = FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
        if (tutorialPanel == null)
        {
            Debug.LogError("TutorialPanel nie znaleziony na scenie!");
        }
    }

    public bool IsTutorialActive() => isTutorialActive;
    public int GetCurrentStepIndex() => currentStepIndex;
}

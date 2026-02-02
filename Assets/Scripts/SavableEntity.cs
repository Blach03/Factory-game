using UnityEngine;
using System;
using System.Collections.Generic;

public class SavableEntity : MonoBehaviour
{
    [HideInInspector] public string uniqueID;
    public string prefabNameForSave;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
        }
    }

    public EntityData Save()
    {
        EntityData data = new EntityData();

        data.prefabName = prefabNameForSave;
        data.worldPosition = new float[] { transform.position.x, transform.position.y, transform.position.z };
        data.worldRotation = new float[] { transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z };
        data.uniqueID = uniqueID;
        // Trim() usuwa ewentualne zbêdne spacje na koñcach

        // Zapis pozycji
        data.worldPosition[0] = transform.position.x;
        data.worldPosition[1] = transform.position.y;
        data.worldPosition[2] = transform.position.z;

        data.layer = gameObject.layer;

        // --- DODATEK: Zapis rotacji (opcjonalne, ale zalecane) ---
        // Musisz dodaæ pole 'public float[] rotation = new float[3];' do klasy EntityData
        // data.rotation[0] = transform.eulerAngles.x;
        // data.rotation[1] = transform.eulerAngles.y;
        // data.rotation[2] = transform.eulerAngles.z;

        // Zapis pozycji w siatce (tylko dla budynków)
        GridObject gridObj = GetComponent<GridObject>();
        if (gridObj != null)
        {
            data.gridPosition[0] = gridObj.occupiedPosition.x;
            data.gridPosition[1] = gridObj.occupiedPosition.y;
        }

        // Zapis specyficznych danych komponentu (np. receptury, limity)
        data.jsonComponentData = SaveComponentData();

        return data;
    }

    public void Load(EntityData data)
    {
        transform.position = new Vector3(data.worldPosition[0], data.worldPosition[1], data.worldPosition[2]);

        GridObject gridObj = GetComponent<GridObject>();
        if (gridObj != null)
        {
            gridObj.occupiedPosition = new Vector2Int(data.gridPosition[0], data.gridPosition[1]);
        }

        LoadComponentData(data.jsonComponentData);
    }


    public virtual string SaveComponentData()
    {
        return "{}";
    }

    public virtual void LoadComponentData(string json)
    {

    }

    public virtual string GetSerializedData() { return ""; }

}
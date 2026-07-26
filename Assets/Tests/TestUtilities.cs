using System;
using Object = UnityEngine.Object;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public abstract class FactoryGameTestBase
{
    protected List<GameObject> createdGameObjects;

    [SetUp]
    public virtual void SetUp()
    {
        createdGameObjects = new List<GameObject>();
        ResetStaticState();
    }

    [TearDown]
    public virtual void TearDown()
    {
        for (int i = createdGameObjects.Count - 1; i >= 0; i--)
        {
            if (createdGameObjects[i] != null)
            {
                Object.DestroyImmediate(createdGameObjects[i]);
            }
        }

        createdGameObjects.Clear();
        ResetStaticState();
    }

    protected T CreateTestObject<T>(string name = null) where T : Component
    {
        GameObject go = new GameObject(name ?? typeof(T).Name);
        createdGameObjects.Add(go);
        return go.AddComponent<T>();
    }

    protected GameObject CreateGameObject(string name = "TestObject")
    {
        GameObject go = new GameObject(name);
        createdGameObjects.Add(go);
        return go;
    }

    protected T GetPrivateStaticFieldValue<T>(Type type, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found on type {type.FullName}");
        }

        object value = field.GetValue(null);
        if (value == null)
        {
            return default;
        }

        return (T)value;
    }

    protected T GetPrivateInstanceFieldValue<T>(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found on type {instance.GetType().FullName}");
        }

        object value = field.GetValue(instance);
        if (value == null)
        {
            return default;
        }

        return (T)value;
    }

    protected void SetPrivateInstanceFieldValue(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found on type {instance.GetType().FullName}");
        }

        field.SetValue(instance, value);
    }

    protected void InvokePrivateInstanceMethod(object instance, string methodName, params object[] args)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found on type {instance.GetType().FullName}");
        }

        method.Invoke(instance, args);
    }

    private void ResetStaticState()
    {
        SetStaticPropertyToNull(typeof(GridManager), "Instance");
        SetStaticPropertyToNull(typeof(SaveManager), "Instance");
        SetStaticPropertyToNull(typeof(PlacementManager), "Instance");
        ResetTransportTickManagerStatics();
    }

    private void SetStaticPropertyToNull(Type type, string propertyName)
    {
        FieldInfo field = type.GetField($"<{propertyName}>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(null, null);
            return;
        }

        field = type.GetField(propertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(null, null);
        }
    }

    private void ResetTransportTickManagerStatics()
    {
        Type type = typeof(TransportTickManager);
        SetFieldToNull(type, "instance");
        ClearCollectionField(type, "conveyorRegistry");
        ClearCollectionField(type, "readyConveyors");
        ClearCollectionField(type, "scheduledConveyors");
        ClearCollectionField(type, "delayedConveyorBuckets");
        ClearCollectionField(type, "overheadConveyors");
        ClearCollectionField(type, "movingItems");
        ClearCollectionField(type, "overheadConveyorSet");
        ClearCollectionField(type, "movingItemSet");

        TransportTickManager[] managers = Object.FindObjectsOfType<TransportTickManager>();
        foreach (var manager in managers)
        {
            if (manager != null && manager.gameObject != null)
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }
    }

    private void SetFieldToNull(Type type, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(null, null);
        }
    }

    private void ClearCollectionField(Type type, string fieldName)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            return;
        }

        object value = field.GetValue(null);
        if (value == null)
        {
            return;
        }

        MethodInfo clearMethod = value.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
        clearMethod?.Invoke(value, null);
    }
}

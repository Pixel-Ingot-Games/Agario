using UnityEngine;
using System.Collections.Generic;

public class FoodObjectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private int poolSize = 300;
    [SerializeField] private bool expandPool = true;
    [SerializeField] private int maxPoolSize = 500;
    
    private Queue<GameObject> availableFood = new Queue<GameObject>();
    private List<GameObject> activeFood = new List<GameObject>();
    
    void Awake()
    {
        InitializePool();
    }
    
    void InitializePool()
    {
        if (foodPrefab == null)
        {
            Debug.LogError("Food prefab is not assigned to the pool!");
            return;
        }
        
        // Pre-instantiate all food objects
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewFoodObject();
        }
    }
    
    void CreateNewFoodObject()
    {
        GameObject food = Instantiate(foodPrefab, Vector3.zero, Quaternion.identity, transform);
        food.SetActive(false);
        availableFood.Enqueue(food);
    }
    
    public GameObject GetFood()
    {
        GameObject food;
        
        if (availableFood.Count > 0)
        {
            // Get food from available pool
            food = availableFood.Dequeue();
        }
        else if (expandPool && activeFood.Count < maxPoolSize)
        {
            // Create new food if pool can expand
            CreateNewFoodObject();
            food = availableFood.Dequeue();
        }
        else
        {
            // No food available and can't expand
            return null;
        }
        
        // Add to active list
        activeFood.Add(food);
        return food;
    }
    
    public void ReturnFood(GameObject food)
    {
        if (food == null) return;
        
        // Remove from active list
        if (activeFood.Contains(food))
        {
            activeFood.Remove(food);
        }
        
        // Reset food properties
        food.SetActive(false);
        food.transform.position = Vector3.zero;
        food.transform.localScale = Vector3.one;
        
        // Reset the eaten bool to false for respawning
        Food foodScript = food.GetComponent<Food>();
        if (foodScript != null)
        {
            foodScript.eaten = false;
        }
        
        // Return to available pool
        availableFood.Enqueue(food);
    }
    
    public void ReturnAllFood()
    {
        // Return all active food to the pool
        for (int i = activeFood.Count - 1; i >= 0; i--)
        {
            if (activeFood[i] != null)
            {
                ReturnFood(activeFood[i]);
            }
        }
    }
    
    public int GetAvailableFoodCount()
    {
        return availableFood.Count;
    }
    
    public int GetActiveFoodCount()
    {
        return activeFood.Count;
    }
    
    public int GetTotalPoolSize()
    {
        return availableFood.Count + activeFood.Count;
    }
    
    // Clean up destroyed food from active list
    void Update()
    {
        activeFood.RemoveAll(food => food == null);
    }
}

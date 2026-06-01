using System.Collections.Generic;
using UnityEngine;

public class PlayerGarage : MonoBehaviour
{
    public static PlayerGarage Instance;

    [Header("Owned Cars")]
    public List<CarProfile> ownedCars = new List<CarProfile>();

    [Header("Current Car")]
    public CarProfile currentCar;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public bool CurrentCarMatches(VehicleType requiredType)
    {
        if (requiredType == VehicleType.Any)
            return currentCar != null;

        if (currentCar == null)
            return false;

        return currentCar.vehicleType == requiredType;
    }

    public CarProfile GetFirstCompatibleOwnedCar(VehicleType requiredType)
    {
        if (requiredType == VehicleType.Any)
            return currentCar;

        foreach (CarProfile car in ownedCars)
        {
            if (car != null && car.vehicleType == requiredType)
                return car;
        }

        return null;
    }

    public bool HasCompatibleOwnedCar(VehicleType requiredType)
    {
        return GetFirstCompatibleOwnedCar(requiredType) != null;
    }

    public void SwitchToCar(CarProfile newCar)
    {
        if (newCar == null)
            return;

        currentCar = newCar;
        Debug.Log("Switched to car: " + newCar.displayName);

        // Later:
        // despawn current player car
        // spawn chosen car
        // transfer player state
    }
}
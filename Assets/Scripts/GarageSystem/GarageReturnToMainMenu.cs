using UnityEngine;
using UnityEngine.SceneManagement;

public class GarageReturnToMainMenu : MonoBehaviour
{
    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Input")]
    public KeyCode keyboardBackKey = KeyCode.Escape;
    public KeyCode controllerBackKey = KeyCode.JoystickButton1;

    private void Update()
    {
        if (Input.GetKeyDown(keyboardBackKey) || Input.GetKeyDown(controllerBackKey))
        {
            ReturnToMainMenuGarageItem();
        }
    }

    public void ReturnToMainMenuGarageItem()
    {
        MainMenuReturnState.RequestItem(MainMenuRequestedItem.Garage);

        if (SceneLoaderWithLoadingScreen.Instance != null)
        {
            SceneLoaderWithLoadingScreen.Instance.LoadScene(mainMenuSceneName, "Back to Main Menu...");
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
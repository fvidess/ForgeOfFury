using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics.Contracts;
using TMPro;
using Fusion;

//using UnityEditor.Overlays;

public class PauseButton : MonoBehaviour
{
    public Canvas pauseMenu;
    public Canvas helpMenu;
    public Button pauseButton;
    public GameObject gameMaster;
    public Canvas drawReqScreen;
    public Canvas drawAccepted;
    public Canvas drawDenied;
    public GameCore game;
    public bool mouseOver = false;

    public delegate void RestartNetworkingGame();
    public static event RestartNetworkingGame OnNetworkingGameRestart;
    // Start is called before the first frame update
    void Start()
    {
        pauseButton.gameObject.SetActive(true);
        drawDenied.enabled = false;
        drawReqScreen.enabled = false;
        drawAccepted.enabled = false;
        pauseMenu.enabled = false;
        helpMenu.enabled = false;
        game = GameObject.FindObjectOfType<GameCore>();
    }

    public void openMenu()
    {
        pauseMenu.enabled = true;
        pauseButton.gameObject.SetActive(false);
        //Time.timeScale = 0;
        gameMaster.GetComponent<GameCore>().gamePaused = true;
        GameObject.Find("GameMaster").GetComponent<GameCore>().buttonsCanvas.enabled = false;
    }

    public void closeMenu()
    {
        if (pauseMenu.enabled)
        {
            pauseMenu.enabled = false;
            pauseButton.gameObject.SetActive(true);
            Time.timeScale = 1;
            gameMaster.GetComponent<GameCore>().gamePaused = false;
            GameObject.Find("GameMaster").GetComponent<GameCore>().buttonsCanvas.enabled = true;
        }
    }

    public async void returnToMain()
    {
        GameObject networkingManger = GameObject.Find("NetworkManager");

        // Disconnect from photon if the game is being played online
        if (networkingManger != null && networkingManger.GetComponent<NetworkingManager>()._runner)
        {
            await networkingManger.GetComponent<NetworkingManager>().DisconnectFromPhoton();
        }

        Time.timeScale = 1;
        SceneManager.LoadScene(1);
    }

    public void openHelpMenu()
    {
        pauseMenu.enabled = false;
        helpMenu.enabled = true;
    }

    public void closeHelpMenu()
    {
        helpMenu.enabled = false;
        pauseMenu.enabled = true;
    }

    public void restartGame()
    {
        MenuController menuController = gameObject.GetComponent<MenuController>();
        Time.timeScale = 1;
        switch (gameMaster.GetComponent<GameCore>().currentGameMode)
        {
            case GameType.AIEasy:
                menuController.NewEasyGame();
                break;
            case GameType.AIHard:
                break;
            case GameType.Local:
                menuController.LocalGame();
                break;
            case GameType.Online:
                OnNetworkingGameRestart.Invoke();
                break;
        }
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    public void requestDraw(bool bypass = false)
    {
        if (gameMaster.GetComponent<GameCore>().currentGameMode == GameType.Online && !bypass)
        {
            NetworkedPlayer localPlayer = GetNetworkedLocalPlayer();
            localPlayer.RpcOfferDraw(localPlayer.PlayerRef);
            return;
        }

        GameObject header = drawReqScreen.transform.Find("Background/Header/Congrats").gameObject;

        int playerNumber = gameMaster.GetComponent<GameCore>().currentPlayer.piece == 'X' ? 1 : 2;
        if (Data.CURRENT_LANGUAGE == "English")
        {
            header.GetComponent<TMP_Text>().text = "Player " + playerNumber + " is requesting a draw";
        }
        else if (Data.CURRENT_LANGUAGE == "Espa�ol")
        {
            header.GetComponent<TMP_Text>().text = "Jugador " + playerNumber + " solicita un empate";
        }
        drawReqScreen.enabled = true;
        gameMaster.GetComponent<GameCore>().gamePaused = true;
        pauseButton.gameObject.SetActive(false);
        GameObject.Find("GameMaster").GetComponent<GameCore>().buttonsCanvas.enabled = false;
        //GameObject.Find("Game Master").GetComponent<GameCore>().buttonHandler.gameObject.SetActive(false);
        //GameObject.Find("Game Master").GetComponent<GameCore>().drawButton.gameObject.SetActive(false);
    }

    public void acceptDraw(bool bypass = false)
    {
        if (gameMaster.GetComponent<GameCore>().currentGameMode == GameType.Online && !bypass)
        {
            NetworkedPlayer localPlayer = GetNetworkedLocalPlayer();
            localPlayer.RpcAcceptDraw();
            return;
        }

        GameCore game = gameMaster.GetComponent<GameCore>();
        game.gameOver = true;
        game.drawButton.gameObject.SetActive(false);
        game.buttonsCanvas.enabled = false;
        pauseButton.gameObject.SetActive(false);

        drawReqScreen.enabled = false;
        drawAccepted.enabled = true;
    }

    public void denyDraw(bool bypass = false)
    {
        if (gameMaster.GetComponent<GameCore>().currentGameMode == GameType.Online && !bypass)
        {
            NetworkedPlayer localPlayer = GetNetworkedLocalPlayer();
            localPlayer.RpcDenyDraw();
            return;
        }

        drawReqScreen.enabled = false;
        drawDenied.enabled = true;
    }

    public void closeDrawMenu()
    {
        drawDenied.enabled = false;

        GameCore game = GameObject.Find("GameMaster").GetComponent<GameCore>();

        game.gamePaused = false;
        pauseButton.gameObject.SetActive(true);
        game.buttonsCanvas.enabled = true;
        game.drawButton.gameObject.SetActive(true);
    }

    private NetworkedPlayer GetNetworkedLocalPlayer()
    {
        NetworkingManager networkingManager = GameObject.Find("NetworkManager").GetComponent<NetworkingManager>();
        return networkingManager.GetNetworkedPlayer(networkingManager._runner.LocalPlayer);
    }

    public void HideAllDrawMenus()
    {
        drawReqScreen.enabled = false;
        drawAccepted.enabled = false;
        drawDenied.enabled = false;
    }
}

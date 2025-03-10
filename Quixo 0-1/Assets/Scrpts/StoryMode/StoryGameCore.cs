using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Fusion;
using static UnityEngine.Rendering.DebugUI.Table;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine.UI;

public class StoryGameCore : MonoBehaviour
{
    public GameObject piecePrefab;
    public WinType winType;
    public Material playerOneSpace;
    public Material playerTwoSpace;
    public StoryButtonHandler buttonHandler;
    public GameObject AI;
    private StoryPieceLogic storyPieceLogic;
    public StoryPieceLogic chosenPiece;
    public GameObject[,] gameBoard = new GameObject[5, 6];
    private Renderer rd;
    public IPlayer currentPlayer;
    public IPlayer p1;
    public IPlayer p2;
    public int SMLvl = 1;
    public bool gamePaused;
    public bool gameOver = false;
    public bool aiMoving = false;
    public List<(int, int)> winnerPieces = new List<(int, int)>();

    [SerializeField] public AudioClip pieceClickSound;

    [SerializeField] private AudioClip hotPieceMoveSound;
    [SerializeField] private AudioClip coldPieceMoveSound;
    [SerializeField] private AudioClip victory;
    [SerializeField] private AudioClip defeat;
    [SerializeField] private AudioClip growl;
    [SerializeField] private AudioClip swordWin;
    [SerializeField] private AudioClip spearWin;
    [SerializeField] private AudioClip axeWin;
    [SerializeField] private AudioClip hammerHit;

    public Canvas loseScreen;
    public Canvas winScreen;
    public Canvas SMLvl2;
    public Canvas SMLvl3;
    public Canvas SMLvl4;
    private EasyAI easyAI;
    private HardAI hardAI;
    private bool playAI = false;

    public bool playHard = false; 

    public Canvas IntroSMLvl1;
    public Canvas IntroSMLvl2;
    public Canvas IntroSMLvl3;
    public Canvas IntroSMLvl4;
    public Camera CameraPosition;
    public Canvas buttonCanvas;

    public GameObject swordPrefab;
    public GameObject axePrefab;
    public GameObject spearPrefab;
    public GameObject helmetPrefab;

    //Event for sending chosen piece to the NetworkingManager
    public delegate void ChosenPieceEvent(int row, int col);
    public static event ChosenPieceEvent OnChosenPiece;

    void Start()
    {
        GameObject curPlayerVisual;
        CameraPosition = Camera.main;
        SMLvl2.enabled = false;
        SMLvl3.enabled = false;
        SMLvl4.enabled = false;
        winScreen.enabled = false;
        loseScreen.enabled = false;

        IntroSMLvl1.enabled = false;
        IntroSMLvl2.enabled = false;
        IntroSMLvl3.enabled = false;
        IntroSMLvl4.enabled = false;

        buttonCanvas.enabled = false;

        gamePaused = true;
    }

    public void StartStoryGame(bool hardMode)
    {
        playAI = true;
        playHard = hardMode;

        GameObject player1Object = new GameObject("Player1");
        p1 = player1Object.AddComponent<LocalPlayer>();
        p1.Initialize('X');

        GameObject player2Object = new GameObject("Player2");
        p2 = player2Object.AddComponent<LocalPlayer>();
        p2.Initialize('O');

        currentPlayer = p1; //F: make X the first player/move
        buttonHandler = GameObject.FindObjectOfType<StoryButtonHandler>();
        easyAI = AI.AddComponent(typeof(EasyAI)) as EasyAI;
        hardAI = AI.AddComponent(typeof(HardAI)) as HardAI;
        populateBoard(); //Initialize board
    }

    IEnumerator RotateCamera()
    {
        float timeelapsed = 0;

        Quaternion currentRotation = CameraPosition.transform.rotation;

        // Define the target rotation
        Quaternion targetRotation = Quaternion.Euler(-25f, 270f, 0f);

        // One second delay before rotation starts
        yield return new WaitForSeconds(2.5f);

        SoundFXManage.Instance.PlaySoundFXClip(growl, transform, 1f);

        yield return new WaitForSeconds(1.0f);

        while (timeelapsed < 1)
        {
            // Smoothly rotate the camera towards the target rotation
            CameraPosition.transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, timeelapsed / 1);
            timeelapsed += Time.deltaTime;
            yield return null;
        }

        CameraPosition.transform.rotation = targetRotation;

        SoundFXManage.Instance.PlaySoundFXClip(defeat, transform, 1f);
        // One second delay after rotation ends
        yield return new WaitForSeconds(2.75f);
        loseScreen.enabled = true;
    }

    public void openDialogMenu()
    {
        switch (SMLvl)
        {
            case 1:
                IntroSMLvl1.enabled = true;
                IntroSMLvl2.enabled = false;
                IntroSMLvl3.enabled = false;
                IntroSMLvl4.enabled = false;
                Debug.Log("Made it Here 1");
                break;
            case 2:
                IntroSMLvl1.enabled = false;
                IntroSMLvl2.enabled = true;
                IntroSMLvl3.enabled = false;
                IntroSMLvl4.enabled = false;
                Debug.Log("Made it Here 2");
                break;
            case 3:
                IntroSMLvl1.enabled = false;
                IntroSMLvl2.enabled = false;
                IntroSMLvl3.enabled = true;
                IntroSMLvl4.enabled = false;
                break;
            case 4:
                IntroSMLvl1.enabled = false;
                IntroSMLvl2.enabled = false;
                IntroSMLvl3.enabled = false;
                IntroSMLvl4.enabled = true;
                break;
                //Time.timeScale = 0;
                //gamePaused = true;
        }
    }

    private System.Collections.IEnumerator winAnimation()
    {
        yield return new WaitForSeconds(2f);
        List<int> verPos = new List<int> { -2866, -2876, -2856, -2846, -2836 };
        List<int> horPos = new List<int> { -10, -20, 0, 10, 20 };
        List<(int, int)> leftDiagPos = new List<(int, int)> { (-2866, -10), (-2876, -20), (-2856, 0), (-2846, 10), (-2836, 20) };
        List<(int, int)> rightDiagPos = new List<(int, int)> { (-2866, 10), (-2876, 20), (-2856, 0), (-2846, -10), (-2836, -20) };
        List<(int, int)> helmetPos = new List<(int, int)> { (-2866, 0), (-2856, -10), (-2846, -10),  (-2856, 10), (-2846, 10) };
        List<StoryPieceLogic> listOfPieces = new List<StoryPieceLogic>();

        for (int i = 0; i < 5; i++)
        {
            StoryPieceLogic curPiece = gameBoard[winnerPieces[i].Item1, winnerPieces[i].Item2].GetComponent<StoryPieceLogic>();
            listOfPieces.Add(curPiece);
            if (winType == WinType.vertical)
            {
                SoundFXManage.Instance.PlaySoundFXClip(hotPieceMoveSound, transform, 1f);
                yield return StartCoroutine(MovePieceSmoothly(curPiece, new Vector3(verPos[i], 140, 0)));
            }
            else if (winType == WinType.horizontal)
            {
                SoundFXManage.Instance.PlaySoundFXClip(hotPieceMoveSound, transform, 1f);
                yield return StartCoroutine(MovePieceSmoothly(curPiece, new Vector3(-2856, 140, horPos[i])));
            }
            else if(winType== WinType.helmet)
            {
                SoundFXManage.Instance.PlaySoundFXClip(hotPieceMoveSound, transform, 1f);
                yield return StartCoroutine(MovePieceSmoothly(curPiece, new Vector3(helmetPos[i].Item1, 140, helmetPos[i].Item2)));
            }
            else
            {
                if (winnerPieces.Contains((0, 0))) //means it is left diagonal
                {
                    SoundFXManage.Instance.PlaySoundFXClip(hotPieceMoveSound, transform, 1f);
                    yield return StartCoroutine(MovePieceSmoothly(curPiece, new Vector3(leftDiagPos[i].Item1, 140, leftDiagPos[i].Item2)));
                }
                else //right diagonal
                {
                    SoundFXManage.Instance.PlaySoundFXClip(hotPieceMoveSound, transform, 1f);
                    yield return StartCoroutine(MovePieceSmoothly(curPiece, new Vector3(rightDiagPos[i].Item1, 140, rightDiagPos[i].Item2)));
                }
            }
            SoundFXManage.Instance.PlaySoundFXClip(hammerHit, transform, 1f);
        }
        foreach (StoryPieceLogic piece in listOfPieces)
        {
            piece.gameObject.SetActive(false);
        }
        if (winType == WinType.vertical)
        {
            SoundFXManage.Instance.PlaySoundFXClip(swordWin, transform, 1f);
            GameObject sword = Instantiate(swordPrefab, new Vector3(-2815, 135, 0), Quaternion.identity);
            Vector3 scale = sword.transform.localScale;
            scale.y = 100f;
            scale.x = 100f;
            scale.z = 100f;
            sword.transform.localScale = scale;
            sword.transform.Rotate(90.0f, 0f, 90.0f, Space.Self);
        }
        if (winType == WinType.Leftdiagonal)
        {
            SoundFXManage.Instance.PlaySoundFXClip(axeWin, transform, 1f);
            GameObject axe = Instantiate(axePrefab, new Vector3(-2847, 140, 5), Quaternion.identity);
            Vector3 scale = axe.transform.localScale;
            scale.y = 90;
            scale.x = 90;
            scale.z = 90;
            axe.transform.localScale = scale;
            axe.transform.Rotate(90.0f, 0, 135.0f, Space.Self);
        }
        if (winType == WinType.horizontal)
        {
            SoundFXManage.Instance.PlaySoundFXClip(spearWin, transform, 1f);
            GameObject spear = Instantiate(spearPrefab, new Vector3(-2851, 140, 18), Quaternion.identity);
            Vector3 scale = spear.transform.localScale;
            scale.y = 100f;
            scale.x = 100f;
            scale.z = 100f;
            spear.transform.localScale = scale;
            spear.transform.Rotate(90f, 0, 0, Space.Self);
        }
        if (winType == WinType.Rightdiagonal)
        {
            SoundFXManage.Instance.PlaySoundFXClip(axeWin, transform, 1f);
            GameObject axe = Instantiate(axePrefab, new Vector3(-2844, 140, -2), Quaternion.identity);
            Vector3 scale = axe.transform.localScale;
            scale.y = 90;
            scale.x = 90;
            scale.z = 90;
            axe.transform.localScale = scale;
            axe.transform.Rotate(-90.0f, 0, 135.0f, Space.Self);
        }
        if (winType == WinType.helmet)
        {
            SoundFXManage.Instance.PlaySoundFXClip(axeWin, transform, 1f);
            GameObject helmet = Instantiate(helmetPrefab, new Vector3(-2851, 140, -0), Quaternion.identity);
            Vector3 scale = helmet.transform.localScale;
            scale.y = 100;
            scale.x = 100;
            scale.z = 100;
            helmet.transform.localScale = scale;
            helmet.transform.Rotate(-45f, 90, 0f, Space.Self);
        }
        gameOver = true;
        
    }

    private void highlightPieces()
    {
        for (int i = 0; i < 5; i++)
        {
            gameBoard[winnerPieces[i].Item1, winnerPieces[i].Item2].AddComponent<Outline>();
            gameBoard[winnerPieces[i].Item1, winnerPieces[i].Item2].GetComponent<Outline>().OutlineWidth = 10;
        }
    }

    private bool horizontalWin()
    {
        Debug.Log("checking for horizontal win");
        bool success;
        bool removed = false;
        char baseSymbol = '-';
        char pieceToCheck = '-';
        int winNum = 0;

        for (int row = 0; row < 5; row++)
        {
            success = true;
            baseSymbol = gameBoard[row, 0].GetComponent<StoryPieceLogic>().player; //F: first value of every row is base
            for (int col = 0; col < 5; col++)
            {
                pieceToCheck = gameBoard[row, col].GetComponent<StoryPieceLogic>().player; //F: assigned to a variable instead of callind GetComponent twice in the if
                winnerPieces.Add((row, col));
                if (pieceToCheck != baseSymbol || pieceToCheck == '-') //F: compare every item to the baseSymbol, ignore immediately if it is blank
                {
                    winnerPieces.RemoveRange(winnerPieces.Count - col - 1, col + 1);
                    success = false; //F: if changed, not same symbols
                    break; //F: get out if not same symbol or blank, and try the next
                }
            }
            if (success) //F: If unchanged, we have a win
            {
                winNum++;
                if (p1.piece == baseSymbol)
                {
                    p1.won = true;
                    currentPlayer = p1;
                }
                else
                {
                    p2.won = true;
                    currentPlayer = p2;
                }
            }
        }

        if (winNum == 1)
        {
            return true;
        }
        else if (winNum == 2)
        {
            for (int i = 0; winnerPieces.Count != 5; i++)
            {
                if (removed) { i--; removed = false; }
                if (gameBoard[winnerPieces[i].Item1, winnerPieces[i].Item2].GetComponent<PieceLogic>().player != currentPlayer.piece)
                {
                    winnerPieces.Remove((winnerPieces[i].Item1, winnerPieces[i].Item2));
                    removed = true;
                }
            }
            return true;
        }
        winnerPieces.Clear();
        return false;
    }

    private bool verticalWin()
    {
        Debug.Log("checking for vertical win");
        int winNum = 0;
        bool success;
        bool removed = false;
        char baseSymbol = '-';
        char pieceToCheck = '-';
        for (int col = 0; col < 5; col++)
        {
            success = true;
            baseSymbol = gameBoard[0, col].GetComponent<StoryPieceLogic>().player; ;
            for (int row = 0; row < 5; row++)
            {
                pieceToCheck = gameBoard[row, col].GetComponent<StoryPieceLogic>().player;
                winnerPieces.Add((row, col));
                if (pieceToCheck != baseSymbol || pieceToCheck == '-')
                {
                    winnerPieces.RemoveRange(winnerPieces.Count - row - 1, row + 1);
                    success = false;
                    break;
                }
            }

            if (success)
            {
                winNum++;
                if (p1.piece == baseSymbol)
                {
                    p1.won = true;
                    currentPlayer = p1;
                }
                else
                {
                    p2.won = true;
                    currentPlayer = p2;
                }
            }
        }
        if (winNum == 1)
        {
            return true;
        }
        else if (winNum == 2)
        {
            for (int i = 0; winnerPieces.Count != 5; i++)
            {
                if (removed) { i--; removed = false; }
                if (gameBoard[winnerPieces[i].Item1, winnerPieces[i].Item2].GetComponent<PieceLogic>().player != currentPlayer.piece)
                {
                    winnerPieces.Remove((winnerPieces[i].Item1, winnerPieces[i].Item2));
                    removed = true;
                }
            }
            return true;
        }
        winnerPieces.Clear();
        return false;
    }

    private bool leftDiagonalWin()
    {
        Debug.Log("check leftdiagonal win");
        char baseSymbol = '-';
        char pieceToCheck = '-';
        bool success = true;
        //check for top left to bottom right win
        baseSymbol = gameBoard[0, 0].GetComponent<StoryPieceLogic>().player;
        winnerPieces.Add((0, 0));
        for (int i = 1; i < 5; i++)
        {
            pieceToCheck = gameBoard[i, i].GetComponent<StoryPieceLogic>().player;
            winnerPieces.Add((i, i));
            if (pieceToCheck != baseSymbol || pieceToCheck == '-')
            {
                success = false;
                break;
            }
        }
        if (success)
        {
            if (p1.piece == baseSymbol)
            {
                p1.won = true;
                currentPlayer = p1;
            }
            else
            {
                p2.won = true;
                currentPlayer = p2;
            }
            return true;
        }
        winnerPieces.Clear();

        return false;
    }

    private bool rightDiagonalWin()
    {
        //check for bottom left to top right 
        char pieceToCheck = '-';
        char baseSymbol = gameBoard[0, 4].GetComponent<StoryPieceLogic>().player;
        bool success = true;
        for (int i = 0; i < 5; i++)
        {
            pieceToCheck = gameBoard[i, 4 - i].GetComponent<StoryPieceLogic>().player;
            winnerPieces.Add((i, 4 - i));
            if (pieceToCheck != baseSymbol || pieceToCheck == '-')
            {
                success = false;
                break;
            }
        }

        if (success)
        {
            if (p1.piece == baseSymbol)
            {
                p1.won = true;
                currentPlayer = p1;
            }
            else
            {
                p2.won = true;
                currentPlayer = p2;
            }
            return true;
        }
        winnerPieces.Clear();
        return false;
    }

    private bool helmetWin()
    {
        Debug.Log("check helmet win");

        //check for top left to bottom right win
        char helmetPart1 = gameBoard[3, 1].GetComponent<StoryPieceLogic>().player;
        winnerPieces.Add((3, 1));
        char helmetPart2 = gameBoard[2, 1].GetComponent<StoryPieceLogic>().player;
        winnerPieces.Add((2, 1));
        char helmetPart3 = gameBoard[1, 2].GetComponent<StoryPieceLogic>().player;
        winnerPieces.Add((1, 2));
        char helmetPart4 = gameBoard[2, 3].GetComponent<StoryPieceLogic>().player;
        winnerPieces.Add((2, 3));
        char helmetPart5 = gameBoard[3, 3].GetComponent<StoryPieceLogic>().player;
        winnerPieces.Add((3, 3));

        if ((helmetPart1 == helmetPart2 && helmetPart2 == helmetPart3 && helmetPart3 == helmetPart4 && helmetPart4 == helmetPart5) && helmetPart5!= '-')
        {
            if(helmetPart1 == 'X')
            {
                p1.won = true;
                currentPlayer = p1;
            }
            else
            {
                p2.won = true;
                currentPlayer = p2;
            }
            return true;
        }
        winnerPieces.Clear();
        return false;
    }

    private void chooseCanvasAndWinner(ref Canvas canvasToShow)
    {
        //AI game, either SM or normal
        if (currentPlayer == p1)
        {
            canvasToShow.enabled = true;
        }
    }

    IEnumerator DelayedCanvasSelection(Canvas canvasType)
    {
        buttonCanvas.enabled = false;
        GameObject.Find("Menu Manager").GetComponent<StoryPauseButton>().pauseButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(7f); // 1 second delay
        chooseCanvasAndWinner(ref canvasType);
        SoundFXManage.Instance.PlaySoundFXClip(victory, transform, 1f);
    }

    public bool won()
    {
        switch (SMLvl)
        {
            case 1:
                if (verticalWin())
                {
                    winType = WinType.vertical;
                    StartCoroutine(DelayedCanvasSelection(SMLvl2)); return true;
                }
                break;
            case 2:
                if (horizontalWin())
                {
                    winType = WinType.horizontal;
                    StartCoroutine(DelayedCanvasSelection(SMLvl3)); return true;
                }
                break;
            case 3:
                if (leftDiagonalWin())
                {
                    winType = WinType.Leftdiagonal;
                    StartCoroutine(DelayedCanvasSelection(SMLvl4)); return true;
                }
                else if (rightDiagonalWin())
                {
                    winType = WinType.Rightdiagonal;
                    StartCoroutine(DelayedCanvasSelection(SMLvl4)); return true;
                }
                break;
            case 4:
                if (helmetWin())
                {
                    winType = WinType.helmet;
                    StartCoroutine(DelayedCanvasSelection(winScreen)); return true;
                }
                break;
            default: return false;
        }

        return false;
    }

    public void currentPlayerSFX()
    {
        if (currentPlayer == p1)
        {
            SoundFXManage.Instance.PlaySoundFXClip(hotPieceMoveSound, transform, 1f);
        }
        else
        {
            SoundFXManage.Instance.PlaySoundFXClip(coldPieceMoveSound, transform, 1f);
        }
    }

    public void shiftBoard(char dir, char currentPiece)
    {
        Debug.Log(dir);
        gameBoard[0, 5] = gameBoard[chosenPiece.row, chosenPiece.col]; // Store the selected piece temporarily
        Material pieceColor;
        gamePaused = true;
        switch (currentPiece)
        {
            case 'X':
                pieceColor = playerOneSpace;
                break;
            default:
                pieceColor = playerTwoSpace;
                break;
        }

        if (dir == 'U')
        {
            for (int i = chosenPiece.row; i > 0; i--)
            {
                StoryPieceLogic currentPieceObject = gameBoard[i - 1, chosenPiece.col].GetComponent<StoryPieceLogic>();
                currentPieceObject.GetComponent<StoryPieceLogic>().row = i;
                Vector3 newPosition = currentPieceObject.transform.position + new Vector3(20, 0, 0);
                StartCoroutine(MovePieceSmoothly(currentPieceObject, newPosition));
                currentPlayerSFX();
                gameBoard[i, chosenPiece.col] = gameBoard[i - 1, chosenPiece.col];
            }
            StartCoroutine(moveChosenPiece(0, chosenPiece.col, pieceColor, currentPiece, (-40 + -2856), 100f, gameBoard[1, chosenPiece.col].transform.position.z));
        }
        else if (dir == 'D')
        {
            for (int i = chosenPiece.row; i < 4; i++)
            {
                StoryPieceLogic currentPieceObject = gameBoard[i + 1, chosenPiece.col].GetComponent<StoryPieceLogic>();
                currentPieceObject.GetComponent<StoryPieceLogic>().row = i;
                Vector3 newPosition = currentPieceObject.transform.position - new Vector3(20, 0, 0);
                StartCoroutine(MovePieceSmoothly(currentPieceObject, newPosition));
                currentPlayerSFX();
                gameBoard[i, chosenPiece.col] = gameBoard[i + 1, chosenPiece.col];
            }
            StartCoroutine(moveChosenPiece(4, chosenPiece.col, pieceColor, currentPiece, (40 + -2856), 100f, gameBoard[1, chosenPiece.col].transform.position.z));
        }
        else if (dir == 'R')
        {
            for (int i = chosenPiece.col; i < 4; i++)
            {
                StoryPieceLogic currentPieceObject = gameBoard[chosenPiece.row, i + 1].GetComponent<StoryPieceLogic>();
                currentPieceObject.GetComponent<StoryPieceLogic>().col = i;
                Vector3 newPosition = currentPieceObject.transform.position - new Vector3(0, 0, 20);
                StartCoroutine(MovePieceSmoothly(currentPieceObject, newPosition));
                currentPlayerSFX();
                gameBoard[chosenPiece.row, i] = gameBoard[chosenPiece.row, i + 1];
            }
            StartCoroutine(moveChosenPiece(chosenPiece.row, 4, pieceColor, currentPiece, gameBoard[chosenPiece.row, 1].transform.position.x, 100f, 40));
        }
        else if (dir == 'L')
        {
            for (int i = chosenPiece.col; i > 0; i--)
            {
                StoryPieceLogic currentPieceObject = gameBoard[chosenPiece.row, i - 1].GetComponent<StoryPieceLogic>();
                currentPieceObject.GetComponent<StoryPieceLogic>().col = i;
                Vector3 newPosition = currentPieceObject.transform.position + new Vector3(0, 0, 20);
                StartCoroutine(MovePieceSmoothly(currentPieceObject, newPosition));
                currentPlayerSFX();
                gameBoard[chosenPiece.row, i] = gameBoard[chosenPiece.row, i - 1];
            }
            StartCoroutine(moveChosenPiece(chosenPiece.row, 0, pieceColor, currentPiece, gameBoard[chosenPiece.row, 1].transform.position.x, 100f, -40));
        }
    }

    public System.Collections.IEnumerator MovePieceSmoothly(StoryPieceLogic piece, Vector3 targetPosition)
    {
        float duration = 0.5f; // Adjust as needed
        Vector3 startPosition = piece.transform.position;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            piece.transform.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        piece.transform.position = targetPosition; // Ensure it reaches the target position precisely
    }

    private System.Collections.IEnumerator moveChosenPiece(int row, int col, Material pieceColor, char currentPiece, float x, float y, float z)
    {
        gameBoard[row, col] = gameBoard[0, 5]; //F: set the selected piece to its new position in the array
        gameBoard[row, col].GetComponent<StoryPieceLogic>().player = currentPiece; //F: changing the moved piece's symbol to the current
        gameBoard[row, col].GetComponent<Renderer>().material = pieceColor; //F: changing the moved piece's material (color) 
        Vector3 target = new Vector3(x, y + 15, z);
        yield return StartCoroutine(MovePieceSmoothly(gameBoard[row, col].GetComponent<StoryPieceLogic>(), target));
        gameBoard[row, col].GetComponent<StoryPieceLogic>().row = row; //F: changing the moved piece's row
        gameBoard[row, col].GetComponent<StoryPieceLogic>().col = col; //F: changing the moved piece's col
        yield return StartCoroutine(MovePieceSmoothly(gameBoard[row, col].GetComponent<StoryPieceLogic>(), new Vector3(target.x, 96f, target.z)));
        if (GameObject.Find("Menu Manager").GetComponent<StoryPauseButton>().pauseMenu.enabled == false && aiMoving)
            gamePaused = false;

    }

    public void AIWin()
    {
        buttonCanvas.enabled = false;
        GameObject.Find("Menu Manager").GetComponent<StoryPauseButton>().pauseButton.gameObject.SetActive(false);
        StartCoroutine(RotateCamera());
        highlightPieces();
        Debug.Log(currentPlayer.piece + " won!");
    }

    public void usrWin()
    {
        StartCoroutine(winAnimation());
        highlightPieces();
        Debug.Log(currentPlayer.piece + " won!");
    }

    public bool makeMove(char c)
    {
        if (gamePaused)
        {
            return false;
        }
        if (validPiece(chosenPiece.row, chosenPiece.col) && moveOptions(chosenPiece.row, chosenPiece.col).Contains(c))
        {
            shiftBoard(c, currentPlayer.piece);
            buttonHandler.changeArrowsBack(); //F: change arrows back for every new piece selected
            if (won())
            {
                if (currentPlayer.piece == 'X')
                {
                    usrWin();
                    return true;
                }
                else
                {
                    AIWin();
                    return true;
                }
            }
            //F: if not won, we change the currentPlayer
            else if (currentPlayer.piece == 'X')
            {
                currentPlayer = p2;
            }
            else
            {
                currentPlayer = p1;
            }
            gamePaused = false;
            aiMoving = true;

            if (playAI)
            {
                    EasyAIMove(easyAI);
            }

            return true;
        }
        return false;
    }

    async void EasyAIMove(EasyAI easyAI)
    {
        Debug.Log("Fernando's mother");
        char[,] board = translateBoard();

        await Task.Delay(1500);
        (Piece, char) move = await Task.Run(() => easyAI.FindBestMove(board, 0, level: SMLvl));

        //await WaitFor();
        validPiece(move.Item1.row, move.Item1.col, true);
        shiftBoard(move.Item2, currentPlayer.piece);
        Debug.Log("Row: " + move.Item1.row + "Col: " + move.Item1.col + ":" + move.Item2);
        if (won())
        {
            if(currentPlayer.piece == 'O')
            {
                AIWin();
            }
            else
            {
                usrWin();
            }
        }
        else if (currentPlayer.piece == 'X')
        {
            currentPlayer = p2;
        }
        else
        {
            currentPlayer = p1;
        }
        gamePaused = false;
        await Task.Delay(750);

        aiMoving = false;
    }



  
    private async Task WaitFor()
    {
        await Task.Delay(1000);
    }

    public List<char> moveOptions(int row, int col)
    {
        buttonHandler.changeArrowsBack();
        List<char> moveList = new List<char>();
        if (row > 0)
        {
            moveList.Add('U');
            buttonHandler.changeArrowColor('U');
        }
        if (row < 4)
        {
            moveList.Add('D');
            buttonHandler.changeArrowColor('D');
        }
        if (col > 0)
        {
            moveList.Add('L');
            buttonHandler.changeArrowColor('L');
        }
        if (col < 4)
        {
            moveList.Add('R');
            buttonHandler.changeArrowColor('R');
        }
        return moveList;
    }

    //checks to see if the passed piece is a selectable piece for the player to choose
    public bool validPiece(int row, int col, bool aiTurn = false)
    {
        if (gameOver || (aiMoving && !aiTurn))
        {
            return false;
        }
        StoryPieceLogic piece = gameBoard[row, col].GetComponent<StoryPieceLogic>();
        if ((row == 0 || row == 4) || (col == 0 || col == 4))
        {
            if (piece.player == '-' || currentPlayer.piece == piece.player)
            {
                chosenPiece = piece;

                OnChosenPiece?.Invoke(row, col);

                return true;
            }
        }
        return false;
    }

    //fills the board with GamePiece Objects and sets the important fields
    public void populateBoard()
    {
        int x = -40;
        int z = -40;
        for (int i = 0; i < 5; i++)
        {
            z = -40;
            for (int j = 0; j < 5; j++)
            {
                gameBoard[i, j] = Instantiate(piecePrefab, new Vector3((-2856 + x), 100f, z), Quaternion.identity);
                gameBoard[i, j].GetComponent<StoryPieceLogic>().row = i;
                gameBoard[i, j].GetComponent<StoryPieceLogic>().col = j;
                gameBoard[i, j].GetComponent<StoryPieceLogic>().player = '-';
                gameBoard[i, j].GetComponent<StoryPieceLogic>().game = this;
                z += 20;
            }
            x += 20;
        }
        openDialogMenu();
    }

    public char[,] translateBoard()
    {
        char[,] aiBoard = new char[5, 5];
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                aiBoard[i, j] = gameBoard[i, j].GetComponent<StoryPieceLogic>().player;
            }
        }

        return aiBoard;
    }

    void makeRightDiagonalWin()
    {
        for (int i = 0; i < 4; i++)
        {
            gameBoard[i, 4 - i].GetComponent<StoryPieceLogic>().player = 'X';
            gameBoard[i, 4 - i].GetComponent<Renderer>().material = playerOneSpace;
        }
    }

    void makeDiagonalWin()
    {
        for (int i = 1; i < 5; i++)
        {
            gameBoard[i, i].GetComponent<StoryPieceLogic>().player = 'X';
            gameBoard[i, i].GetComponent<Renderer>().material = playerOneSpace;
        }
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.D)) { makeDiagonalWin(); }
        //if (Input.GetKeyDown(KeyCode.R)) { makeRightDiagonalWin(); }
    }



}


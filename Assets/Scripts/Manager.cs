using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;
using UnityEngine.UI;
public class Manager : MonoBehaviourPunCallbacks
{
    public GameObject playerRowPrefab;    
    public GameObject startBtn;

    public Transform playerRowsParent;

    public GameObject gamePanel;
    public GameObject leaderboardPanel;
    public GameObject winnerPanel;
    public TextMeshProUGUI firstPlaceName, secondPlaceName, thirdPlaceName, firstPlacePoints, secondPlacePoints, thirdPlacePoints, firstPlaceCorrectAns, secondPlaceCorrectAns, thirdPlaceCorrectAns;
    public Transform leaderboardRowParent;
    public GameObject countdownOverlay;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI questionText, answerText;
    public TMP_InputField answerInputField;
    public Button sendButton;
    public Button skipLeaderboardButton;

    public GameObject cardPrefab;
    public Transform cardParent;
    
    Menu menu;

    bool startCountdown = false;
    float timerTime = 3;
    float countdownTimer = 3;

    int currentRound = 0;
    float timeDuringRound = 0;
    bool playingRound = false;

    bool addedPoints = false;

    List<string> questions = new List<string> { "Quien es su cantante favorito?", "Cual es su color favorito" };

    string[] questionsInOrder;
    List<AnswerInfo> answerInfo = new List<AnswerInfo>();

    List<Card> cardsList;

    Player[] playerList;

    Dictionary<int, long> pointsDictionary = new Dictionary<int, long>();

    Dictionary<int, int> correctAnswers = new Dictionary<int, int>();
    bool gameOver = false;
    // Start is called before the first frame update
    void Awake()
    {
        menu = GameObject.FindObjectOfType<Menu>();
        if(PhotonNetwork.IsMasterClient)
        {
            startBtn.SetActive(true);
        } else
        {
            startBtn.SetActive(false);
        }

        questionsInOrder = new string[questions.Count];

        playerList = PhotonNetwork.PlayerList;

        // makes the score for everybody 0
        for(int i = 0; i < playerList.Length; i++)
        {
            pointsDictionary.Add(playerList[i].ActorNumber, 0);
            correctAnswers.Add(playerList[i].ActorNumber, 0);
        }

        DisplayAllPlayers();
        //Debug.Log(GenerateRandomOrderOfQuestions());

    }

    private void Start()
    {
        cardsList = new List<Card>();
        // add to card list when new player joins
        for (int i = 0; i <= playerList.Count(); i++)
        {
            cardsList.Add(null);
        }
    }

    // updates ui to show list of players
    void DisplayAllPlayers()
    {
        Photon.Realtime.Player[] playerList = PhotonNetwork.PlayerList;
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            CreatePlayerUI(playerList[i]);
        }
    }

    void CreatePlayerUI(Photon.Realtime.Player player)
    {
        GameObject gameObject = Instantiate(cardPrefab, playerRowsParent);
        Card card = gameObject.GetComponent<Card>();
        card.InLobby(player.NickName);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        CreatePlayerUI(newPlayer);
        playerList = PhotonNetwork.PlayerList;
        pointsDictionary.Add(newPlayer.ActorNumber, 0);
        correctAnswers.Add(newPlayer.ActorNumber, 0);
        cardsList.Add(null);
    }


    // Update is called once per frame
    void Update()
    {
        
        if (startCountdown)
        {
            countdownTimer -= Time.deltaTime;
            countdownText.text = ((int)(countdownTimer + 0.99f)).ToString();
            if (countdownTimer <= 0)
            {
                startCountdown = false;
                playingRound = true;

                // disable countdown screen
                ShowCountdownOverlay(false);
            }
        }
        else if (playingRound)
        {
            timeDuringRound += Time.deltaTime;
        }
    }

    #region starting procedures

    void ShowCountdownOverlay(bool show)
    {
        countdownOverlay.SetActive(show);
    }
    public void OnStartButtonClicked()
    {
        // send rpc with question order
        SendQuestionOrder();
    }

    void SendQuestionOrder()
    {
        photonView.RPC("StartingGame", RpcTarget.AllBuffered, GenerateRandomOrderOfQuestions());
    }

    long GenerateRandomOrderOfQuestions()
    {
        string indexes = "";
        List<string> temp = questions;
        int numOfQuestions = temp.Count;

        for (int i = 0; i < numOfQuestions; i++)
        {
            int randomQuestion = Random.Range(0, temp.Count);
            char num = char.Parse(randomQuestion.ToString());
            while (indexes.ToLower().Contains(num))
            {
                randomQuestion = Random.Range(0, temp.Count);
                num = char.Parse(randomQuestion.ToString());
            }

            indexes += randomQuestion + " ";
        }
        return long.Parse(RemoveWhiteSpace(indexes.ToString()));
    }
    string RemoveWhiteSpace(string input)
    {
        return new string(input.ToCharArray()
            .Where(c => !System.Char.IsWhiteSpace(c))
            .ToArray());
    }

    [PunRPC]
    void StartingGame(long questionIndexes)
    {
        questionsInOrder = LongToArray(questionIndexes);

        ShowGamePanel(true);
    }

    void StartCountdown()
    {
        startCountdown = true;
    }

    string[] LongToArray(long num)
    {
        string[] array = questionsInOrder;
        for (int i = array.Length - 1; i >= 0; i--)
        {
            array[array.Length - 1 - i] = questions[(int) num % 10];
            num /= 10;
        }
        return array;
    }  

    void ShowGamePanel(bool show)
    {
        if (!show)
        {
            gamePanel.SetActive(false);
            ResetGamePanel();
            return;
        }

        StartCountdown();
        ResetGamePanel();

        // overlay covering game panel. Shows countdown.
        ShowCountdownOverlay(true);

        menu.DisableLobbyPanel();

        // show countdown

        SetQuestionText();

        gamePanel.SetActive(true);

        // enable these elements again
        answerInputField.gameObject.SetActive(true);
        sendButton.gameObject.SetActive(true);
    }

    void ResetGamePanel()
    {
        for (int i = 0; i < cardParent.childCount; i++)
        {
            Destroy(cardParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < cardsList.Count; i++)
        {
            cardsList[i] = null;
        }

        answerInputField.gameObject.SetActive(true);
        answerInputField.text = "";
        sendButton.gameObject.SetActive(true);
        answerInfo.Clear();
        answerText.text = "A: ????";
        addedPoints = false;
        countdownTimer = timerTime;
        timeDuringRound = 0;
    }

    void SetQuestionText()
    {
        questionText.text = "Q: " + questionsInOrder[currentRound];
    }

    #endregion starting procedures

    public void OnSubmitClicked()
    {
        // send rpc that contains the answer, and the time it took to submit
        if (playingRound && answerInputField.text.Trim().Length > 0)
        {
            playingRound = false;
            photonView.RPC("SendAnswerViaRPC", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, answerInputField.text, timeDuringRound);

            // hide the input field and button.
            // keep the button if is master.
            answerInputField.gameObject.SetActive(false);
            if (!PhotonNetwork.IsMasterClient)
            {
                sendButton.gameObject.SetActive(false);
            }
        } else if (!playingRound && !addedPoints) // this button was clicked again to submit the correct answers.
        {

            if (!HaveAllPlayersAnswered()) // if not everyone answered, then this was clicked to skip.
            {
                ShowAnswersOnCards();
                return;
            }

            string selectedCards = "";
            // submit answers.
            for(int i = 0; i < cardsList.Count; i++)
            {
                bool setAnswerText = false;
                if (cardsList[i] != null && cardsList[i].IsSelected())
                {
                    if (!setAnswerText)
                    { 
                        photonView.RPC("SetAnswerText", RpcTarget.AllBuffered, cardsList[i].GetAnswerInfo().answer);
                        setAnswerText = true;                       
                    }

                    selectedCards += cardsList[i].GetAnswerInfo().actorNumber + " ";                   
                }
            }
            if (selectedCards.Length == 0)
            {
                answerText.text = "A: Nadie supo la respuesta.";
            }

            // call add points rpc to add points on all devices
            photonView.RPC("AddPoints", RpcTarget.AllBuffered, selectedCards);

           
        } else if (addedPoints)
        {
            // if finished last question
            if (currentRound == questions.Count() - 1)
            {
                Debug.Log(currentRound + " " + questions.Count());
                photonView.RPC("GoToWinnerScreen", RpcTarget.AllBuffered);
                return;
            }

            // skip the waiting period and go to leaderboard.
            Debug.Log("Go to leaderboard");
            photonView.RPC("GoToLeaderboard", RpcTarget.AllBuffered);       
        }
    }

    [PunRPC]
    void SetAnswerText(string answer)
    {
        answerText.text = "A: " + answer;
    }

    [PunRPC]
    void GoToWinnerScreen()
    {
        // show winner
        gameOver = true;

        ShowWinnerScreen();
    }

    [PunRPC]
    void GoToLeaderboard()
    {
        gamePanel.SetActive(false);
        ShowLeaderboardPanel();
    }

    

    [PunRPC]
    void SendAnswerViaRPC(int actorNumber, string answer, float time)
    {
        AnswerInfo info = new AnswerInfo(actorNumber, answer, time);
        answerInfo.Add(info);

        GameObject gameObject = Instantiate(cardPrefab, cardParent);
        Card card = gameObject.GetComponent<Card>();

        cardsList[actorNumber] = card;

        card.SetAnswerInfo(info);
        card.SetName(GetNameFromActorNum(actorNumber));

        if (HaveAllPlayersAnswered())
        {
            // show the answers
            ShowAnswersOnCards();
        }
    }
    string GetNameFromActorNum(int actorNum)
    {
        var playerList = PhotonNetwork.PlayerList;
        for (int i = 0; i < playerList.Length; i++)
        {
            if (playerList[i].ActorNumber == actorNum)
            {
                return playerList[i].NickName;
            }
        }

        return "ErrorWithName";
    }

    bool HaveAllPlayersAnswered()
    {
        for(int i = 1; i < cardsList.Count; i++)
        {
            if (cardsList[i] == null)
            {
                return false;
            }                     
        }
        return true;
    }

    void ShowAnswersOnCards()
    {
        Debug.Log("Is Master: PhotonNetwork.IsMasterClient");
        foreach(Card card in cardsList)
        {
            if (card != null)
            {
                card.SetInteractable(PhotonNetwork.IsMasterClient);
                card.ShowAnswer();
            }
        }
    }


    [PunRPC]
    void AddPoints(string actorNumbers)
    {
        addedPoints = true;
        string[] actorNums = actorNumbers.Split(' ');
        
        for(int i = 0; i < cardsList.Count; i++)
        {
            if (cardsList[i] == null) 
                continue;
            if (actorNumbers.Contains(i.ToString())) 
            {
                // give points
                long addedPoints = CalculatePointsFromTime(cardsList[i].GetTime());
                pointsDictionary[i] += addedPoints;
                correctAnswers[i] += 1;
                cardsList[i].SetPoints(addedPoints);
                cardsList[i].SetInteractable(false);

            } else
            {
                // give 0 points
                cardsList[i].SetPoints(0);
            }
        }
    }

    long CalculatePointsFromTime(float time)
    {
        return (long) (6000 / time);
    }

    void ShowLeaderboardPanel()
    {
        for(int i = 0; i < leaderboardRowParent.childCount; i++)
        {
            Destroy(leaderboardRowParent.GetChild(i).gameObject);
        }

        leaderboardPanel.SetActive(true);
        ShowGamePanel(false);
        skipLeaderboardButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        // populate leaderboard.
        List<long> pointsOrder = new List<long>();
        List<int> actorNumberOrder = new List<int>();
        for(int i = 1; i <= pointsDictionary.Count; i++)
        {
            if (pointsDictionary.ContainsKey(i))
            {
                pointsOrder.Add(pointsDictionary[i]);
                actorNumberOrder.Add(i);
            }
        }

        for (int i = 0; i < pointsOrder.Count; i++)
        {
            for (int j = i + 1; j < pointsOrder.Count; j++)
            {
                if (pointsOrder[j] > pointsOrder[i])
                {
                    long temp = pointsOrder[i];
                    pointsOrder[i] = pointsOrder[j];
                    pointsOrder[j] = temp;

                    int otherTemp = actorNumberOrder[i];
                    actorNumberOrder[i] = actorNumberOrder[j];
                    actorNumberOrder[j] = otherTemp;
                }
            }
        }

        // spawn the rows
        for (int i = 0; i < actorNumberOrder.Count; i++)
        {
            GameObject row = Instantiate(playerRowPrefab, leaderboardRowParent);
            //sets the place
            row.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = (i + 1) + ".";
            // sets the name
            row.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = GetNameFromActorNum(actorNumberOrder[i]);
            // sets the points text
            row.transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>().text = string.Format("{0:#,###0}", pointsDictionary[actorNumberOrder[i]]);
        }
    }

    public void OnSkipLeaderboardClicked()
    {
        photonView.RPC("GoToNextRound", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void GoToNextRound()
    {
        currentRound++;

        leaderboardPanel.SetActive(false);
        ShowGamePanel(true);// resets everything inside this method
    }

    void ShowWinnerScreen()
    {

        ShowGamePanel(false);
        winnerPanel.SetActive(true);
        leaderboardPanel.SetActive(false);

        List<long> pointsOrder = new List<long>();
        List<int> actorNumberOrder = new List<int>();
        for (int i = 1; i <= pointsDictionary.Count; i++)
        {
            if (pointsDictionary.ContainsKey(i))
            {
                pointsOrder.Add(pointsDictionary[i]);
                actorNumberOrder.Add(i);
            }
        }

        for (int i = 0; i < pointsOrder.Count; i++)
        {
            for (int j = i + 1; j < pointsOrder.Count; j++)
            {
                if (pointsOrder[j] > pointsOrder[i])
                {
                    long temp = pointsOrder[i];
                    pointsOrder[i] = pointsOrder[j];
                    pointsOrder[j] = temp;

                    int otherTemp = actorNumberOrder[i];
                    actorNumberOrder[i] = actorNumberOrder[j];
                    actorNumberOrder[j] = otherTemp;
                }
            }
        }

        // spawn the rows
        for (int i = 0; i < actorNumberOrder.Count; i++)
        {
            if (i == 0)
            {
                firstPlaceName.text = GetNameFromActorNum(actorNumberOrder[i]);
                firstPlaceCorrectAns.text = correctAnswers[actorNumberOrder[i]].ToString() + " out of " + questions.Count();
                firstPlacePoints.text = string.Format("{0:#,###0}", pointsDictionary[actorNumberOrder[i]].ToString());
            }
            else if (i == 1)
            {
                secondPlaceName.text = GetNameFromActorNum(actorNumberOrder[i]);
                secondPlaceCorrectAns.text = correctAnswers[actorNumberOrder[i]].ToString() + " out of " + questions.Count();
                secondPlacePoints.text = string.Format("{0:#,###0}", pointsDictionary[actorNumberOrder[i]].ToString());
            }
            else if (i == 2)
            {
                thirdPlaceName.text = GetNameFromActorNum(actorNumberOrder[i]);
                thirdPlaceCorrectAns.text = correctAnswers[actorNumberOrder[i]].ToString() + " out of " + questions.Count();
                thirdPlacePoints.text = string.Format("{0:#,###0}", pointsDictionary[actorNumberOrder[i]].ToString());
                return;
            }
        }
        
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        PhotonNetwork.ReconnectAndRejoin();
    }
}

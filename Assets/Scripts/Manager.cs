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

    List<string> questions = new List<string> { "Quien es tu cantante favorito?", "Que es tu color favorito", "3", "4", "5", "6", "7" };

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
        for (int i = 0; i < 8; i++)
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
        for(int i = cardsList.Count - 1; i < playerList.Length; i++)
        {
            cardsList.Add(null);
        }
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

        answerInputField.gameObject.SetActive(true);
        answerInputField.text = "";
        sendButton.gameObject.SetActive(true);
        answerInfo.Clear();
        answerText.text = "A: ????";
        addedPoints = false;
        countdownTimer = timerTime;
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
            string selectedCards = "";
            // submit answers.
            for(int i = 0; i < cardsList.Count; i++)
            {
                if (cardsList[i] != null && cardsList[i].IsSelected())
                {
                    // sets the correct answer text to the answer of the first card with correct answer
                    if (selectedCards.Length < 1)
                    {
                        answerText.text = "A: " + cardsList[i].GetAnswerInfo().answer;
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
            addedPoints = true;
            // start timer to wait for going to leaderboard

        } else if (addedPoints)
        {
            // skip the waiting period and go to leaderboard.
            Debug.Log("Go to leaderboard");
            gamePanel.SetActive(false);
            ShowLeaderboardPanel();

            // if finished last question
            if (currentRound == questions.Count() - 1)
            {
                Debug.Log(currentRound + " " + questions.Count());
                // show winner
                gameOver = true;

                ShowWinnerScreen();
            }
        }
    }

    void ShowWinnerScreen()
    {
        ShowGamePanel(false);
        winnerPanel.SetActive(true);

        List<long> pointsOrder = new List<long>();
        List<int> actorNumberOrder = new List<int>();
        for (int i = 0; i < pointsDictionary.Count; i++)
        {
            if (pointsOrder.Count > 0)
            {
                if (pointsDictionary[i] > pointsOrder[0])
                {
                    pointsOrder.Insert(0, pointsDictionary[i]);
                    actorNumberOrder.Insert(0, i);
                }
                else if (pointsDictionary[i] == pointsOrder[0]) // tie breaker. 
                {
                    //if this user has more correct answers than first place
                    if (correctAnswers[i] > correctAnswers[actorNumberOrder[0]])
                    {
                        pointsOrder.Insert(0, pointsDictionary[i]);
                        actorNumberOrder.Insert(0, i);
                    }
                    else
                    {
                        // put in second place
                        pointsOrder.Insert(1, pointsDictionary[i]);
                        actorNumberOrder.Insert(1, i);
                    }
                }
            }
            else
            {
                pointsOrder.Add(pointsDictionary[1]);
                actorNumberOrder.Add(1);
            }
        }

        // spawn the rows
        for (int i = 0; i < actorNumberOrder.Count; i++)
        {
            if (i == 0)
            {
                firstPlaceName.text = GetNameFromActorNum(actorNumberOrder[i]);
                firstPlaceCorrectAns.text = correctAnswers[actorNumberOrder[i]].ToString() + " out of " + questions.Count();
                firstPlacePoints.text = pointsDictionary[actorNumberOrder[i]].ToString();
            } else if (i == 0)
            {
                secondPlaceName.text = GetNameFromActorNum(actorNumberOrder[i]);
                secondPlaceCorrectAns.text = correctAnswers[actorNumberOrder[i]].ToString() + " out of " + questions.Count();
                secondPlacePoints.text = pointsDictionary[actorNumberOrder[i]].ToString();
            } else if (i == 3)
            {
                thirdPlaceName.text = GetNameFromActorNum(actorNumberOrder[i]);
                thirdPlaceCorrectAns.text = correctAnswers[actorNumberOrder[i]].ToString() + " out of " + questions.Count();
                thirdPlacePoints.text = pointsDictionary[actorNumberOrder[i]].ToString();
                return;
            }
        }
    }

    [PunRPC]
    void SendAnswerViaRPC(int actorNumber, string answer, float time)
    {
        AnswerInfo info = new AnswerInfo(actorNumber, answer, time);
        answerInfo.Add(info);

        GameObject gameObject = Instantiate(cardPrefab, cardParent);
        Card card = gameObject.GetComponent<Card>();

        cardsList[actorNumber] = card;

        if (PhotonNetwork.IsMasterClient)
        {
            card.SetInteractable(true);
        }

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
        return cardsList.Count >= playerList.Length;       
    }

    void ShowAnswersOnCards()
    {
        foreach(Card card in cardsList)
        {
            if (card != null)
            {
                card.ShowAnswer();
            }
        }
    }


    [PunRPC]
    void AddPoints(string actorNumbers)
    {
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

        skipLeaderboardButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        // populate leaderboard.
        List<long> pointsOrder = new List<long>();
        List<int> actorNumberOrder = new List<int>();
        for (int i = 0; i < pointsDictionary.Count; i++)
        {
            if (pointsOrder.Count > 0)
            {
                if (pointsDictionary[i] > pointsOrder[0])
                {
                    pointsOrder.Insert(0, pointsDictionary[i]);
                    actorNumberOrder.Insert(0, i);
                }
                else if (pointsDictionary[i] == pointsOrder[0]) // tie breaker. 
                {
                    //if this user has more correct answers than first place
                    if (correctAnswers[i] > correctAnswers[actorNumberOrder[0]])
                    {
                        pointsOrder.Insert(0, pointsDictionary[i]);
                        actorNumberOrder.Insert(0, i);
                    }
                    else
                    {
                        // put in second place
                        pointsOrder.Insert(1, pointsDictionary[i]);
                        actorNumberOrder.Insert(1, i);
                    }
                }
            }
            else
            {
                pointsOrder.Add(pointsDictionary[1]);
                actorNumberOrder.Add(1);
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
            row.transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>().text = pointsDictionary[actorNumberOrder[i]].ToString();
        }
    }

    public void OnSkipLeaderboardClicked()
    {
        currentRound++;

        leaderboardPanel.SetActive(false);
        ShowGamePanel(true);// resets everything inside this method

    }



    public override void OnDisconnected(DisconnectCause cause)
    {
        PhotonNetwork.ReconnectAndRejoin();
    }
}

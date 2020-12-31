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
    public TextMeshProUGUI questionText;
    public TMP_InputField answerInputField;
    public Button sendButton;

    public GameObject cardPrefab;
    public Transform cardParent;
    
    Menu menu;

    bool startCountdown = false;
    float countdownTimer = 0;

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
        card.SetName(player.NickName);
        card.InLobby();
        //GameObject row = Instantiate(cardsList, playerRowsParent);
        //row.GetComponentInChildren<TextMeshProUGUI>().text = player.NickName;
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
            if (countdownTimer <= 0)
            {
                startCountdown = false;
                ShowGamePanel();
            }
        }

        if (playingRound)
        {
            timeDuringRound += Time.deltaTime;
        }
    }

    #region starting procedures

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
        StartCountdown();

        questionsInOrder = LongToArray(questionIndexes);
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

    void ShowGamePanel()
    {
        menu.DisableLobbyPanel();

        SetQuestionText();

        gamePanel.SetActive(true);

        // enable these elements again
        answerInputField.gameObject.SetActive(true);
        sendButton.gameObject.SetActive(true);

        // start timer.
        playingRound = true;
    }

    void SetQuestionText()
    {
        questionText.text = questionsInOrder[currentRound];
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
                    selectedCards += cardsList[i].GetAnswerInfo().actorNumber + " ";
                }
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
            // delete all the cards.
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
        leaderboardPanel.SetActive(true);
        // populate leaderboard.

        List<Dictionary<int, long>> order = new List<Dictionary<int, long>> ();
        for(int i = 0; i < pointsDictionary.Count; i++)
        {

            // TODO: fix this so that it makes a list with actor numbers in order from 0 to 7.
            //if (order.Count > 0)
            //{
            //    if (pointsDictionary[i] > order[i])
            //    {
            //        order.Insert(0, pointsDictionary[i]);
            //    }
            //} else
            //{
            //    order.Add(pointsDictionary[i]);
            //}
        }


    }
}

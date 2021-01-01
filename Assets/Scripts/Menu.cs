using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
public class Menu : MonoBehaviourPunCallbacks
{
    public Manager gameManager;
    public TMP_InputField nameInput;
    public Button loginButton;
    public GameObject menuPanel;
    public GameObject lobbyPanel;


    float hold = 5;

    const string playerPrefab = "Player";   

    private void Awake()
    {
    }
    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    void Update()
    {

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            loginButton.interactable = false;
        } else
        {
            loginButton.interactable = true;
        }
        //if (!menuPanel.activeInHierarchy)
        //{
        //    return;
        //}
        //if (Input.GetMouseButton(0))
        //{
        //    hold -= Time.deltaTime;
        //} else if (Input.GetMouseButtonUp(0))
        //{
        //    hold = 5;
        //}
        //if (hold <= 0)
        //{
        //    PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
        //}
    }

    public void OnInputFieldUpdated(string input)
    {

        if (IsStringAcceptable(input))
        {
            loginButton.gameObject.SetActive(true);
        }
        else
        {
            //nameInput.placeholder.GetComponentInChildren<TextMeshPro>().text = "That is not your name.";
        }
    }

    bool IsStringAcceptable(string input)
    {
        return input.Trim().Length > 0;
    }

    public void OnLoginPressed()
    {
        if (!IsStringAcceptable(nameInput.ToString()))
        {
            nameInput.text = "";
            //nameInput.placeholder.GetComponentInChildren<TextMeshPro>().text = "That is not your name.";
            return;
        }

        string nickName = nameInput.text.ToString();
        PhotonNetwork.NickName = nickName;

        if (PhotonNetwork.CountOfRooms > 0)
        {        
            PhotonNetwork.JoinRoom("Room");
            Debug.Log("Room available");
        }
        else
        {
            RoomOptions options = new RoomOptions { MaxPlayers = 8 };

            PhotonNetwork.CreateRoom("Room", options, null);
            Debug.Log("Room not available");

        }
    }

    public void DisableLobbyPanel()
    {
        lobbyPanel.SetActive(false);
    }


    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log(message);
        PhotonNetwork.JoinRoom("Room");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        PhotonNetwork.JoinRoom("Room");
    }

    public override void OnJoinedRoom()
    {      
        menuPanel.SetActive(false);

        // enable game manager and ui;
        lobbyPanel.SetActive(true);
        gameManager.gameObject.SetActive(true);

        // spawn player prefab
        //PhotonNetwork.Instantiate(playerPrefab, Vector2.zero, Quaternion.identity);
    }

    
}

﻿using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update

    int AvatarId = 0;
    readonly int AvatersCount = 2;

    void Start()
    {
        if (!PhotonNetwork.IsConnected) 
        { 
            Connect();
        }    
    }

    public void JoinAuctionHouse(int avatar_id, string name, string email, string mobile)
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("id"))
        {
            PhotonNetwork.LocalPlayer.CustomProperties["id"] = avatar_id;
            PhotonNetwork.LocalPlayer.CustomProperties["name"] = name;
            PhotonNetwork.LocalPlayer.CustomProperties["email"] = email;
            PhotonNetwork.LocalPlayer.CustomProperties["mobile"] = mobile;
        }
        else
        {
            ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable
        {
            { "id", avatar_id }, { "name", name }, { "email", email }, { "mobile", mobile}
        };
            PhotonNetwork.SetPlayerCustomProperties(playerProps);
        }
        PhotonNetwork.JoinRandomRoom();
    }

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void Connect()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public void IncAvatarId()
    {
        AvatarId = (AvatarId+1) % AvatersCount;
    }

    public void DecAvatarId()
    {
        AvatarId--;
        if (AvatarId < 0)
        {
            AvatarId = AvatersCount;
        }
    }

    public void Play()
    {

        string name = GameObject.Find("NameFieldText").GetComponent<TMPro.TextMeshProUGUI>().text;
        string email = GameObject.Find("EmailFieldText").GetComponent<TMPro.TextMeshProUGUI>().text;
        string mobile = GameObject.Find("MobileNoFieldText").GetComponent<TMPro.TextMeshProUGUI>().text;
        JoinAuctionHouse(AvatarId, name, email, mobile);
        
        //PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Tried to join the room and failed");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined a room!");
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(1);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}

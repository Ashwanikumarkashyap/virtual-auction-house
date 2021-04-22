﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;
using Photon.Realtime;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    public float speed = 20f;

    public Transform avatar;
    private float gravity = 10f;

    //now the camera so we can move it up and down
    public Transform cameraTransform;

    public GameObject dashboard;
    public int[] currentBids;
    public string[] currentBidders = {"Host", "Host", "Host", "Host", "Host", "Host"};
    public string[] auctionItemNames;
    public string[] previousBids = { "-", "-", "-", "-", "-", "-" };
    public string[] previousBidders = { "-", "-", "-", "-", "-", "-" };

    //the charachtercompononet for moving us
    CharacterController cc;

    Animator walkingAnimator;
    AudioSource boathAudioSource;

    public GameObject playerCameraWrapper;
    public GameObject playerCamera;

    AnimateItems animateItemsScript;
    public bool isLocalPlayer = false;

    public GameObject auctionTimeText;
    public float auctionTime = 3600;


    private void Start()
    {

        animateItemsScript = GetComponent<AnimateItems>();
        walkingAnimator = GetComponentInChildren<Animator>();

        boathAudioSource = GameObject.Find("Boat").GetComponent<AudioSource>();
        boathAudioSource.Stop();
        cc = GetComponent<CharacterController>();
        if (!photonView.IsMine)
        {
            GetComponentInChildren<Camera>().enabled = false;
            GetComponentInChildren<AudioListener>().enabled = false;
        } else
        {
            isLocalPlayer = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Camera c = gameObject.GetComponentInChildren<Camera>();

        if (auctionTime > 0)
        {
            auctionTime -= Time.deltaTime;
            DisplayTime(auctionTime);
        } else
        {
            GameManager mgr = GameObject.Find("GameManager").GetComponent<GameManager>();
            mgr.Leave();
        }


        if (gameObject.GetComponentInChildren<Camera>() != null)
        {
            if (photonView.IsMine)
            {
                var ray = playerCamera.transform;
                RaycastHit hit;

                if (Physics.Raycast(ray.position, ray.forward, out hit) && (Input.GetButtonUp("js5") || Input.GetKeyUp("space") || Input.GetMouseButtonUp(0)))

                {
                    var selection = hit.transform;
                    if (selection.CompareTag("BidAmount"))
                    {
                        string bidClicked = selection.name;
                        string[] bid_id_value = bidClicked.Split('_');
                        int id = int.Parse(bid_id_value[1]) - 1;
                        int value = int.Parse(bid_id_value[2]);
                        previousBids[id] = currentBids[id].ToString();
                        currentBids[id] = currentBids[id] + value;

                        GameObject auctionItemInfo = GameObject.Find("AuctionItemInfoPanel_" + (id + 1).ToString());
                        Transform currentBid = auctionItemInfo.transform.GetChild(1);
                        TMPro.TextMeshProUGUI textComponent = currentBid.GetComponent<TMPro.TextMeshProUGUI>();
                        textComponent.SetText("Current Bid: $" + currentBids[id].ToString());

                        string name = (string)PhotonNetwork.LocalPlayer.CustomProperties["name"];

                        TMPro.TextMeshProUGUI currentBidderText = auctionItemInfo.transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>();
                        previousBidders[id] = currentBidders[id];
                        currentBidders[id] = name;
                        currentBidderText.SetText("Current Bidder: " + currentBidders[id]);


                        // RPC call
                        PhotonView photonView = PhotonView.Get(this);
                        photonView.RPC("RecieveUpdatedBids", RpcTarget.All, currentBids, currentBidders, previousBids, previousBidders);

                        //UpdateAuctionUi();
                    }
                    if (selection.CompareTag("AuctionItem"))
                    {
                        string AuctionItemName = selection.name;
                        Animator anime;
                        switch (AuctionItemName)
                        {
                            case "OldBoat":
                                playerCamera.transform.parent = GameObject.Find("BoatCameraWrapper").transform;
                                //GameObject.Find("Boat").transform.localPosition = new Vector3(-0f, -0f, 0f);
                                //playerCamera.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                                boathAudioSource.Play();
                                break;
                            case "VanityDeskWhite":
                                animateItemsScript.animateVanity(selection.Find("drawers").GetComponent<Animator>());
                                break;
                            case "Classic_car":
                                animateItemsScript.animateVanity(selection.GetComponent<Animator>());
                                break;
                            case "Drone_Guard":
                                animateItemsScript.animateDrone(selection.GetComponent<Animator>());
                                break;
                            case "RobotSphere":
                                animateItemsScript.animateRobot(selection.GetComponent<Animator>());
                                break;
                            case "Spartan_Warrior":
                                animateItemsScript.animateSpartan(selection.GetComponent<Animator>());
                                break;
                            default:
                                Debug.Log("Default case");
                                break;
                        }
                    }
                }

                if (Input.GetKeyUp("tab") || Input.GetButtonUp("js3"))
                {
                    if (!dashboard.activeInHierarchy)
                    {
                        updateAuctionBoard();
                        dashboard.SetActive(true);
                    }
                    else
                    {
                        dashboard.SetActive(false);
                    }
                }

                if (!dashboard.activeInHierarchy)
                {
                    Move();
                }
            }
        }
        else if (Input.GetKeyUp("space") || Input.GetButtonUp("js5") || Input.GetMouseButtonUp(0) || (Input.GetButtonUp("js0")))
        {
            boathAudioSource.Stop();
            playerCamera.transform.parent = playerCameraWrapper.transform;
        }
    }


    void DisplayTime(float timeToDisplay)
    {

        timeToDisplay += 1;

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        string time = string.Format("{0:00}:{1:00}", minutes, seconds);

        TMPro.TMP_Text textObj = auctionTimeText.GetComponent<TMPro.TextMeshProUGUI>();
        textObj.SetText("Time left: " + time);
    }

    void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical);
        Vector3 velocity = direction * speed;

        if (velocity.magnitude > 0.01f)
        {
            walkingAnimator.SetBool("Moving", true);
        }
        else
        {
            walkingAnimator.SetBool("Moving", false);
        }


        velocity = Camera.main.transform.TransformDirection(velocity);
        velocity.y -= gravity;
        cc.Move(velocity * Time.deltaTime);

        var CharacterRotation = cameraTransform.transform.rotation;
        CharacterRotation.x = 0;
        CharacterRotation.z = 0;

        avatar.rotation = CharacterRotation;
    }

    public void UpdateAuctionUi()
    {

        GameObject[] auctionItemInfo = GameObject.FindGameObjectsWithTag("AuctionItemInfoPanel");

        for (int i = 0; i < auctionItemInfo.Length; i++)
        {
            int itemId = int.Parse(auctionItemInfo[i].name.Split('_')[1]);
            Transform currentBid = auctionItemInfo[i].transform.GetChild(1);
            
            TMPro.TextMeshProUGUI textComponent = currentBid.GetComponent<TMPro.TextMeshProUGUI>();
            textComponent.SetText("Current Bid: $" + currentBids[itemId-1].ToString());

            TMPro.TextMeshProUGUI currentBiddder = auctionItemInfo[i].transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>();
            currentBiddder.SetText("Current Bidder: " + currentBidders[itemId - 1]);

        }
    }
    public void updateAuctionBoard()
    {
        Transform itemList = dashboard.transform.GetChild(1).GetChild(1).GetChild(1);
        int itemCount = itemList.childCount;
        for (int i = 0; i < itemCount; i++)
        {
            Transform item = itemList.GetChild(i);
            UnityEngine.UI.Text itemName = item.GetChild(0).GetComponent<UnityEngine.UI.Text>();
            UnityEngine.UI.Text Cbid = item.GetChild(1).GetComponent<UnityEngine.UI.Text>();
            UnityEngine.UI.Text Cbidder = item.GetChild(2).GetComponent<UnityEngine.UI.Text>();
            UnityEngine.UI.Text pbid = item.GetChild(3).GetComponent<UnityEngine.UI.Text>();
            UnityEngine.UI.Text pbidder = item.GetChild(4).GetComponent<UnityEngine.UI.Text>();
            itemName.text = auctionItemNames[i];
            Cbid.text = currentBids[i].ToString();
            Cbidder.text = currentBidders[i];
            pbid.text = previousBids[i];
            pbidder.text = previousBidders[i];
        }
    }

    public void UpdateCart()
    {
        ShoppingCartManager cart = GameObject.Find("ShoppingCartManager").GetComponent<ShoppingCartManager>();
        cart.AuctionItems.Clear();
        string name = (string)PhotonNetwork.LocalPlayer.CustomProperties["name"];
        for (int i=0; i < currentBidders.Length; i++)
        {
            if (currentBidders[i].Equals(name)) {
                cart.AddToCart(auctionItemNames[i], currentBids[i].ToString());
            }
        }
    }

    public void UpdateClientBids(int[] currentBids, string[] currentBidders, string[] previousBids, string[] previousBidders)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            PlayerMovement pScript = player.GetComponent<PlayerMovement>();
            pScript.currentBids = currentBids;
            pScript.currentBidders = currentBidders;
            pScript.previousBids = previousBids;
            pScript.previousBidders = previousBidders;
            pScript.updateAuctionBoard();
        }
    }

    public bool isBidChanged(int[] updatedBids)
    {

        for (int i = 0; i < updatedBids.Length; i++)
        {
            if (updatedBids[i] > currentBids[i])
            {
                return true;
            }
        }

        return false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //stream.SendNext(currentBids);
            //stream.SendNext(currentBidders);
        }
        else
        {
            //int[] updatedBids = (int[])stream.ReceiveNext();
            //string[] updatedBidders = (string[])stream.ReceiveNext();
            //if (isBidChanged(updatedBids))
            //{
            //    for (int i = 0; i < updatedBids.Length; i++)
            //    {
            //        if (updatedBids[i] > currentBids[i])
            //        {
            //            currentBids[i] = updatedBids[i];
            //            currentBidders[i] = updatedBidders[i];
            //        }
            //    }
            //    //currentBids = updatedBids;
            //    //currentBidders = updatedBidders;
            //    UpdateAuctionUi();
            //    UpdateClientBids(currentBids);
            //}

        }
    }


    [PunRPC]
    void RecieveUpdatedBids(int[] updatedBids, string[] updatedBidders, string[] updatedPreviousBids, string[] updatedPreviousBidders)
    {
        if (isBidChanged(updatedBids))
        {
            for (int i = 0; i < updatedBids.Length; i++)
            {
                if (updatedBids[i] > currentBids[i])
                {
                    previousBids[i] = updatedPreviousBids[i];
                    currentBids[i] = updatedBids[i];
                    previousBidders[i] = updatedPreviousBidders[i];
                    currentBidders[i] = updatedBidders[i];
                }
            }
            UpdateAuctionUi();
            UpdateClientBids(currentBids, currentBidders, previousBids, previousBidders);
        }
    }

    [PunRPC]
    void syncAuctionTime(float auctionTime)
    {
        this.auctionTime = auctionTime;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            PlayerMovement pScript = player.GetComponent<PlayerMovement>();
            pScript.auctionTime = auctionTime;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        PhotonView photonView = PhotonView.Get(this);
        photonView.RPC("RecieveUpdatedBids", RpcTarget.All, currentBids, currentBidders, previousBids, previousBidders);

        photonView.RPC("syncAuctionTime", RpcTarget.All, auctionTime);

        base.OnPlayerEnteredRoom(newPlayer);
    }
}
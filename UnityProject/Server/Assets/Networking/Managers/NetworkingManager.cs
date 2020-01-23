using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Package;
using static Package.PackageID;
using UnityEditor;

public class ClientNetworker : Networker {

    List<GameObject> players = new List<GameObject>();

    IPAddress address;
    bool receivedLoginResponse = false;
    LoginResponse loginResponse;
    State state = State.IDLING;
    string playerName;
    GameObject playerPrefab;
    GameObject playerObject;
    int lastProcessedTick = -1;

    ClientBoundData clientBound = new ClientBoundData();

    string latestPackage = "Empty";

    public ClientNetworker(string playerName, string addressString, GameObject playerPrefab, ref GameObject playerObject) : base(NetConstants.PORT) {
        address = IPAddress.Parse(addressString);
        this.playerName = playerName;
        this.playerPrefab = playerPrefab;
        this.playerObject = playerObject;
    }

    public void ClientUpdate() {
        switch (state) {
            case State.IDLING:
                break;
            case State.WAITING_FOR_RESPONSE:
                /*if (receivedLoginResponse) {
                    GameObject.FindGameObjectWithTag("Respawn").GetComponent<Text>().text = "LoginResponse: " +loginResponse.response + "; " +loginResponse.msg;
                }*/
                break;
            case State.CONNECTED:
                CreateAndSendClientPackage();
                foreach(Player netPlayer in clientBound.netPlayers) {
                    GameObject foundPlayer = null;
                    foreach (GameObject checkPlayer in GameObject.FindGameObjectsWithTag("NetPlayer")) {
                        if (checkPlayer.name.Equals(netPlayer.name)) {
                            foundPlayer = checkPlayer;
                        }
                    }
                    if (foundPlayer == null) {
                        foundPlayer = GameObject.Instantiate(playerPrefab);
                        foundPlayer.tag = "NetPlayer";
                    }
                    foundPlayer.transform.position = netPlayer.position;
                }
                GameObject.FindGameObjectWithTag("Respawn").GetComponent<Text>().text = "NetPlayers: " + clientBound.netPlayers;
                GameObject.FindGameObjectWithTag("YEET").GetComponent<Text>().text = latestPackage;
                break;
        }
    }

    private void CreateAndSendClientPackage() {
        ServerBoundData package = new ServerBoundData(0, lastProcessedTick, new Player(playerName, playerObject.transform.position));
        SendPackage(ID_SERVER_BOUND, GetBytes(package), address);
    }

    private void HandleClientBoundPackage(ClientBoundData package) {
        clientBound = package;
        lastProcessedTick = package.tick;
    }

    public override void PacketReceived(byte[] bytes, IPEndPoint endPoint) {
        byte[] objectBytes = new byte[bytes.Length - 1];
        latestPackage = "Received: " + encoding.GetString(bytes);
        Array.Copy(bytes, 1, objectBytes, 0, bytes.Length-1);
        switch (bytes[0]) {
            case ID_LOGIN_RESPONSE:
                LoginResponse response = GetObject<LoginResponse>(objectBytes);
                loginResponse = response;
                Debug.Log("Response received " + response);
                HandleLoginResponse(response);
                break;
            case ID_CLIENT_BOUND:
                ClientBoundData clientBound = GetObject<ClientBoundData>(objectBytes);
                HandleClientBoundPackage(clientBound);
                Debug.Log("clientBound received: " + clientBound);
                break;
            default:
                Debug.LogError("Invalid package received, type id: " + bytes[0]);
                break;
        }
    }

    private void HandleLoginResponse(LoginResponse response) {
        receivedLoginResponse = true;
        loginResponse = response;
        state = State.CONNECTED;
    }

    public void Connect() {
        StartListener();
        Login login = new Login {
            name = playerName
        };
        SendPackage(ID_LOGIN, GetBytes(login), address);
        state = State.WAITING_FOR_RESPONSE;
    }

    enum State {
        WAITING_FOR_RESPONSE,
        CONNECTED,
        IDLING
    }
}

public class NetworkingManager : MonoBehaviour {

    public int framesPerTick = 60;
    ClientNetworker clientNetworker;
    public GameObject playerPrefab;
    public GameObject playerObject;

    void Start() {
        GameObject.FindGameObjectWithTag("Respawn").GetComponent<Text>().text = "Empty";
        string playerName = GameObject.FindGameObjectWithTag("ButtonManager").GetComponent<ButtonClick>().playerName;
        string ip = GameObject.FindGameObjectWithTag("ButtonManager").GetComponent<ButtonClick>().ip;
        clientNetworker = new ClientNetworker(playerName, ip, playerPrefab, ref playerObject);
        clientNetworker.Connect();
    }

    void Update() {

    }

    int counter = 0;

    private void FixedUpdate() {
        counter++;
        if (counter == framesPerTick) {

            // SendPackage(bytes, address);
            clientNetworker.ClientUpdate();
            counter = 0;
        }
    }
}
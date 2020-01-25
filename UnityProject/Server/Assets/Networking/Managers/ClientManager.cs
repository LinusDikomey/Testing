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
    LoginResponse loginResponse;
    bool receivedLoginResponse = false;
    State state = State.IDLING;
    string playerName;
    GameObject playerPrefab;
    GameObject playerObject;
    uint lastProcessedTick = 0;

    ClientBoundData lastClientBound = new ClientBoundData(0, new ComponentPacket[0]);
    NetPlayer.PlayerInput input;
    private Dictionary<uint, NetBehaviour> netComponents;

    uint clientID = 9999;

    public ClientNetworker(string playerName, string addressString, GameObject playerPrefab, ref GameObject playerObject, ref Dictionary<uint, NetBehaviour> netComponents) : base(NetConstants.PORT) {
        address = IPAddress.Parse(addressString);
        this.playerName = playerName;
        this.playerPrefab = playerPrefab;
        this.playerObject = playerObject;
        this.netComponents = netComponents;
    }

    public void Tick(uint tick) {
        switch (state) {
            case State.IDLING:
                break;
            case State.WAITING_FOR_RESPONSE:
                if (receivedLoginResponse) {
                    GameObject.FindGameObjectWithTag("Respawn").GetComponent<Text>().text = "LoginResponse: " + loginResponse.response + "; " + loginResponse.msg;
                }
                break;
            case State.CONNECTED:
                ConnectedTick(tick);
                break;
        }
        
    }

    private void ConnectedTick(uint tick) {
        Dictionary<uint, byte[]> componentUpdatePackets = new Dictionary<uint, byte[]>();
        foreach (ComponentPacket compPacket in lastClientBound.componentUpdates) {
            componentUpdatePackets.Add(compPacket.componentID, compPacket.bytes);
            if(!netComponents.ContainsKey(compPacket.componentID)) {
                //error
                Debug.Log("Error, data with invalid component id was parsed");
            } else {
                netComponents[compPacket.componentID].ClientUpdate(compPacket.bytes);
            }
        }
        foreach (KeyValuePair<uint, NetBehaviour> netComp in netComponents) {
            netComp.Value.ClientUpdate(componentUpdatePackets[netComp.Key]);
        }
        CreateAndSendClientPackage();
    }

    private void CreateAndSendClientPackage() {
        ServerBoundData package = new ServerBoundData(clientID, lastProcessedTick, input);
        SendPackage(ID_SERVER_BOUND, PackageSerializer.GetBytes(package), address);
    }

    private void HandleClientBoundPackage(ClientBoundData package) {
        lastClientBound = package;
        lastProcessedTick = package.tick;
    }

    public override void PacketReceived(byte[] bytes, IPEndPoint endPoint) {
        byte[] objectBytes = new byte[bytes.Length - 1];
        Array.Copy(bytes, 1, objectBytes, 0, bytes.Length-1);
        switch (bytes[0]) {
            case ID_LOGIN_RESPONSE:
                LoginResponse response = PackageSerializer.GetObject<LoginResponse>(objectBytes);
                loginResponse = response;
                Debug.Log("Response received " + response);
                HandleLoginResponse(response);
                break;
            case ID_CLIENT_BOUND:
                ClientBoundData clientBound = PackageSerializer.GetObject<ClientBoundData>(objectBytes);
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
        clientID = response.clientID;
        state = State.CONNECTED;
    }

    public void Connect() {
        StartListener();
        Login login = new Login {
            name = playerName
        };
        SendPackage(ID_LOGIN, PackageSerializer.GetBytes(login), address);
        state = State.CONNECTED; //SHOULD BE WAITING FOR --------------------
    }

    public void SetInput(NetPlayer.PlayerInput input) {
        this.input = input;
    }

    enum State {
        WAITING_FOR_RESPONSE,
        CONNECTED,
        IDLING
    }
}

public class ClientManager : NetManager {

    public ClientManager() : base(Side.CLIENT) {}

    ClientNetworker clientNetworker;
    public GameObject playerPrefab;
    public GameObject playerObject;

    new void Start() {
        base.Start();
        GameObject.FindGameObjectWithTag("Respawn").GetComponent<Text>().text = "Empty";
        string playerName = GameObject.FindGameObjectWithTag("ButtonManager").GetComponent<ButtonClick>().playerName;
        string ip = GameObject.FindGameObjectWithTag("ButtonManager").GetComponent<ButtonClick>().ip;
        clientNetworker = new ClientNetworker(playerName, ip, playerPrefab, ref playerObject, ref netComponents);
        clientNetworker.Connect();
    }

    private void OnApplicationQuit() {
        clientNetworker.Terminate();
    }

    public void SetInput(NetPlayer.PlayerInput input) {
        clientNetworker.SetInput(input);
    }

    public override void Tick(uint tick) {
        clientNetworker.Tick(tick);
    }
}
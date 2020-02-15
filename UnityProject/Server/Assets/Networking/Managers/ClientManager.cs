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
using System.Net.Sockets;

public class ClientManager : NetManager {

    private const int RTT_AVERAGE_COUNT = 20;
    private const int DEVIATION_TOLERANCE = 100;
    private const short RTT_SAFETY_BUFFER = 50; //MS

    enum State {
        WAITING_FOR_RESPONSE,
        CONNECTED,
        IDLING
    }

    public ClientManager() : base(Side.CLIENT) {

    }

    Networker networker;
    private PlayerInput input = new PlayerInput(false, false, false, false);

    LoginResponse loginResponse;
    bool receivedLoginResponse = false;
    State state = State.IDLING;
    string playerName;
    uint lastProcessedTick = 0;
    uint clientID;
    //uint[] lastRTTs = new uint[RTT_AVERAGE_COUNT];
    //uint rttAverage;

    uint connectedTickCounter;

    Dictionary<uint, long> sentPackageTimestamps = new Dictionary<uint, long>();
    string debug;

    IPAddress address;
    volatile List<ClientBoundData> clientBoundReceived = new List<ClientBoundData>();

    new void Start() {
        base.Start();
        if (GameObject.FindGameObjectWithTag("ButtonManager") == null)
            Application.Quit();
        playerName = GameObject.FindGameObjectWithTag("ButtonManager").GetComponent<ButtonClick>().playerName;
        string ip = GameObject.FindGameObjectWithTag("ButtonManager").GetComponent<ButtonClick>().ip;
        networker = new Networker(GetFreeTcpPort(), NetConstants.PORT, PacketReceived);
        address = IPAddress.Parse(ip);
        Debug.Log("ClientNetworker start");
    }

    public void Connect() {
        Debug.Log("connecting");
        GameObject.Destroy(GameObject.FindGameObjectWithTag("ConnectMenu"));
        networker.StartListener();
        Login login = new Login(playerName);

        networker.SendPacket(ID_LOGIN, PackageSerializer.GetBytes(login), address);
        state = State.WAITING_FOR_RESPONSE;
    }

    public void PacketReceived(byte[] bytes, IPEndPoint endPoint) {
        Debug.LogFormat("packet received: {0}, from {1}", PackageSerializer.encoding.GetString(bytes), endPoint);
        byte[] objectBytes = new byte[bytes.Length - 1];
        Array.Copy(bytes, 1, objectBytes, 0, bytes.Length - 1);
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

    private void CreateAndSendClientPackage() {
        ServerBoundData package = new ServerBoundData(clientID, tick, lastProcessedTick, input);
        sentPackageTimestamps.Add(tick, GetTimestamp());
        networker.SendPacket(ID_SERVER_BOUND, PackageSerializer.GetBytes(package), address);
    }



    private void HandleLoginResponse(LoginResponse response) {
        receivedLoginResponse = true;
        loginResponse = response;
        clientID = response.clientID;
        state = State.CONNECTED;
    }

    uint lastSyncedTick = 0;
    string noToleranceText = "";

    private void HandleClientBoundPackage(ClientBoundData package) {
        clientBoundReceived.Add(package);
        uint rtt;
        if(package.lastReceivedTick != 0) {
            rtt = (uint) (GetTimestamp() - sentPackageTimestamps[package.lastReceivedTick] - package.timeSinceTick);
            long targetTicksMillis = package.tick * TICKRATE + rtt + RTT_SAFETY_BUFFER; //should be + bufferedRTTAverage
            long clientTicksMillis = tick * TICKRATE + (GetTimestamp() - lastTickTimestamp);
            
            debug = "\n tTick: " + targetTicksMillis / TICKRATE;
            long difference = clientTicksMillis - targetTicksMillis;
            debug += " | difference: " + difference;

            if(Math.Abs(difference) > DEVIATION_TOLERANCE) {
                SyncTick((uint) (targetTicksMillis / TICKRATE), GetTimestamp() - targetTicksMillis % TICKRATE);
                lastSyncedTick = tick;
            }
            debug += "\n Last sync: " + lastSyncedTick + ", " + noToleranceText;
        }
    }
    private void OnApplicationQuit() {
        networker.Terminate();
    }

    public void SetInput(PlayerInput input) {
        this.input = input;
    }

    private int GetFreeTcpPort() {
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    public override void Tick(uint tick) {
        GameObject.FindGameObjectWithTag("Debug2").GetComponent<Text>().text = debug;
        switch (state) {
            case State.IDLING:
                break;
            case State.WAITING_FOR_RESPONSE:
                if (receivedLoginResponse) {
                    GameObject.FindGameObjectWithTag("Respawn").GetComponent<Text>().text = "LoginResponse: " + loginResponse.response + "; " + loginResponse.msg;
                }
                break;
            case State.CONNECTED:
                try {
                    ConnectedTick(tick);
                } catch (InvalidOperationException e) {
                    Debug.LogError("[caught] InvalidOperationException: " + e);
                }
                
                break;
        }
    }

    private void ConnectedTick(uint tick) {
        input = new PlayerInput(false, false, false, false);
        Dictionary<uint, byte[]> componentUpdatePackets = new Dictionary<uint, byte[]>();

        ClientBoundData[] packetCopy = clientBoundReceived.ToArray();
        
        foreach (ClientBoundData data in packetCopy) {
            foreach (uint destroyID in data.objDestroys) {
                GameObject.Destroy(netIdentities[destroyID].gameObject);
            }
            foreach (ObjectInitializer objInit in data.objInits) {
                if(netIdentities.ContainsKey(objInit.id)) {
                    Debug.LogError("Already existing id was tried to create. Id: " + objInit.id);
                    continue;
                }
                GameObject prefab = Prefabs.prefabs[objInit.prefab];
                GameObject init = GameObject.Instantiate(prefab);
                init.GetComponent<NetIdentity>().Create(objInit.id, objInit.prefab);
            }

            foreach (ObjectPacket obj in data.objUpdates) {
                if (!netIdentities.ContainsKey(obj.objID)) {
                    Debug.LogError("Nonexistent object update received: " + obj.objID);
                    continue;
                }
                netIdentities[obj.objID].ClientTick(obj.compPackets, ref input);
            }
            lastProcessedTick = data.tick;
        }
        clientBoundReceived.Clear();
        CreateAndSendClientPackage();
    }
}
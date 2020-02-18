using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.Net;
using Package;
using static Package.PackageID;
using System.Net.Sockets;

public class ClientManager : NetManager {

    private const int DEVIATION_AVERAGE_COUNT = 10;
    private const int DEVIATION_TOLERANCE = 15 * 5;
    private const short RTT_SAFETY_BUFFER = 25 * 5; //should probably be larger than deviation tolarance

    enum State {
        WAITING_FOR_RESPONSE,
        CONNECTED,
        IDLING
    }

    struct Frame {
        public Dictionary<uint, ObjectData> objectData;

        public Frame(Dictionary<uint, ObjectData> objectData) {
            this.objectData = objectData;
        }
    }

    public ClientManager() : base(Side.CLIENT) {

    }

    Networker networker;

    LoginResponse loginResponse;
    bool receivedLoginResponse = false;
    State state = State.IDLING;
    string playerName;
    uint clientID;
    long[] deviations = new long[DEVIATION_AVERAGE_COUNT];

    Dictionary<uint, long> sentPackageTimestamps = new Dictionary<uint, long>();
    string debug;

    IPAddress address;
    volatile Dictionary<uint, ClientBoundData> clientBoundReceived = new Dictionary<uint, ClientBoundData>();


    Dictionary<uint, PlayerInput> unconfirmedInputs = new Dictionary<uint, PlayerInput>();
    Dictionary<uint, Frame> unconfirmedFrames = new Dictionary<uint, Frame>(); //data at start of frame
    uint lastConfirmedTick = 0;

    new void Start() {
        base.Start();
        if (GameObject.FindGameObjectWithTag("ButtonManager") == null)
            Application.Quit();
        playerName = GameObject.FindGameObjectWithTag("ButtonManager").GetComponent<ButtonClick>().playerName;
        string ip = GameObject.FindGameObjectWithTag("ButtonManager").GetComponent<ButtonClick>().ip;
        networker = new Networker(NetConstants.PORT, PacketReceived); //Use automatically provided port
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
                Debug.LogError("Invalid package id received, type id: " + bytes[0]);
                break;
        }
    }

    private void CreateAndSendClientPackage() {
        ServerBoundData package = new ServerBoundData(clientID, tick, lastConfirmedTick, unconfirmedInputs[tick]);
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

    private void HandleClientBoundPackage(ClientBoundData package) {
        clientBoundReceived.Add(package.tick, package);
        uint rtt;
        if(package.lastReceivedTick != 0) {
            rtt = (uint) (GetTimestamp() - sentPackageTimestamps[package.lastReceivedTick] - package.timeSinceTick);
            long targetTicksMillis = package.tick * TICKRATE + rtt + RTT_SAFETY_BUFFER; //should be + bufferedRTTAverage
            long clientTicksMillis = tick * TICKRATE + (GetTimestamp() - lastTickTimestamp);
            
            debug = "\n tTick: " + targetTicksMillis / TICKRATE;
            long deviation = clientTicksMillis - targetTicksMillis;
            debug += " | deviation: " + deviation + ", rtt: " + rtt;

            if(Math.Abs(deviation) > DEVIATION_TOLERANCE) {
                SyncTick((uint) (targetTicksMillis / TICKRATE), GetTimestamp() - targetTicksMillis % TICKRATE);
                lastSyncedTick = tick;
            }
            debug += "\n Last sync: " + lastSyncedTick;
        }
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
                    ApplyServerUpdates();
                    unconfirmedFrames.Clear(); //no unconfirmed frames currently existing, PredictToCurrentTick will recreate them
                    ClientTick();
                    PredictToCurrentTick();
                    CreateAndSendClientPackage();
                } catch (InvalidOperationException e) {
                    Debug.LogError("[caught] InvalidOperationException: " + e);
                }
                
                break;
        }
    }

    private void ClientTick() {
        PlayerInput thisInput = new PlayerInput();
        foreach(NetIdentity netIdentity in netIdentities.Values) {
            netIdentity.ClientTick(ref thisInput);
        }
        unconfirmedInputs.Add(tick, thisInput);
    }

    private void PredictToCurrentTick() {
        for(uint predictTick = lastConfirmedTick + 1; predictTick <= tick; predictTick++) {
            Dictionary<uint, PlayerInput> clientInput = new Dictionary<uint, PlayerInput>();
            clientInput.Add(clientID, unconfirmedInputs[predictTick]);

            Dictionary<uint, ObjectData> objectData = new Dictionary<uint, ObjectData>();
            foreach (NetIdentity netIdentity in netIdentities.Values) {
                ObjectUpdatePacket objUpdate = netIdentity.ServerTick(ref clientInput);
                netIdentity.ApplyUpdates(objUpdate.compPackets);
                objectData.Add(netIdentity.id, netIdentity.GetObjectData());
            }

            unconfirmedFrames.Add(predictTick, new Frame(objectData));
        }
    }

    private void ApplyServerUpdates() {
        Dictionary<uint, byte[]> componentUpdatePackets = new Dictionary<uint, byte[]>();

        ClientBoundData[] packetCopy = new ClientBoundData[clientBoundReceived.Count];
        clientBoundReceived.Values.CopyTo(packetCopy, 0);

        foreach (ClientBoundData packet in packetCopy) {
            //Frame unconfirmedFrame = unconfirmedFrames[packet.tick];
            ApplyNetworkUpdates(packet.objUpdates, packet.objInits, packet.objDestroys, packet.tick);

            lastConfirmedTick = packet.tick;
            unconfirmedFrames.Remove(packet.tick);
            unconfirmedInputs.Remove(packet.tick);
        }
        clientBoundReceived.Clear();
    }

    void ApplyNetworkUpdates(ObjectUpdatePacket[] objectUpdates, ObjectInitializer[] objectInits, uint[] objectsDestroyed, uint tick) {
        foreach (uint destroyID in objectsDestroyed) {
            if (netIdentities.ContainsKey(destroyID))
                GameObject.Destroy(netIdentities[destroyID].gameObject);
        }
        foreach (ObjectInitializer objInit in objectInits) {
            if (netIdentities.ContainsKey(objInit.id)) {
                netIdentities.Remove(objInit.id);
            }
            InstantiateNetworked(objInit.prefab, objInit.id);
        }

        List<uint> updatedIds = new List<uint>();
        foreach (ObjectUpdatePacket updateObject in objectUpdates) {
            if (!netIdentities.ContainsKey(updateObject.objID)) {
                Frame tickData;
                if(unconfirmedFrames.TryGetValue(tick, out tickData)) {
                    ObjectData objData;
                    if(tickData.objectData.TryGetValue(updateObject.objID, out objData)) {
                        InstantiateNetworked(objData.prefab, updateObject.objID); //object was removed later, readd
                    }else {
                        Debug.LogError("Nonexistent object update received, checked frame data, not found: " + updateObject.objID);
                        continue;
                    }
                } else {
                    Debug.LogError("Nonexistent object update received: " + updateObject.objID);
                }
                continue;
            }
            netIdentities[updateObject.objID].ApplyUpdates(updateObject.compPackets);
        }
    }
}
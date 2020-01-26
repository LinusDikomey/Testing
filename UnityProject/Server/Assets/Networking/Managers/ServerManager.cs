﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Package;
using static Package.PackageID;
using UnityEngine.UI;

public class ServerManager : NetManager {

    private struct ConnectedClient {
        public Vector3 position;
        public IPEndPoint endPoint;
        public string name;

        public ConnectedClient(Vector3 position, IPEndPoint endPoint, string name) {
            this.position = position;
            this.endPoint = endPoint;
            this.name = name;
        }
    }

    public GameObject playerPrefab;

    Dictionary<uint, IPEndPoint> connectedClients = new Dictionary<uint, IPEndPoint>();
    Dictionary<uint, PlayerInput> playerInputs = new Dictionary<uint, PlayerInput>();
    Dictionary<Login, IPEndPoint> loginQueue = new Dictionary<Login, IPEndPoint>();

    Networker networker;

    private List<uint> netObjectsLastTick = new List<uint>();

    uint nextID;

    public ServerManager() : base(Side.SERVER) {
        networker = new Networker(NetConstants.PORT, NetConstants.PORT, (byte[] bytes, IPEndPoint packetReceived) => PacketReceived(bytes, packetReceived));
    }

    new public void Start() {
        base.Start();
        networker.StartListener();
    }

    private void OnApplicationQuit() {
        networker.Terminate();
    }

    public GameObject CreateObject(string prefab, uint id) {
        GameObject obj = GameObject.Instantiate(Prefabs.prefabs[prefab]);
        obj.GetComponent<NetIdentity>().Create(id, prefab);
        return obj;
    }

    public override void Tick(uint tick) {
        foreach (KeyValuePair<Login, IPEndPoint> queueItem in loginQueue) {
            uint id = GetFreeID();
            LoginResponse response = new LoginResponse(Response.LOGIN_OK, "Login success!", id);
            networker.SendPacket(ID_LOGIN_RESPONSE, PackageSerializer.GetBytes(response), queueItem.Value);
            connectedClients.Add(id, queueItem.Value);
            CreateObject("player", id).GetComponent<Transform>().position = new Vector3();
        }
        loginQueue.Clear();

        ObjectPacket[] objUpdates = new ObjectPacket[netIdentities.Count];
        List<ObjectInitializer> objInits = new List<ObjectInitializer>();
        int index = 0;
        foreach (KeyValuePair<uint, NetIdentity> netIDPair in netIdentities) {
            objUpdates[index++] = netIDPair.Value.ServerTick();
            if(!netObjectsLastTick.Contains(netIDPair.Key)) {
                objInits.Add(new ObjectInitializer(netIDPair.Key, netIDPair.Value.prefab));
            }
        }
        List<uint> destroyedObjects = new List<uint>();
        foreach(uint id in netObjectsLastTick) {
            if (!netIdentities.ContainsKey(id))
                destroyedObjects.Add(id);
        }
        netObjectsLastTick.Clear();
        foreach(uint id in netIdentities.Keys) {
            netObjectsLastTick.Add(id);
        }
        ClientBoundData packet = new ClientBoundData(tick, objInits.ToArray(), destroyedObjects.ToArray(), objUpdates);
        foreach (KeyValuePair<uint, IPEndPoint> currentClient in connectedClients) {
            networker.SendPacket(ID_CLIENT_BOUND, PackageSerializer.GetBytes(packet), currentClient.Value);
        }
    }

    public void PacketReceived(byte[] bytes, IPEndPoint endPoint) {
        Debug.LogFormat("packet received: {0}, from {1}", PackageSerializer.encoding.GetString(bytes), endPoint);
        byte[] objectBytes = new byte[bytes.Length - 1];

        Array.Copy(bytes, 1, objectBytes, 0, bytes.Length - 1);
        switch (bytes[0]) {
            case ID_LOGIN:
                Login loginPackage = PackageSerializer.GetObject<Login>(objectBytes);
                HandleLogin(loginPackage, endPoint);
                break;
            case ID_SERVER_BOUND:
                ServerBoundData serverBoundPackage = PackageSerializer.GetObject<ServerBoundData>(objectBytes);
                HandleServerBound(serverBoundPackage, endPoint);
                break;
        }
    }

    private void HandleServerBound(ServerBoundData package, IPEndPoint endPoint) {
        IPEndPoint realConnection;
        if (!connectedClients.TryGetValue(package.clientId, out realConnection)) {
            Debug.LogError("Invalid client id sent server bound package");
            return;
        }
        if (!realConnection.Equals(endPoint)) {
            Debug.LogError("Server bound package from invalid ip received");
            return;
        }
        //valid client
        playerInputs.Remove(package.clientId);
        playerInputs.Add(package.clientId, package.input);
    }

    private void HandleLogin(Login login, IPEndPoint endPoint) {
        loginQueue.Add(login, endPoint);
        Debug.Log("Login detected: " + endPoint);
    }

    public bool PlayerInputExists(uint playerComponentID) {
        return playerInputs.ContainsKey(playerComponentID);
    }

    public PlayerInput GetPlayerInput(uint playerComponentID) {
        PlayerInput input;
        playerInputs.TryGetValue(playerComponentID, out input);
        return input;
    }
}
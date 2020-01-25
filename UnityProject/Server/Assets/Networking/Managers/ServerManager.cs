using System.Collections;
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

public class ServerNetworker : Networker {

    private struct ConnectedPlayer {
        public Vector3 position;
        public IPEndPoint endPoint;
        public string name;

        public ConnectedPlayer(Vector3 position, IPEndPoint endPoint, string name) {
            this.position = position;
            this.endPoint = endPoint;
            this.name = name;
        }
    }

    GameObject playerPrefab;

    Dictionary<uint, IPEndPoint> connectedClients = new Dictionary<uint, IPEndPoint>();
    Dictionary<uint, NetBehaviour> netComponents;
    Dictionary<uint, NetPlayer.PlayerInput> playerInputs = new Dictionary<uint, NetPlayer.PlayerInput>();
    Dictionary<Login, IPEndPoint> loginQueue = new Dictionary<Login, IPEndPoint>();

    uint nextID  = 100;

    public ServerNetworker(GameObject playerPrefab, ref Dictionary<uint, NetBehaviour> netComponents, ref uint nextID) : base(NetConstants.PORT) {
        this.playerPrefab = playerPrefab;
        this.netComponents = netComponents;
        //this.nextID = nextID;
        StartListener();
    }

    public void Tick(uint tick) {
        foreach(KeyValuePair<Login, IPEndPoint> queueItem in loginQueue) {
            GameObject instance = GameObject.Instantiate(playerPrefab);
            uint id = nextID++;
            instance.GetComponent<NetPlayer>().SetID(id);
            //netComponents.Add(id, instance.GetComponent<NetPlayer>());
            LoginResponse response = new LoginResponse(Response.LOGIN_OK, "Login success!", id);
            SendPackage(ID_LOGIN_RESPONSE, PackageSerializer.GetBytes(response), queueItem.Value);
            connectedClients.Add(id, queueItem.Value);
        }
        loginQueue.Clear();

        List<ComponentPacket> compPacketList = new List<ComponentPacket>();
        foreach(KeyValuePair<uint, NetBehaviour> netIDPair in netComponents) {
            byte[] bytes = netIDPair.Value.ServerUpdate();
            compPacketList.Add(new ComponentPacket(netIDPair.Key, bytes));
        }
        ClientBoundData packet = new ClientBoundData(tick, compPacketList.ToArray());

        foreach (KeyValuePair<uint, IPEndPoint> currentClient in connectedClients) {
            SendPackage(ID_CLIENT_BOUND, PackageSerializer.GetBytes(packet), currentClient.Value);
        }
    }

    public override void PacketReceived(byte[] bytes, IPEndPoint endPoint) {
        byte[] objectBytes = new byte[bytes.Length - 1];

        Array.Copy(bytes, 1, objectBytes, 0, bytes.Length -1);
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
        if(!connectedClients.TryGetValue(package.clientId, out realConnection)) {
            Debug.LogError("Invalid client id sent server bound package");
            return;
        }
        if(!realConnection.Equals(endPoint)) {
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

    /*public void SetInput(NetPlayer.PlayerInput input, uint playerComponentID) {
        playerInputs.Add(playerComponentID, input);
    }*/

    public bool PlayerInputExists(uint playerComponentID) {
        return playerInputs.ContainsKey(playerComponentID);
    }

        public NetPlayer.PlayerInput GetPlayerInput(uint playerComponentID) {
        NetPlayer.PlayerInput input;
        playerInputs.TryGetValue(playerComponentID, out input);
        return input;
    }
}

public class ServerManager : NetManager {

    public GameObject playerPrefab;

    public ServerManager() : base(Side.SERVER) {}

    public ServerNetworker serverNetworker;

    new public void Start() {
        base.Start();
        serverNetworker = new ServerNetworker(playerPrefab, ref netComponents, ref nextID);
    }

    private void OnApplicationQuit() {
        serverNetworker.Terminate();
    }

    public override void Tick(uint tick) {
        serverNetworker.Tick(tick);
    }
}
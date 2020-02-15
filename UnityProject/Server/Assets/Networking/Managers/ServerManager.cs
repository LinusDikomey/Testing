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

public class ServerManager : NetManager {

    private struct FrameInputs {
        public Dictionary<uint, PlayerInput> playerInputs;

        public FrameInputs(Dictionary<uint, PlayerInput> inputs) {
            playerInputs = inputs;
        }
    }

    private struct ConnectedClient {
        public uint id;
        public IPEndPoint endPoint;
        public string name;
        public bool receivedState;
        public uint lastClientTick;
        public long lastTickTime;

        public ConnectedClient(uint id, IPEndPoint endPoint, string name) {
            this.id = id;
            this.endPoint = endPoint;
            this.name = name;
            receivedState = false;
            lastClientTick = 0;
            lastTickTime = 0;
        }
    }

    Dictionary<uint, ConnectedClient> connectedClients = new Dictionary<uint, ConnectedClient>();
    Dictionary<uint, FrameInputs> frameInputs = new Dictionary<uint, FrameInputs>();
    Dictionary<Login, IPEndPoint> loginQueue = new Dictionary<Login, IPEndPoint>();

    Networker networker;

    private List<uint> netObjectsLastTick = new List<uint>();

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
        //login queue
        foreach (KeyValuePair<Login, IPEndPoint> queueItem in loginQueue) {
            uint id = GetFreeID();
            LoginResponse response = new LoginResponse(Response.LOGIN_OK, "Login success!", id);
            networker.SendPacket(ID_LOGIN_RESPONSE, PackageSerializer.GetBytes(response), queueItem.Value);
            connectedClients.Add(id, new ConnectedClient(id, queueItem.Value, queueItem.Key.name));
            GameObject player = GameObject.Instantiate(Prefabs.prefabs["player"]);
            player.GetComponent<NetIdentity>().Create(id, "player");
            NetPlayer netPlayer = (NetPlayer) player.GetComponent<NetIdentity>().netAttributes[0];
            netPlayer.name = queueItem.Key.name;

        }
        loginQueue.Clear();

        //Update packets
        ObjectPacket[] objUpdates = new ObjectPacket[netIdentities.Count];
        List<ObjectInitializer> objInits = new List<ObjectInitializer>();
        int index = 0;
        FrameInputs inputsStruct = frameInputs.ContainsKey(tick) ? frameInputs[tick] : new FrameInputs(new Dictionary<uint, PlayerInput>());
        foreach (KeyValuePair<uint, NetIdentity> netIDPair in netIdentities) {
            objUpdates[index++] = netIDPair.Value.ServerTick(ref inputsStruct.playerInputs);
            if(!netObjectsLastTick.Contains(netIDPair.Key)) {
                objInits.Add(new ObjectInitializer(netIDPair.Key, netIDPair.Value.prefab));
            }
        }
        //destroy packets
        List<uint> destroyedObjects = new List<uint>();
        foreach(uint id in netObjectsLastTick) {
            if (!netIdentities.ContainsKey(id))
                destroyedObjects.Add(id);
        }
        netObjectsLastTick.Clear();
        foreach(uint id in netIdentities.Keys) {
            netObjectsLastTick.Add(id);
        }

        //create and send packets
        foreach (KeyValuePair<uint, ConnectedClient> currentClient in connectedClients) {
            List<ObjectInitializer> currentObjInit = new List<ObjectInitializer>(); //HIER IST KAPUTT WENN KAPUTT
            if (!currentClient.Value.receivedState) {
                currentObjInit.Clear();
                foreach (KeyValuePair<uint, NetIdentity> identity in netIdentities) {
                    //if (identity.Key == currentClient.Key) continue;
                    currentObjInit.Add(new ObjectInitializer(identity.Key, identity.Value.prefab));
                }

                ConnectedClient connected = connectedClients[currentClient.Key];
                connected.receivedState = true;
                connectedClients.Remove(currentClient.Key);
                connectedClients.Add(currentClient.Key, connected);
            } else {
                foreach(ObjectInitializer current in objInits) {
                    currentObjInit.Add(current);
                }
            }
            uint timeSinceReceived = (uint) (GetTimestamp() - currentClient.Value.lastTickTime);
            ClientBoundData packet = new ClientBoundData(tick, currentClient.Value.lastClientTick, timeSinceReceived, currentObjInit.ToArray(), destroyedObjects.ToArray(), objUpdates);
            networker.SendPacket(ID_CLIENT_BOUND, PackageSerializer.GetBytes(packet), currentClient.Value.endPoint);
        }
    }

    public void PacketReceived(byte[] bytes, IPEndPoint endPoint) {
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
        ConnectedClient connected;
        if (!connectedClients.TryGetValue(package.clientId, out connected)) {
            Debug.LogError("Invalid client id sent server bound package");
            return;
        }
        if (!connected.endPoint.Equals(endPoint)) {
            Debug.LogError("Server bound package from invalid ip received");
            return;
        }
        //valid client
        if (!frameInputs.ContainsKey(package.tick)) 
            frameInputs[package.tick] = new FrameInputs(new Dictionary<uint, PlayerInput>());
        frameInputs[package.tick].playerInputs[package.clientId] = package.input;
        connected.lastClientTick = package.tick;
        connected.lastTickTime = GetTimestamp();
        connectedClients[package.clientId] = connected;
    }

    private void HandleLogin(Login login, IPEndPoint endPoint) {
        loginQueue.Add(login, endPoint);
        Debug.Log("Login detected: " + endPoint);
    }
}
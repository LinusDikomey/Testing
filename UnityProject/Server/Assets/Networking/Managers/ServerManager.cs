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

    Dictionary<string, ConnectedPlayer> connectedPlayers = new Dictionary<string, ConnectedPlayer>();

    public ServerNetworker() : base(NetConstants.PORT) {
        StartListener();
    }

    public void ServerUpdate(int tick) {
        GameObject.FindGameObjectWithTag("Respawn").GetComponent<Text>().text = "Connected : " + connectedPlayers.Count + ", last tick: " + tick;
        //send update package to all connected clients
        foreach (KeyValuePair<string, ConnectedPlayer> currentClient in connectedPlayers) {
            Player[] otherPlayers = new Player[connectedPlayers.Count - 1]; //make list of other players without self
            int index = 0;
            foreach (KeyValuePair<string, ConnectedPlayer> otherPlayerEntry in connectedPlayers) {
                if(!otherPlayerEntry.Key.Equals(currentClient.Key)) {
                    otherPlayers[index] = new Player(otherPlayerEntry.Value.name, otherPlayerEntry.Value.position);
                    index++;
                }
            }
            ClientBoundData clientPackage = new ClientBoundData(tick, otherPlayers);
            SendPackage(ID_CLIENT_BOUND, GetBytes(clientPackage), currentClient.Value.endPoint);
        }
        Debug.Log("Sent update package to " + connectedPlayers.Count  + " clients");
    }

    public override void PacketReceived(byte[] bytes, IPEndPoint endPoint) {
        byte[] objectBytes = new byte[bytes.Length - 1];

        Array.Copy(bytes, 1, objectBytes, 0, bytes.Length -1);
        switch (bytes[0]) {
            case ID_LOGIN:
                Login loginPackage = GetObject<Login>(objectBytes);
                HandleLogin(loginPackage, endPoint);
                break;
            case ID_SERVER_BOUND:
                ServerBoundData serverBoundPackage = GetObject<ServerBoundData>(objectBytes);
                HandleServerBound(serverBoundPackage, endPoint);
                break;
        }
    }

    private void HandleServerBound(ServerBoundData package, IPEndPoint endPoint) {
        ConnectedPlayer player;
        if(!connectedPlayers.TryGetValue(package.player.name, out player)) {
            Debug.Log("non-connected player sent update package!");
            return;
        }
        if(!package.player.name.Equals(player.name)) {
            Debug.Log("Player sent update package with wrong player name!");
            return;
        }
        player.position = package.player.position;
        connectedPlayers[package.player.name] = player;
    }

    private void HandleLogin(Login login, IPEndPoint endPoint) {
        Debug.Log("Login detected: " + endPoint);
        LoginResponse response = new LoginResponse() {
            response = Response.LOGIN_OK,
            msg = "Login sucess!"
        };
        SendPackage(ID_LOGIN_RESPONSE, GetBytes(response), endPoint);
        connectedPlayers.Add(login.name, new ConnectedPlayer(new Vector3(), endPoint, login.name));
    }
}

public class ServerManager : MonoBehaviour {

    public int framesPerTick = 60;
    private Socket udpSocket;
    private Thread listener;

    ServerNetworker serverNetworker;

    void Start() {
        serverNetworker = new ServerNetworker();
    }

    int counter = 0;
    int tickCounter = 0;
    private void FixedUpdate() {
        counter++;
        if(counter == framesPerTick) {
            serverNetworker.ServerUpdate(tickCounter++);
            counter = 0;
        }
    }
}
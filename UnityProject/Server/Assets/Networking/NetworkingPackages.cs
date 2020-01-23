using System;
using UnityEngine;

public class NetConstants {
    public const int PORT = 27015;
}
//
namespace Package {

    public class PackageID {
        public const byte 
            ID_CLIENT_BOUND = 1,
            ID_SERVER_BOUND = 4,
            ID_LOGIN = 2,
            ID_LOGIN_RESPONSE = 3;
    }

    [Serializable]
    public struct ServerBoundData {
        public int clientId;
        public int lastProcessedTick;
        public Player player;

        public ServerBoundData(int clientId, int lastProcessedTick, Player player) {
            this.clientId = clientId;
            this.lastProcessedTick = lastProcessedTick;
            this.player = player;
        }
    }

    [Serializable]
    public struct ClientBoundData {
        public int tick;
        public Player[] netPlayers;

        public ClientBoundData(int tick, Player[] netPlayers) {
            this.tick = tick;
            this.netPlayers = netPlayers;
        }
    }

    [Serializable]
    public struct Player {
        public string name;
        public Vector3 position;

        public Player(string name, Vector3 position) {
            this.name = name;
            this.position = position;
        }
    }

    [Serializable]
    public struct Login {
        public string name;

        public Login(string name) {
            this.name = name;
        }
    }

    [Serializable]
    public struct LoginResponse {
        public byte response;   //response
        public string msg;      //message

        public LoginResponse(byte response, string msg) {
            this.response = response;
            this.msg = msg;
        }
    }
    
    public class Response {
        public const byte LOGIN_OK = 0, LOGIN_ERROR = 1;
}
    
}

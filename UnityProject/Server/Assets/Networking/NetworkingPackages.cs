using System;
using UnityEngine;

public class NetConstants {
    public const int PORT = 27015;
}

namespace Package {

    public class PackageID {
        public const byte
            ID_CLIENT_BOUND = 1,
            ID_SERVER_BOUND = 2,
            ID_LOGIN = 3,
            ID_LOGIN_RESPONSE = 4;
    }

    [Serializable]
    public struct ServerBoundData {
        public uint clientId;
        public uint lastProcessedTick;
        public NetPlayer.PlayerInput input;

        public ServerBoundData(uint clientId, uint lastProcessedTick, NetPlayer.PlayerInput input) {
            this.clientId = clientId;
            this.lastProcessedTick = lastProcessedTick;
            this.input = input;
        }
    }

    [Serializable]
    public struct ClientBoundData {
        public uint tick;
        public ComponentPacket[] componentUpdates;

        public ClientBoundData(uint tick, ComponentPacket[] componentUpdates) {
            this.tick = tick;
            this.componentUpdates = componentUpdates;
        }
    }

    [Serializable]
    public struct ComponentPacket {
        public uint componentID;
        public byte[] bytes;

        public ComponentPacket(uint componentID, byte[] bytes) {
            this.componentID = componentID;
            this.bytes = bytes;
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
        public uint clientID;

        public LoginResponse(byte response, string msg, uint clientID) {
            this.response = response;
            this.msg = msg;
            this.clientID = clientID;
        }
    }

    public class Response {
        public const byte LOGIN_OK = 0, LOGIN_ERROR = 1;
    }


    public class PackageSerializer {
        public static System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

        public static T GetObject<T>(byte[] bytes) {
            T t = JsonUtility.FromJson<T>(encoding.GetString(bytes));
            return t;
        }

        public static byte[] GetBytes(object obj) {
            string json = JsonUtility.ToJson(obj);
            return encoding.GetBytes(json);
        }
    }
}

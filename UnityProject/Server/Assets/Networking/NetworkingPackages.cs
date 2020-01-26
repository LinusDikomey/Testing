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
        public PlayerInput input;

        public ServerBoundData(uint clientId, uint lastProcessedTick, PlayerInput input) {
            this.clientId = clientId;
            this.lastProcessedTick = lastProcessedTick;
            this.input = input;
        }
    }

    [Serializable]
    public struct ClientBoundData {
        public uint tick;
        public ObjectInitializer[] objInits;
        public uint[] objDestroys;
        public ObjectPacket[] objUpdates;

        public ClientBoundData(uint tick, ObjectInitializer[] objInits, uint[] objDestroys, ObjectPacket[] objUpdates) {
            this.tick = tick;
            this.objInits = objInits;
            this.objDestroys = objDestroys;
            this.objUpdates = objUpdates;
        }
    }

    [Serializable]
    public struct ObjectPacket {
        public uint objID;
        public ComponentPacket[] compPackets;

        public ObjectPacket(uint objID, ComponentPacket[] compPackets) {
            this.objID = objID;
            this.compPackets = compPackets;
        }
    }

    [Serializable]
    public struct ComponentPacket {
        public byte[] bytes;

        public ComponentPacket(byte[] bytes) {
            this.bytes = bytes;
        }
    }

    [Serializable]
    public struct ObjectInitializer {
        public uint id;
        public string prefab;

        public ObjectInitializer(uint id, string prefab) {
            this.id = id;
            this.prefab = prefab;
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


    [Serializable]
    public struct PlayerInput {
        public bool forward, left, right, back;

        public PlayerInput(bool forward, bool left, bool right, bool back) {
            this.forward = forward;
            this.left = left;
            this.right = right;
            this.back = back;
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

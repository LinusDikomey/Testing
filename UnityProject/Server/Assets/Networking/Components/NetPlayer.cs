using Package;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetPlayer : NetBehaviour {

    private struct ClientBound {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public ClientBound(Vector3 position, Quaternion rotation, Vector3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }

    public struct PlayerInput {
        bool forward, left, right, back;

        public PlayerInput(bool forward, bool left, bool right, bool back) {
            this.forward = forward;
            this.left = left;
            this.right = right;
            this.back = back;
        }
    }

    private Transform playerTransform;

    private void Start() {
        playerTransform = GetComponent<Transform>();
    }

    public override void ClientUpdate(byte[] dataPackage) {
        if(dataPackage != null) {
            ClientBound data = PackageSerializer.GetObject<ClientBound>(dataPackage);
            playerTransform.position = data.position;
            playerTransform.rotation = data.rotation;
            playerTransform.localScale = data.scale;
        }
        ((ClientManager)netManager).SetInput(new PlayerInput(false, false, false, false));
    }

    public override byte[] ServerUpdate() {
        ClientBound data = new ClientBound(playerTransform.position, playerTransform.rotation, playerTransform.localScale);
        return PackageSerializer.GetBytes(data);
    }
}

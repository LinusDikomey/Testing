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
        public bool forward, left, right, back;

        public PlayerInput(bool forward, bool left, bool right, bool back) {
            this.forward = forward;
            this.left = left;
            this.right = right;
            this.back = back;
        }
    }

    private Transform playerTransform;

    new public void Start() {
        base.Start();
        playerTransform = GetComponent<Transform>();
    }

    public override void ClientUpdate(byte[] dataPackage) {
        if(dataPackage != null) {
            ClientBound data = PackageSerializer.GetObject<ClientBound>(dataPackage);
            playerTransform.position = data.position;
            playerTransform.rotation = data.rotation;
            playerTransform.localScale = data.scale;
        }
        PlayerInput i = new PlayerInput(false, false, false, false);
        if (Input.GetKey("W"))
            i.forward = true;
        else if (Input.GetKey("S"))
                i.back = true;
        if (Input.GetKey("A"))
            i.left = true;
        else if (Input.GetKey("D"))
            i.right = true;
        ((ClientManager)netManager).SetInput(i);
    }

    public override byte[] ServerUpdate() {
        if (((ServerManager)netManager).serverNetworker.PlayerInputExists(id)) {
            PlayerInput input = ((ServerManager)netManager).serverNetworker.GetPlayerInput(id);

            Vector3 movement = new Vector3();
            if (input.forward) movement.y = -0.05f;
            else if (input.back) movement.y = +0.05f;
            if (input.left) movement.x = -0.05f;
            else if (input.right) movement.x = +0.05f;
        }
        ClientBound data = new ClientBound(playerTransform.position, playerTransform.rotation, playerTransform.localScale);
        return PackageSerializer.GetBytes(data);
    }
}

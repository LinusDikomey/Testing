using Package;
using System;
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
        if (Input.GetAxis("Horizontal") > 0.5f)
            i.forward = true;
        else if (Input.GetAxis("Horizontal") < -0.5f)
                i.back = true;
        if (Input.GetAxis("Vertical") < -0.5f)
            i.left = true;
        else if (Input.GetAxis("Vertical") > 0.5f)
            i.right = true;
        ((ClientManager)netManager).SetInput(i);
        Debug.Log("Input set");
    }

    public override byte[] ServerUpdate() {
        Debug.Log("----------------------------: " + ((ServerManager)netManager).serverNetworker.PlayerInputExists(id) + "  |  " + id);
        if (((ServerManager)netManager).serverNetworker.PlayerInputExists(id)) {
            PlayerInput input = ((ServerManager)netManager).serverNetworker.GetPlayerInput(id);
            Debug.Log("yeet:::::: " + input.forward + ", " + input.left + ", " + input.right + ", " + input.back);
            Vector3 movement = new Vector3();
            if (input.forward) movement.y = -0.05f;
            else if (input.back) movement.y = +0.05f;
            if (input.left) movement.x = -0.05f;
            else if (input.right) movement.x = +0.05f;
            playerTransform.transform.Translate(movement);
        }
        ClientBound data = new ClientBound(playerTransform.position, playerTransform.rotation, playerTransform.localScale);
        return PackageSerializer.GetBytes(data);
    }
}

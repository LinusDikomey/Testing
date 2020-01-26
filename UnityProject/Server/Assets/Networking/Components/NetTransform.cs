using Package;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetTransform : NetAttribute {

    [Serializable]
    public struct Data {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public Data(Vector3 position, Quaternion rotation, Vector3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }

    public override void ClientTick(byte[] dataPackage, ref PlayerInput input) {
        if (dataPackage == null)
            return;
        Data data = PackageSerializer.GetObject<Data>(dataPackage);
        Transform t = obj.GetComponent<Transform>();
        t.position = data.position;
        t.rotation = data.rotation;
        t.localScale = data.scale;

        if(Input.GetAxis("Vertical") < -0.5f) {
            input.forward = true;
        } else if (Input.GetAxis("Vertical") > 0.5f) {
            input.back = true;
        }
        if (Input.GetAxis("Horizontal") < -0.5f) {
            input.left = true;
        } else if (Input.GetAxis("Horizontal") > 0.5f) {
            input.right = true;
        }
    }

    public override byte[] ServerTick() {
        Transform t = obj.GetComponent<Transform>();
        return PackageSerializer.GetBytes(new Data(t.position, t.rotation, t.localScale));
    }
}

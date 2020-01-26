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

        if(Input.GetKey("W")) {
            input.forward = true;
        } else if (Input.GetKey("S")) {
            input.back = true;
        }
        if (Input.GetKey("A")) {
            input.left = true;
        } else if (Input.GetKey("D")) {
            input.right = true;
        }
    }

    public override byte[] ServerTick() {
        Transform t = obj.GetComponent<Transform>();
        return PackageSerializer.GetBytes(new Data(t.position, t.rotation, t.localScale));
    }
}

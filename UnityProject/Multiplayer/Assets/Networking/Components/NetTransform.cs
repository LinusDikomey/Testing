using Package;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetTransform : NetBehaviour {

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

    public override byte[] GetData() {
        Transform t = obj.GetComponent<Transform>();
        return PackageSerializer.GetBytes(new Data(t.position, t.rotation, t.localScale));
    }

    public override void SetData(byte[] dataBytes) {
        if (dataBytes == null)
            return;
        Data data = PackageSerializer.GetObject<Data>(dataBytes);
        Transform t = obj.GetComponent<Transform>();
        t.position = data.position;
        t.rotation = data.rotation;
        t.localScale = data.scale;
    }
}

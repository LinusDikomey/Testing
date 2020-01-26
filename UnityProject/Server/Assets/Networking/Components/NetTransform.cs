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
    }

    public override void ClientTick(byte[] dataPackage) {
        if (dataPackage == null)
            return;
        Data data = PackageSerializer.GetObject<Data>(dataPackage);
    }

    public override byte[] ServerTick() {
        return null;
    }
}

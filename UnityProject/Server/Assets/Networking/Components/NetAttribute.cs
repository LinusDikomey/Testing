using Package;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetAttribute {

    protected NetManager netManager;

    protected GameObject obj;
    protected uint id { private set; get; }

    public void SetParentObj(GameObject obj, uint id) {
        this.obj = obj;
        this.id = id;
    }

    public abstract void ClientTick(byte[] dataPackage, ref PlayerInput input);
    public abstract byte[] ServerTick(ref Dictionary<uint, PlayerInput> inputs);

}

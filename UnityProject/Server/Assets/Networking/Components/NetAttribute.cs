using Package;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetAttribute {

    protected NetManager netManager;

    protected GameObject obj;

    public NetAttribute() {
        
    }

    public void SetParentObj(GameObject obj) {
        this.obj = obj;
    }

    public abstract void ClientTick(byte[] dataPackage, ref PlayerInput input);
    public abstract byte[] ServerTick();

}

using Package;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetBehaviour {

    protected NetManager netManager;

    protected GameObject obj;
    protected uint id { private set; get; }

    public void SetParentObj(GameObject obj, uint id) {
        this.obj = obj;
        this.id = id;
    }

    public virtual void Start() {}
    public virtual void ClientTick(ref PlayerInput input) { }
    public virtual void ServerTick(ref Dictionary<uint, PlayerInput> inputs) { }
    public abstract byte[] GetData();
    public abstract void SetData(byte[] dataBytes);

}
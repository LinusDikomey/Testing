using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class NetBehaviour : MonoBehaviour {

    protected NetManager netManager;
    protected uint id;

    public void Start() {
        netManager = (NetManager) GameObject.FindGameObjectWithTag("NetManager").GetComponent<NetManager>();
        Debug.Log(netManager);
        id = netManager.RegisterComponent(this);
    }

    public void SetID(uint id) {
        netManager.SetComponentID(this.id, id);
        this.id = id;
    }

    public abstract void ClientUpdate(byte[] dataPackage);
    public abstract byte[] ServerUpdate();


    private void OnDestroy() {
        netManager.RemoveComponent(id);
    }
}
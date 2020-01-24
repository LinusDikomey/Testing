using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetBehaviour : MonoBehaviour {

    protected NetManager netManager;

    private void Start() {
        netManager = (NetManager) GameObject.FindGameObjectWithTag("NetManager").GetComponent<NetManager>();
    }

    public abstract void ClientUpdate();
    public abstract void ServerUpdate();
}

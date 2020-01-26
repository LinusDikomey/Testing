﻿using Package;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetIdentity : MonoBehaviour {

    public class NetAttribIDs {
        public const byte ID_TRANSFORM = 0;
    }

    public static NetAttribute GetNewInstanceByID(byte id) {
        switch (id) {
            case NetAttribIDs.ID_TRANSFORM:
                return new NetTransform();
            default:
                return null;
        }
    }

    private const uint MAX_UINT = 4294967295;
    public uint id { get; private set; } = MAX_UINT;

    public byte[] netComponents;
    private NetManager netManager;
    [SerializeField]
    public string prefab { get; private set; }

    private List<NetAttribute> netAttributes = new List<NetAttribute>();

    void Start() {
        
    }

    public void Create(uint id, string prefab) {
        this.id = id;
        this.prefab = prefab;
        netManager = (NetManager) GameObject.FindGameObjectWithTag("NetManager").GetComponent<NetManager>();
        foreach(byte netComp in netComponents) {
            NetAttribute newComp = GetNewInstanceByID(netComp);
            newComp.SetParentObj(gameObject);
            netAttributes.Add(newComp);
        }
        netManager.RegisterObject(this, id);
    }

    public void ClientTick(ComponentPacket[] compPackets) {
        if (compPackets == null) return;
        int index = 0;
        foreach(ComponentPacket compPacket in compPackets) {
            netAttributes[index].ClientTick(compPacket.bytes);

            index++;
        }
    }

    public ObjectPacket ServerTick() {
        ComponentPacket[] compPackets = new ComponentPacket[netAttributes.Count];
        int index = 0;
        foreach(NetAttribute attrib in netAttributes) {
            compPackets[index] = new ComponentPacket(attrib.ServerTick());
            index++;
        }
        return new ObjectPacket(id, compPackets);
    }

    void Update() {
        
    }
}
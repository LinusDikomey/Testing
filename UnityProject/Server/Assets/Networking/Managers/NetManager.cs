using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetManager : MonoBehaviour {

    private Side side;

    public NetManager(Side side) {
        this.side = side;
    }

    public Side GetSide() {
        return side;
    }

    /*public void SubscribeComponent(Method) {

    }*/

    public enum Side {
        CLIENT,
        SERVER
    }
}

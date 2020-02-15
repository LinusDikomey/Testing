using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prefabs : MonoBehaviour {

    public static Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

    public GameObject cube;
    public GameObject ball;
    public GameObject player;

    void Start() {
        prefabs.Add("cube", cube);
        prefabs.Add("ball", ball);
        prefabs.Add("player", player);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [System.Serializable]
    public class MenuItem {
        public GameObject prefab;
    }
}

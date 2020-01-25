using Package;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonClick : MonoBehaviour {

    private void Start() {
        Debug.Log(PackageSerializer.encoding.GetString(PackageSerializer.GetBytes(new ServerBoundData(0, 12, new NetPlayer.PlayerInput(true, false, true, false)))));
    }

    public string ip;
    public string playerName;
    public string clientScene, serverScene;

    public InputField ipInput;
    public InputField nameInput;

    public void PlayButton() {
        int i = 5;
        Debug.Log(i++);
        Debug.Log(i);
        Debug.Log("Play pressed");
        ip = ipInput.text;
        playerName = nameInput.text;
        DontDestroyOnLoad(this);
        SceneManager.LoadScene(clientScene);
    }

    public void HostButton() {
        SceneManager.LoadScene(serverScene);
    }
}

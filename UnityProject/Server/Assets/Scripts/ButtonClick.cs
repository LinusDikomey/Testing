using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonClick : MonoBehaviour {

    public string ip;
    public string playerName;
    public string clientScene, serverScene;

    public InputField ipInput;
    public InputField nameInput;

    public void PlayButton() {
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

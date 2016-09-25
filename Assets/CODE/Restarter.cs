using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Restarter : MonoBehaviour {

    float startTime = 5;


    void OnEnable() {
        startTime = Time.time;
    }
    
	void Update () {
	    if(Time.time > startTime + 3.0F && Input.anyKeyDown)
            SceneManager.LoadScene( 0 );
	}
}

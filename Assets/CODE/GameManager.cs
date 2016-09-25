using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

    public GameObject[] destroyAtStart;

    bool gameHasStarted;


	void Update () {
	    if(gameHasStarted == false && Input.anyKey) {
	        for (int i = 0; i < destroyAtStart.Length; i++) {
	            Destroy( destroyAtStart[i], 0.1F );
	        }
	    }

	    if (Input.GetKeyDown( KeyCode.Escape)) {
	      Application.CancelQuit();
	    }
	}
}

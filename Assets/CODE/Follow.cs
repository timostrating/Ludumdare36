using UnityEngine;
using System.Collections;

public class Follow : MonoBehaviour {

    public Transform target;
    private Transform myTransform;

    public bool ignoreX;
    public bool ignoreY;
    public bool ignoreZ;

    public enum FollowType { Update, LateUpdate}
    public FollowType followType;


    void Awake() {
        myTransform =  this.transform;
    }

	void Update () {
        if (target == null)
            return;

	    if (followType == FollowType.Update)
	        myTransform.position = new Vector3( 
                (ignoreX)? myTransform.position.x : target.position.x,
                (ignoreY)? myTransform.position.y : target.position.y,
                (ignoreZ)? myTransform.position.z : target.position.z);
	}

    void LateUpdate () {
	    if (followType == FollowType.LateUpdate)
	        myTransform.position = new Vector3( 
                (ignoreX)? myTransform.position.x : target.position.x,
                (ignoreY)? myTransform.position.y : target.position.y,
                (ignoreZ)? myTransform.position.z : target.position.z);
	}
}

using UnityEngine;

public class Plane : MonoBehaviour {

    public GameObject deathExplosion;
    public GameObject endText;

    public float speed = 15;
    public float chaseSpeed = 0.05f;

    [Range(0,5)] public float vSpeed = 0.25f;
    [Range(0,5)] public float hSpeed = 3f;

    [Range(0,5)] public float speedUp = 0.5f;
    [Range(0,25)] public float speedDown = 25f;

    [Range(0,99)] public float minSpeed = 10f;
    [Range(0,99)] public float maxSpeed = 99f;

//    public Camera cam;
    public Vector3 cameraPositionOffset;
    public Vector3 cameraLookOffset;

    private Transform myTransform;


    void Awake () {
        myTransform = transform;
//        if (cam == null) 
//            cam = Camera.main;
    }

    void FixedUpdate () {
        //        Vector3 velocity = Vector3.zero;
        //        Camera.main.transform.position = Vector3.SmoothDamp( Camera.main.transform.position, myTransform.position + cameraPositionOffset, ref velocity, smoothPercent  );

        float chaseSpeed = 0.05f;

        Camera.main.transform.position += ((myTransform.transform.position + cameraPositionOffset) - Camera.main.transform.position) * chaseSpeed;  
        Camera.main.transform.LookAt( myTransform.position + cameraLookOffset);

        myTransform.Rotate( Input.GetAxis( "Vertical" ) * vSpeed, 0f, -Input.GetAxis( "Horizontal" ) * hSpeed );
        myTransform.position += transform.forward * speed * Time.deltaTime;

        speed -= (Input.GetKey( KeyCode.Space ))? -speedUp : speedDown * Time.deltaTime;
        speed = Mathf.Clamp( speed, minSpeed, maxSpeed );

        Camera.main.fieldOfView = Remap( speed, minSpeed, maxSpeed, 60, 90 );

        //myTransform.position = new Vector3( myTransform.position.x, myTransform.position.y, myTransform.position.z);
    }

    void OnCollisionEnter(Collision collision) {
        Destroy( Instantiate( deathExplosion, transform.position, Quaternion.identity), 2.5f );
        endText.SetActive( true );
        Destroy( this.gameObject, 0.01f );
    }

    int Remap(int value, int from1, int from2, int to1, int to2 ) {
        return from2 + (value - from1) * (to2 - from2) / (to1 - from1);
    }

    float Remap(float value, float from1, float from2, float to1, float to2 ) {
        return from2 + (value - from1) * (to2 - from2) / (to1 - from1);
    }
}
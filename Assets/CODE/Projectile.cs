using UnityEngine;


public class Projectile : MonoBehaviour {

    public LayerMask collisionMask;
    public float speed;

    float lifetime = 3f;


    void Start() {
        Destroy (gameObject, lifetime);
    }

    void Update () {
        float moveDistance = speed * Time.deltaTime;
        CheckCollisions (moveDistance);
        transform.Translate (Vector3.forward * moveDistance);
    }

    void CheckCollisions(float moveDistance) {
        Ray ray = new Ray (transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, moveDistance, collisionMask, QueryTriggerInteraction.Collide)) {
            Destroy (hit.transform.gameObject);
        }
    }
}
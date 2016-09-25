using UnityEngine;

public class Gun : MonoBehaviour {

    public Transform[] projectileSpawn;
    public Projectile projectile;

    float msBetweenShots = 200;

    float nextShotTime;



    void Start () {
    
    }


    void Update () {
        if (Input.GetMouseButton(0)) {
            shoot();
        } else {
            nextShotTime = 0;
        }
    }


    void shoot() {
        if (Time.time > nextShotTime) {

            for (int i =0; i < projectileSpawn.Length; i ++) {
                nextShotTime = Time.time + msBetweenShots / 1000;
                Instantiate (projectile, projectileSpawn[i].position, projectileSpawn[i].rotation);
            }

        }
    }
}

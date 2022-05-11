using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour {

    public float covidRadius = 2f;

    // Access the fish prefab
    public GameObject fishPrefab;
    // Starting number of fish
    public int numFish = 20;
    public float spawnRadius = 20f;
    // Array of fish prefabs
    public Flock[] allFish;

    const int threadGroupSize = 1024;
    public BoidSettings settings;
    public ComputeShader compute;

    void Awake () {
        // Allocate the allFish array
        allFish = new Flock[numFish];
        // Loop throught the array instantiating the prefabs. In this case fish
        for (int i = 0; i < numFish; i++) {
            Vector3 pos = this.transform.position + new Vector3(Random.Range(-spawnRadius, spawnRadius),0,Random.Range(-spawnRadius, spawnRadius));
            allFish[i] = Instantiate(fishPrefab, pos, Quaternion.identity).GetComponent<Flock>();
            allFish[i].Initialize(settings, null);

            if(i==0) { // patient zero
                allFish[i].covid = 1;
            }
        }
    }

    void Update() {
        if(allFish == null) return;

        int numBoids = allFish.Length;
            var boidData = new BoidData[numBoids];

            for (int i = 0; i < allFish.Length; i++) {
                boidData[i].position = allFish[i].position;
                boidData[i].direction = allFish[i].forward;
                boidData[i].covid = allFish[i].covid;
            }

            var boidBuffer = new ComputeBuffer (numBoids, BoidData.Size);
            boidBuffer.SetData (boidData);

            compute.SetBuffer (0, "boids", boidBuffer);
            compute.SetInt ("numBoids", allFish.Length);
            compute.SetFloat ("viewRadius", settings.perceptionRadius);
            compute.SetFloat ("avoidRadius", settings.avoidanceRadius);
            compute.SetFloat ("covidRadius", covidRadius);

            int threadGroups = Mathf.CeilToInt (numBoids / (float) threadGroupSize);
            compute.Dispatch (0, threadGroups, 1, 1);

            boidBuffer.GetData (boidData);

            for (int i = 0; i < allFish.Length; i++) {
                allFish[i].avgFlockHeading = boidData[i].flockHeading;
                allFish[i].centreOfFlockmates = boidData[i].flockCentre;
                allFish[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
                allFish[i].numPerceivedFlockmates = boidData[i].numFlockmates;
                if (allFish[i].covid==0 && boidData[i].covid>0) { // only change the boid's covid value if it's now a 1
                    allFish[i].covid = boidData[i].covid;
                    // Debug.Log(i + " has covid " + boidData[i].covid);
                }

                allFish[i].UpdateBoid ();
            }

            boidBuffer.Release ();

    }

    public struct BoidData {
        public Vector3 position;
        public Vector3 direction;
        public int covid;

        public Vector3 flockHeading;
        public Vector3 flockCentre;
        public Vector3 avoidanceHeading;
        public int numFlockmates;

        public static int Size {
            get {
                return sizeof (float) * 3 * 5 + sizeof (int) * 2;
            }
        }
    }
}

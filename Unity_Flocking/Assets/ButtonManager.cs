using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    public FlockManager flockManager;
    public BoidSettings settings;

    private Flock[] allFish;
    private int numFish;

    void Start() {
        allFish = flockManager.allFish;
        numFish = flockManager.numFish;
        Init();
    }

    void Init() {
        // Reset default BoidSettings
        settings.minSpeed = 5;
        settings.maxSpeed = 10;
        settings.perceptionRadius = 10f;
        settings.avoidanceRadius = 5;
        settings.maxSteerForce = 3;

        settings.alignWeight = 1;
        settings.cohesionWeight = 1;
        settings.seperateWeight = 10;

        settings.targetWeight = 1;
        settings.boundsRadius = .5f;
        settings.avoidCollisionWeight = 200;
        settings.collisionAvoidDst = 5;
    }

    public void ReloadScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Init();
    }

    public void LoadScene(string s) {
        SceneManager.LoadScene(s);
    }

    public void EnableRadii(bool b) {
        for(int i = 0; i < numFish; i++) {
            Transform r = allFish[i].transform.GetChild(1); // grab the radius ring
            r.gameObject.SetActive(b);
            r = allFish[i].transform.GetChild(2); // grab the radius ring
            r.gameObject.SetActive(b);
            r = allFish[i].transform.GetChild(3); // grab the radius ring
            r.gameObject.SetActive(b);
        }
    }

    public void AdjustPerceptionRadius(float f) {
        settings.perceptionRadius = f;

        for(int i = 0; i < numFish; i++) {
            Transform r = allFish[i].transform.GetChild(1); // grab the radius ring
            r.localScale = new Vector3(f, r.localScale.y, f);
        }
    }

    public void AdjustAvoidanceRadius(float f) {
        settings.avoidanceRadius = f;

        for(int i = 0; i < numFish; i++) {
            Transform r = allFish[i].transform.GetChild(2); // grab the radius ring
            r.localScale = new Vector3(f, r.localScale.y, f);
        }
    }

    public void AdjustCovidRadius(float f) {
        flockManager.covidRadius = f;

        for(int i = 0; i < numFish; i++) {
            Transform r = allFish[i].transform.GetChild(3); // grab the radius ring
            r.localScale = new Vector3(f, r.localScale.y, f);
        }
    }

    public void AdjustAlignWeight(float f) {
        settings.alignWeight = f;
    }

    public void AdjustCohesionWeight(float f) {
        settings.cohesionWeight = f;
    }

    public void AdjustSeperateWeight(float f) {
        settings.seperateWeight = f;
    }

    public void QuitGame() {
        Application.Quit();
    }
}

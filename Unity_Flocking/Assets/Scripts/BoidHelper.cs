using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoidHelper {

    const int numViewDirections = 60;
    public static readonly Vector3[] directions;

    static BoidHelper () {
        directions = new Vector3[BoidHelper.numViewDirections];

        float angleIncrement = 2 * Mathf.PI / numViewDirections;

        // 0 degrees
        directions[0] = new Vector3 (Mathf.Cos(Mathf.PI/2), 0, Mathf.Sin(Mathf.PI/2));

        // (minus 2 to count 0 and 180, and div 2 to do the pos and neg)
        for (int i = 1; i < (numViewDirections/2); i++) {
            float right_degree = Mathf.PI/2 - angleIncrement * i;
            float left_degree = Mathf.PI/2 + angleIncrement * i;
            directions[i*2-1] = new Vector3 (Mathf.Cos(right_degree), 0, Mathf.Sin(right_degree));
            directions[i*2] = new Vector3 (Mathf.Cos(left_degree), 0, Mathf.Sin(left_degree));
        }

        // 180 degrees
        directions[numViewDirections-1] = new Vector3 (Mathf.Cos(3*Mathf.PI/2), 0, Mathf.Sin(3*Mathf.PI/2));
    }

}
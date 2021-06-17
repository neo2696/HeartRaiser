using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class follower : MonoBehaviour
{
    public PathCreator pathCreator;
    public float speed = 5;
    public float distanceTravelled;

    void Update()
    {
        distanceTravelled += speed * Time.deltaTime;
       // follow the path (point by point) with speed
        transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled);
        // rotate object along
        transform.rotation = pathCreator.path.GetRotationAtDistance(distanceTravelled);
    }
}

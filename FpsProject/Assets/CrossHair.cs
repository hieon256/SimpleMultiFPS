using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossHair : MonoBehaviour
{
    public Transform left;
    public Transform right;
    public Transform up;
    public Transform down;

    Vector3 originLeft;
    Vector3 originRight;
    Vector3 originUp;
    Vector3 originDown;

    public float recovery;
    public float currentAccuracy;
    public float maximumAccuracy;

    private void Start()
    {
        originLeft = left.localPosition;
        originRight = right.localPosition;
        originUp = up.localPosition;
        originDown = down.localPosition;
    }
    private void Update()
    {
        if(currentAccuracy > 0)
        {
            recovCrosshair();
        }
    }
    private void recovCrosshair()
    {
        rebound( -Time.deltaTime * recovery );
    }
    public void rebound(float value)
    {
        currentAccuracy += value;
        if(currentAccuracy < 0)
        {
            currentAccuracy = 0;
            left.localPosition = originLeft;
            right.localPosition = originRight;
            up.localPosition = originUp;
            down.localPosition = originDown;
            return;
        }
        if (currentAccuracy > maximumAccuracy)
        {
            currentAccuracy = maximumAccuracy;
            left.localPosition = originLeft + (Vector3.left * maximumAccuracy);
            right.localPosition = originRight + (Vector3.right * maximumAccuracy);
            up.localPosition = originUp + (Vector3.up * maximumAccuracy);
            down.localPosition = originDown + (Vector3.down * maximumAccuracy);
            return;
        }

        left.localPosition = originLeft + (Vector3.left * currentAccuracy);
        right.localPosition = originRight + (Vector3.right * currentAccuracy);
        up.localPosition = originUp + (Vector3.up * currentAccuracy);
        down.localPosition = originDown + (Vector3.down * currentAccuracy);
    }
}

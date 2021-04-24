using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    private static BulletManager instance;
    public static BulletManager Instance
    {
        get { return instance; }
    }

    public void Start()
    {
        instance = GetComponent<BulletManager>();
    }

    public GameObject HolePrefab;
    Queue<GameObject> HoleQueue = new Queue<GameObject>();

    public ParticleSystem BloodPrefab;
    public GameObject BulletTrailPrefab;

    int maxHole = 100;




    public void playBulletTrail(Vector3 muzzlePos, Vector3 hitPos)
    {
        GameObject trail = Instantiate(BulletTrailPrefab, muzzlePos, Quaternion.Euler(0, 0, 0));
        StartCoroutine(nextFrame(trail,hitPos));
        Destroy(trail, 0.1f);
    }
    IEnumerator nextFrame(GameObject obj , Vector3 pos)
    {
        yield return new WaitForSeconds(Time.deltaTime);
        try
        {
            obj.transform.position = pos;
        }
        catch { }
    }
    public void playBlood(Vector3 pos)
    {
        ParticleSystem blood = Instantiate(BloodPrefab, pos, Quaternion.Euler(0,0,0));
        blood.Play();
    }
    public void pushObject()
    {

    }
    public GameObject pullObject()
    {
        GameObject obj;

        if(HoleQueue.Count > maxHole)
        {
            obj = HoleQueue.Dequeue();
        }
        else
        {
            obj = createObject();
            HoleQueue.Enqueue(obj);
        }

        return obj;
    }
    public GameObject createObject()
    {
        GameObject obj = Instantiate(HolePrefab);
        obj.SetActive(true);
        return obj;
    }
}

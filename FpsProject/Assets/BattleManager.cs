using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BattleManager : MonoBehaviour
{
    private static BattleManager instance;
    public static BattleManager Instance
    {
        get
        {
            return instance;
        }
    }

    public void Start()
    {
        instance = GetComponent<BattleManager>();
        originCameraRot = Camera.main.transform.rotation;
        originCameraPos = Camera.main.transform.position;
        originRiflePos = rifle.transform.localPosition;
        originRifleRot = rifle.transform.localRotation;
    }

    public GameObject connectUI;
    public Text nameTxt;

    private Quaternion originCameraRot;
    private Vector3 originCameraPos;
    private Vector3 originCameraLocalPos;
    private Vector3 originRiflePos;
    private Quaternion originRifleRot;

    public GameObject player;
    public PlayerController playerController;
    public GameObject enemyPrefab;

    public GameObject playerUI;
    public Slider playerHp;
    public Text playerName;
    public GameObject rifle;
    public ParticleSystem blood;

    Stack<GameObject> enemyStack = new Stack<GameObject>();
    public List<GameObject> enemies = new List<GameObject>();
    Dictionary<string, EnemyController> enemiesController = new Dictionary<string, EnemyController>();

    public Transform[] spawnArea;

    float deadTime = 0f;
    float shakeTime = 0;
    public void DisconnectGame()
    {
        connectUI.SetActive(true);

        // 적들 삭제.
        foreach (GameObject enemy in enemies)
        {
            enemiesController.Remove(enemy.name);
            pushObject(enemy);
        }
        enemies = new List<GameObject>();

        // 카메라 위치.
        Camera.main.transform.SetParent(null,true);
        Camera.main.transform.position = originCameraPos;
        Camera.main.transform.rotation = originCameraRot;
        player.SetActive(false);

        // 플레이어 이름.
        nameTxt.text = ServerManager.Instance.userName;
        ServerManager.Instance.userName = string.Empty;

        //플레이어 ui.
        playerUI.SetActive(false);
        rifle.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
    }
    public void ConnectGame()
    {
        try
        {
            connectUI.SetActive(false);

            // 카메라 위치.
            player.SetActive(true);
            Camera.main.transform.SetParent(player.transform, true);
            Camera.main.transform.localPosition = new Vector3(0, 0.105f, 0);
            Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
            rifle.transform.localPosition = originRiflePos;
            rifle.transform.localRotation = originRifleRot;

            // 플레이어 체력게이지.
            playerController.Hp = playerController.maxHp;
            playerHp.value = 1.0f;

            // 플레이어 이름.
            playerName.text = nameTxt.text;

            //플레이어 ui.
            playerUI.SetActive(true);
            rifle.SetActive(true);

            // 플레이어 초기화.
            playerController.PlayerInit();

            // 랜덤 스폰.
            int ran = Random.Range(0, spawnArea.Length - 1);

            float x = spawnArea[ran].localScale.x;
            float y = spawnArea[ran].localScale.z;
            Vector3 pos = new Vector3(spawnArea[ran].position.x + Random.Range(-x / 2, x / 2), 0, spawnArea[ran].position.y + Random.Range(-y / 2, y / 2));
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

            player.transform.position = pos;
            player.transform.rotation = rot;

            Cursor.lockState = CursorLockMode.Locked;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }
    public void EnemyUpdate(string enemyName, string data)
    {
        CharacterClass cc = JsonUtility.FromJson<CharacterClass>(data);

        foreach (GameObject enemy in enemies)
        {
            if (enemy.name == enemyName)
            {
                enemiesController[enemyName].cc = cc;

                return;
            }
        }

        GameObject e = pullObject();
        e.name = enemyName;
        enemies.Add(e);
        enemiesController[enemyName] = e.GetComponent<EnemyController>();
        enemiesController[enemyName].cc = cc;
    }
    public void EnemyShoot(string enemyName, string data)
    {
        ShootingClass sc = JsonUtility.FromJson<ShootingClass>(data);

        foreach (GameObject enemy in enemies)
        {
            if (enemy.name == enemyName)
            {
                int targetType = sc.targetType;

                BulletManager.Instance.playBulletTrail(enemiesController[enemyName].muzzle.position, sc.hitPos);

                if (targetType == 0) // 허공에 쏨.
                {
                }
                else if (targetType == 1) // 벽에 쏨.
                {
                    GameObject hole = BulletManager.Instance.pullObject();
                    hole.transform.position = sc.hitPos;
                    hole.transform.rotation = Quaternion.FromToRotation(Vector3.up, sc.hitNormal);
                }
                else if (targetType == 2) // 사람한테 쏨.
                {
                    if (sc.targetName == ServerManager.Instance.userName)
                    {
                        if (sc.part == 0)
                        {
                            playerController.Hp -= 1;
                        }
                        else if (sc.part == 1)
                        {
                            playerController.Hp -= 2;
                        }
                        else if (sc.part == 2)
                        {
                            playerController.Hp -= 3;
                        }
                        playerHp.value = (float)(playerController.Hp) / playerController.maxHp;
                        blood.Play();
                        originCameraLocalPos = Camera.main.transform.localPosition;
                        StartCoroutine(CameraShake(0.015f, 0.1f));
                    }
                    else
                    {
                        BulletManager.Instance.playBlood(sc.hitPos);
                    }
                }
                return;
            }
        }
    }
    public void EnemyDelete(string enemyName)
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy.name == enemyName)
            {
                enemies.Remove(enemy);
                enemiesController.Remove(enemyName);
                pushObject(enemy);
            }
        }
    }
    public void EnemyDead(GameObject enemy)
    {
        enemiesController.Remove(enemy.name);
        StartCoroutine(waitDead(enemy));
    }
    public IEnumerator waitDead(GameObject enemy)
    {
        deadTime = 0f;
        yield return new WaitForSeconds(4.9f);
        pushObject(enemy);
    }

    public IEnumerator CameraShake(float amount, float time)
    {
        shakeTime = 0;
        while (shakeTime <= time)
        {
            Camera.main.transform.localPosition = (Vector3)Random.insideUnitCircle * amount + originCameraLocalPos;

            shakeTime += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.localPosition = originCameraLocalPos;

    }
    public void pushObject(GameObject obj)
    {
        obj.SetActive(false);
        enemyStack.Push(obj);
    }
    public GameObject pullObject()
    {
        GameObject obj;

        if (enemyStack.Count > 0)
        {
            obj = enemyStack.Pop();
        }
        else
        {
            obj = createObject();
        }

        obj.SetActive(true);
        return obj;
    }
    public GameObject createObject()
    {
        GameObject obj = Instantiate(enemyPrefab);
        obj.SetActive(true);
        return obj;
    }
}

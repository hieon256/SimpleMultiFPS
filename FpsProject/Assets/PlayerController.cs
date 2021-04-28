using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CharacterClass
{
    public Vector3 pos;
    public Vector3 rot;
    public Vector3 camRot;

    public int state;
}
public class ShootingClass
{
    public int targetType; // 0 none. 1 wall. 2 character.
    public Vector3 hitPos;
    public Vector3 hitNormal;
    public string targetName;
    public int part; // 0 armandleg. 1 body. 2 head.
}
public class PlayerController : MonoBehaviour
{
    public GameObject characterObj;

    public Animator animator;
    public float walkSpeed;
    public float runSpeed;
    public float rotSpeed;

    public int maxHp;
    public int Hp;

    [Space(15)]
    // weapon.
    public Transform muzzle;
    public ParticleSystem ShotEffect;

    public Transform realMuzzle;
    [Space(15)]
    AudioSource muzzleSound;
    public AudioClip gunSound;

    [Space(15)]
    float fireTime = 0.1f;
    public float rpm = 0.1f;
    public float maxRange = 100f;

    [Space(15)]
    public CrossHair crossHair;
    public float rebound;

    bool isFire = false;
    bool isDead = false;
    float deadTime = 0f;
    private void Start()
    {
        muzzleSound = muzzle.GetComponent<AudioSource>();
    }
    public void PlayerInit()
    {
        isDead = false;
        deadTime = 0f;
    }
    void Update()
    {
        if(Hp <= 0)
        {
            if (isDead)
            {
                if(deadTime > 1.5f)
                {
                    ServerManager.Instance.CloseConnection();
                    return;
                }

                deadTime += Time.deltaTime;
                Camera.main.transform.localPosition -= new Vector3(0,Time.deltaTime * 0.1f,0);
                Camera.main.transform.Rotate(Camera.main.transform.forward, Time.deltaTime * 70f);
                return;
            }

            CharacterClass deadcc = new CharacterClass();
            deadcc.pos = transform.position;
            deadcc.rot = transform.eulerAngles;
            deadcc.camRot = Camera.main.transform.eulerAngles;

            int dead = 4;

            deadcc.state = dead;

            ServerManager.Instance.PlayerUpdate(deadcc);
            isDead = true;
            return;
        }

        isFire = false;

        Movement();
        Rotation();
        ShootingClass sc = Fire();

        CharacterClass cc = new CharacterClass();
        cc.pos = transform.position;
        cc.rot = transform.eulerAngles;
        cc.camRot = Camera.main.transform.eulerAngles;

        int state = 0;
        if (animator.GetBool("Walk"))
            state = 1;
        if (animator.GetBool("Run"))
            state = 2;
        if (isFire)
            state = 3;

        cc.state = state;

        ServerManager.Instance.PlayerUpdate(cc);
        if(sc != null)
            ServerManager.Instance.PlayerShoot(sc);
    }
    void Movement()
    {
        animator.SetBool("Walk", false);
        animator.SetBool("Run", false);

        string move = "Walk";
        float speed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift) == true && !Input.GetMouseButton(0))
        {
            move = "Run";
            speed = runSpeed;
        }

        if (Input.GetKey(KeyCode.W))
        {
            animator.SetBool(move, true);

            gameObject.transform.Translate(Vector3.forward * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.S))
        {
            animator.SetBool(move, true);

            gameObject.transform.Translate(Vector3.back * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.A))
        {
            animator.SetBool(move, true);

            gameObject.transform.Translate(Vector3.left * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.D))
        {
            animator.SetBool(move, true);

            gameObject.transform.Translate(Vector3.right * Time.deltaTime * speed);
        }

    }
    void Rotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotSpeed;

        float rotX = Camera.main.transform.eulerAngles.x - mouseY;

        if (rotX < 180f)
            rotX = Mathf.Clamp(rotX, -1f, 90f);
        else
            rotX = Mathf.Clamp(rotX, 271f, 361f);

        gameObject.transform.localRotation *= Quaternion.Euler(0, mouseX, 0);
        Camera.main.transform.localRotation = Quaternion.Euler(rotX, 0, 0);
    }
    ShootingClass Fire()
    {
        fireTime += Time.deltaTime;
        
        if (Input.GetMouseButton(0))
        {
            isFire = true;

            if (fireTime < rpm)
                return null;

            fireTime = 0f;
            ShotEffect.Play();
            animator.CrossFadeInFixedTime("GunShot", 0.01f);
            muzzleSound.PlayOneShot(gunSound);

            // 명중률 계산.
            float ca = crossHair.currentAccuracy;
            ca /= 1500f;
            Vector3 accForward = new Vector3(Random.Range(muzzle.forward.x - ca, muzzle.forward.x + ca), Random.Range(muzzle.forward.y - ca, muzzle.forward.y + ca), Random.Range(muzzle.forward.z - ca, muzzle.forward.z + ca));
            float diff = Random.Range(0f, 360f);

            // 바운드.
            crossHair.rebound(rebound);
            Camera.main.transform.localRotation *= Quaternion.Euler(-rebound /10, 0, 0);
            float x = Random.Range(-rebound,rebound);
            gameObject.transform.localRotation *= Quaternion.Euler(0, x / 20, 0);

            RaycastHit[] hit = Physics.RaycastAll(muzzle.position, accForward, maxRange).OrderBy(h => h.distance).ToArray();

            int targetType = 0;
            Vector3 hitPos = muzzle.position + muzzle.forward * 100f;
            Vector3 hitNormal = new Vector3(0,0,0);
            string targetName = "";
            int part = 0;

            for (int i = 0; i < hit.Length; i++)
            {
                string hittag = hit[i].collider.transform.tag;
                if (hittag == "Head" || hittag == "Body" || hittag == "ArmAndLeg")
                {
                    Debug.Log(hit[i].collider.transform.position);
                    BulletManager.Instance.playBlood(hit[i].point);

                    targetType = 2;
                    hitPos = hit[i].point;
                    hitNormal = hit[i].normal;
                    targetName = hit[i].transform.parent.name;
                    if (hittag == "Head")
                        part = 2;
                    else if (hittag == "Body")
                        part = 1;
                    else if (hittag == "ArmAndLeg")
                        part = 0;

                    break;
                }
                if (hittag == "Wall")
                {
                    GameObject hole = BulletManager.Instance.pullObject();
                    hole.transform.position = hit[i].point;
                    hole.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit[i].normal);

                    targetType = 1;
                    hitPos = hit[i].point;
                    hitNormal = hit[i].normal;
                    break;
                }
            }

            ShootingClass sc = new ShootingClass();
            sc.targetType = targetType;
            sc.hitPos = hitPos;
            sc.hitNormal = hitNormal;
            sc.targetName = targetName;
            sc.part = part;

            BulletManager.Instance.playBulletTrail(realMuzzle.position, hitPos);

            return sc;
        }

        return null;
    }
}

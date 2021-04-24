using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

    public Animator animator;
    public Transform spine;

    public ParticleSystem ShotEffect;

    AudioSource muzzleSound;
    public AudioClip gunSound;

    public Transform muzzle;

    public CharacterClass cc;

    float fireTime;
    private void Start()
    {
        muzzleSound = muzzle.GetComponent<AudioSource>();
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
    }
    private void Update()
    {
        if (cc == null)
            return;

        fireTime += Time.deltaTime;
        EnemyUpdate(cc);
    }
    private void LateUpdate()
    {
        if (cc == null)
            return;

        Vector3 rot = new Vector3(cc.camRot.z, cc.camRot.y - 90f, -cc.camRot.x - 88.137f);
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Shot") || animator.GetCurrentAnimatorStateInfo(0).IsName("GunShot"))
            rot = new Vector3(rot.x , rot.y + 45f, rot.z);

        muzzle.RotateAround(spine.position, spine.forward, -cc.camRot.x);
        spine.eulerAngles = rot;
    }
    public void EnemyUpdate(CharacterClass cc)
    {
        moveState(cc);

        transform.position = cc.pos;
        transform.eulerAngles = cc.rot;
    }
    public void moveState(CharacterClass cc)
    {
        int state = cc.state;
        if (state == 0)
        {
            animator.SetBool("Walk", false);
            animator.SetBool("Run", false);
            animator.SetBool("Shot", false);
        }
        else if (state == 1)
        {
            animator.SetBool("Walk", true);
            animator.SetBool("Run", false);
            animator.SetBool("Shot", false);
        }
        else if (state == 2)
        {
            animator.SetBool("Walk", false);
            animator.SetBool("Run", true);
            animator.SetBool("Shot", false);
        }
        else if (state == 3)
        {
            if (fireTime < 0.1f)
                return;

            fireTime = 0f;
            if (cc.pos != transform.position)
            {
                animator.SetBool("Walk", false);
                animator.SetBool("Run", false);
                animator.SetBool("Shot", true);
                animator.CrossFadeInFixedTime("GunShot", 0.01f, 1);
            }
            else
                animator.CrossFadeInFixedTime("GunShot", 0.01f, 0);

            ShotEffect.Play();
            muzzleSound.PlayOneShot(gunSound);
        }
        else if (state == 4)
        {
            BattleManager.Instance.enemies.Remove(gameObject);
            animator.SetTrigger("Dead");
            BattleManager.Instance.EnemyDead(gameObject);
            this.cc = null;
        }
    }
}

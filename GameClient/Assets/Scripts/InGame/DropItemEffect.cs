using DG.Tweening;
using UnityEngine;

public class DropItemEffect : MonoBehaviour
{
    public float dropTime = 2f;
    public float absorbTime = 1f;
    public float spawnPower = 5f;

    Rigidbody rigid;
    Transform target;
    ItemState state;

    float dropEndTime;
    float absorbEndTime;
    Vector3 absorbStartPos;

    enum ItemState
    {
        Idle,
        Dropping,
        Absorbing
    }

    private void OnEnable()
    {
        rigid = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (state == ItemState.Dropping)
        {
            if (Time.time > dropEndTime)
                AbsorbItem();
        }
        else if (state == ItemState.Absorbing)
        {
            if (Time.time > absorbEndTime)
                AcquireEffect();
            else if (absorbTime != 0)
                transform.position = Vector3.Lerp(absorbStartPos, target.position, 1f - (absorbEndTime - Time.time) / absorbTime);
        }
    }

    public void DropItem(Vector3 spawnPos, Transform target)
    {
        var forceDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 0.4f;
        var forceVec = new Vector3(forceDir.x, 1f, forceDir.y);

        gameObject.SetActive(true);
        rigid.isKinematic = false;
        rigid.MovePosition(spawnPos);
        rigid.AddForce(forceVec * spawnPower, ForceMode.Impulse);
        this.target = target;
        dropEndTime = Time.time + dropTime;
        
        SetState(ItemState.Dropping);
    }

    void AbsorbItem()
    {
        rigid.isKinematic = true;
        absorbEndTime = Time.time + absorbTime;
        absorbStartPos = transform.position;

        SetState(ItemState.Absorbing);
    }

    void AcquireEffect()
    {
        gameObject.SetActive(false);
        PlayerDataManager.Instance?.GainMoney(0, true);

        SetState(ItemState.Idle);
    }

    void SetState(ItemState state)
    {
        this.state = state;
    }
}

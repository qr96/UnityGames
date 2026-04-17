using System;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("참조")]
    public GameObject model;
    public Rigidbody rigid;

    [Header("회전")]
    public float rotateSpeed = 180f;

    [Header("흡수 설정")]
    public float jumpPower = 2f;
    public float flyDuration = 0.5f;
    public Vector3 targetOffset = new Vector3(0f, 1.8f, 0f);

    [Header("착지 판정")]
    public float landedSpeedThreshold = 0.1f;

    enum State { Flying, Landed, Attracting }

    int _itemId;
    int _itemCode;
    Action<int, int, DroppedItem> _onGetItem;

    Transform _attractTarget;
    Vector3 _startPos;
    float _elapsed;
    State _state;

    void Update()
    {
        switch (_state)
        {
            case State.Flying: FlyingUpdate(); break;
            case State.Landed: LandedUpdate(); break;
            case State.Attracting: AttractingUpdate(); break;
        }
    }

    void FlyingUpdate()
    {
        if (rigid.linearVelocity.magnitude <= landedSpeedThreshold)
            _state = State.Landed;

        RotateModel();
    }

    void LandedUpdate()
    {
        RotateModel();
    }

    void AttractingUpdate()
    {
        if (_attractTarget == null) { ReturnToPool(); return; }

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / flyDuration);
        float eased = t * t * (3f - 2f * t);
        float arc = Mathf.Sin(t * Mathf.PI) * jumpPower;

        var target = _attractTarget.position + targetOffset;
        transform.position = Vector3.Lerp(_startPos, target, eased) + Vector3.up * arc;

        if (t >= 1f)
        {
            transform.position = target;
            _onGetItem?.Invoke(_itemId, _itemCode, this);
            ReturnToPool();
        }
    }

    public void Attract(Transform player)
    {
        if (_state != State.Landed) return;

        _state = State.Attracting;
        _attractTarget = player;
        _startPos = transform.position;
        _elapsed = 0f;

        rigid.isKinematic = true;
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
    }

    public void SpawnItem(int itemId, int itemCode, Vector3 force, Action<int, int, DroppedItem> onGetItem)
    {
        _itemId = itemId;
        _itemCode = itemCode;
        _onGetItem = onGetItem;
        _state = State.Flying;
        _attractTarget = null;
        _elapsed = 0f;

        rigid.isKinematic = false;
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        gameObject.SetActive(true);
        rigid.AddForce(force, ForceMode.Impulse);
    }

    void RotateModel()
    {
        var r = model.transform.localEulerAngles;
        r.y = (r.y + Time.deltaTime * rotateSpeed) % 360f;
        model.transform.localEulerAngles = r;
    }

    // 기존: SetActive(false)만 했음 → 풀에 반납 안 됨
    void ReturnToPool()
    {
        GetComponent<Poolable>().ReleaseSelf();
    }

    void OnDisable()
    {
        _state = State.Flying;
        _attractTarget = null;
        _elapsed = 0f;
    }
}
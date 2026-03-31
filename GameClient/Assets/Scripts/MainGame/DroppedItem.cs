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
    public float jumpPower = 5f;
    public float flyDuration = 0.5f;
    public Vector3 targetOffset = new Vector3(0f, 1.8f, 0f);

    [Header("착지 판정")]
    public float landedSpeedThreshold = 0.1f;  // 이 속도 이하면 착지로 판정

    enum State { Flying, Landed, Attracting }

    int _itemId;
    int _itemCode;
    Action<int, int, DroppedItem> _onGetItem;
    Transform _attractTarget;
    Vector3 _velocity;
    float _flyTimer;
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

    // 스폰 후 물리로 날아다니는 동안
    void FlyingUpdate()
    {
        // 속도가 충분히 줄면 착지로 전환
        if (rigid.linearVelocity.magnitude <= landedSpeedThreshold)
            _state = State.Landed;

        RotateModel();
    }

    // 착지 후 대기 중
    void LandedUpdate()
    {
        RotateModel();
    }

    // 플레이어 쪽으로 날아가는 중
    void AttractingUpdate()
    {
        if (_attractTarget == null)
        {
            ReturnToPool();
            return;
        }

        _flyTimer -= Time.deltaTime;

        var target = _attractTarget.position + targetOffset;

        _velocity += Vector3.down * 9.8f * Time.deltaTime;
        _velocity += (target - transform.position).normalized * 15f * Time.deltaTime;
        transform.position += _velocity * Time.deltaTime;

        if (_flyTimer <= 0f)
        {
            transform.position = target;
            _onGetItem?.Invoke(_itemId, _itemCode, this);
            ReturnToPool();
        }
    }

    // 플레이어가 호출 (착지 상태일 때만 흡수 시작)
    public void Attract(Transform player)
    {
        if (_state != State.Landed) return;  // ← 착지 전엔 무시

        _state = State.Attracting;
        _attractTarget = player;
        _flyTimer = flyDuration;

        rigid.isKinematic = true;
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        _velocity = Vector3.up * jumpPower;
    }

    public void SpawnItem(int itemId, int itemCode, Vector3 force, Action<int, int, DroppedItem> onGetItem)
    {
        _itemId = itemId;
        _itemCode = itemCode;
        _onGetItem = onGetItem;
        _state = State.Flying;
        _attractTarget = null;

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

    void ReturnToPool()
    {
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        _state = State.Flying;
        _attractTarget = null;
    }
}
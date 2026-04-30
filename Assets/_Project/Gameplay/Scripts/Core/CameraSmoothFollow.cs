using UnityEngine;
using Unity.Cinemachine;

public class CameraSmoothFollow : MonoBehaviour
{
    public CinemachineCamera vcam; 
    public float offsetAmount = 2.5f; 
    public float smoothTime = 0.6f;   
    
    private CinemachineFollow _followComponent;
    private int _activeTweenId = -1;
    private float _lastDirection = 0; // Lưu lại hướng cũ để tránh gọi Tween liên tục
    private bool _isTracking = true;

    void Start()
    {
        _followComponent = vcam.GetComponent<CinemachineFollow>();
    }

    public void SetTracking(bool track)
    {
        _isTracking = track;
        
        // Stop any active camera movement tweens when disabling tracking
        if (!track && LeanTween.isTweening(_activeTweenId))
        {
            LeanTween.cancel(_activeTweenId);
        }
    }

    // Hàm này sẽ được gọi từ Script di chuyển của Player
    public void UpdateCameraDirection(float moveX)
    {
        if (!_isTracking) return;

        // Chỉ thực hiện nếu hướng di chuyển thay đổi rõ rệt (tránh số 0 khi đứng yên)
        if (Mathf.Abs(moveX) > 0.1f)
        {
            float targetDirection = moveX > 0 ? 1 : -1;

            // Nếu hướng mới khác với hướng cũ thì mới chạy Tween
            if (targetDirection != _lastDirection)
            {
                _lastDirection = targetDirection;
                StartOffsetTween(targetDirection * offsetAmount);
            }
        }
    }

    private void StartOffsetTween(float targetX)
    {
        if (LeanTween.isTweening(_activeTweenId))
        {
            LeanTween.cancel(_activeTweenId);
        }

        _activeTweenId = LeanTween.value(vcam.gameObject, _followComponent.FollowOffset.x, targetX, smoothTime)
            .setEase(LeanTweenType.easeInOutSine)
            .setOnUpdate((float val) =>
            {
                Vector3 offset = _followComponent.FollowOffset;
                offset.x = val;
                _followComponent.FollowOffset = offset;
            }).id;
    }

    public void SnapImmediate()
    {
        // Cancel any running offset tween
        if (LeanTween.isTweening(_activeTweenId))
            LeanTween.cancel(_activeTweenId);

        _lastDirection = 0;

        // Reset offset to center so there's no stale directional lean
        if (_followComponent != null)
        {
            Vector3 offset = _followComponent.FollowOffset;
            offset.x = 0f;
            _followComponent.FollowOffset = offset;
        }

        // Tell Cinemachine to hard-snap on next update instead of interpolating
        if (vcam != null)
            vcam.PreviousStateIsValid = false;
    }
}
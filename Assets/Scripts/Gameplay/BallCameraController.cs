using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BallCameraController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera ballCamera;

    [Header("Ball Camera")]
    [SerializeField] private Vector3 ballCameraOffset = new Vector3(-1.25f, 1.6f, -3.5f);
    [SerializeField] private float blendTime = 1f;

    private CinemachineBrain brain;
    private Transform currentBall;
    private Transform ballCameraTarget;

    private void Awake()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime);

        ballCameraTarget = new GameObject("BallCameraTarget").transform;
        ballCameraTarget.SetParent(transform);

        playerCamera.Priority.Enabled = true;
        playerCamera.Priority.Value = 1;
        ballCamera.Priority.Enabled = true;
        ballCamera.Priority.Value = 0;

        ballCamera.Follow = ballCameraTarget;
        ballCamera.LookAt = ballCameraTarget;
        ballCamera.GetComponent<CinemachineFollow>().FollowOffset = ballCameraOffset;
    }

    private void LateUpdate()
    {
        if (currentBall != null)
            ballCameraTarget.position = currentBall.position;
    }

    public IEnumerator StartFollowingBall(Transform _ball, Vector3 _target)
    {
        currentBall = _ball;

        Vector3 direction = _target - _ball.position;
        direction.y = 0;
        ballCameraTarget.position = _ball.position;
        ballCameraTarget.rotation = Quaternion.LookRotation(direction);

        Vector3 cameraPosition = ballCameraTarget.TransformPoint(ballCameraOffset);
        ballCamera.PreviousStateIsValid = false;
        ballCamera.ForceCameraPosition(cameraPosition, Quaternion.LookRotation(_ball.position - cameraPosition));

        ballCamera.Priority.Value = 1;
        playerCamera.Priority.Value = 0;
        yield return WaitForBlend(ballCamera);
    }

    public IEnumerator StopFollowingBall()
    {
        playerCamera.Priority.Value = 1;
        ballCamera.Priority.Value = 0;
        yield return WaitForBlend(playerCamera);

        currentBall = null;
    }

    private IEnumerator WaitForBlend(CinemachineCamera _camera)
    {
        yield return null;
        while (brain.IsBlending || (Object)brain.ActiveVirtualCamera != _camera)
            yield return null;
    }
}

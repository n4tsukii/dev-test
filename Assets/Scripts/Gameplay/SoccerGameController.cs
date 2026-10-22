using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoccerGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] balls;
    [SerializeField] private Transform[] goals;
    [SerializeField] private BallCameraController cameraController;
    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private GameObject kickButton;

    [Header("Kick")]
    [SerializeField] private float kickDistance = 1.5f;
    [SerializeField] private float ballSpeed = 12f;
    [SerializeField] private float flightHeight = 1f;
    private bool isBallFlying;
    private List<Transform> kickedBalls = new List<Transform>();

    [Header("Aim Line")]
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private float aimPreviewDistance = 30f;
    [SerializeField] private Color readyAimColor = Color.green;
    [SerializeField] private Color blockedAimColor = Color.red;

    private void Update()
    {
        if (isBallFlying)
        {
            kickButton.SetActive(false);
            aimLine.enabled = true;
            return;
        }

        Transform ball = GetNearestBall();
        Vector3 target = Vector3.zero;
        bool canKick = CanKick(ball, ref target);

        kickButton.SetActive(canKick);
        DrawAimLine(ball, target, canKick);
    }

    public void OnKickButton()
    {
        if (isBallFlying) return;

        Transform ball = GetNearestBall();
        Vector3 target = Vector3.zero;
        if (CanKick(ball, ref target))
            StartCoroutine(KickBall(ball, target));
    }

    public void OnAutoKickButton()
    {
        if (isBallFlying) return;

        Transform ball = GetFarthestBall();
        if (ball != null)
        {
            Transform goal = GetNearestGoal(ball.position);
            Vector3 target = new Vector3(goal.position.x, ball.position.y, goal.position.z);
            StartCoroutine(KickBall(ball, target));
        }
    }

    public void OnResetButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator KickBall(Transform _ball, Vector3 _target)
    {
        isBallFlying = true;
        kickedBalls.Add(_ball);
        kickButton.SetActive(false);
        aimLine.enabled = false;

        // wait for the camera to get behind the ball first
        yield return cameraController.StartFollowingBall(_ball, _target);

        Vector3 start = _ball.position;
        float duration = Vector3.Distance(start, _target) / ballSpeed;
        float timer = 0;
        Vector3 rollAxis = Vector3.Cross(Vector3.up, _target - start).normalized;
        float radius = 0.33f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            if (t > 1)
                t = 1;

            Vector3 oldPosition = _ball.position;
            _ball.position = Vector3.Lerp(start, _target, t) + Vector3.up * (4 * flightHeight * t * (1 - t));

            float moved = Vector3.Distance(oldPosition, _ball.position);
            _ball.Rotate(rollAxis, moved / radius * Mathf.Rad2Deg, Space.World);
            yield return null;
        }
        _ball.position = _target;

        GameObject confetti = Instantiate(confettiPrefab, _target, confettiPrefab.transform.rotation);
        confetti.GetComponent<ParticleSystem>().Play();
        Destroy(confetti, 6);

        yield return new WaitForSeconds(2);
        yield return cameraController.StopFollowingBall();

        isBallFlying = false;
    }

    private bool CanKick(Transform _ball, ref Vector3 _target)
    {
        if (_ball == null) return false;

        Vector3 direction = _ball.position - player.position;
        direction.y = 0;
        direction.Normalize();
        _target = _ball.position + direction * aimPreviewDistance;

        bool hitGoal = false;
        Ray ray = new Ray(_ball.position, direction);
        foreach (Transform goal in goals)
        {
            BoxCollider goalArea = goal.GetComponent<BoxCollider>();
            RaycastHit hit;
            if (goalArea.Raycast(ray, out hit, 100))
            {
                // only count it if the ball goes in from the front of the goal (goal's blue axis faces the pitch)
                if (Vector3.Dot(hit.normal, goal.forward) > 0.99f)
                {
                    float depth = Vector3.Dot(hit.point - goal.position, goal.forward);
                    _target = hit.point + direction * depth;
                    hitGoal = true;
                }
            }
        }

        if (!hitGoal) return false;
        return Vector3.Distance(player.position, _ball.position) <= kickDistance;
    }

    private void DrawAimLine(Transform _ball, Vector3 _target, bool _canKick)
    {
        if (_ball == null)
        {
            aimLine.enabled = false;
            return;
        }

        aimLine.enabled = true;
        aimLine.positionCount = 3;
        aimLine.SetPosition(0, new Vector3(player.position.x, _ball.position.y, player.position.z));
        aimLine.SetPosition(1, _ball.position);
        aimLine.SetPosition(2, _target);

        if (_canKick)
        {
            aimLine.startColor = readyAimColor;
            aimLine.endColor = readyAimColor;
        }
        else
        {
            aimLine.startColor = blockedAimColor;
            aimLine.endColor = blockedAimColor;
        }
    }

    private Transform GetNearestBall()
    {
        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;
        foreach (Transform ball in balls)
        {
            if (kickedBalls.Contains(ball)) continue;

            float distance = Vector3.Distance(player.position, ball.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = ball;
            }
        }
        return nearest;
    }

    private Transform GetFarthestBall()
    {
        Transform farthest = null;
        float farthestDistance = 0;
        foreach (Transform ball in balls)
        {
            if (kickedBalls.Contains(ball)) continue;

            float distance = Vector3.Distance(player.position, ball.position);
            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthest = ball;
            }
        }
        return farthest;
    }

    private Transform GetNearestGoal(Vector3 _position)
    {
        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;
        foreach (Transform goal in goals)
        {
            float distance = Vector3.Distance(_position, goal.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = goal;
            }
        }
        return nearest;
    }
}

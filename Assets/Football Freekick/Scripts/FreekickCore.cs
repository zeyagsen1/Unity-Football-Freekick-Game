using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FreekickCore : MonoBehaviour
{

    // Add these variables at the top of your FreekickCore class
[Header("Goalkeeper Reaction Settings")]
public float goalkeeperReactionSpeed = 1.0f; // Base reaction speed
public float maxReactionDelay = 2.0f; // Maximum delay in seconds
public float minReactionDelay = 0.1f; // Minimum delay in secon

    [Header("Goalkeeper")]
public Animator goalkeeperAnimator;

    [Header("Ball and Football Arena")]
    public Rigidbody ball;
    public AudioSource shootAudioSource;
    public AudioClip shootAudioClip;
    public GameObject footballArena;
    
    [Header("UI")]
    public RectTransform ballHitPos;
    public Slider shootSlider;
    public TrajectoryLineRenderer trajectoryLineRenderer;
    public GameObject ballHitCanvas, directionCanvas, freekickPositionSelectionPanel, freekickSettingPanel, freekickCompletedPanel;
    public TextMeshProUGUI ballDistanceText;

    [Header("Points")] 
    public Transform freekickPoint;
    public Transform farTarget, nearTarget, goalCenter;
    
    private float _power;
    private const float XMin = -34.8f, XMax = 34.8f, ZMin = -103f, ZMax = 0, BallHitPosRadius = 0.435f;
    private Vector3 _farTargetLocalPos, _nearTargetLocalPos;
    private bool _isUnavailableForNewFreekick, _isShooting, _isBallHit, _isSetShootSlider;
    private bool _isRightKickAngle, _isLeftKickAngle;
    private bool _isForwardFreekickPosition, _isBackFreekickPosition, _isRightFreekickPosition, _isLeftFreekickPosition;
    private bool _isUpBallHitPos, _isDownBallHitPos, _isRightBallHitPos, _isLeftBallHitPos;

    private void Awake()
    {
        _farTargetLocalPos = farTarget.localPosition;
        _nearTargetLocalPos = nearTarget.localPosition;
    }

    private void Update()
    {
        // Opens and closes "Football Arena" gameObject if pressing "F" button.
        if(Input.GetKeyDown(KeyCode.F)) footballArena.SetActive(!footballArena.activeSelf);
        
        // When available creates new freekick if pressing "Enter" button.
        if (!_isUnavailableForNewFreekick && Input.GetKeyDown(KeyCode.Return)) NewFreekickCreate();
        
        // Set Kick Angles.
        _isRightKickAngle = !_isBallHit && Input.GetKey(KeyCode.RightArrow);
        _isLeftKickAngle = !_isBallHit && Input.GetKey(KeyCode.LeftArrow);
        
        // Set Freekick Position.
        _isForwardFreekickPosition = !_isShooting && Input.GetKey(KeyCode.W);
        _isBackFreekickPosition = !_isShooting && Input.GetKey(KeyCode.S);
        _isRightFreekickPosition = !_isShooting && Input.GetKey(KeyCode.D);
        _isLeftFreekickPosition = !_isShooting && Input.GetKey(KeyCode.A);

        if (_isShooting)
        {
            if (directionCanvas.activeSelf) directionCanvas.SetActive(false);
            if (!_isBallHit)
            {
                // Applying UI changes for shooting.
                if (!ballHitCanvas.activeSelf) ballHitCanvas.SetActive(true);
                if (!trajectoryLineRenderer.gameObject.activeSelf) trajectoryLineRenderer.gameObject.SetActive(true);
                SetInstructionPanels(false, true, false);

                // Set New Freekick Shoot Ball Hit Pos (curve).
                _isUpBallHitPos = Input.GetKey(KeyCode.W);
                _isDownBallHitPos = Input.GetKey(KeyCode.S);
                _isRightBallHitPos = Input.GetKey(KeyCode.D);
                _isLeftBallHitPos = Input.GetKey(KeyCode.A);

                var originPoint = Vector3.zero;
                var ballHitPosLocal = ballHitPos.localPosition;
                var distance = Vector3.Distance(ballHitPosLocal, originPoint);
                var fromOriginToObject = ballHitPosLocal - originPoint;
                
                if (distance > BallHitPosRadius)
                {
                    fromOriginToObject *= BallHitPosRadius / distance;
                    ballHitPos.localPosition = originPoint + fromOriginToObject;
                }

                // Set Freekick Curve Settings.
                ballHitPosLocal = ballHitPos.localPosition;
                farTarget.localPosition = new Vector3(_farTargetLocalPos.x - ballHitPosLocal.x * 20f, _farTargetLocalPos.y - ballHitPosLocal.y * 5f, _farTargetLocalPos.z);
                nearTarget.localPosition = new Vector3(_nearTargetLocalPos.x - ballHitPosLocal.x * 0.5f, _nearTargetLocalPos.y - ballHitPosLocal.y * 0.5f, _nearTargetLocalPos.z);
                trajectoryLineRenderer.CreateTrajectoryLine(ball.transform, nearTarget, farTarget);
                trajectoryLineRenderer.transform.position = Vector3.zero;

                // Set slider and shoot freekick.
                _isSetShootSlider = Input.GetKey(KeyCode.Space);

                // Apply force to ball for shooting.
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    ShootToTargets();
                    StartCoroutine(WaitForResetShootSlider());
                }
            }
            else
            {
                // Set Trajectory lines.
                if (ballHitCanvas.activeSelf) ballHitCanvas.SetActive(false);
                if (trajectoryLineRenderer.gameObject.activeSelf) trajectoryLineRenderer.gameObject.SetActive(false);
            }
        }
        else SetNewFreekickUI();
    }

    private void FixedUpdate()
    {
        // Applying Kick Angles Input.
        if(_isRightKickAngle) SetKickAngle(1);
        if(_isLeftKickAngle) SetKickAngle(-1);
        
        // Applying Freekick Position Input.
        if(_isForwardFreekickPosition) TranslateFreekickPointWithInput(Vector3.forward);
        if(_isBackFreekickPosition) TranslateFreekickPointWithInput(-Vector3.forward);
        if(_isRightFreekickPosition) TranslateFreekickPointWithInput(Vector3.right);
        if(_isLeftFreekickPosition) TranslateFreekickPointWithInput(-Vector3.right);

        // Applying Freekick Ball Hit Pos Input.
        if (_isUpBallHitPos || _isDownBallHitPos || _isRightBallHitPos || _isLeftBallHitPos)
        {
            var ballHitDiff = ballHitCanvas.transform.localScale.magnitude;
            if (_isUpBallHitPos) TranslateBallHitPosWithInput(Vector3.up, ballHitDiff);
            if(_isDownBallHitPos) TranslateBallHitPosWithInput(-Vector3.up, ballHitDiff);
            if(_isRightBallHitPos) TranslateBallHitPosWithInput(Vector3.right, ballHitDiff);
            if(_isLeftBallHitPos) TranslateBallHitPosWithInput(-Vector3.right, ballHitDiff);
        }
        
        // Applying Shoot Slider Input.
        if(_isSetShootSlider) SetShootSlider();
    }

    private void NewFreekickCreate()
    {
        // When creating new freekick, all scene components and booleans are setting here.
        if (trajectoryLineRenderer.gameObject.activeSelf) trajectoryLineRenderer.gameObject.SetActive(false);
        _isBallHit = false;
        _isShooting = !_isShooting;
        ball.Sleep();
        var ballTransform = ball.transform;
        ballTransform.localPosition = Vector3.zero;
        ballTransform.localEulerAngles = Vector3.zero;
        ResetGoalkeeperAnimation();
    }

    private void SetNewFreekickUI()
    {
        // Sets all UI components for new freekick.
        if (!directionCanvas.activeSelf) directionCanvas.SetActive(true);
        if (ballHitCanvas.activeSelf) ballHitCanvas.SetActive(false);
        if (!ballDistanceText.gameObject.activeSelf) ballDistanceText.gameObject.SetActive(true);
        
        ballDistanceText.text = DistanceConversion.Distance((ball.position - goalCenter.position).magnitude);
        SetInstructionPanels(true, false, false);
    }

    private void TranslateFreekickPointWithInput(Vector3 direction)
    {
        freekickPoint.Translate(direction * (12f * Time.fixedDeltaTime));
        
        // restricting the ball in an area.
        if (freekickPoint.transform.localPosition.x > XMax || freekickPoint.transform.localPosition.x < XMin ||
            freekickPoint.transform.localPosition.z > ZMax || freekickPoint.transform.localPosition.z < ZMin)
        {
            freekickPoint.Translate(-direction * (12f * Time.fixedDeltaTime));
        }
    }
    
    private void TranslateBallHitPosWithInput(Vector3 direction, float ballHitDiff)
    {
        // Sets Ball Hit Position.
        ballHitPos.Translate(direction * (ballHitDiff * 0.2f * Time.fixedDeltaTime));
    }
    
    private void SetKickAngle(int direction)
    {
        // Rotates the freekick point with kick angles.
        freekickPoint.transform.Rotate(freekickPoint.up, direction * 0.6f);
        trajectoryLineRenderer.transform.eulerAngles = Vector3.zero;
    }

    private void SetInstructionPanels(bool positionSelection, bool setting, bool completed)
    {
        // Set UI visibilities.
        if (freekickPositionSelectionPanel.activeSelf != positionSelection) freekickPositionSelectionPanel.SetActive(positionSelection);
        if (freekickSettingPanel.activeSelf != setting) freekickSettingPanel.SetActive(setting);
        if (freekickCompletedPanel.activeSelf != completed) freekickCompletedPanel.SetActive(completed);
    }

    private void SetShootSlider()
    {
        // Set shoot slider if player presses "Space" key.
        if (_power >= 30f) return;
        _power++;
        shootSlider.value = _power / 30f;
    }

    private void ResetShootSlider()
    {
        // Resets power, shoot slider and getting available for new freekick.
        _power = 0;
        shootSlider.value = 0;
        _isUnavailableForNewFreekick = false;
    }

    private IEnumerator WaitForResetShootSlider()
    {
        // Waits seconds after shooting for avoid the possibility of the player accidentally skipping the shoot and taking a new freekick.
        _isBallHit = true;
        _isUnavailableForNewFreekick = true;
        yield return new WaitForSecondsRealtime(4f);
        SetInstructionPanels(false, false, true);
        ResetShootSlider();
    }

    private void ShootToTargets()
    {
        // Shooting near and far targets.
        if (ballDistanceText.gameObject.activeSelf) ballDistanceText.gameObject.SetActive(false);
        SetInstructionPanels(false, false, false);
        BallSoundPlayer.PlaySound(shootAudioSource, shootAudioClip, 1 + (2 - _power * 0.0666f));
        StartCoroutine(ShootToNearTarget());
    }

    private IEnumerator ShootToNearTarget()
    {
        // Shooting near target for initial movement.
        while ((ball.position - nearTarget.position).magnitude > 0.2f)
        {
            ball.position = Vector3.MoveTowards(ball.position, nearTarget.position, 0.1f + _power / 60f);
            yield return new WaitForSeconds(0.01f);
        }

        ShootToFarTarget();
        yield return null;
    }

private void ShootToFarTarget()
{
    // Adding force if ball arrives the near target.
    var shoot = (farTarget.position - ball.position).normalized;
    ball.AddForce((shoot + new Vector3(0f, _power / 105f, 0f)) * _power / 2.4f, ForceMode.Impulse);
    
    // Calculate reaction delay based on power and distance
    float reactionDelay = CalculateGoalkeeperReactionDelay();
    
    // Trigger goalkeeper animation with delay
    StartCoroutine(DelayedGoalkeeperReaction(reactionDelay));
}


private float CalculateGoalkeeperReactionDelay()
{
    // Calculate distance between ball and goalkeeper
    float distanceToGoal = Vector3.Distance(ball.position, goalkeeperAnimator.transform.position);
    
    // Normalize power (0-1 range, where 1 is max power)
    float normalizedPower = _power / 30f;
    
    // Calculate base reaction time
    // Higher power = faster ball = less time to react = shorter delay
    // Greater distance = more time to see ball coming = longer delay possible
    float powerFactor = 1f - normalizedPower; // Inverted: high power = low factor
    float distanceFactor = Mathf.Clamp01(distanceToGoal / 50f); // Normalize distance
    
    // Combine factors to get reaction delay
    // Close + Fast ball = very quick reaction needed
    // Far + Slow ball = more time to react
    float reactionDelay = minReactionDelay + (maxReactionDelay * powerFactor * distanceFactor);
    
    // Add some randomness for realism (±20%)
    float randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
    reactionDelay *= randomFactor;
    
    Debug.Log($"Ball Power: {_power}, Distance: {distanceToGoal:F1}m, Reaction Delay: {reactionDelay:F2}s");
    
    return Mathf.Clamp(reactionDelay, minReactionDelay, maxReactionDelay);
}

private IEnumerator DelayedGoalkeeperReaction(float delay)
{
    yield return new WaitForSeconds(delay);
    
    // Check if ball is still coming towards goal (not already scored/missed)
    if (_isBallHit && ball.linearVelocity.magnitude > 1f)
    {
        TriggerGoalkeeperAnimation();
    }
}

// Enhanced goalkeeper animation trigger with success rate
private void TriggerGoalkeeperAnimation()
{
    if (goalkeeperAnimator == null) return;
    
    // Calculate if goalkeeper can successfully block based on reaction time
    bool canBlock = CalculateBlockingChance();
    
    // Calculate the direction from ball to far target (where ball is heading)
    Vector3 ballDirection = (farTarget.position - ball.position).normalized;
    float horizontalDirection = ballDirection.x;
    
    if (horizontalDirection > 0.1f) // Ball going to goalkeeper's right
    {
        if (canBlock)
        {
            goalkeeperAnimator.SetTrigger("right");
            Debug.Log("Goalkeeper diving RIGHT - Good reaction!");
        }
        else
        {
            // Delayed or weak reaction
            goalkeeperAnimator.SetTrigger("right");
            Debug.Log("Goalkeeper diving RIGHT - Too slow!");
        }
    }
    else if (horizontalDirection < -0.1f) // Ball going to goalkeeper's left  
    {
        if (canBlock)
        {
            goalkeeperAnimator.SetTrigger("left");
            Debug.Log("Goalkeeper diving LEFT - Good reaction!");
        }
        else
        {
            goalkeeperAnimator.SetTrigger("left");
            Debug.Log("Goalkeeper diving LEFT - Too slow!");
        }
    }
}



private bool CalculateBlockingChance()
{
    float distanceToGoal = Vector3.Distance(ball.position, goalkeeperAnimator.transform.position);
    float normalizedPower = _power / 30f;
    
    // Calculate success chance based on various factors
    float distanceBonus = Mathf.Clamp01(distanceToGoal / 30f); // More distance = better chance
    float powerPenalty = normalizedPower; // More power = harder to block
    float reactionSpeedBonus = goalkeeperReactionSpeed / 2f; // Goalkeeper skill
    
    float blockingChance = (distanceBonus + reactionSpeedBonus - powerPenalty) * 0.5f;
    blockingChance = Mathf.Clamp01(blockingChance);
    
    // Add some randomness
    float randomResult = UnityEngine.Random.Range(0f, 1f);
    
    Debug.Log($"Blocking chance: {blockingChance:F2}, Random: {randomResult:F2}");
    
    return randomResult < blockingChance;
}

private void SetGoalkeeperAnimationSpeed(float urgency)
{
    if (goalkeeperAnimator == null) return;
    
    // urgency: 0 = relaxed, 1 = very urgent
    float animationSpeed = Mathf.Lerp(0.8f, 1.5f, urgency);
    goalkeeperAnimator.speed = animationSpeed;
    
    Debug.Log($"Goalkeeper animation speed: {animationSpeed:F2}");
}
private void ResetGoalkeeperAnimation()
{
    if (goalkeeperAnimator == null) return;
    
    // Reset the goalkeeper back to idle state
    goalkeeperAnimator.SetTrigger("reset");
    goalkeeperAnimator.transform.position = new Vector3(0, 0.27f, 5.55f);
    goalkeeperAnimator.transform.eulerAngles = new Vector3(0, 180f, 0);
    Debug.Log("Goalkeeper reset to idle");
}
}
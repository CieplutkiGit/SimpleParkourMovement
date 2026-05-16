using UnityEngine;

[RequireComponent(typeof(MovementSystem), typeof(JumpSystem), typeof(LedgeGrabSystem))]
[RequireComponent(typeof(GroundDetector), typeof(WallDetector), typeof(LedgeDetector))]
[RequireComponent(typeof(WallRunSystem))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;

    public Rigidbody RB { get; private set; }
    public IInputProvider InputProvider { get; private set; }
    
    public MovementSystem Movement { get; private set; }
    public JumpSystem Jump { get; private set; }
    public LedgeGrabSystem LedgeGrab { get; private set; }
    public WallRunSystem WallRun { get; private set; }
    
    public IGroundDetector GroundDetector { get; private set; }
    public IWallDetector WallDetector { get; private set; }
    public ILedgeDetector LedgeDetector { get; private set; }

    public StateMachine StateMachine { get; private set; }
    public GroundedState GroundedState { get; private set; }
    public AirborneState AirborneState { get; private set; }
    public FallingState FallingState { get; private set; }
    public WallRunState WallRunState { get; private set; }
    public WallRunUpState WallRunUpState { get; private set; }
    public LedgeGrabState LedgeGrabState { get; private set; }
    public ShimmyState ShimmyState { get; private set; }
    public ClimbUpState ClimbUpState { get; private set; }

    private void Awake()
    {
        if (_inputReader == null)
            Debug.LogError("FATAL: InputReader is not assigned in PlayerController Inspector!");

        RB = GetComponent<Rigidbody>();
        InputProvider = _inputReader;

        Movement = GetComponent<MovementSystem>();
        Jump = GetComponent<JumpSystem>();
        LedgeGrab = GetComponent<LedgeGrabSystem>();
        WallRun = GetComponent<WallRunSystem>();
        
        GroundDetector = GetComponent<IGroundDetector>();
        WallDetector = GetComponent<IWallDetector>();
        LedgeDetector = GetComponent<ILedgeDetector>();

        StateMachine = new StateMachine();
        GroundedState = new GroundedState(this);
        AirborneState = new AirborneState(this);
        FallingState = new FallingState(this);
        WallRunState = new WallRunState(this);
        WallRunUpState = new WallRunUpState(this);
        LedgeGrabState = new LedgeGrabState(this);
        ShimmyState = new ShimmyState(this);
        ClimbUpState = new ClimbUpState(this);
    }

    private void Start()
    {
        StateMachine.ChangeState(GroundedState);
    }

    private void Update()
    {
        StateMachine.Tick();
    }

    private void FixedUpdate()
    {
        StateMachine.FixedTick();
    }
}
using UnityEngine;

[RequireComponent(typeof(MovementSystem), typeof(JumpSystem), typeof(IGroundDetector))]
public class PlayerController : MonoBehaviour
{
    public Rigidbody RB { get; private set; }
    public MovementSystem Movement { get; private set; }
    public JumpSystem Jump { get; private set; }
    public IGroundDetector GroundDetector { get; private set; }

    public StateMachine StateMachine { get; private set; }
    public GroundedState GroundedState { get; private set; }
    public AirborneState AirborneState { get; private set; }

    private void Awake()
    {
        RB = GetComponent<Rigidbody>();
        Movement = GetComponent<MovementSystem>();
        Jump = GetComponent<JumpSystem>();
        GroundDetector = GetComponent<IGroundDetector>();

        StateMachine = new StateMachine();
        GroundedState = new GroundedState(this);
        AirborneState = new AirborneState(this);
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
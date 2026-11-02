using UnityEngine;

namespace Genesis.Character
{
    /// <summary>Samples the existing legacy bindings once per motor frame.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerMovementInput : MonoBehaviour
    {
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string jumpButton = "Jump";
        [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode alternateRunKey = KeyCode.RightShift;
        [Tooltip("Hold to sprint. Shift-to-run and Space-to-jump are preserved.")]
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftAlt;

        public readonly struct Frame
        {
            public readonly Vector2 Move;
            public readonly bool Run;
            public readonly bool Sprint;
            public readonly bool Jump;

            public Frame(Vector2 move, bool run = false, bool sprint = false, bool jump = false)
            {
                Move = move;
                Run = run;
                Sprint = sprint;
                Jump = jump;
            }
        }

        public Frame Read()
        {
            return new Frame(new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis)),
                Input.GetKey(runKey) || Input.GetKey(alternateRunKey), Input.GetKey(sprintKey),
                Input.GetButtonDown(jumpButton));
        }
    }
}

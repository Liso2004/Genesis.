using UnityEngine;

namespace Genesis.Character
{
    /// <summary>
    /// Base identity component for anything that exists as an
    /// active actor in the Genesis simulation.
    /// </summary>
    public sealed class GenesisActor : MonoBehaviour
    {
        [Header("Identity")]

        [SerializeField]
        private string actorId;

        [SerializeField]
        private ActorType actorType = ActorType.Player;

        public string ActorId => actorId;

        public ActorType ActorType => actorType;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                actorId = System.Guid.NewGuid().ToString();
            }
        }
    }

    public enum ActorType
    {
        Player,
        NPC,
        Enemy,
        Companion
    }
}
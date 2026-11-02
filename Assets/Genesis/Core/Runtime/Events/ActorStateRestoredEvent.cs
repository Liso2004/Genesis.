namespace Genesis.Core.Events
{
    /// <summary>A completed restore, not new gameplay progress or newly acquired rewards.</summary>
    public readonly struct ActorStateRestoredEvent : IGenesisEvent
    {
        public readonly string ActorId;
        public ActorStateRestoredEvent(string actorId) { ActorId = actorId; }
    }
}

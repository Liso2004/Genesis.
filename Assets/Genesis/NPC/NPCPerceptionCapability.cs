using UnityEngine;

namespace Genesis.NPC
{
    public static class NPCPerceptionCapability
    {
        public static float GetRangeMultiplier(
            int masteryStage)
        {
            switch (masteryStage)
            {
                case 0:
                    return 0.85f;

                case 1:
                    return 1.0f;

                case 2:
                    return 1.1f;

                case 3:
                    return 1.2f;

                default:
                    return 1.25f;
            }
        }

        public static float GetFieldOfViewMultiplier(
            int masteryStage)
        {
            switch (masteryStage)
            {
                case 0:
                    return 0.85f;

                case 1:
                    return 1.0f;

                case 2:
                    return 1.05f;

                case 3:
                    return 1.1f;

                default:
                    return 1.15f;
            }
        }
    }
}
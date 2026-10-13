namespace Combat.AttackIndicators
{
    // Zero preserves the behavior of existing serialized attacks and default HitInfo values.
    public enum AttackDefense { Basic = 0, ParryOnly = 1, DodgeOnly = 2 }
    public enum GuardResponse { Hit, Block, Parry }

    public static class AttackDefenseRules
    {
        public static GuardResponse Resolve(AttackDefense attack, bool guarding, bool parrying, bool facing)
        {
            if (!guarding || !facing || attack == AttackDefense.DodgeOnly) return GuardResponse.Hit;
            if (parrying) return GuardResponse.Parry;
            return attack == AttackDefense.Basic ? GuardResponse.Block : GuardResponse.Hit;
        }
    }
}

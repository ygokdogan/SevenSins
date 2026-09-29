using System;
using System.Collections.Generic;

namespace Player.Movement
{
    [Flags]
    public enum MovementRestriction
    {
        None = 0,
        Move = 1,
        Sprint = 2
    }

    public class MovementRestrictions
    {
        private readonly Dictionary<object, MovementRestriction> restrictions = new();
        private readonly Dictionary<object, float> speedModifiers = new();

        public bool CanMove => !Contains(MovementRestriction.Move);
        public bool CanSprint => CanMove && !Contains(MovementRestriction.Sprint);

        public float SpeedMultiplier
        {
            get
            {
                float multiplier = 1f;
                foreach (float value in speedModifiers.Values) multiplier *= value;
                return multiplier;
            }
        }

        public void Add(object source, MovementRestriction restriction)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            restrictions.TryGetValue(source, out MovementRestriction current);
            restrictions[source] = current | restriction;
        }

        public void Remove(object source, MovementRestriction restriction)
        {
            if (!restrictions.TryGetValue(source, out MovementRestriction current)) return;
            current &= ~restriction;
            if (current == MovementRestriction.None) restrictions.Remove(source);
            else restrictions[source] = current;
        }

        public void SetSpeedMultiplier(object source, float multiplier)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            speedModifiers[source] = multiplier;
        }

        public void RemoveSpeedMultiplier(object source) => speedModifiers.Remove(source);

        public void RemoveAll(object source)
        {
            restrictions.Remove(source);
            speedModifiers.Remove(source);
        }

        private bool Contains(MovementRestriction restriction)
        {
            foreach (MovementRestriction value in restrictions.Values)
                if ((value & restriction) != 0) return true;
            return false;
        }
    }
}

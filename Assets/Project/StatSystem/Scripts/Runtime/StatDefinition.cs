using UnityEngine;

namespace StatSystem
{

    [CreateAssetMenu(fileName = "StatDefinition", menuName = "StatSystem/StatDefinition", order = 0)]
    public class StatDefinition : ScriptableObject
    {
        // The initial value of this stat
        [SerializeField] private int m_BaseValue;

        // The maximum value this stat can have 
        [SerializeField] private int m_Capacity = -1;
        public int baseValue => m_BaseValue;
        public int capacity => m_Capacity;
    }

}

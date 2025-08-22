using UnityEngine;

namespace PuppyDragon.uNody
{
    public abstract class BlackboardVar : ScriptableObject
    {
        [SerializeField]
        private string key;

        public string Key { get => key; set => key = value; }
        public abstract object Value { get; set; }
    }
}

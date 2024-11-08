using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.DAI
{
    public class CustomInspector<T> : SingletonScriptableObject<T> where T : ScriptableObject, ICustomInspector
    {
        [SerializeField] DAInspector _inspector;
        public DAInspector Inspector { get => _inspector; set => _inspector = value; }

        [SerializeField] InspectorData _data;
        public InspectorData Data { get => _data; set => _data = value; }

        [SerializeField] ColorScheme _colorScheme;

        protected override void OnCreateInstance()
        {
            Init();
        }

        protected override void OnExitPlayMode()
        {
            Init();
        }

        public void Init()
        {
            _inspector = new DAInspector();
            _inspector.Init(_data, _colorScheme);
        }
    }
}
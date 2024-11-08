using DA_Assets.FCU.Model;
using System;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public struct PrefabStruct
    {
        [SerializeField] SyncHelper current;
        public SyncHelper Current { get => current; set => current = value; }

        [SerializeField] SyncHelper parent;
        public SyncHelper Parent { get => parent; set => parent = value; }

        [Space]

        [SerializeField] SyncHelper prefab;
        public SyncHelper Prefab { get => prefab; set => prefab = value; }
        [SerializeField] SyncHelper instantiatedPrefab;
        public SyncHelper InstantiatedPrefab { get => instantiatedPrefab; set => instantiatedPrefab = value; }

        [Space]

        [SerializeField] SyncHelper[] childs;
        public SyncHelper[] Childs { get => childs; set => childs = value; }

        [Space]

        [SerializeField] int siblingIndex;
        public int SiblingIndex { get => siblingIndex; set => siblingIndex = value; }

        [SerializeField] int hash;
        public int Hash { get => hash; set => hash = value; }

        [SerializeField] string id;
        public string Id { get => id; set => id = value; }

        [SerializeField] int prefabNumber;
        public int PrefabNumber { get => prefabNumber; set => prefabNumber = value; }

        [SerializeField] string prefabPath;
        public string PrefabPath { get => prefabPath; set => prefabPath = value; }
    }
}

using UnityEngine;

[System.Serializable]
public class ZombieSpawnOption
{
    public GameObject prefab;
    [Range(0f, 100f)] public float weight = 1f;
}

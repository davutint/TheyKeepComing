using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "DeadWalls/Mobile Castle/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        public string Id = "zombie_basic";
        public GameObject Prefab;

        [Header("Base Combat Stats")]
        [Min(1f)] public float BaseHP = 20f;
        [Min(0f)] public float BaseDamage = 5f;
        [Min(0.05f)] public float BaseMoveSpeed = 0.85f;
        [Min(0.01f)] public float Scale = 1.4f;
        [Min(0)] public int XPReward = 10;

        [Header("Spawn / Future Pool Metadata")]
        [Min(0.01f)] public float SpawnWeight = 1f;
        [Min(0)] public int PoolPrewarm = 128;
        [Min(1)] public int PoolExpandBatch = 128;

        public List<string> ValidateDefinition()
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) problems.Add("Enemy Id bos.");
            if (Prefab == null) problems.Add($"'{Id}' prefab bos.");
            if (BaseHP <= 0f) problems.Add($"'{Id}' BaseHP sifirdan buyuk olmali.");
            if (BaseDamage < 0f) problems.Add($"'{Id}' BaseDamage negatif olamaz.");
            if (BaseMoveSpeed <= 0f) problems.Add($"'{Id}' BaseMoveSpeed sifirdan buyuk olmali.");
            if (Scale <= 0f) problems.Add($"'{Id}' Scale sifirdan buyuk olmali.");
            if (SpawnWeight <= 0f) problems.Add($"'{Id}' SpawnWeight sifirdan buyuk olmali.");
            if (PoolPrewarm < 0) problems.Add($"'{Id}' PoolPrewarm negatif olamaz.");
            if (PoolExpandBatch <= 0) problems.Add($"'{Id}' PoolExpandBatch sifirdan buyuk olmali.");
            return problems;
        }
    }
}

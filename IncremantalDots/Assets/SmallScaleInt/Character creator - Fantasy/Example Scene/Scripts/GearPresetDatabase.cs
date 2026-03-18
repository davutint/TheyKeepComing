// Assets/CharacterCreator/Editor/GearPresetDatabase.cs
using UnityEngine;
using System.Collections.Generic;

namespace SmallScaleInc.CharacterCreatorFantasy
{
    [CreateAssetMenu(
        fileName = "GearPresetDatabase",
        menuName = "Character Creator/Gear Preset Database")]
    public class GearPresetDatabase : ScriptableObject
    {
        [Tooltip("All of your saved presets — this asset is serialized to disk.")]
        public List<GearPresetManager.GearPreset> presets = new List<GearPresetManager.GearPreset>();
    }
}

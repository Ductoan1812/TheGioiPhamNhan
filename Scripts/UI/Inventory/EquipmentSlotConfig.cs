using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Equipment Slots", fileName = "EquipmentSlotConfig")]
public class EquipmentSlotConfig : ScriptableObject
{
    public List<string> slotIds = new() { "weapon_L", "weapon_R", "helmet", "armor", "ring_L", "ring_R", "foot", "body", "pet" };
}

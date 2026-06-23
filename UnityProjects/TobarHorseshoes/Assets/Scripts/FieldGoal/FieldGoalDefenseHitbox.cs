using UnityEngine;

public class FieldGoalDefenseHitbox : MonoBehaviour
{
    public FieldGoalDefenseController controller;
    public int blockerIndex;
    public string blockerLabel = "TIMING BAR";

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(blockerLabel))
        {
            return blockerLabel;
        }

        return "TIMING BAR " + (blockerIndex + 1);
    }
}

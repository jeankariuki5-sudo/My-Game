using UnityEngine;


[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Brotato Clone/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    public string upgradeName;

    public enum StatType
    {
        Damage,
        FireRate,
        Speed,
        MaxHealth
    }

    public StatType stat; //This will hold the stat this upgrade affects
    public float value; //How much to change the stat
    public int cost; //base material cost

    [Header("Cost Scaling")]
    [Tooltip("How much the cost increases each time the threshold below is hit")]
    public int costIncreaseAmount = 2;
    [Tooltip("Cost increases every N purchases (e.g. 3 = every 3rd purchase)")]
    public int purchasesBeforeIncrease = 3;

    public Sprite icon; //icon for the button
}

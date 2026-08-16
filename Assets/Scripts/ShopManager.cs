using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Shop UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button continueButton;

    [SerializeField] public Button damageButton;
    [SerializeField] public Button fireRateButton;
    [SerializeField] public Button speedButton;
    [SerializeField] public Button healthButton;
    [SerializeField] public TextMeshProUGUI materialsText;


    [Header("List of Upgrades")]
    [SerializeField] public List<UpgradeSO> allUpgrades;


    // Buy gun from shop
    [Header("Gun Purchase")]
    [SerializeField] private Button[] buyGunButtons;
    [SerializeField] private TextMeshProUGUI gunCountText;
    [SerializeField] private int[] gunCosts;
    [SerializeField] private List<Gun> gunpool;

    [Header("Gun Slot Swap UI (shown when all 6 slots are full)")]
    [SerializeField] private GameObject gunSlotSelectionPanel;
    [SerializeField] private Button[] gunSlotButtons; // one per active gun slot (expects 6)
    [SerializeField] private Button cancelGunSlotSelectionButton;

    // which gunpool index the player is trying to buy while slot selection is open
    private int pendingGunPoolIndex = -1;

    // tracks purchases per upgrade so cost can escalate over time
    private Dictionary<UpgradeSO, int> upgradePurchaseCounts = new Dictionary<UpgradeSO, int>();


    [Header("Audio Settings")]
    [SerializeField] private AudioClip popSound;
    [SerializeField] private AudioClip purchaseSound;
    [SerializeField] float volume = 0.4f;
    private AudioSource audioSource;


    // On closed event listener
    private Action onShopClosed;

    private bool isShopOpen = false;

    public static ShopManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // Ensure shop panelis hidden at start
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        // Ensure gun slot selection panel is hidden at start
        if (gunSlotSelectionPanel != null)
        {
            gunSlotSelectionPanel.SetActive(false);
        }

        if (cancelGunSlotSelectionButton != null)
        {
            cancelGunSlotSelectionButton.onClick.RemoveAllListeners();
            cancelGunSlotSelectionButton.onClick.AddListener(CancelGunSlotSelection);
        }

        // Connect continue button
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(CloseShop);
        }

        // Connect damage button
        // RemoveAllListeners() first guards against a double-charge: if this button also
        // has an entry in its own On Click() list in the Inspector (easy to add by
        // accident while poking around), every click would fire BuyDamage() twice - two
        // deductions computed a purchase-count apart, while CostText only ever shows the
        // state after both fired. That mismatch between what's shown and what's charged
        // is exactly what "cost and deduction don't match" looks like.
        if (damageButton != null)
        {
            damageButton.onClick.RemoveAllListeners();
            damageButton.onClick.AddListener(BuyDamage);
        }

        // connect firerate button
        if (fireRateButton != null)
        {
            fireRateButton.onClick.RemoveAllListeners();
            fireRateButton.onClick.AddListener(BuyFireRate);
        }

        // Connect speed button
        if (speedButton != null)
        {
            speedButton.onClick.RemoveAllListeners();
            speedButton.onClick.AddListener(BuySpeed);
        }

        // connect health button
        if (healthButton != null)
        {
            healthButton.onClick.RemoveAllListeners();
            healthButton.onClick.AddListener(BuyHealth);
        }

        // Wire up each gun button
        for (int i = 0; i < buyGunButtons.Length && i < gunpool.Count; i++)
        {
            int index = i;
            if (buyGunButtons[index] != null)
            {
                buyGunButtons[index].onClick.RemoveAllListeners();
                buyGunButtons[index].onClick.AddListener(() => BuySpecificGun(index));
            }
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = popSound;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;


    }

    void FixedUpdate()
    {
        UpdateMaterialsUI();
        UpdateGunCountUI();
        UpdateGunButtonCosts();
        UpdateUpgradeButtonCosts();
    }


    // Note: the shop is opened by WaveManager (after its "Wave Cleared" text delay, or
    // immediately on a wave timeout), never in response to GameEvents.OnWaveCleared directly.
    // A previous version subscribed to that event here too, which caused the shop to open twice.



    // this opens the shop panel and call this from wavemanager after a wave is cleared
    public void OpenShop(Action onClosedCallBack)
    {
        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }
        onShopClosed = onClosedCallBack;

        // Show panel
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        isShopOpen = true;

        // pause the game
        Time.timeScale = 0f;


        // Broadcast that the shop is open
        GameEvents.ShopOpened();


        // Refresh every shop display immediately - can't rely on FixedUpdate() for this,
        // since Time.timeScale = 0f above means FixedUpdate effectively stops firing while
        // the shop is open. Without this, CostText/etc. would show whatever was left over
        // from the last time the shop was open (or nothing, the very first time), while the
        // actual cost charged on purchase is always computed live and correct - a mismatch
        // between what's displayed and what gets deducted.
        UpdateMaterialsUI();
        UpdateGunCountUI();
        UpdateGunButtonCosts();
        UpdateUpgradeButtonCosts();

        Debug.Log("shop opened");
    }

    public void CloseShop()
    {
        // Hide shop panel
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        isShopOpen = false;

        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }

        // Resume Game - unless the pause menu is also up, in which case leave it frozen;
        // PauseManager's own ResumeGame() will unfreeze it once the player actually resumes
        if (PauseManager.Instance == null || !PauseManager.Instance.IsPaused())
        {
            Time.timeScale = 1f;
        }

        // /notify wavemanager to start countdown
        onShopClosed?.Invoke();
        // Booadcast tha shop is closed
        GameEvents.ShopClosed();
    }

    public bool IsShopOpen() => isShopOpen;

    public void BuyDamage()
    {
        PurchaseUpgrade(GetUpgradeByStat(UpgradeSO.StatType.Damage));
    }

    public void BuyFireRate()
    {
        PurchaseUpgrade(GetUpgradeByStat(UpgradeSO.StatType.FireRate));
    }

    public void BuySpeed()
    {
        PurchaseUpgrade(GetUpgradeByStat(UpgradeSO.StatType.Speed));
    }

    public void BuyHealth()
    {
        PurchaseUpgrade(GetUpgradeByStat(UpgradeSO.StatType.MaxHealth));
    }

    // Helper method to find the first upgrade of any given stat
    private UpgradeSO GetUpgradeByStat(UpgradeSO.StatType stat)
    {
        foreach (UpgradeSO upgrade in allUpgrades)
        {
            if (upgrade.stat == stat)
            {
                return upgrade;
            }
        }
        return null;
    }

    // core purchase logic for upgrades
    private void PurchaseUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null)
        {
            return;
        }

        int currentCost = GetCurrentUpgradeCost(upgrade);

        if (Player.Instance.GetMaterials() >= currentCost)
        {
            // deduct currency
            Player.Instance.AddMaterials(-currentCost);

            // apply the upgrade
            Player.Instance.ApplyUpgrade(upgrade);

            // track purchase count so future cost can escalate
            if (!upgradePurchaseCounts.ContainsKey(upgrade))
            {
                upgradePurchaseCounts[upgrade] = 0;
            }
            upgradePurchaseCounts[upgrade]++;

            // broadcast when player purchases an upgrade
            GameEvents.UpgradePurchased(upgrade);

            // Update currrency UI
            UpdateMaterialsUI();
            UpdateUpgradeButtonCosts();
        }
        else
        {
            Debug.Log("Not Enough Currency");
        }

        PlayPurchaseSound();
    }

    // Calculates the current cost of an upgrade based on how many times it's been bought.
    // Cost increases by upgrade.costIncreaseAmount every upgrade.purchasesBeforeIncrease purchases.
    private int GetCurrentUpgradeCost(UpgradeSO upgrade)
    {
        int purchases = upgradePurchaseCounts.TryGetValue(upgrade, out int count) ? count : 0;

        int threshold = Mathf.Max(upgrade.purchasesBeforeIncrease, 1); // avoid divide by zero
        int increments = purchases / threshold;

        return upgrade.cost + increments * upgrade.costIncreaseAmount;
    }

    // Refreshes the cost shown on each upgrade button's cost display (a child "CostText"
    // object, not the button's own label - keeps your custom button styling untouched)
    private void UpdateUpgradeButtonCosts()
    {
        UpdateSingleUpgradeCostText(damageButton, UpgradeSO.StatType.Damage);
        UpdateSingleUpgradeCostText(fireRateButton, UpgradeSO.StatType.FireRate);
        UpdateSingleUpgradeCostText(speedButton, UpgradeSO.StatType.Speed);
        UpdateSingleUpgradeCostText(healthButton, UpgradeSO.StatType.MaxHealth);
    }

    private void UpdateSingleUpgradeCostText(Button button, UpgradeSO.StatType stat)
    {
        if (button == null) return;

        UpgradeSO upgrade = GetUpgradeByStat(stat);
        if (upgrade == null) return;

        // Searches all descendants, not just direct children - CostText can live
        // nested inside another child object (e.g. under an Image), not only directly
        // under the button itself
        Transform costTransform = FindDeepChild(button.transform, "CostText");
        if (costTransform == null) return;

        TextMeshProUGUI costText = costTransform.GetComponent<TextMeshProUGUI>();
        if (costText == null) return;

        costText.text = "" + GetCurrentUpgradeCost(upgrade);
    }

    // Recursively searches a transform's entire hierarchy (children, grandchildren, etc.)
    // for the first child whose name matches, unlike Transform.Find which only checks
    // direct children.
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;

            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // Buy specific gun  by index(0, 1, 2, 3)
    private void BuySpecificGun(int gunIndex)
    {
        // validate index
        if (gunIndex < 0 || gunIndex >= gunpool.Count)
        {
            return;
        }

        GunManager gunManager = FindObjectOfType<GunManager>();
        if (gunManager == null)
        {
            return;
        }

        // Check cost of gun against player coins/materials
        int cost = gunCosts[gunIndex];
        if (Player.Instance.GetMaterials() < cost)
        {
            return;
        }

        // Check if gun exists in gunpool
        Gun gun = gunpool[gunIndex];
        if (gun == null)
        {
            return;
        }

        // If all 6 slots are full, let the player pick which one to replace instead of blocking the buy
        if (gunManager.GetActiveGunCount() >= 6)
        {
            OpenGunSlotSelection(gunIndex);
            return;
        }

        // Deduct coins for gun purchase
        Player.Instance.AddMaterials(-cost);

        // give player the gun
        gunManager.AddGun(gun);

        UpdateMaterialsUI();
        UpdateGunCountUI();
        UpdateGunButtonCosts();
        PlayPurchaseSound();


    }

    // Opens the slot-picker UI so the player can choose which equipped gun to replace
    private void OpenGunSlotSelection(int gunPoolIndex)
    {
        GunManager gunManager = FindObjectOfType<GunManager>();
        if (gunManager == null) return;

        pendingGunPoolIndex = gunPoolIndex;

        List<Gun> currentGuns = gunManager.GetActiveGuns();

        if (gunSlotSelectionPanel != null)
        {
            gunSlotSelectionPanel.SetActive(true);
        }

        for (int i = 0; i < gunSlotButtons.Length; i++)
        {
            if (gunSlotButtons[i] == null) continue;

            // show the icon of the gun currently occupying this slot
            Transform iconTransform = gunSlotButtons[i].transform.Find("Icon");
            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();
                if (icon != null)
                {
                    bool hasGun = i < currentGuns.Count && currentGuns[i] != null;
                    icon.enabled = hasGun && currentGuns[i].icon != null;
                    if (hasGun)
                    {
                        icon.sprite = currentGuns[i].icon;
                    }
                }
            }

            int slotIndex = i; // capture for closure
            gunSlotButtons[i].onClick.RemoveAllListeners();
            gunSlotButtons[i].onClick.AddListener(() => ConfirmGunSlotReplace(slotIndex));
        }
    }

    // Called when the player taps a slot in the slot-picker UI
    private void ConfirmGunSlotReplace(int slotIndex)
    {
        if (pendingGunPoolIndex < 0) return;

        GunManager gunManager = FindObjectOfType<GunManager>();
        if (gunManager == null)
        {
            CancelGunSlotSelection();
            return;
        }

        int cost = gunCosts[pendingGunPoolIndex];

        // re-check funds in case player spent materials elsewhere while panel was open
        if (Player.Instance.GetMaterials() < cost)
        {
            CancelGunSlotSelection();
            return;
        }

        Gun gunPrefab = gunpool[pendingGunPoolIndex];
        if (gunPrefab == null)
        {
            CancelGunSlotSelection();
            return;
        }

        Player.Instance.AddMaterials(-cost);
        gunManager.ReplaceGun(slotIndex, gunPrefab);

        CloseGunSlotSelection();

        UpdateMaterialsUI();
        UpdateGunCountUI();
        UpdateGunButtonCosts();
        PlayPurchaseSound();
    }

    // Player backed out of the slot swap without picking one
    private void CancelGunSlotSelection()
    {
        CloseGunSlotSelection();
    }

    private void CloseGunSlotSelection()
    {
        pendingGunPoolIndex = -1;
        if (gunSlotSelectionPanel != null)
        {
            gunSlotSelectionPanel.SetActive(false);
        }
    }

    private void UpdateGunCountUI()
    {
        if (gunCountText != null)
        {
            GunManager gunManager = FindObjectOfType<GunManager>();
            int count = 0;
            if (gunManager != null)
            {
                count = gunManager.GetActiveGunCount();
            }
            gunCountText.text = "Guns: " + count + "/6";
        }
    }

    private void UpdateGunButtonCosts()
    {
        for (int i = 0; i < buyGunButtons.Length; i++)
        {
            // button text is set up in the editor and no longer overwritten here
        }
    }

    private void UpdateMaterialsUI()
    {
        if (materialsText != null)
        {
            materialsText.text = "" + Player.Instance.GetMaterials();
        }
    }

    private void PlayPurchaseSound()
    {
        if (purchaseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(purchaseSound, volume);
        }
    }

}
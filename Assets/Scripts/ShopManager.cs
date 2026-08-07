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

    void FixedUpdate()
    {
        UpdateMaterialsUI();
        UpdateGunCountUI();
        UpdateGunButtonCosts();
        UpdateUpgradeButtonCosts();
    }

    private void Awake()
    {

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
            cancelGunSlotSelectionButton.onClick.AddListener(CancelGunSlotSelection);
        }

        // Connect continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(CloseShop);
        }

        // Connect damage button
        if (damageButton != null)
        {
            damageButton.onClick.AddListener(BuyDamage);
        }

        // connect firerate button
        if (fireRateButton != null)
        {
            fireRateButton.onClick.AddListener(BuyFireRate);
        }

        // Connect speed button
        if (speedButton != null)
        {
            speedButton.onClick.AddListener(BuySpeed);
        }

        // connect health button
        if (healthButton != null)
        {
            healthButton.onClick.AddListener(BuyHealth);
        }

        // Wire up each gun button
        for (int i = 0; i < buyGunButtons.Length && i < gunpool.Count; i++)
        {
            int index = i;
            if (buyGunButtons[index] != null)
            {
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


    // subscribe to wavecleared event
    private void OnEnable()
    {
        GameEvents.OnWaveCleared += OpenShopFromEvent;
    }

    private void OnDisable()
    {
        GameEvents.OnWaveCleared -= OpenShopFromEvent;
    }

    private void OpenShopFromEvent()
    {
        OpenShop(null);
    }



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

        // pause the game
        Time.timeScale = 0f;


        // Broadcast that the shop is open
        GameEvents.ShopOpened();


        UpdateMaterialsUI();
        Debug.Log("shop opened");
    }

    public void CloseShop()
    {
        // Hide shop panel
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
        }

        // Resume Game
        Time.timeScale = 1f;

        // /notify wavemanager to start countdown
        onShopClosed?.Invoke();
        // Booadcast tha shop is closed
        GameEvents.ShopClosed();
    }

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

    // Refreshes the cost shown on each upgrade button's label
    private void UpdateUpgradeButtonCosts()
    {
        UpdateSingleUpgradeButtonLabel(damageButton, UpgradeSO.StatType.Damage);
        UpdateSingleUpgradeButtonLabel(fireRateButton, UpgradeSO.StatType.FireRate);
        UpdateSingleUpgradeButtonLabel(speedButton, UpgradeSO.StatType.Speed);
        UpdateSingleUpgradeButtonLabel(healthButton, UpgradeSO.StatType.MaxHealth);
    }

    private void UpdateSingleUpgradeButtonLabel(Button button, UpgradeSO.StatType stat)
    {
        if (button == null) return;

        UpgradeSO upgrade = GetUpgradeByStat(stat);
        if (upgrade == null) return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = upgrade.upgradeName + " (" + GetCurrentUpgradeCost(upgrade) + ")";
        }
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

            // label each slot button with the gun currently occupying it
            TextMeshProUGUI label = gunSlotButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && i < currentGuns.Count && currentGuns[i] != null)
            {
                label.text = currentGuns[i].name;
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
            if (buyGunButtons[i] != null)
            {
                //  get buttons text child
                TextMeshProUGUI btnText = buyGunButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null && i < gunpool.Count)
                {
                    Gun gun = gunpool[i];
                    string gunName = gun != null ? gun.name : "Gun";
                    btnText.text = gunName + "(" + gunCosts[i] + ")";
                }
            }
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

using BepInEx;
using HarmonyLib;
using Pigeon.Movement;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class GunDataDisplayMod : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.displaygunstats";
    public const string PluginName = "DisplayGunStats";
    public const string PluginVersion = "1.1.0";

    private Harmony harmony;
    private static Gun currentGun;
    private static FieldInfo playerField;
    private static PropertyInfo activeProp;
    private float updateTimer = 0f;
    private const float UpdateInterval = 0.5f;
    private static bool hudEnabled = true;

    private static readonly Color sky = new Color(0.529f, 0.808f, 0.922f);
    private static readonly Color orchid = new Color(0.855f, 0.439f, 0.839f);
    private static readonly Color rose = new Color(0.8901960784313725f, 0.1411764705882353f, 0.16862745098039217f);
    private static readonly Color macaroon = new Color(0.9764705882352941f, 0.8784313725490196f, 0.4627450980392157f);
    private static readonly Color shamrock = new Color(0.011764705882352941f, 0.6745098039215687f, 0.07450980392156863f);

    private GameObject hudContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI[] statTexts;
    private const int NUM_STAT_LINES = 25;

    private void Awake()
    {
        var harmony = new Harmony(PluginGUID);
        harmony.PatchAll();
        Logger.LogInfo($"{PluginName} loaded successfully.");

        playerField = typeof(Gun).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);
        activeProp = typeof(IGear).GetProperty("Active");
    }

    private void CreateHUD()
    {
        if (hudContainer != null) return;

        var parent = Player.LocalPlayer.PlayerLook.Reticle;
        hudContainer = new GameObject("GunStatsHUD");
        hudContainer.transform.SetParent(parent, false);

        var containerRect = hudContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.18f, 1.08f);
        containerRect.anchorMax = new Vector2(0.18f, 1.08f);
        containerRect.anchoredPosition = new Vector2(0f, 0f);
        containerRect.sizeDelta = new Vector2(350f, 400f);

        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(hudContainer.transform, false);
        titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 18;
        titleText.color = Color.white;
        titleText.enableWordWrapping = false;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = new Vector2(1f, 0f);
        titleRect.anchoredPosition = new Vector2(0f, 0f);
        titleRect.sizeDelta = new Vector2(0f, 25f);

        statTexts = new TextMeshProUGUI[NUM_STAT_LINES];
        for (int i = 0; i < NUM_STAT_LINES; i++)
        {
            var statGO = new GameObject($"StatText{i}");
            statGO.transform.SetParent(hudContainer.transform, false);
            statTexts[i] = statGO.AddComponent<TextMeshProUGUI>();
            statTexts[i].fontSize = 16;
            statTexts[i].color = Color.white;
            statTexts[i].enableWordWrapping = false;
            statTexts[i].alignment = TextAlignmentOptions.Left;
            statTexts[i].verticalAlignment = VerticalAlignmentOptions.Middle;
            var statRect = statGO.GetComponent<RectTransform>();
            statRect.anchorMin = Vector2.zero;
            statRect.anchorMax = new Vector2(1f, 0f);
            statRect.anchoredPosition = new Vector2(0f, -(i * 18f + 25f));
            statRect.sizeDelta = new Vector2(0f, 18f);
        }
    }

    private void UpdateHUD()
    {
        if (hudContainer == null || titleText == null || statTexts == null) return;

        var containerRect = hudContainer.GetComponent<RectTransform>();
        float yOffset = 0f;
        GameObject damageMeterHUD = GameObject.Find("DamageMeterHUD");
        if (damageMeterHUD != null && damageMeterHUD.activeSelf)
        {
            yOffset -= 100f;
        }
        GameObject speedometerHUD = GameObject.Find("SpeedometerHUD");
        if (speedometerHUD != null && speedometerHUD.activeSelf)
        {
            yOffset -= 25f;
        }
        containerRect.anchoredPosition = new Vector2(0f, yOffset);

        if (currentGun == null)
        {
            titleText.text = "No Gun Active";
            for (int i = 0; i < statTexts.Length; i++)
            {
                statTexts[i].text = "";
            }
            return;
        }

        IWeapon weapon = (IWeapon)currentGun;
        UpgradeStatChanges statChanges = new UpgradeStatChanges();
        ref GunData data = ref weapon.GunData;

        Dictionary<string, StatInfo> primaryStats = new Dictionary<string, StatInfo>();
        var primaryEnum = weapon.EnumeratePrimaryStats(statChanges);
        while (primaryEnum.MoveNext())
        {
            primaryStats[primaryEnum.Current.name] = primaryEnum.Current;
        }

        Dictionary<string, StatInfo> secondaryStats = new Dictionary<string, StatInfo>();
        var secondaryEnum = weapon.EnumerateSecondaryStats(statChanges);
        while (secondaryEnum.MoveNext())
        {
            if (secondaryEnum.Current.name != "Aim Zoom")
            {
                secondaryStats[secondaryEnum.Current.name] = secondaryEnum.Current;
            }
        }

        List<string> lines = new List<string>();
        lines.Add("Current Gun Stats:");

        AddStatFromEnum(ref lines, secondaryStats, "Damage");
        AddStatFromEnum(ref lines, primaryStats, "Damage Type");
        AddStatFromEnum(ref lines, secondaryStats, "Fire Rate");
        lines.Add($"Burst Size: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.burstSize}</color>");
        lines.Add($"Burst Interval: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.burstFireInterval.ToString("F2")}</color>");
        AddStatFromEnumWithCustomLabel(ref lines, secondaryStats, "Ammo Capacity", "Magazine Size");
        lines.Add($"Ammo Capacity: <color=#{ColorUtility.ToHtmlStringRGB(sky)}>{data.ammoCapacity}</color>");
        AddStatFromEnum(ref lines, secondaryStats, "Reload Duration");
        AddStatFromEnum(ref lines, secondaryStats, "Charge Duration");
        lines.Add($"Explosion Size: <color=#{ColorUtility.ToHtmlStringRGB(orchid)}>{Mathf.Round(data.hitForce)}</color>");
        AddStatFromEnum(ref lines, secondaryStats, "Range");
        lines.Add($"Recoil: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>X({Mathf.Round(data.recoilData.recoilX.x)}, {Mathf.Round(data.recoilData.recoilX.y)}) Y({Mathf.Round(data.recoilData.recoilY.x)}, {Mathf.Round(data.recoilData.recoilY.y)})</color>");
        lines.Add($"Spread: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>Size({Mathf.Round(data.spreadData.spreadSize.x)}, {Mathf.Round(data.spreadData.spreadSize.y)})</color>");
        lines.Add($"Fire Mode: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{(data.automatic == 1 ? "Automatic" : "Semi Automatic")}</color>");

        titleText.text = lines[0];
        for (int i = 1; i < lines.Count && i <= statTexts.Length; i++)
        {
            statTexts[i - 1].text = lines[i];
        }
        for (int i = lines.Count - 1; i < statTexts.Length; i++)
        {
            statTexts[i].text = "";
        }
    }

    private void AddStatFromEnumWithCustomLabel(ref List<string> lines, Dictionary<string, StatInfo> stats, string statName, string displayLabel)
    {
        if (stats.TryGetValue(statName, out StatInfo stat))
        {
            string label = displayLabel + ":";
            string value = stat.value;
            Color valueColor = GetStatValueColor(statName);
            lines.Add($"{label} <color=#{ColorUtility.ToHtmlStringRGB(valueColor)}>{value}</color>");
        }
    }

    private void AddStatFromEnum(ref List<string> lines, Dictionary<string, StatInfo> stats, string statName)
    {
        if (stats.TryGetValue(statName, out StatInfo stat))
        {
            string label = stat.name + ":";
            string value = stat.value;
            Color valueColor = (stat.name == "Damage Type") ? stat.color : GetStatValueColor(stat.name);
            lines.Add($"{label} <color=#{ColorUtility.ToHtmlStringRGB(valueColor)}>{value}</color>");
        }
    }

    private Color GetStatValueColor(string statName)
    {
        return statName switch
        {
            "Damage" => rose,
            "Fire Rate" => macaroon,
            "Ammo Capacity" => sky,
            "Reload Duration" => sky,
            "Charge Duration" => sky,
            "Range" => shamrock,
            _ => Color.white
        };
    }

    private void Update()
    {
        if (hudContainer == null && Player.LocalPlayer != null && Player.LocalPlayer.PlayerLook != null && Player.LocalPlayer.PlayerLook.Reticle != null)
        {
            CreateHUD();
        }

        updateTimer += Time.deltaTime;
        if (updateTimer >= UpdateInterval)
        {
            updateTimer = 0f;
            UpdateCurrentGun();
            UpdateHUD();
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            hudEnabled = !hudEnabled;
            if (hudContainer != null)
            {
                hudContainer.SetActive(hudEnabled);
            }
        }
    }

    public static void UpdateCurrentGun()
    {
        if (Player.LocalPlayer == null) return;

        var guns = Object.FindObjectsOfType<Gun>();
        foreach (var gun in guns)
        {
            var player = (Player)playerField.GetValue(gun);
            if (player == Player.LocalPlayer)
            {
                var gear = (IGear)gun;
                bool isActive = (bool)activeProp.GetValue(gear);
                if (isActive)
                {
                    currentGun = gun;
                    return;
                }
            }
        }
        currentGun = null;
    }

    private void OnDestroy()
    {
        if (hudContainer != null)
        {
            UnityEngine.Object.Destroy(hudContainer);
        }
        harmony.UnpatchSelf();
    }
}

[HarmonyPatch(typeof(Gun), "Enable")]
class GunEnablePatch
{
    [HarmonyPostfix]
    static void Postfix(Gun __instance)
    {
        GunDataDisplayMod.UpdateCurrentGun();
    }
}

[HarmonyPatch(typeof(Gun), "Disable")]
class GunDisablePatch
{
    [HarmonyPostfix]
    static void Postfix(Gun __instance)
    {
        GunDataDisplayMod.UpdateCurrentGun();
    }
}

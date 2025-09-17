using BepInEx;
using HarmonyLib;
using Pigeon.Movement;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MycopunkGunDataDisplay
{
    [BepInPlugin("com.yourname.displaygunstats", "DisplayGunStats", "1.0.0")]
    [MycoMod(null, ModFlags.IsClientSide)]
    public class GunDataDisplayMod : BaseUnityPlugin
    {
        private Harmony harmony;
        private static Gun currentGun;
        private static FieldInfo playerField;
        private static PropertyInfo activeProp;
        private float updateTimer = 0f;
        private const float UpdateInterval = 0.5f; // Check every 0.5 seconds to avoid performance hit
        private static bool hudEnabled = true;

        private void Awake()
        {
            var harmony = new Harmony("com.yourname.displaygunstats");
            harmony.PatchAll();
            Logger.LogInfo($"{harmony.Id} loaded!");

            // Reflection setup
            playerField = typeof(Gun).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);
            if (playerField == null)
            {
                //Logger.LogError("Failed to find 'player' field in Gun class.");
            }

            activeProp = typeof(IGear).GetProperty("Active");
            if (activeProp == null)
            {
                //Logger.LogError("Failed to find 'Active' property in IGear interface.");
            }
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= UpdateInterval)
            {
                updateTimer = 0f;
                UpdateCurrentGun();
            }

            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                hudEnabled = !hudEnabled;
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
                        return; // Assume only one active gun at a time; take the first found
                    }
                }
            }
            currentGun = null; // No active gun found
        }

        private void OnGUI()
        {
            if (currentGun == null || !hudEnabled) return;

            ref GunData data = ref ((IWeapon)currentGun).GunData;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21,
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold
            };

            float width = 450f;
            float height = 30f;

            // Collect all display lines
            List<string> lines = new List<string>();
            lines.Add("Current Gun Data:");
            lines.Add($"Damage: {data.damage}");
            lines.Add($"Element: {data.damageEffect}");
            lines.Add($"Element On-Hit: {data.damageEffectAmount}");
            lines.Add($"Fire Rate: {(1f / data.fireInterval).ToString("F2")}");
            lines.Add($"Magazine Size: {data.magazineSize}");
            lines.Add($"Ammo Capacity: {data.ammoCapacity}");
            lines.Add($"Reload Duration: {data.reloadDuration}");
            //lines.Add($"Bullet Speed: {data.bulletSpeed}");
            lines.Add($"Bullets Per Shot: {data.bulletsPerShot}");
            lines.Add($"Burst Size: {data.burstSize}");
            lines.Add($"Burst Fire Interval: {data.burstFireInterval}");
            lines.Add($"Automatic: {(data.automatic == 1 ? "Yes" : "No")}");
            lines.Add($"Max Bounces: {data.maxBounces}");
            lines.Add($"Explosion Size: {data.hitForce}");

            // RecoilData
            lines.Add("Recoil Data:");
            lines.Add($"  Recoil X: ({data.recoilData.recoilX.x}, {data.recoilData.recoilX.y})");
            lines.Add($"  Recoil Y: ({data.recoilData.recoilY.x}, {data.recoilData.recoilY.y})");
            //lines.Add($"  Recoil Z: ({data.recoilData.recoilZ.x}, {data.recoilData.recoilZ.y})");
            //lines.Add($"  Max Recoil Z: {data.recoilData.maxRecoilZ}");
            // Add more recoil fields as needed...

            // SpreadData
            lines.Add("Spread Data:");
            lines.Add($"  Spread Type: {data.spreadData.spreadType}");
            lines.Add($"  Spread Size: ({data.spreadData.spreadSize.x}, {data.spreadData.spreadSize.y})");
            // Concentric sizes if any...

            // RangeData
            lines.Add("Range Data:");
            lines.Add($"  Falloff Start: {data.rangeData.falloffStartDistance}");
            lines.Add($"  Falloff End: {data.rangeData.falloffEndDistance}");
            lines.Add($"  Max Damage Range: {data.rangeData.maxDamageRange}");

            // ChargeData
            lines.Add("Charge Data:");
            lines.Add($"  Duration: {data.chargeData.duration}");
            // Add more...

            // Fire Constraints
            //lines.Add("Fire Constraints:");
            //lines.Add($"  Can Fire While Sprinting: {data.fireConstraints.canFireWhileSprinting}");
            // Add more...

            // Calculate total height
            float totalHeight = lines.Count * height;

            float x = Screen.width - width - 10f;
            float y = Screen.height - totalHeight - 10f;

            // Draw semi-transparent black background
            Color originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0f, 0f, 1f); // 80% opacity black
            GUI.Box(new Rect(x - 5f, y - 5f, width + 10f, totalHeight + 10f), GUIContent.none);

            // Reset background color
            GUI.backgroundColor = originalBackgroundColor;

            // Draw labels
            for (int i = 0; i < lines.Count; i++)
            {
                GUI.Label(new Rect(x, y + i * height, width, height), lines[i], style);
            }
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    // Optional Harmony patches if needed for more precise timing
    [HarmonyPatch(typeof(Gun), "Enable")]
    class GunEnablePatch
    {
        [HarmonyPostfix]
        static void Postfix(Gun __instance)
        {
            // Force an update when a gun is enabled
            GunDataDisplayMod.UpdateCurrentGun();
        }
    }

    [HarmonyPatch(typeof(Gun), "Disable")]
    class GunDisablePatch
    {
        [HarmonyPostfix]
        static void Postfix(Gun __instance)
        {
            // Force an update when a gun is disabled
            GunDataDisplayMod.UpdateCurrentGun();
        }
    }
}
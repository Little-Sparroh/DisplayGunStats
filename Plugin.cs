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

        private static readonly Color sky = new Color(0.529f, 0.808f, 0.922f); // #87CEEB
        private static readonly Color orchid = new Color(0.855f, 0.439f, 0.839f); // #DA70D6
        private static readonly Color rose = new Color(0.8901960784313725f, 0.1411764705882353f, 0.16862745098039217f); // #E3242B
        private static readonly Color macaroon = new Color(0.9764705882352941f, 0.8784313725490196f, 0.4627450980392157f); // #F9E076
        private static readonly Color shamrock = new Color(0.011764705882352941f, 0.6745098039215687f, 0.07450980392156863f); // #03AC13

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

        private Color GetElementColor(EffectType effect)
        {
            return effect switch
            {
                EffectType.Normal => Color.white,
                EffectType.Shock => sky,
                EffectType.Fire => rose,
                EffectType.Acid => shamrock,
                EffectType.Decay => orchid,
                EffectType.Bees => macaroon,
                _ => Color.white
            };
        }

        private void OnGUI()
        {
            if (currentGun == null || !hudEnabled) return;

            ref GunData data = ref ((IWeapon)currentGun).GunData;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21,
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold,
                richText = true
            };

            float height = 30f;
            float padding = 10f;

            // Collect all display lines
            List<string> lines = new List<string>();
            lines.Add("Current Gun Data:");
            lines.Add($"Damage: <color=#{ColorUtility.ToHtmlStringRGB(rose)}>{Mathf.Round(data.damage)}</color>");
            var elementColor = GetElementColor(data.damageEffect);
            lines.Add($"Element: <color=#{ColorUtility.ToHtmlStringRGB(elementColor)}>{data.damageEffect}</color>");
            lines.Add($"Element On-Hit: <color=#{ColorUtility.ToHtmlStringRGB(elementColor)}>{Mathf.Round(data.damageEffectAmount)}</color>");
            lines.Add($"Fire Rate: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{(1f / data.fireInterval).ToString("F2")}</color>");
            lines.Add($"Magazine Size: <color=#{ColorUtility.ToHtmlStringRGB(sky)}>{data.magazineSize}</color>");
            lines.Add($"Ammo Capacity: <color=#{ColorUtility.ToHtmlStringRGB(sky)}>{data.ammoCapacity}</color>");
            lines.Add($"Reload Duration: <color=#{ColorUtility.ToHtmlStringRGB(sky)}>{data.reloadDuration.ToString("F1")}</color>");
            //lines.Add($"Bullet Speed: {data.bulletSpeed}");
            lines.Add($"Bullets Per Shot: <color=#{ColorUtility.ToHtmlStringRGB(orchid)}>{data.bulletsPerShot}</color>");
            lines.Add($"Burst Size: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.burstSize}</color>");
            lines.Add($"Burst Interval: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{data.burstFireInterval.ToString("F2")}</color>");
            lines.Add($"Automatic: <color=#{ColorUtility.ToHtmlStringRGB(macaroon)}>{(data.automatic == 1 ? "Yes" : "No")}</color>");
            lines.Add($"Bounces: <color=#{ColorUtility.ToHtmlStringRGB(orchid)}>{data.maxBounces}</color>");
            lines.Add($"Explosion Size: <color=#{ColorUtility.ToHtmlStringRGB(orchid)}>{Mathf.Round(data.hitForce)}</color>");

            // RecoilData
            lines.Add("Recoil Data:");
            lines.Add($"  Recoil X: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>({Mathf.Round(data.recoilData.recoilX.x)}, {Mathf.Round(data.recoilData.recoilX.y)})</color>");
            lines.Add($"  Recoil Y: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>({Mathf.Round(data.recoilData.recoilY.x)}, {Mathf.Round(data.recoilData.recoilY.y)})</color>");
            //lines.Add($"  Recoil Z: ({data.recoilData.recoilZ.x}, {data.recoilData.recoilZ.y})");
            //lines.Add($"  Max Recoil Z: {data.recoilData.maxRecoilZ}");
            // Add more recoil fields as needed...

            // SpreadData
            lines.Add("Spread Data:");
            lines.Add($"  Spread Type: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>{data.spreadData.spreadType}</color>");
            lines.Add($"  Spread Size: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>({Mathf.Round(data.spreadData.spreadSize.x)}, {Mathf.Round(data.spreadData.spreadSize.y)})</color>");
            // Concentric sizes if any...

            // RangeData
            lines.Add("Range Data:");
            lines.Add($"  Falloff Start: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>{Mathf.Round(data.rangeData.falloffStartDistance)}</color>");
            lines.Add($"  Falloff End: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>{Mathf.Round(data.rangeData.falloffEndDistance)}</color>");
            lines.Add($"  Max Damage Range: <color=#{ColorUtility.ToHtmlStringRGB(shamrock)}>{Mathf.Round(data.rangeData.maxDamageRange)}</color>");

            // ChargeData
            lines.Add("Charge Data:");
            lines.Add($"  Duration: <color=#{ColorUtility.ToHtmlStringRGB(sky)}>{data.chargeData.duration.ToString("F1")}</color>");
            // Add more...

            // Fire Constraints
            //lines.Add("Fire Constraints:");
            //lines.Add($"  Can Fire While Sprinting: {data.fireConstraints.canFireWhileSprinting}");
            // Add more...

            // Calculate max width and total height
            float maxWidth = 0f;
            float totalHeight = 0f;
            foreach (string line in lines)
            {
                Vector2 size = style.CalcSize(new GUIContent(line));
                maxWidth = Mathf.Max(maxWidth, size.x);
                totalHeight += height;
            }
            maxWidth += padding * 2; // Add padding on both sides

            float x = 10f;
            float y = 10f;

            // Draw semi-transparent black background
            Color originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0f, 0f, 1f); // Fully opaque black
            GUI.Box(new Rect(x - padding / 2f, y - padding / 2f, maxWidth + padding, totalHeight + padding), GUIContent.none);

            // Reset background color
            GUI.backgroundColor = originalBackgroundColor;

            // Draw labels
            for (int i = 0; i < lines.Count; i++)
            {
                GUI.Label(new Rect(x, y + i * height, maxWidth, height), lines[i], style);
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
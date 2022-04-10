using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;
using TheEpicRoles.Patches;

/*
 * This patch adds two buttons to the main menu:
 * - Discord
 * - GitHub
 * And it changes the color of the exit button
 */

namespace TheEpicRoles.Patches {
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class MainMenuButtonsPatch {

        private static bool horseButtonState = MapOptions.enableHorseMode;
        private static Sprite horseModeOffSprite = null;
        private static Sprite horseModeOnSprite = null;

        private static Color buttonColor = new Color(0, 1, 0.9f, 1);
        private static Color buttonColorHover = new Color(0, 0.5f, 1, 1);
        private static Color buttonExitColor = Color.red;
        private static Color buttonExitColorHover = new Color(1, 0.3f, 0.3f, 1);
        private static void Prefix(MainMenuManager __instance) {
            // ---- Exit Button -----
            // Get ExitGameButton
            var btnExit = GameObject.Find("ExitGameButton");

            // Get Button Sprite + Text
            SpriteRenderer btnExitSprite = btnExit.GetComponent<SpriteRenderer>();
            var textExit = btnExit.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
           
            // Change Init Color        
            btnExitSprite.color = textExit.color = buttonExitColor;            

            // Get PassivButton and add listeners
            PassiveButton btnExitPassive = btnExit.GetComponent<PassiveButton>();
            btnExitPassive.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)delegate {
                btnExitSprite.color = textExit.color = buttonExitColorHover;
            });
            btnExitPassive.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)delegate {
                btnExitSprite.color = textExit.color = buttonExitColor;         
            });

            // ---- Discord Button -----
            // Clone ExitGameButton and change position
            var btnDiscord = UnityEngine.Object.Instantiate(btnExit, null);
            btnDiscord.transform.localPosition = new Vector3(btnDiscord.transform.localPosition.x, btnDiscord.transform.localPosition.y + 0.6f, btnDiscord.transform.localPosition.z);
            
            // Get Button Sprite + Text and change Text
            SpriteRenderer btnDiscordSprite = btnDiscord.GetComponent<SpriteRenderer>();
            var textDiscord = btnDiscord.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            __instance.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) => {
                textDiscord.SetText("Join TER\nDiscord");                
            })));

            // Change Init Color
            btnDiscordSprite.color = textDiscord.color = buttonColor;

            // Get PassivButton and add listeners
            PassiveButton btnDiscordPassive = btnDiscord.GetComponent<PassiveButton>();
            btnDiscordPassive.OnClick.AddListener((UnityEngine.Events.UnityAction)delegate {
                Application.OpenURL("https://discord.gg/WjegsxEuAD");
            });
            btnDiscordPassive.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)delegate {
                btnDiscordSprite.color = textDiscord.color = buttonColorHover;
            });
            btnDiscordPassive.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)delegate {
                btnDiscordSprite.color = textDiscord.color = buttonColor;
            });

            // ---- GitHub Button -----
            // Clone ExitGameButton and change position
            var btnGithub = UnityEngine.Object.Instantiate(btnExit, null);
            btnGithub.transform.localPosition = new Vector3(btnGithub.transform.localPosition.x, btnGithub.transform.localPosition.y + 1.2f, btnGithub.transform.localPosition.z);

            // Get Button Sprite + Text and change Text
            SpriteRenderer btnGithubsprite = btnGithub.GetComponent<SpriteRenderer>();
            var textGithub = btnGithub.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            __instance.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) => {
                textGithub.SetText("Open\nGitHub");                
            })));

            // Change Init Color
            btnGithubsprite.color = textGithub.color = buttonColor;

            // Get PassivButton and add listeners
            PassiveButton btnGithubPassive = btnGithub.GetComponent<PassiveButton>();
            btnGithubPassive.OnClick.AddListener((UnityEngine.Events.UnityAction)delegate {
                Application.OpenURL("https://github.com/LaicosVK/TheEpicRoles");
            });
            btnGithubPassive.OnMouseOver.AddListener((UnityEngine.Events.UnityAction)delegate {
                btnGithubsprite.color = textGithub.color = buttonColorHover;
            });
            btnGithubPassive.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)delegate {
                btnGithubsprite.color = textGithub.color = buttonColor;
            });


            // Horse mode stuff
            var horseModeSelectionBehavior = new ClientOptionsPatch.SelectionBehaviour("Enable Horse Mode", () => MapOptions.enableHorseMode = TheEpicRolesPlugin.EnableHorseMode.Value = !TheEpicRolesPlugin.EnableHorseMode.Value, TheEpicRolesPlugin.EnableHorseMode.Value);

            var bottomTemplate = GameObject.Find("InventoryButton");
            if (bottomTemplate == null) return;
            var horseButton = Object.Instantiate(bottomTemplate, bottomTemplate.transform.parent);
            var passiveHorseButton = horseButton.GetComponent<PassiveButton>();
            var spriteHorseButton = horseButton.GetComponent<SpriteRenderer>();

            horseModeOffSprite = Helpers.loadSpriteFromResources("TheEpicRoles.Resources.HorseModeButtonOff.png", 75f);
            horseModeOnSprite = Helpers.loadSpriteFromResources("TheEpicRoles.Resources.HorseModeButtonOn.png", 75f);

            spriteHorseButton.sprite = horseButtonState ? horseModeOnSprite : horseModeOffSprite;

            passiveHorseButton.OnClick = new ButtonClickedEvent();

            passiveHorseButton.OnClick.AddListener((UnityEngine.Events.UnityAction)delegate {
                horseButtonState = horseModeSelectionBehavior.OnClick();
                if (horseButtonState)
                {
                    if (horseModeOnSprite == null) horseModeOnSprite = Helpers.loadSpriteFromResources("TheEpicRoles.Resources.HorseModeButtonOn.png", 75f);
                    spriteHorseButton.sprite = horseModeOnSprite;
                }
                else
                {
                    if (horseModeOffSprite == null) horseModeOffSprite = Helpers.loadSpriteFromResources("TheEpicRoles.Resources.HorseModeButtonOff.png", 75f);
                    spriteHorseButton.sprite = horseModeOffSprite;
                }
                CredentialsPatch.LogoPatch.updateSprite();
                // Avoid wrong Player Particles floating around in the background
                var particles = GameObject.FindObjectOfType<PlayerParticles>();
                if (particles != null)
                {
                    particles.pool.ReclaimAll();
                    particles.Start();
                }
            });
        }
    }
}
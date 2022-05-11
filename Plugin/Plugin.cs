using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(FileAccessPlugin.Guid)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    public partial class MassMovePlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Mass Move Plug-In";
        public const string Guid = "org.lordashes.plugins.massmove";
        public const string Version = "2.0.0.0";

        // Configuration
        private static ConfigEntry<KeyboardShortcut> triggerFollow { get; set; }
        private static ConfigEntry<KeyboardShortcut> triggerFormationSave { get; set; }
        private static ConfigEntry<KeyboardShortcut> triggerFormationRestore { get; set; }
        private static ConfigEntry<bool> automaticFormationSave { get; set; }

        private static MassMovePlugin self = null;

        void Awake()
        {
            UnityEngine.Debug.Log("Mass Move Plugin: " + typeof(MassMovePlugin).AssemblyQualifiedName + " Active.");

            self = this;

            triggerFollow = Config.Bind("Settings", "Toggle Follow Mode", new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl));
            triggerFormationSave = Config.Bind("Settings", "Save Formation", new KeyboardShortcut(KeyCode.F, KeyCode.RightControl));
            triggerFormationRestore = Config.Bind("Settings", "Restore Formation", new KeyboardShortcut(KeyCode.F, KeyCode.RightShift));
            automaticFormationSave = Config.Bind("Settings", "Automatic Formation Save", true);

            var harmony = new Harmony(MassMovePlugin.Guid);
            harmony.PatchAll();

            RadialUI.RadialSubmenu.EnsureMainMenuItem(MassMovePlugin.Guid, RadialUI.RadialSubmenu.MenuType.character, "Mass Move", FileAccessPlugin.Image.LoadSprite("MassMove.png"));
            if (!automaticFormationSave.Value)
            {
                RadialUI.RadialSubmenu.CreateSubMenuItem(MassMovePlugin.Guid, new MapMenu.ItemArgs()
                {
                    Action = (o, s) => { PatchMovableBoardAssetDrop.SaveFormation(); },
                    CloseMenuOnActivate = true,
                    FadeName = true,
                    Icon = FileAccessPlugin.Image.LoadSprite("MassMoveFormationSave.png"),
                    Title = "Establish Formation",
                    ValueText = "Establish Formation"
                }, null, () => { return true; });
            }

            RadialUI.RadialSubmenu.CreateSubMenuItem(MassMovePlugin.Guid, new MapMenu.ItemArgs()
            {
                Action = (o, s) => { PatchMovableBoardAssetDrop.RestoreFormation(); },
                CloseMenuOnActivate = true,
                FadeName = true,
                Icon = FileAccessPlugin.Image.LoadSprite("MassMoveFormation.png"),
                Title = "Restore Formation",
                ValueText = "Restore Formation"
            }, null, () => { return true; });

            RadialUI.RadialSubmenu.CreateSubMenuItem(MassMovePlugin.Guid, new MapMenu.ItemArgs()
            {
                Action = (o, s) => { PatchMovableBoardAssetDrop.StartLeading(); },
                CloseMenuOnActivate = true,
                FadeName = true,
                Icon = FileAccessPlugin.Image.LoadSprite("MassMoveLine.png"),
                Title = "Single File",
                ValueText = "Single File"
            }, null, () => { return followMode == CreatureGuid.Empty; });

            RadialUI.RadialSubmenu.CreateSubMenuItem(MassMovePlugin.Guid, new MapMenu.ItemArgs()
            {
                Action = (o, s) => { PatchMovableBoardAssetDrop.StartLeading(); },
                CloseMenuOnActivate = true,
                FadeName = true,
                Icon = FileAccessPlugin.Image.LoadSprite("MassMove.png"),
                Title = "Mass Move",
                ValueText = "Mass Move"
            }, null, () => { return followMode != CreatureGuid.Empty; });


            Utility.PostOnMainPage(this.GetType());
        }

        private void conditions(CreatureGuid arg1, string arg2, MapMenuItem arg3)
        {
            throw new NotImplementedException();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            {
                control = true;
            }
            else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
            {
                control = false;
            }
            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            {
                alt = true;
            }
            else if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt))
            {
                alt = false;
            }
            if (Utility.StrictKeyCheck(triggerFollow.Value))
            {
                if (followMode == CreatureGuid.Empty)
                {
                    SystemMessage.DisplayInfoText("Follow Mode On");
                    PatchMovableBoardAssetDrop.StartLeading();
                }
                else
                {
                    SystemMessage.DisplayInfoText("Follow Mode Off");
                    PatchMovableBoardAssetDrop.EndLeading();
                }
            }
            if (Utility.StrictKeyCheck(triggerFormationSave.Value))
            {
                SystemMessage.DisplayInfoText("Save Formation");
                PatchMovableBoardAssetDrop.SaveFormation();
            }
            if (Utility.StrictKeyCheck(triggerFormationRestore.Value))
            {
                SystemMessage.DisplayInfoText("Restore Formation");
                PatchMovableBoardAssetDrop.RestoreFormation();
            }
        }
    }
}

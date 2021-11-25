﻿using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Numerics;

namespace AetherCompass.Configs
{
    [Serializable]
    public class PluginConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool Enabled = false;
        public bool ShowScreenMark = false;
        public float ScreenMarkSizeScale = 1;
        [JsonIgnore]
        public const float ScreenMarkSizeScaleMin = .1f;
        [JsonIgnore]
        public const float ScreenMarkSizeScaleMax = 10;
        // L,D,R,U; how much to squeeze into centre on each side, so generally should be positive
        public Vector4 ScreenMarkConstraint = new(80, 80, 80, 80);
        [JsonIgnore]
        public const float ScreenMarkConstraintMin = 2;
        public bool ShowDetailWindow = false;
        public bool HideDetailInContents = false;
        public bool HideInEvent = false;
        public bool HideWhenCraftGather = false;
        public bool NotifyChat = false;
        public bool NotifySe = false;
        public bool NotifyToast = false;

        public AetherCurrentCompassConfig AetherCurrentConfig { get; private set; } = new();
        public MobHuntCompassConfig MobHuntConfig { get; private set; } = new();
        public GatheringPointCompassConfig GatheringConfig { get; private set; } = new();
        public QuestCompassConfig QuestConfig { get; private set; } = new();

#if DEBUG
        [JsonIgnore]
        public bool DebugTestAllGameObjects = false;
        [JsonIgnore]
        public DebugCompassConfig DebugConfig { get; private set; } = new();
#endif

        public void CheckValueValidity(Vector2 screenSize)
        {
            if (ScreenMarkSizeScale < .1f) ScreenMarkSizeScale = .1f;
            if (ScreenMarkSizeScale > 10) ScreenMarkSizeScale = 10;

            if (ScreenMarkConstraint.X < ScreenMarkConstraintMin) 
                ScreenMarkConstraint.X = ScreenMarkConstraintMin;
            if (ScreenMarkConstraint.Y < ScreenMarkConstraintMin) 
                ScreenMarkConstraint.Y = ScreenMarkConstraintMin;
            if (ScreenMarkConstraint.Z < ScreenMarkConstraintMin) 
                ScreenMarkConstraint.Z = ScreenMarkConstraintMin;
            if (ScreenMarkConstraint.W < ScreenMarkConstraintMin) 
                ScreenMarkConstraint.W = ScreenMarkConstraintMin;
            if (ScreenMarkConstraint.X > screenSize.X / 2 - 10) 
                ScreenMarkConstraint.X = screenSize.X / 2 - 10;
            if (ScreenMarkConstraint.Y > screenSize.Y / 2 - 10) 
                ScreenMarkConstraint.Y = screenSize.Y / 2 - 10;
            if (ScreenMarkConstraint.Z > screenSize.X / 2 - 10)
                ScreenMarkConstraint.Z = screenSize.X / 2 - 10;
            if (ScreenMarkConstraint.W > screenSize.Y / 2 - 10)
                ScreenMarkConstraint.W = screenSize.Y / 2 - 10;

            AetherCurrentConfig.CheckValueValidity();
            MobHuntConfig.CheckValueValidity();
            GatheringConfig.CheckValueValidity();
            QuestConfig.CheckValueValidity();
#if DEBUG
            DebugConfig.CheckValueValidity();
#endif
        }

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        public void Load(PluginConfig config)
        {
            if (Version == config.Version)
            {
                Enabled = config.Enabled;
                ShowScreenMark = config.ShowScreenMark;
                ScreenMarkSizeScale = config.ScreenMarkSizeScale;
                ScreenMarkConstraint = config.ScreenMarkConstraint;
                ShowDetailWindow = config.ShowDetailWindow;
                HideDetailInContents = config.HideDetailInContents;
                HideInEvent = config.HideInEvent;
                HideWhenCraftGather = config.HideWhenCraftGather;
                NotifyChat = config.NotifyChat;
                NotifySe = config.NotifySe;
                NotifyToast = config.NotifyToast;

                AetherCurrentConfig.Load(config.AetherCurrentConfig);
                MobHuntConfig.Load(config.MobHuntConfig);
                GatheringConfig.Load(config.GatheringConfig);
                QuestConfig.Load(config.QuestConfig);
            }
            // NOTE: config ver conversion if needed
        }

        public static PluginConfig GetSavedPluginConfig()
            => Plugin.PluginInterface.GetPluginConfig() as PluginConfig ?? new();
    }
}

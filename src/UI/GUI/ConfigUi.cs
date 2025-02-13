﻿using AetherCompass.Compasses;
using AetherCompass.Game;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;

namespace AetherCompass.UI.GUI
{
    public static class ConfigUi
    {
        internal static bool IsFocus;

        public static void Draw()
        {
            if (IsFocus)
            {
                ImGui.SetNextWindowCollapsed(false);
                ImGui.SetNextWindowFocus();
                IsFocus = false;
            }

            if (ImGui.Begin("AetherCompass: Configuration"))    // not collapsed
            {
                ImGuiEx.Checkbox("启用插件", ref Plugin.Config.Enabled,
                    "启用/禁用此插件。 \n" +
                    "无论此设置如何，所有指南针都将在某些区域（如PvP区域）自动暂停。");
                Plugin.SetEnabledIfConfigChanged();
                if (Plugin.Config.Enabled)
                {
                    ImGui.NewLine();

                    if (ImGui.BeginTabBar("AetherCompass_Configuration_MainTabBar"))
                    {
                        if (ImGui.BeginTabItem("插件设置"))
                        {
                            ImGui.TreePush("AetherCompass_Configuration_TabPluginSettings");
                            ImGui.NewLine();
                            DrawPluginSettingsTab();
                            ImGui.TreePop();
                            ImGui.EndTabItem();
                        }
                        if (ImGui.BeginTabItem("指南针设置"))
                        {
                            ImGui.TreePush("AetherCompass_Configuration_TabCompassSettings");
                            ImGui.NewLine();
                            Plugin.CompassManager.DrawCompassConfigUi();
                            ImGui.TreePop();
                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }
                }

                ImGuiEx.Separator(false, true);
                if (ImGui.Button("保存"))
                    Plugin.Config.Save();
                if (ImGui.Button("保存并关闭"))
                {
                    Plugin.Config.Save();
                    Plugin.InConfig = false;
                    Plugin.Reload();
                }
                ImGui.NewLine();
                if (ImGui.Button("关闭并放弃所有更改"))
                {
                    Plugin.InConfig = false;
                    Plugin.Config.Load();
                    Plugin.Reload();
                }
            }
            ImGui.End();

            Plugin.Config.CheckValueValidity(ImGui.GetMainViewport().Size);

            var displayArea = GetDisplayAreaFromConfigScreenMarkConstraint();
            Plugin.Overlay.AddDrawAction(() => ImGui.GetWindowDrawList().AddRect(
                    new(displayArea.X, displayArea.W), new(displayArea.Z, displayArea.Y),
                    ImGui.ColorConvertFloat4ToU32(new(1, 0, 0, 1)), 0, ImDrawFlags.Closed, 4));
            Plugin.Overlay.AddDrawAction(Compass.GenerateConfigDummyMarkerDrawAction(
                    $"Marker size scale: {Plugin.Config.ScreenMarkSizeScale:0.00},\n" +
                    $"Text rel size scale: {Plugin.Config.ScreenMarkTextRelSizeScale:0.0}",
                    Plugin.Config.ScreenMarkSizeScale, Plugin.Config.ScreenMarkTextRelSizeScale));
        }


        private static void DrawPluginSettingsTab()
        {
            // ScreenMark
            ImGuiEx.Checkbox(
                "启用在屏幕上标记检测到的对象", ref Plugin.Config.ShowScreenMark,
                "If enabled, will allow Compasses to mark objects detected by them on screen," +
                "showing the direction and distance.\n\n" +
                "You can configure this for each compass separately below.");
            if (Plugin.Config.ShowScreenMark)
            {
                ImGui.TreePush();
                ImGuiEx.DragFloat("标记 大小刻度", 100, ref Plugin.Config.ScreenMarkSizeScale,
                    .01f, PluginConfig.ScreenMarkSizeBound.Min, PluginConfig.ScreenMarkSizeBound.Max);
                ImGuiEx.DragFloat("标记 文本大小比例", 100, ref Plugin.Config.ScreenMarkTextRelSizeScale,
                    .1f, PluginConfig.ScreenMarkTextRelSizeBound.Min, PluginConfig.ScreenMarkTextRelSizeBound.Max, "%.1f",
                    tooltip: "Set the size scale for the markers' label text, relative to the marker size");
                var viewport = ImGui.GetMainViewport().Pos;
                var vsize = ImGui.GetMainViewport().Size;
                Vector4 displayArea = GetDisplayAreaFromConfigScreenMarkConstraint();
                ImGuiEx.DragFloat4("标记显示区域（左/下/右/上）", ref displayArea,
                    1, PluginConfig.ScreenMarkConstraintMin, 9999, v_fmt: "%.0f",
                    tooltip: "Set the display area for the markers.\n\n" +
                        "The display area is shown as the red rectangle on the screen while configuration window is open.\n" +
                        "Detected objects will be marked on screen within this area.");
                Plugin.Config.ScreenMarkConstraint = new(
                    displayArea.X - viewport.X, // L
                    viewport.Y + vsize.Y - displayArea.Y, // D
                    viewport.X + vsize.X - displayArea.Z, // R
                    displayArea.W - viewport.Y); // U
                ImGui.Text($"(* 全屏显示区域为 " +
                    $"<{viewport.X:0}, {viewport.Y + vsize.Y:0}, {viewport.X + vsize.X:0}, {viewport.Y:0}> )");
                ImGuiEx.Checkbox("当铭牌位于标记显示区域内时隐藏标记", 
                    ref Plugin.Config.HideScreenMarkIfNameplateInsideDisplayArea,
                    "If enabled, markers will be hidden if the nameplate is inside the marker display area as set above\n" +
                    "AND if the nameplate is also of certain size.\n\n" +
                    "NOTE: if these two conditions are met but the nameplate is also hidden from view " +
                    "due to it being behind other objects (e.g. a wall), \n" +
                    "the plugin will not know this and will continue to hide the markers.");
                if (Plugin.Config.HideScreenMarkIfNameplateInsideDisplayArea)
                {
                    ImGui.TreePush();
                    ImGuiEx.DragInt("仅当对象位于时隐藏", Language.Unit.Yalm,
                        50, ref Plugin.Config.HideScreenMarkEnabledDistance, 1, 
                        PluginConfig.HideScreenMarkEnabledDistanceBound.Min, 
                        PluginConfig.HideScreenMarkEnabledDistanceBound.Max,
                        "Markers will be hidden only when the object is also\n" +
                        "within this distance from player's character.");
                    ImGui.TreePop();
                }
                ImGui.TreePop();
            }
            ImGui.NewLine();

            // DetailWindow
            ImGuiEx.Checkbox("显示检测到的对象的详细信息", ref Plugin.Config.ShowDetailWindow,
                "If enabled, will show a window listing details of detected objects.\n\n" +
                "You can configure this for each compass separately below.");
            if (Plugin.Config.ShowDetailWindow)
            {
                ImGui.TreePush();
                ImGuiEx.Checkbox("不显示在实例化的内容中", ref Plugin.Config.HideDetailInContents,
                    "If enabled, will auto hide the detail window in instance contents such as dungeons, trials and raids.");
                ImGui.TreePop();
            }
            ImGui.NewLine();

            // Hiding options
            if (Plugin.Config.ShowScreenMark || Plugin.Config.ShowDetailWindow)
            {
                ImGuiEx.Checkbox("在事件中隐藏指南针UI", ref Plugin.Config.HideInEvent, 
                    "If enabled, will auto hide both markers on screen and the detail window in certain conditions\n" +
                    "such as in event, in cutscene and when using summoning bells");
                ImGuiEx.Checkbox("制作/收集/钓鱼时隐藏指南针UI", ref Plugin.Config.HideWhenCraftGather);
            }
            ImGui.NewLine();
            
            // Norification
            ImGuiEx.Checkbox("启用聊天通知功能", ref Plugin.Config.NotifyChat,
                "If enabled, will allow compasses to send notifications in game chat when detected an object.\n\n" +
                "You can configure this for each compass separately below. ");
            if (Plugin.Config.NotifyChat)
            {
                ImGui.TreePush();
                ImGuiEx.Checkbox("同时启用声音通知功能", ref Plugin.Config.NotifySe,
                    "If enabled, will allow compasses to make sound notification alongside chat notification.\n\n" +
                    "You can configure this for each compass separately below.");
                ImGui.TreePop();
            }
            ImGuiEx.Checkbox("启用Toast通知", ref Plugin.Config.NotifyToast,
                "If enabled, will allow compasses to make Toast notifications on screen when detected an object.\n\n" +
                "You can configure this for each compass separately below.");
            ImGui.NewLine();

#if DEBUG
            // Debug
            ImGuiEx.Checkbox("[DEBUG] 测试所有游戏对象", ref Plugin.Config.DebugTestAllGameObjects);
            ImGui.NewLine();
#endif

            ImGui.Checkbox("Show Sponsor/Support button", ref Plugin.Config.ShowSponsor);
            if (Plugin.Config.ShowSponsor)
            {
                ImGui.Indent();
                ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);
                if (ImGuiEx.Button("Buy Yomishino a Coffee",
                    "You can support me and buy me a coffee if you want.\n" +
                    "(Will open external link to Ko-fi in your browser)"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://ko-fi.com/yomishino",
                        UseShellExecute = true
                    });
                }
                ImGui.PopStyleColor(3);
                ImGui.Unindent();
            }
            ImGui.NewLine();
        }

        private static Vector4 GetDisplayAreaFromConfigScreenMarkConstraint()
        {
            var viewport = ImGui.GetMainViewport().Pos;
            var vsize = ImGui.GetMainViewport().Size;
            return new(
                viewport.X + Plugin.Config.ScreenMarkConstraint.X, // L
                viewport.Y + vsize.Y - Plugin.Config.ScreenMarkConstraint.Y, // D
                viewport.X + vsize.X - Plugin.Config.ScreenMarkConstraint.Z, // R
                viewport.Y + Plugin.Config.ScreenMarkConstraint.W); // U
        }
    }
}

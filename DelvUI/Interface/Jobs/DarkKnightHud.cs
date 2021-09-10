﻿using Dalamud.Game.ClientState.Structs;
using Dalamud.Game.ClientState.Structs.JobGauge;
using DelvUI.Config;
using DelvUI.Config.Attributes;
using DelvUI.Helpers;
using DelvUI.Interface.Bars;
using DelvUI.Interface.GeneralElements;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DelvUI.Interface.Jobs
{
    public class DarkKnightHud : JobHud
    {
        private new DarkKnightConfig Config => (DarkKnightConfig)_config;

        public DarkKnightHud(string id, DarkKnightConfig config) : base(id, config)
        {

        }

        private PluginConfigColor EmptyColor => GlobalColors.Instance.EmptyColor;
        private PluginConfigColor PartialFillColor => GlobalColors.Instance.PartialFillColor;

        public override void Draw(Vector2 origin)
        {
            if (Config.ShowManaBar)
            {
                DrawManaBar(origin);
            }

            if (Config.ShowBloodGauge)
            {
                DrawBloodGauge(origin);
            }

            if (Config.ShowBuffBar)
            {
                DrawBuffBar(origin);
            }

            if (Config.ShowLivingShadowBar)
            {
                DrawLivingShadowBar(origin);
            }
        }

        private void DrawManaBar(Vector2 origin)
        {
            var actor = PluginInterface.ClientState.LocalPlayer;
            var darkArtsBuff = PluginInterface.ClientState.JobGauges.Get<DRKGauge>().HasDarkArts();

            var posX = origin.X + Config.Position.X + Config.ManaBarPosition.X - Config.ManaBarSize.X / 2f;
            var posY = origin.Y + Config.Position.Y + Config.ManaBarPosition.Y - Config.ManaBarSize.Y / 2f;

            BarBuilder builder = BarBuilder.Create(posX, posY, Config.ManaBarSize.Y, Config.ManaBarSize.X).SetBackgroundColor(EmptyColor.Background);

            if (Config.ChunkManaBar)
            {
                builder.SetChunks(3).SetChunkPadding(Config.ManaBarPadding).AddInnerBar(actor.CurrentMp, 9000, Config.ManaBarColor.Map, PartialFillColor.Map);
            }
            else
            {
                builder.AddInnerBar(actor.CurrentMp, actor.MaxMp, Config.ManaBarColor.Map);
            }

            if (Config.ShowManaBarText)
            {
                var formattedManaText = TextTags.GenerateFormattedTextFromTags(actor, "[mana:current-short]");

                builder.SetTextMode(BarTextMode.Single).SetText(BarTextPosition.CenterLeft, BarTextType.Custom, formattedManaText);
            }

            if (darkArtsBuff)
            {
                builder.SetGlowSize(2);
                builder.SetGlowColor(Config.DarkArtsColor.Base);
                builder.SetChunksColors(Config.DarkArtsColor.Map);
                builder.SetPartialFillColor(Config.DarkArtsColor.Map);
                builder.SetBackgroundColor(Config.DarkArtsColor.Background);
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            builder.Build().Draw(drawList);
        }

        private void DrawBloodGauge(Vector2 origin)
        {
            var gauge = PluginInterface.ClientState.JobGauges.Get<DRKGauge>();

            var posX = origin.X + Config.Position.X + Config.BloodGaugePosition.X - Config.BloodGaugeSize.X / 2f;
            var posY = origin.Y + Config.Position.Y + Config.BloodGaugePosition.Y - Config.BloodGaugeSize.Y / 2f;

            BarBuilder builder = BarBuilder.Create(posX, posY, Config.BloodGaugeSize.Y, Config.BloodGaugeSize.X).SetBackgroundColor(EmptyColor.Background);

            if (Config.ChunkBloodGauge)
            {
                builder.SetChunks(2).SetChunkPadding(Config.BloodGaugePadding).AddInnerBar(gauge.Blood, 100, Config.BloodColor.Map, PartialFillColor.Map);
            }
            else
            {
                if (gauge.Blood == 100)
                {
                    builder.AddInnerBar(gauge.Blood, 100, Config.BloodColorFull.Map);
                }
                else if (gauge.Blood > 100)
                {
                    builder.AddInnerBar(gauge.Blood, 100, Config.BloodColor.Map);
                }
                else
                {
                    builder.AddInnerBar(gauge.Blood, 100, PartialFillColor.Map);
                }
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            builder.Build().Draw(drawList);
        }

        private void DrawBuffBar(Vector2 origin)
        {
            IEnumerable<StatusEffect> bloodWeaponBuff = PluginInterface.ClientState.LocalPlayer.StatusEffects.Where(o => o.EffectId == 742);
            IEnumerable<StatusEffect> deliriumBuff = PluginInterface.ClientState.LocalPlayer.StatusEffects.Where(o => o.EffectId == 1972);

            var xPos = origin.X + Config.Position.X + Config.BuffBarPosition.X - Config.BuffBarSize.X / 2f;
            var yPos = origin.Y + Config.Position.Y + Config.BuffBarPosition.Y - Config.BuffBarSize.Y / 2f;

            BarBuilder builder = BarBuilder.Create(xPos, yPos, Config.BuffBarSize.Y, Config.BuffBarSize.X).SetBackgroundColor(EmptyColor.Background);

            if (bloodWeaponBuff.Any())
            {
                var fightOrFlightDuration = Math.Abs(bloodWeaponBuff.First().Duration);
                builder.AddInnerBar(fightOrFlightDuration, 10, Config.BloodWeaponColor.Map);

                if (Config.ShowBuffBarText)
                {
                    builder.SetTextMode(BarTextMode.EachChunk).SetText(BarTextPosition.CenterLeft, BarTextType.Current, Config.BloodWeaponColor.Vector, Vector4.UnitW, null);
                }
            }

            if (deliriumBuff.Any())
            {
                var deliriumDuration = Math.Abs(deliriumBuff.First().Duration);
                builder.AddInnerBar(deliriumDuration, 10, Config.DeliriumColor.Map);

                if (Config.ShowBuffBarText)
                {
                    builder.SetTextMode(BarTextMode.EachChunk).SetText(BarTextPosition.CenterRight, BarTextType.Current, Config.DeliriumColor.Vector, Vector4.UnitW, null);
                }
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            builder.Build().Draw(drawList);
        }

        private void DrawLivingShadowBar(Vector2 origin)
        {
            var actor = PluginInterface.ClientState.LocalPlayer;
            var shadowTimeRemaining = PluginInterface.ClientState.JobGauges.Get<DRKGauge>().ShadowTimeRemaining / 1000;
            var livingShadow = actor.Level >= 80 && shadowTimeRemaining is > 0 and <= 24;

            var xPos = origin.X + Config.Position.X + Config.LivingShadowBarPosition.X - Config.LivingShadowBarSize.X / 2f;
            var yPos = origin.Y + Config.Position.Y + Config.LivingShadowBarPosition.Y - Config.LivingShadowBarSize.Y / 2f;

            BarBuilder builder = BarBuilder.Create(xPos, yPos, Config.LivingShadowBarSize.Y, Config.LivingShadowBarSize.X).SetBackgroundColor(EmptyColor.Background);

            if (livingShadow)
            {
                builder.AddInnerBar(shadowTimeRemaining, 24, Config.LivingShadowColor.Map);

                if (Config.ShowLivingShadowBarText)
                {
                    builder.SetTextMode(BarTextMode.EachChunk).SetText(BarTextPosition.CenterLeft, BarTextType.Current, Config.LivingShadowColor.Vector, Vector4.UnitW, null);
                }
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            builder.Build().Draw(drawList);
        }
    }

    [Serializable]
    [Section("Job Specific Bars")]
    [SubSection("Tank", 0)]
    [SubSection("Dark Knight", 1)]
    public class DarkKnightConfig : JobConfig
    {
        [JsonIgnore] public new uint JobId = JobIDs.DRK;
        public new static DarkKnightConfig DefaultConfig() { return new DarkKnightConfig(); }

        #region Mana Bar
        [Checkbox("Show Mana Bar")]
        [CollapseControl(30, 0)]
        public bool ShowManaBar = true;

        [Checkbox("Show Text" + "##DRKManaBar")]
        [CollapseWith(0, 0)]
        public bool ShowManaBarText = false;

        [Checkbox("Chunk Mana Bar")]
        [CollapseWith(5, 0)]
        public bool ChunkManaBar = true;

        [DragFloat2("Position" + "##DRKManaBar", min = -4000f, max = 4000f)]
        [CollapseWith(10, 0)]
        public Vector2 ManaBarPosition = new Vector2(0, HUDConstants.JobHudsBaseY - 61);

        [DragFloat2("Size" + "##DRKManaBar", min = 0, max = 4000f)]
        [CollapseWith(15, 0)]
        public Vector2 ManaBarSize = new Vector2(254, 10);

        [DragInt("Padding" + "##DRKManaBar", min = 0)]
        [CollapseWith(20, 0)]
        public int ManaBarPadding = 1;

        [CollapseWith(25, 0)]
        [ColorEdit4("Mana Color" + "##DRKManaBar")]
        public PluginConfigColor ManaBarColor = new(new Vector4(0f / 255f, 142f / 255f, 254f / 255f, 100f / 100f));

        [ColorEdit4("Dark Arts Buff Color" + "##DRKManaBar")]
        [CollapseWith(30, 0)]
        public PluginConfigColor DarkArtsColor = new(new Vector4(210f / 255f, 33f / 255f, 33f / 255f, 100f / 100f));
        #endregion

        #region Blood Gauge
        [Checkbox("Show Blood Gauge")]
        [CollapseControl(35, 1)]
        public bool ShowBloodGauge = true;

        [Checkbox("Chunk Blood Gauge")]
        [CollapseWith(0, 1)]
        public bool ChunkBloodGauge = true;

        [DragFloat2("Position" + "##DRKBloodGauge", min = -4000f, max = 4000f)]
        [CollapseWith(10, 1)]
        public Vector2 BloodGaugePosition = new Vector2(0, HUDConstants.JobHudsBaseY - 49);

        [DragFloat2("Size" + "##DRKBloodGauge", min = 0, max = 4000f)]
        [CollapseWith(15, 1)]
        public Vector2 BloodGaugeSize = new Vector2(254, 10);

        [DragInt("Padding" + "##DRKBloodGauge", min = 0)]
        [CollapseWith(20, 1)]
        public int BloodGaugePadding = 2;

        [ColorEdit4("Blood Color Left" + "##DRKBloodGauge")]
        [CollapseWith(25, 1)]
        public PluginConfigColor BloodColor = new(new Vector4(196f / 255f, 20f / 255f, 122f / 255f, 100f / 100f));

        [ColorEdit4("Blood Color Full" + "##DRKBloodGauge")]
        [CollapseWith(30, 1)]
        public PluginConfigColor BloodColorFull = new(new Vector4(216f / 255f, 0f / 255f, 73f / 255f, 100f / 100f));
        #endregion

        #region Buff Bar
        [Checkbox("Show Buff Bar")]
        [CollapseControl(40, 2)]
        public bool ShowBuffBar = false;

        [Checkbox("Show Text" + "##DRKBuffBar")]
        [CollapseWith(0, 2)]
        public bool ShowBuffBarText = true;

        [DragFloat2("Position" + "##DRKBuffBar", min = -4000f, max = 4000f)]
        [CollapseWith(5, 2)]
        public Vector2 BuffBarPosition = new Vector2(0, HUDConstants.JobHudsBaseY - 32);

        [DragFloat2("Size" + "##DRKBuffBar", min = 0, max = 4000f)]
        [CollapseWith(10, 2)]
        public Vector2 BuffBarSize = new Vector2(254, 20);

        [DragInt("Padding" + "##DRKBuffBar", min = 0)]
        [CollapseWith(15, 2)]
        public int BuffBarPadding = 2;

        [ColorEdit4("Blood Weapon Color" + "##DRKBuffBar")]
        [CollapseWith(20, 2)]
        public PluginConfigColor BloodWeaponColor = new(new Vector4(160f / 255f, 0f / 255f, 0f / 255f, 100f / 100f));

        [ColorEdit4("Delirium Color" + "##DRKBuffBar")]
        [CollapseWith(25, 2)]
        public PluginConfigColor DeliriumColor = new(new Vector4(255f / 255f, 255f / 255f, 255f / 255f, 100f / 100f));
        #endregion

        #region Living Shadow
        [Checkbox("Show Living Shadow Bar")]
        [CollapseControl(45, 3)]
        public bool ShowLivingShadowBar = false;

        [Checkbox("Show Text" + "##DRKLivingShadow")]
        [CollapseWith(0, 3)]
        public bool ShowLivingShadowBarText = true;

        [DragFloat2("Position" + "##DRKLivingShadow", min = -4000f, max = 4000f)]
        [CollapseWith(5, 3)]
        public Vector2 LivingShadowBarPosition = new Vector2(0, HUDConstants.JobHudsBaseY - 10);

        [DragFloat2("Size" + "##DRKLivingShadow", min = 0, max = 4000f)]
        [CollapseWith(10, 3)]
        public Vector2 LivingShadowBarSize = new Vector2(254, 20);

        [DragInt("Padding" + "##DRKLivingShadow", min = 0)]
        [CollapseWith(15, 3)]
        public int LivingShadowPadding = 2;

        [ColorEdit4("Color" + "##DRKLivingShadow")]
        [CollapseWith(20, 3)]
        public PluginConfigColor LivingShadowColor = new(new Vector4(225f / 255f, 105f / 255f, 205f / 255f, 100f / 100f));
        #endregion
    }
}
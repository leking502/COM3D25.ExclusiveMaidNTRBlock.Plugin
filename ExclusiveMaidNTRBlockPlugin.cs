using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Kasizuki;
using MaidStatus;
using Schedule;
using UnityEngine;
using Yotogis;

using MaidStatusStatus = MaidStatus.Status;
using PlayerStatusStatus = PlayerStatus.Status;

namespace ExclusiveMaidNTRBlock
{
    internal sealed class ExclusiveMaidNTRBlockSettings
    {
        private readonly ConfigFile _config;

        internal readonly ConfigEntry<bool> PluginEnabled;
        internal readonly ConfigEntry<KeyboardShortcut> ToggleWindowShortcut;
        internal readonly ConfigEntry<bool> BlockScenarioEvents;
        internal readonly ConfigEntry<bool> BlockFreeModeEveryday;
        internal readonly ConfigEntry<bool> BlockPrivateModeEvents;
        internal readonly ConfigEntry<bool> BlockEmpireLifeMode;
        internal readonly ConfigEntry<bool> BlockSchedule;
        internal readonly ConfigEntry<bool> BlockKasizuki;
        internal readonly ConfigEntry<bool> BlockHoneymoon;
        internal readonly ConfigEntry<bool> BlockYotogiClassList;
        internal readonly ConfigEntry<bool> BlockYotogiSkillList;
        internal readonly ConfigEntry<bool> BlockYotogiSkillSelect;
        internal readonly ConfigEntry<bool> BlockYotogiResult;
        internal readonly ConfigEntry<bool> BlockFreeYotogiSkillSelect;

        internal ExclusiveMaidNTRBlockSettings(ConfigFile config)
        {
            _config = config;
            PluginEnabled = config.Bind("General", "Enabled", true, "启用专属女仆 NTR 屏蔽。");
            ToggleWindowShortcut = config.Bind(
                "General",
                "ToggleWindowShortcut",
                new KeyboardShortcut(KeyCode.F10),
                "打开或关闭游戏内配置窗口。");
            BlockScenarioEvents = config.Bind("Block", "ScenarioEvents", true, "剧情 NTR 事件。");
            BlockFreeModeEveryday = config.Bind("Block", "FreeModeEveryday", true, "自由模式日常 NTR 事件。");
            BlockPrivateModeEvents = config.Bind("Block", "PrivateModeEvents", true, "私有模式 NTR 事件。");
            BlockEmpireLifeMode = config.Bind("Block", "EmpireLifeMode", true, "EmpireLife NTR 内容。");
            BlockSchedule = config.Bind("Block", "Schedule", true, "日程与设施中的 NTR 任务。");
            BlockKasizuki = config.Bind("Block", "Kasizuki", true, "傅き模式中主人公以外的顾客。");
            BlockHoneymoon = config.Bind("Block", "Honeymoon", true, "Honeymoon 中的 NTR 事件。");
            BlockYotogiClassList = config.Bind("Block", "YotogiClassList", true, "夜伽职业列表中的 NTR 职业。");
            BlockYotogiSkillList = config.Bind("Block", "YotogiSkillList", true, "夜伽技能列表中的 NTR 技能。");
            BlockYotogiSkillSelect = config.Bind("Block", "YotogiSkillSelect", true, "正常夜伽技能选择中的 NTR 类别。");
            BlockYotogiResult = config.Bind("Block", "YotogiResult", true, "夜伽结果页中的 NTR 技能获得显示。");
            BlockFreeYotogiSkillSelect = config.Bind("Block", "FreeYotogiSkillSelect", true, "自由夜伽技能选择中的 NTR 技能类别。");
        }

        internal bool IsEnabled(ConfigEntry<bool> entry)
        {
            return PluginEnabled.Value && entry != null && entry.Value;
        }

        internal void Save()
        {
            _config.Save();
        }
    }

    [BepInPlugin("com3d25.exclusivemaidntrblock.plugin", "COM3D25 Exclusive Maid NTR Block", "0.2.0")]
    public class ExclusiveMaidNTRBlockPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ExclusiveMaidNTRBlockSettings Settings;

        private const int ConfigWindowId = 24070901;
        private Harmony _harmony;
        private Rect _configWindowRect = new Rect(40f, 80f, 430f, 500f);
        private Vector2 _configScroll;
        private bool _showConfigWindow;

        private void Awake()
        {
            Log = Logger;
            Settings = new ExclusiveMaidNTRBlockSettings(Config);
            _harmony = new Harmony("com3d25.exclusivemaidntrblock.plugin");

            try
            {
                PatchPostfix(AccessTools.PropertyGetter(typeof(PlayerStatusStatus), "lockNTRPlay"),
                    nameof(ExclusiveMaidNTRBlockPatches.PlayerStatus_lockNTRPlay_Postfix));

                PatchWithFinalizer(
                    AccessTools.Method(
                        typeof(FreeModeItemEveryday),
                        "CreateItemEverydayList",
                        new[] { typeof(FreeModeItemEveryday.ScnearioType), typeof(MaidStatusStatus) }),
                    nameof(ExclusiveMaidNTRBlockPatches.FreeModeItemEveryday_CreateItemEverydayList_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(PrivateEventSelectPanel), "Setup"),
                    nameof(ExclusiveMaidNTRBlockPatches.PrivateEventSelectPanel_Setup_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchPrefix(
                    AccessTools.Method(typeof(ScenarioData), "AddEventMaid", new[] { typeof(Maid) }),
                    nameof(ExclusiveMaidNTRBlockPatches.ScenarioData_AddEventMaid_Prefix));

                PatchWithPostfixAndFinalizer(
                    AccessTools.Method(typeof(ScenarioData), "CheckPlayableCondition", new[] { typeof(bool) }),
                    nameof(ExclusiveMaidNTRBlockPatches.ScenarioData_CheckPlayableCondition_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.ScenarioData_CheckPlayableCondition_Postfix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopScenarioContext_Finalizer));

                PatchPostfix(
                    AccessTools.Method(
                        typeof(EmpireLifeModeAPI),
                        "IsCorrectMaid",
                        new[] { typeof(EmpireLifeModeData.Data), typeof(Maid) }),
                    nameof(ExclusiveMaidNTRBlockPatches.EmpireLifeModeAPI_IsCorrectMaid_Postfix));

                PatchPostfix(
                    AccessTools.Method(
                        typeof(ScheduleAPI),
                        "VisibleNightWork",
                        new[] { typeof(int), typeof(Maid), typeof(bool) }),
                    nameof(ExclusiveMaidNTRBlockPatches.ScheduleAPI_VisibleNightWork_Postfix));

                PatchPostfix(
                    AccessTools.Method(
                        typeof(ScheduleAPI),
                        "EnableNightWork",
                        new[] { typeof(int), typeof(Maid), typeof(bool), typeof(bool) }),
                    nameof(ExclusiveMaidNTRBlockPatches.ScheduleAPI_EnableNightWork_Postfix));

                PatchPrefix(
                    AccessTools.Method(
                        typeof(ScheduleAPI),
                        "CheckWork",
                        new[] { typeof(Maid), typeof(int), typeof(bool), typeof(ScheduleMgr.ScheduleTime) }),
                    nameof(ExclusiveMaidNTRBlockPatches.ScheduleAPI_CheckWork_Prefix));

                PatchPostfix(
                    AccessTools.Constructor(
                        typeof(ScheduleWork),
                        new[] { typeof(ScheduleType), typeof(Slot), typeof(int) }),
                    nameof(ExclusiveMaidNTRBlockPatches.ScheduleWork_Ctor_Postfix));

                PatchPrefix(
                    AccessTools.Method(
                        typeof(ScheduleScene),
                        "SetNoonWorkSlot_Safe",
                        new[] { typeof(ScheduleMgr.ScheduleTime), typeof(int), typeof(int) }),
                    nameof(ExclusiveMaidNTRBlockPatches.ScheduleScene_SetNoonWorkSlot_Safe_Prefix));

                PatchPrefix(
                    AccessTools.Method(
                        typeof(Facility),
                        "AllocationMaid",
                        new[] { typeof(Maid), typeof(ScheduleMgr.ScheduleTime) }),
                    nameof(ExclusiveMaidNTRBlockPatches.Facility_AllocationMaid_Prefix));

                PatchPrefix(
                    AccessTools.Method(typeof(ScheduleTaskViewer), "Call"),
                    nameof(ExclusiveMaidNTRBlockPatches.ScheduleTaskViewer_Call_Prefix));

                PatchPrefix(
                    AccessTools.Method(typeof(ScheduleTaskViewer), "AddYotogiTaskUnit"),
                    nameof(ExclusiveMaidNTRBlockPatches.ScheduleTaskViewer_AddYotogiTaskUnit_Prefix));

                PatchPrefix(
                    AccessTools.Method(typeof(ScheduleTaskViewer), "AddWorkTaskUnit"),
                    nameof(ExclusiveMaidNTRBlockPatches.ScheduleTaskViewer_AddWorkTaskUnit_Prefix));

                PatchWithFinalizer(
                    AccessTools.Method(
                        typeof(Honeymoon.HoneymoonManager),
                        "GetExecutableEventList",
                        new[] { typeof(Honeymoon.HoneymoonDatabase.Localtion) }),
                    nameof(ExclusiveMaidNTRBlockPatches.HoneymoonManager_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(Honeymoon.HoneymoonManager), "GetFreeMode"),
                    nameof(ExclusiveMaidNTRBlockPatches.HoneymoonManager_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(YotogiClassListManager), "CreateData", new[] { typeof(Maid) }),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiClassListManager_CreateData_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(YotogiSkillListManager), "CreateData", new[] { typeof(Maid) }),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiSkillListManager_CreateData_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchPostfix(
                    AccessTools.Method(
                        typeof(YotogiSkillListManager),
                        "CreateDatas",
                        new[] { typeof(MaidStatusStatus), typeof(bool), typeof(Skill.Data.SpecialConditionType) }),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiSkillListManager_CreateDatas_Postfix));

                PatchPostfix(
                    AccessTools.Method(typeof(YotogiSkillSelectManager), "OnCall"),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiSkillSelectManager_OnCall_Postfix));

                PatchWithPostfixAndFinalizer(
                    AccessTools.Method(
                        typeof(YotogiSkillSelectManager),
                        "CreateSkillButtons",
                        new[] { typeof(Skill.Data.SpecialConditionType) }),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiSkillSelectManager_CreateSkillButtons_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiSkillSelectManager_CreateSkillButtons_Postfix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopYotogiSkillSelectFilter_Finalizer));

                PatchPostfix(
                    AccessTools.Method(typeof(YotogiOldSkillSelectManager), "OnCall"),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiOldSkillSelectManager_OnCall_Postfix));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(YotogiResultManager), "OnCall"),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiResultManager_OnCall_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(YotogiOldResultManager), "OnCall"),
                    nameof(ExclusiveMaidNTRBlockPatches.YotogiOldResultManager_OnCall_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(FreeSkillSelect), "CreateInstanceButton"),
                    nameof(ExclusiveMaidNTRBlockPatches.FreeSkillSelect_CreateInstanceButton_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(FreeSkillSelect), "CreateCategory"),
                    nameof(ExclusiveMaidNTRBlockPatches.FreeSkillSelect_CreateCategory_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchWithFinalizer(
                    AccessTools.Method(typeof(FreeSkillSelectOld), "CreateCategory"),
                    nameof(ExclusiveMaidNTRBlockPatches.FreeSkillSelectOld_CreateCategory_Prefix),
                    nameof(ExclusiveMaidNTRBlockPatches.PopNtrBlockOverride_Finalizer));

                PatchPostfix(
                    AccessTools.Method(
                        typeof(PlayData.Data),
                        "IsCorrectMaid",
                        new[] { typeof(Maid), typeof(ManDataType), typeof(bool) }),
                    nameof(ExclusiveMaidNTRBlockPatches.Kasizuki_PlayData_IsCorrectMaid_Postfix));

                PatchPrefix(
                    AccessTools.Method(
                        typeof(KasizukiMainMenu),
                        "StartSenario",
                        new[] { typeof(Maid), typeof(RoomData.Data) }),
                    nameof(ExclusiveMaidNTRBlockPatches.KasizukiMainMenu_StartSenario_Prefix));

                PatchPrefix(
                    AccessTools.Method(
                        typeof(KasizukiMainMenu),
                        "StartFree",
                        new[] { typeof(Maid), typeof(RoomData.Data), typeof(ManData.Data), typeof(PlayData.Data) }),
                    nameof(ExclusiveMaidNTRBlockPatches.KasizukiMainMenu_StartFree_Prefix));

                PatchPrefix(
                    AccessTools.Method(
                        typeof(KasizukiPlayInfoCtrl),
                        "OpenManList",
                        new[] { typeof(List<ManData.Data>), typeof(ManData.Data), typeof(Action<UIWFTabButton, ManData.Data>) }),
                    nameof(ExclusiveMaidNTRBlockPatches.KasizukiPlayInfoCtrl_OpenManList_Prefix));

                Logger.LogInfo("COM3D25.ExclusiveMaidNTRBlock.Plugin loaded.");
            }
            catch (Exception ex)
            {
                Logger.LogError("COM3D25.ExclusiveMaidNTRBlock.Plugin patch failed: " + ex);
            }
        }

        private void Update()
        {
            if (Settings != null && Settings.ToggleWindowShortcut.Value.IsDown())
            {
                _showConfigWindow = !_showConfigWindow;
            }
        }

        private void OnGUI()
        {
            if (!_showConfigWindow || Settings == null)
            {
                return;
            }

            _configWindowRect = GUILayout.Window(
                ConfigWindowId,
                _configWindowRect,
                DrawConfigWindow,
                "COM3D25 Exclusive Maid NTR Block",
                GUILayout.Width(430f),
                GUILayout.Height(500f));
        }

        private void DrawConfigWindow(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("专属契约女仆 NTR 屏蔽");
            DrawToggle(Settings.PluginEnabled, "启用插件");

            GUILayout.Space(8f);
            GUILayout.Label("屏蔽范围");
            _configScroll = GUILayout.BeginScrollView(_configScroll, GUILayout.Height(350f));
            DrawToggle(Settings.BlockScenarioEvents, "剧情 NTR 事件");
            DrawToggle(Settings.BlockFreeModeEveryday, "自由模式日常 NTR 事件");
            DrawToggle(Settings.BlockPrivateModeEvents, "私有模式 NTR 事件");
            DrawToggle(Settings.BlockEmpireLifeMode, "EmpireLife NTR 内容");
            DrawToggle(Settings.BlockSchedule, "日程与设施 NTR 任务");
            DrawToggle(Settings.BlockKasizuki, "傅き模式非主人公顾客");
            DrawToggle(Settings.BlockHoneymoon, "Honeymoon NTR 事件");
            DrawToggle(Settings.BlockYotogiClassList, "夜伽职业列表 NTR 职业");
            DrawToggle(Settings.BlockYotogiSkillList, "夜伽技能列表 NTR 技能");
            DrawToggle(Settings.BlockYotogiSkillSelect, "正常夜伽技能选择 NTR 类别");
            DrawToggle(Settings.BlockYotogiResult, "夜伽结果页 NTR 技能显示");
            DrawToggle(Settings.BlockFreeYotogiSkillSelect, "自由夜伽技能选择 NTR 类别");
            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            GUILayout.Label("热键：" + Settings.ToggleWindowShortcut.Value);
            if (GUILayout.Button("关闭窗口"))
            {
                _showConfigWindow = false;
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawToggle(ConfigEntry<bool> entry, string label)
        {
            bool value = GUILayout.Toggle(entry.Value, label);
            if (value != entry.Value)
            {
                entry.Value = value;
                Settings.Save();
            }
        }

        private void PatchPrefix(MethodBase original, string prefixName)
        {
            if (original == null)
            {
                throw new MissingMethodException(prefixName);
            }

            MethodInfo prefix = AccessTools.Method(typeof(ExclusiveMaidNTRBlockPatches), prefixName);
            if (prefix == null)
            {
                throw new MissingMethodException(prefixName);
            }

            _harmony.Patch(original, prefix: new HarmonyMethod(prefix));
        }

        private void PatchPostfix(MethodBase original, string postfixName)
        {
            if (original == null)
            {
                throw new MissingMethodException(postfixName);
            }

            MethodInfo postfix = AccessTools.Method(typeof(ExclusiveMaidNTRBlockPatches), postfixName);
            if (postfix == null)
            {
                throw new MissingMethodException(postfixName);
            }

            _harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }

        private void PatchWithFinalizer(MethodBase original, string prefixName, string finalizerName)
        {
            if (original == null)
            {
                throw new MissingMethodException(prefixName);
            }

            MethodInfo prefix = AccessTools.Method(typeof(ExclusiveMaidNTRBlockPatches), prefixName);
            MethodInfo finalizer = AccessTools.Method(typeof(ExclusiveMaidNTRBlockPatches), finalizerName);
            if (prefix == null || finalizer == null)
            {
                throw new MissingMethodException(prefixName + " / " + finalizerName);
            }

            _harmony.Patch(original, prefix: new HarmonyMethod(prefix), finalizer: new HarmonyMethod(finalizer));
        }

        private void PatchWithPostfixAndFinalizer(MethodBase original, string prefixName, string postfixName, string finalizerName)
        {
            if (original == null)
            {
                throw new MissingMethodException(prefixName);
            }

            MethodInfo prefix = AccessTools.Method(typeof(ExclusiveMaidNTRBlockPatches), prefixName);
            MethodInfo postfix = AccessTools.Method(typeof(ExclusiveMaidNTRBlockPatches), postfixName);
            MethodInfo finalizer = AccessTools.Method(typeof(ExclusiveMaidNTRBlockPatches), finalizerName);
            if (prefix == null || postfix == null || finalizer == null)
            {
                throw new MissingMethodException(prefixName + " / " + postfixName + " / " + finalizerName);
            }

            _harmony.Patch(
                original,
                prefix: new HarmonyMethod(prefix),
                postfix: new HarmonyMethod(postfix),
                finalizer: new HarmonyMethod(finalizer));
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }
    }

    internal static class ExclusiveMaidNTRBlockPatches
    {
        private static readonly FieldInfo FreeSkillSelectMaidField = AccessTools.Field(typeof(FreeSkillSelect), "maid_");
        private static readonly FieldInfo FreeSkillSelectOldMaidField = AccessTools.Field(typeof(FreeSkillSelectOld), "maid_");
        private static readonly FieldInfo YotogiSkillSelectMaidField = AccessTools.Field(typeof(YotogiSkillSelectManager), "maid_");
        private static readonly FieldInfo YotogiSkillSelectCategoryDataArrayField =
            AccessTools.Field(typeof(YotogiSkillSelectManager), "category_data_array_");
        private static readonly FieldInfo YotogiOldSkillSelectMaidField = AccessTools.Field(typeof(YotogiOldSkillSelectManager), "maid_");
        private static readonly FieldInfo YotogiOldSkillSelectCategoryDataArrayField =
            AccessTools.Field(typeof(YotogiOldSkillSelectManager), "category_data_array_");
        private static readonly FieldInfo YotogiResultManagerField = AccessTools.Field(typeof(YotogiResultManager), "yotogi_mgr_");
        private static readonly FieldInfo YotogiOldResultManagerField = AccessTools.Field(typeof(YotogiOldResultManager), "yotogi_mgr_");

        private static FieldInfo _yotogiSkillSelectCategoryObjField;
        private static FieldInfo _yotogiSkillSelectCategoryField;
        private static FieldInfo _yotogiOldSkillSelectCategoryObjField;
        private static FieldInfo _yotogiOldSkillSelectCategoryField;

        [ThreadStatic]
        private static int _ntrBlockOverrideDepth;

        [ThreadStatic]
        private static int _yotogiSkillSelectNtrFilterDepth;

        [ThreadStatic]
        private static Stack<HashSet<int>> _scenarioSkipStack;

        public static void PlayerStatus_lockNTRPlay_Postfix(ref bool __result)
        {
            if (_ntrBlockOverrideDepth > 0 && IsPluginEnabled())
            {
                __result = true;
            }
        }

        public static void FreeModeItemEveryday_CreateItemEverydayList_Prefix(MaidStatusStatus maidStatus, out bool __state)
        {
            __state = IsFreeModeEverydayEnabled() && PushNtrBlockOverrideIfExclusive(maidStatus);
        }

        public static void PrivateEventSelectPanel_Setup_Prefix(object[] __args, out bool __state)
        {
            __state = false;
            if (!IsPrivateModeEventsEnabled())
            {
                return;
            }

            Maid maid = null;
            if (__args != null && __args.Length > 0)
            {
                maid = __args[0] as Maid;
            }

            if (maid == null)
            {
                maid = ResolvePrivateMaid();
            }

            __state = PushNtrBlockOverrideIfExclusive(maid);
        }

        public static Exception PopNtrBlockOverride_Finalizer(bool __state, Exception __exception)
        {
            if (__state)
            {
                _ntrBlockOverrideDepth = Math.Max(0, _ntrBlockOverrideDepth - 1);
            }

            return __exception;
        }

        public static void ScenarioData_CheckPlayableCondition_Prefix(out HashSet<int> __state)
        {
            __state = null;
            if (!IsScenarioEventsEnabled())
            {
                return;
            }

            __state = new HashSet<int>();
            if (_scenarioSkipStack == null)
            {
                _scenarioSkipStack = new Stack<HashSet<int>>();
            }

            _scenarioSkipStack.Push(__state);
        }

        public static void ScenarioData_CheckPlayableCondition_Postfix(
            ScenarioData __instance,
            ref bool __result,
            HashSet<int> __state)
        {
            if (!__result || __instance == null || __state == null || !__state.Contains(__instance.ID))
            {
                return;
            }

            if (IsNtrScenario(__instance) && !HasNonExclusiveEventMaid(__instance))
            {
                __result = false;
            }
        }

        public static Exception PopScenarioContext_Finalizer(Exception __exception)
        {
            if (_scenarioSkipStack != null && _scenarioSkipStack.Count > 0)
            {
                _scenarioSkipStack.Pop();
            }

            return __exception;
        }

        public static bool ScenarioData_AddEventMaid_Prefix(ScenarioData __instance, Maid maid)
        {
            if (!IsScenarioEventsEnabled())
            {
                return true;
            }

            HashSet<int> context = CurrentScenarioContext;
            if (context == null || __instance == null || maid == null)
            {
                return true;
            }

            if (!IsNtrScenario(__instance) || !IsExclusive(maid))
            {
                return true;
            }

            context.Add(__instance.ID);
            return false;
        }

        public static void EmpireLifeModeAPI_IsCorrectMaid_Postfix(
            EmpireLifeModeData.Data data,
            Maid maid,
            ref bool __result)
        {
            if (!IsEmpireLifeModeEnabled() || !__result || data == null || maid == null || !IsExclusive(maid))
            {
                return;
            }

            if (IsLifeModeNtrData(data))
            {
                __result = false;
            }
        }

        public static void ScheduleAPI_VisibleNightWork_Postfix(int workId, Maid maid, ref bool __result)
        {
            if (IsScheduleEnabled() && __result && IsExclusive(maid) && IsNtrScheduleWork(workId))
            {
                __result = false;
            }
        }

        public static void ScheduleAPI_EnableNightWork_Postfix(int workId, Maid maid, ref bool __result)
        {
            if (IsScheduleEnabled() && __result && IsExclusive(maid) && IsNtrScheduleWork(workId))
            {
                __result = false;
            }
        }

        public static bool ScheduleAPI_CheckWork_Prefix(Maid maid, int taskId, ScheduleMgr.ScheduleTime time)
        {
            if (!IsScheduleEnabled() || !IsExclusive(maid) || !IsNtrFacilityWork(taskId))
            {
                return true;
            }

            ClearScheduleWorkId(maid, time);
            UpdateFacilityAssignedMaidData();
            return false;
        }

        public static void ScheduleWork_Ctor_Postfix(ScheduleWork __instance)
        {
            if (!IsScheduleEnabled() || __instance == null || !__instance.visible || !IsExclusive(__instance.maid))
            {
                return;
            }

            if (IsNtrFacilityWork(__instance.id))
            {
                __instance.visible = false;
                __instance.enabled = false;
            }
        }

        public static bool ScheduleScene_SetNoonWorkSlot_Safe_Prefix(
            ScheduleScene __instance,
            ScheduleMgr.ScheduleTime workTime,
            int slotId,
            int workId)
        {
            if (!IsScheduleEnabled())
            {
                return true;
            }

            Maid maid = ResolveScheduleSceneMaid(__instance, slotId);
            return !IsExclusive(maid) || !IsNtrFacilityWork(workId);
        }

        public static bool Facility_AllocationMaid_Prefix(
            Facility __instance,
            Maid maid,
            ScheduleMgr.ScheduleTime scheduleTime)
        {
            if (!IsScheduleEnabled())
            {
                return true;
            }

            return !IsExclusive(maid) || !IsNtrFacility(__instance);
        }

        public static void ScheduleTaskViewer_Call_Prefix(
            Maid maid,
            Dictionary<ScheduleTaskCtrl.TaskType, List<ScheduleTaskViewer.ViewData>> viewDic)
        {
            if (!IsScheduleEnabled() || !IsExclusive(maid) || viewDic == null)
            {
                return;
            }

            List<ScheduleTaskViewer.ViewData> list;
            if (viewDic.TryGetValue(ScheduleTaskCtrl.TaskType.Yotogi, out list) && list != null)
            {
                list.RemoveAll(IsNtrScheduleView);
            }

            if (viewDic.TryGetValue(ScheduleTaskCtrl.TaskType.Work, out list) && list != null)
            {
                list.RemoveAll(IsNtrFacilityView);
            }
        }

        public static bool ScheduleTaskViewer_AddYotogiTaskUnit_Prefix(
            ScheduleTaskViewer __instance,
            ScheduleTaskViewer.ViewData view_data)
        {
            if (!IsScheduleEnabled())
            {
                return true;
            }

            Maid maid = null;
            try
            {
                if (__instance != null && __instance.taskCtrl != null && __instance.taskCtrl.ScheduleCtrl != null)
                {
                    maid = __instance.taskCtrl.ScheduleCtrl.SelectedMaid;
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve schedule maid failed: " + ex.GetType().Name);
                }
            }

            return !IsExclusive(maid) || !IsNtrScheduleView(view_data);
        }

        public static bool ScheduleTaskViewer_AddWorkTaskUnit_Prefix(
            ScheduleTaskViewer __instance,
            ScheduleTaskViewer.ViewData view_data)
        {
            if (!IsScheduleEnabled())
            {
                return true;
            }

            Maid maid = ResolveScheduleViewerMaid(__instance);
            return !IsExclusive(maid) || !IsNtrFacilityView(view_data);
        }

        public static void HoneymoonManager_Prefix(Honeymoon.HoneymoonManager __instance, out bool __state)
        {
            __state = IsHoneymoonEnabled() && PushNtrBlockOverrideIfExclusive(ResolveHoneymoonTargetMaid(__instance));
        }

        public static void YotogiClassListManager_CreateData_Prefix(Maid maid, out bool __state)
        {
            __state = IsYotogiClassListEnabled() && PushNtrBlockOverrideIfExclusive(maid);
        }

        public static void YotogiSkillListManager_CreateData_Prefix(Maid maid, out bool __state)
        {
            __state = IsYotogiSkillListEnabled() && PushNtrBlockOverrideIfExclusive(maid);
        }

        public static void YotogiSkillListManager_CreateDatas_Postfix(
            ref Dictionary<int, YotogiSkillListManager.Data> __result)
        {
            if (_yotogiSkillSelectNtrFilterDepth <= 0 || __result == null)
            {
                return;
            }

            List<int> removeKeys = null;
            foreach (KeyValuePair<int, YotogiSkillListManager.Data> item in __result)
            {
                if (item.Value != null && IsNtrYotogiSkillSelectCategory(item.Value.skillData))
                {
                    if (removeKeys == null)
                    {
                        removeKeys = new List<int>();
                    }

                    removeKeys.Add(item.Key);
                }
            }

            if (removeKeys == null)
            {
                return;
            }

            for (int i = 0; i < removeKeys.Count; i++)
            {
                __result.Remove(removeKeys[i]);
            }
        }

        public static void YotogiSkillSelectManager_OnCall_Postfix(YotogiSkillSelectManager __instance)
        {
            ApplyYotogiSkillSelectCategoryVisibility(__instance);
        }

        public static void YotogiSkillSelectManager_CreateSkillButtons_Prefix(
            YotogiSkillSelectManager __instance,
            out bool __state)
        {
            __state = ShouldHideYotogiSkillSelectNtr(__instance);
            if (__state)
            {
                _yotogiSkillSelectNtrFilterDepth++;
            }
        }

        public static void YotogiSkillSelectManager_CreateSkillButtons_Postfix(YotogiSkillSelectManager __instance)
        {
            ApplyYotogiSkillSelectCategoryVisibility(__instance);
        }

        public static Exception PopYotogiSkillSelectFilter_Finalizer(bool __state, Exception __exception)
        {
            if (__state)
            {
                _yotogiSkillSelectNtrFilterDepth = Math.Max(0, _yotogiSkillSelectNtrFilterDepth - 1);
            }

            return __exception;
        }

        public static void YotogiOldSkillSelectManager_OnCall_Postfix(YotogiOldSkillSelectManager __instance)
        {
            ApplyYotogiOldSkillSelectCategoryVisibility(__instance);
        }

        public static void YotogiResultManager_OnCall_Prefix(YotogiResultManager __instance, out bool __state)
        {
            __state = IsYotogiResultEnabled() && PushNtrBlockOverrideIfExclusive(ResolveYotogiResultMaid(__instance));
        }

        public static void YotogiOldResultManager_OnCall_Prefix(YotogiOldResultManager __instance, out bool __state)
        {
            __state = IsYotogiResultEnabled() && PushNtrBlockOverrideIfExclusive(ResolveYotogiOldResultMaid(__instance));
        }

        public static void FreeSkillSelect_CreateInstanceButton_Prefix(FreeSkillSelect __instance, out bool __state)
        {
            __state = IsFreeYotogiSkillSelectEnabled() && PushNtrBlockOverrideIfExclusive(ResolveFreeSkillSelectMaid(__instance));
        }

        public static void FreeSkillSelect_CreateCategory_Prefix(FreeSkillSelect __instance, out bool __state)
        {
            __state = IsFreeYotogiSkillSelectEnabled() && PushNtrBlockOverrideIfExclusive(ResolveFreeSkillSelectMaid(__instance));
        }

        public static void FreeSkillSelectOld_CreateCategory_Prefix(FreeSkillSelectOld __instance, out bool __state)
        {
            __state = IsFreeYotogiSkillSelectEnabled() && PushNtrBlockOverrideIfExclusive(ResolveFreeSkillSelectOldMaid(__instance));
        }

        public static void Kasizuki_PlayData_IsCorrectMaid_Postfix(
            Maid maid,
            ManDataType manType,
            ref bool __result)
        {
            if (IsKasizukiEnabled() && __result && IsExclusive(maid) && IsKasizukiNtrMan(manType))
            {
                __result = false;
            }
        }

        public static bool KasizukiMainMenu_StartSenario_Prefix(Maid targetMaid)
        {
            if (!IsKasizukiEnabled())
            {
                return true;
            }

            ManDataType manType = ResolveKasizukiCurrentManType();
            if (!IsExclusive(targetMaid) || !IsKasizukiNtrMan(manType))
            {
                return true;
            }

            ShowKasizukiBlockDialog();
            return false;
        }

        public static bool KasizukiMainMenu_StartFree_Prefix(Maid targetMaid, ManData.Data targetMan)
        {
            if (!IsKasizukiEnabled())
            {
                return true;
            }

            if (!IsExclusive(targetMaid) || !IsKasizukiNtrMan(targetMan))
            {
                return true;
            }

            ShowKasizukiBlockDialog();
            return false;
        }

        public static void KasizukiPlayInfoCtrl_OpenManList_Prefix(
            KasizukiPlayInfoCtrl __instance,
            ref List<ManData.Data> manDataList,
            ref ManData.Data selectingData)
        {
            if (!IsKasizukiEnabled() || manDataList == null)
            {
                return;
            }

            Maid maid = null;
            try
            {
                if (__instance != null)
                {
                    maid = __instance.selectedMaid;
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve Kasizuki selected maid failed: " + ex.GetType().Name);
                }
            }

            if (!IsExclusive(maid))
            {
                return;
            }

            manDataList = manDataList.FindAll(IsKasizukiOwnerMan);
            if (selectingData != null && IsKasizukiNtrMan(selectingData))
            {
                selectingData = manDataList.Count > 0 ? manDataList[0] : null;
            }
        }

        private static bool IsPluginEnabled()
        {
            ExclusiveMaidNTRBlockSettings settings = ExclusiveMaidNTRBlockPlugin.Settings;
            return settings == null || settings.PluginEnabled.Value;
        }

        private static bool IsFeatureEnabled(Func<ExclusiveMaidNTRBlockSettings, ConfigEntry<bool>> getEntry)
        {
            ExclusiveMaidNTRBlockSettings settings = ExclusiveMaidNTRBlockPlugin.Settings;
            if (settings == null)
            {
                return true;
            }

            ConfigEntry<bool> entry = getEntry(settings);
            return settings.IsEnabled(entry);
        }

        private static bool IsScenarioEventsEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockScenarioEvents);
        }

        private static bool IsFreeModeEverydayEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockFreeModeEveryday);
        }

        private static bool IsPrivateModeEventsEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockPrivateModeEvents);
        }

        private static bool IsEmpireLifeModeEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockEmpireLifeMode);
        }

        private static bool IsScheduleEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockSchedule);
        }

        private static bool IsKasizukiEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockKasizuki);
        }

        private static bool IsHoneymoonEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockHoneymoon);
        }

        private static bool IsYotogiClassListEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockYotogiClassList);
        }

        private static bool IsYotogiSkillListEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockYotogiSkillList);
        }

        private static bool IsYotogiSkillSelectEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockYotogiSkillSelect);
        }

        private static bool IsYotogiResultEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockYotogiResult);
        }

        private static bool IsFreeYotogiSkillSelectEnabled()
        {
            return IsFeatureEnabled(settings => settings.BlockFreeYotogiSkillSelect);
        }

        private static bool PushNtrBlockOverrideIfExclusive(MaidStatusStatus status)
        {
            if (status == null || status.contract != Contract.Exclusive)
            {
                return false;
            }

            _ntrBlockOverrideDepth++;
            return true;
        }

        private static bool PushNtrBlockOverrideIfExclusive(Maid maid)
        {
            if (!IsExclusive(maid))
            {
                return false;
            }

            _ntrBlockOverrideDepth++;
            return true;
        }

        private static Maid ResolvePrivateMaid()
        {
            try
            {
                if (PrivateMaidMode.PrivateModeMgr.Instance != null &&
                    PrivateMaidMode.PrivateModeMgr.Instance.PrivateMaid != null)
                {
                    return PrivateMaidMode.PrivateModeMgr.Instance.PrivateMaid;
                }

                if (GameMain.Instance != null && GameMain.Instance.CharacterMgr != null)
                {
                    return GameMain.Instance.CharacterMgr.GetMaid(0);
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("ResolvePrivateMaid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static Maid ResolveFreeSkillSelectMaid(FreeSkillSelect selector)
        {
            if (selector == null || FreeSkillSelectMaidField == null)
            {
                return null;
            }

            try
            {
                return FreeSkillSelectMaidField.GetValue(selector) as Maid;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve FreeSkillSelect maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static Maid ResolveFreeSkillSelectOldMaid(FreeSkillSelectOld selector)
        {
            if (selector == null || FreeSkillSelectOldMaidField == null)
            {
                return null;
            }

            try
            {
                return FreeSkillSelectOldMaidField.GetValue(selector) as Maid;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve FreeSkillSelectOld maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static Maid ResolveHoneymoonTargetMaid(Honeymoon.HoneymoonManager manager)
        {
            try
            {
                return manager != null ? manager.targetMaid : null;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve Honeymoon target maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static Maid ResolveYotogiSkillSelectMaid(YotogiSkillSelectManager selector)
        {
            if (selector == null || YotogiSkillSelectMaidField == null)
            {
                return null;
            }

            try
            {
                return YotogiSkillSelectMaidField.GetValue(selector) as Maid;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve YotogiSkillSelect maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static Maid ResolveYotogiOldSkillSelectMaid(YotogiOldSkillSelectManager selector)
        {
            if (selector == null || YotogiOldSkillSelectMaidField == null)
            {
                return null;
            }

            try
            {
                return YotogiOldSkillSelectMaidField.GetValue(selector) as Maid;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve YotogiOldSkillSelect maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static Maid ResolveYotogiResultMaid(YotogiResultManager resultManager)
        {
            try
            {
                if (resultManager == null || YotogiResultManagerField == null)
                {
                    return null;
                }

                YotogiManager manager = YotogiResultManagerField.GetValue(resultManager) as YotogiManager;
                return manager != null ? manager.maid : null;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve YotogiResult maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static Maid ResolveYotogiOldResultMaid(YotogiOldResultManager resultManager)
        {
            try
            {
                if (resultManager == null || YotogiOldResultManagerField == null)
                {
                    return null;
                }

                YotogiOldManager manager = YotogiOldResultManagerField.GetValue(resultManager) as YotogiOldManager;
                return manager != null ? manager.maid : null;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve YotogiOldResult maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static bool ShouldHideYotogiSkillSelectNtr(YotogiSkillSelectManager selector)
        {
            if (IsGlobalNtrPlayLocked())
            {
                return true;
            }

            return IsYotogiSkillSelectEnabled() && IsExclusive(ResolveYotogiSkillSelectMaid(selector));
        }

        private static void ApplyYotogiSkillSelectCategoryVisibility(YotogiSkillSelectManager selector)
        {
            if (selector == null || YotogiSkillSelectCategoryDataArrayField == null)
            {
                return;
            }

            try
            {
                EnsureYotogiSkillSelectCategoryFields();
                if (_yotogiSkillSelectCategoryObjField == null || _yotogiSkillSelectCategoryField == null)
                {
                    return;
                }

                Array categoryDataArray = YotogiSkillSelectCategoryDataArrayField.GetValue(selector) as Array;
                if (categoryDataArray == null)
                {
                    return;
                }

                bool hide = ShouldHideYotogiSkillSelectNtr(selector);
                Transform parent = null;
                for (int i = 0; i < categoryDataArray.Length; i++)
                {
                    object categoryData = categoryDataArray.GetValue(i);
                    GameObject categoryObject = _yotogiSkillSelectCategoryObjField.GetValue(categoryData) as GameObject;
                    object categoryValue = _yotogiSkillSelectCategoryField.GetValue(categoryData);
                    if (categoryObject == null || categoryValue == null)
                    {
                        continue;
                    }

                    if (parent == null)
                    {
                        parent = categoryObject.transform.parent;
                    }

                    Yotogi.Category category = (Yotogi.Category)categoryValue;
                    if (IsNtrYotogiCategory(category))
                    {
                        categoryObject.SetActive(!hide);
                    }
                }

                if (parent != null)
                {
                    UIWFTabPanel tabPanel = parent.GetComponent<UIWFTabPanel>();
                    if (tabPanel != null)
                    {
                        tabPanel.UpdateChildren();
                    }

                    UIGrid grid = parent.GetComponent<UIGrid>();
                    if (grid != null)
                    {
                        grid.Reposition();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Apply YotogiSkillSelect category visibility failed: " + ex.GetType().Name);
                }
            }
        }

        private static bool ShouldHideYotogiOldSkillSelectNtr(YotogiOldSkillSelectManager selector)
        {
            if (IsGlobalNtrPlayLocked())
            {
                return true;
            }

            return IsYotogiSkillSelectEnabled() && IsExclusive(ResolveYotogiOldSkillSelectMaid(selector));
        }

        private static void ApplyYotogiOldSkillSelectCategoryVisibility(YotogiOldSkillSelectManager selector)
        {
            if (selector == null || YotogiOldSkillSelectCategoryDataArrayField == null)
            {
                return;
            }

            try
            {
                EnsureYotogiOldSkillSelectCategoryFields();
                if (_yotogiOldSkillSelectCategoryObjField == null || _yotogiOldSkillSelectCategoryField == null)
                {
                    return;
                }

                Array categoryDataArray = YotogiOldSkillSelectCategoryDataArrayField.GetValue(selector) as Array;
                if (categoryDataArray == null)
                {
                    return;
                }

                bool hide = ShouldHideYotogiOldSkillSelectNtr(selector);
                Transform parent = null;
                for (int i = 0; i < categoryDataArray.Length; i++)
                {
                    object categoryData = categoryDataArray.GetValue(i);
                    GameObject categoryObject = _yotogiOldSkillSelectCategoryObjField.GetValue(categoryData) as GameObject;
                    object categoryValue = _yotogiOldSkillSelectCategoryField.GetValue(categoryData);
                    if (categoryObject == null || categoryValue == null)
                    {
                        continue;
                    }

                    if (parent == null)
                    {
                        parent = categoryObject.transform.parent;
                    }

                    YotogiOld.Category category = (YotogiOld.Category)categoryValue;
                    if (IsNtrYotogiOldCategory(category))
                    {
                        categoryObject.SetActive(!hide);
                    }
                }

                if (parent != null)
                {
                    UIWFTabPanel tabPanel = parent.GetComponent<UIWFTabPanel>();
                    if (tabPanel != null)
                    {
                        tabPanel.UpdateChildren();
                    }

                    UIGrid grid = parent.GetComponent<UIGrid>();
                    if (grid != null)
                    {
                        grid.Reposition();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Apply YotogiOldSkillSelect category visibility failed: " + ex.GetType().Name);
                }
            }
        }

        private static void EnsureYotogiSkillSelectCategoryFields()
        {
            if (_yotogiSkillSelectCategoryObjField != null && _yotogiSkillSelectCategoryField != null)
            {
                return;
            }

            if (YotogiSkillSelectCategoryDataArrayField == null)
            {
                return;
            }

            Type categoryDataType = YotogiSkillSelectCategoryDataArrayField.FieldType.GetElementType();
            if (categoryDataType == null)
            {
                return;
            }

            _yotogiSkillSelectCategoryObjField = AccessTools.Field(categoryDataType, "obj");
            _yotogiSkillSelectCategoryField = AccessTools.Field(categoryDataType, "category");
        }

        private static void EnsureYotogiOldSkillSelectCategoryFields()
        {
            if (_yotogiOldSkillSelectCategoryObjField != null && _yotogiOldSkillSelectCategoryField != null)
            {
                return;
            }

            if (YotogiOldSkillSelectCategoryDataArrayField == null)
            {
                return;
            }

            Type categoryDataType = YotogiOldSkillSelectCategoryDataArrayField.FieldType.GetElementType();
            if (categoryDataType == null)
            {
                return;
            }

            _yotogiOldSkillSelectCategoryObjField = AccessTools.Field(categoryDataType, "obj");
            _yotogiOldSkillSelectCategoryField = AccessTools.Field(categoryDataType, "category");
        }

        private static bool IsExclusive(Maid maid)
        {
            return maid != null && maid.status != null && maid.status.contract == Contract.Exclusive;
        }

        private static bool IsGlobalNtrPlayLocked()
        {
            try
            {
                return GameMain.Instance != null &&
                       GameMain.Instance.CharacterMgr != null &&
                       GameMain.Instance.CharacterMgr.status != null &&
                       GameMain.Instance.CharacterMgr.status.lockNTRPlay;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Read global lockNTRPlay failed: " + ex.GetType().Name);
                }
            }

            return false;
        }

        private static bool IsNtrYotogiSkillSelectCategory(Skill.Data skill)
        {
            return skill != null && IsNtrYotogiCategory(skill.category);
        }

        private static bool IsNtrYotogiCategory(Yotogi.Category category)
        {
            return category == Yotogi.Category.交換 || category == Yotogi.Category.乱交;
        }

        private static bool IsNtrYotogiOldCategory(YotogiOld.Category category)
        {
            return category == YotogiOld.Category.交換 || category == YotogiOld.Category.乱交;
        }

        private static bool IsNtrScenario(ScenarioData data)
        {
            return data != null &&
                   string.Equals(data.IconName, "event_icon_ntr", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasNonExclusiveEventMaid(ScenarioData data)
        {
            try
            {
                List<Maid> maids = data.GetEventMaidList();
                if (maids == null)
                {
                    return false;
                }

                for (int i = 0; i < maids.Count; i++)
                {
                    if (!IsExclusive(maids[i]))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("HasNonExclusiveEventMaid failed: " + ex.GetType().Name);
                }
            }

            return false;
        }

        private static bool IsLifeModeNtrData(EmpireLifeModeData.Data data)
        {
            if (data.dataNTRBlock == null)
            {
                return false;
            }

            return data.dataNTRBlock.Value == false;
        }

        private static bool IsNtrScheduleView(ScheduleTaskViewer.ViewData viewData)
        {
            return viewData.schedule != null && IsNtrScheduleWork(viewData.schedule.id);
        }

        private static bool IsNtrFacilityView(ScheduleTaskViewer.ViewData viewData)
        {
            return viewData.schedule != null && IsNtrFacilityWork(viewData.schedule.id);
        }

        private static bool IsNtrScheduleWork(int workId)
        {
            try
            {
                if (ScheduleCSVData.NetorareFlag != null && ScheduleCSVData.NetorareFlag.Contains(workId))
                {
                    return true;
                }

                if (ScheduleCSVData.YotogiData != null &&
                    ScheduleCSVData.YotogiData.ContainsKey(workId) &&
                    ScheduleCSVData.YotogiData[workId] != null)
                {
                    return ScheduleCSVData.YotogiData[workId].netorareFlag;
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("IsNtrScheduleWork failed: " + ex.GetType().Name);
                }
            }

            return false;
        }

        private static bool IsNtrFacilityWork(int workId)
        {
            try
            {
                if (ScheduleCSVData.WorkData == null || !ScheduleCSVData.WorkData.ContainsKey(workId))
                {
                    return false;
                }

                ScheduleCSVData.Work work = ScheduleCSVData.WorkData[workId];
                return work != null && IsNtrFacility(work.facility);
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("IsNtrFacilityWork failed: " + ex.GetType().Name);
                }
            }

            return false;
        }

        private static bool IsNtrFacility(Facility facility)
        {
            try
            {
                return facility != null && IsNtrFacility(facility.defaultData);
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("IsNtrFacility failed: " + ex.GetType().Name);
                }
            }

            return false;
        }

        private static bool IsNtrFacility(FacilityDataTable.FacilityDefaultData facility)
        {
            return facility != null && !facility.isEnableNTR;
        }

        private static bool IsKasizukiOwnerMan(ManData.Data data)
        {
            return data != null && data.manType == ManDataType.主人公;
        }

        private static bool IsKasizukiNtrMan(ManData.Data data)
        {
            return data != null && IsKasizukiNtrMan(data.manType);
        }

        private static bool IsKasizukiNtrMan(ManDataType manType)
        {
            return manType != ManDataType.主人公;
        }

        private static ManDataType ResolveKasizukiCurrentManType()
        {
            try
            {
                if (GameMain.Instance != null && GameMain.Instance.KasizukiMgr != null)
                {
                    return (ManDataType)GameMain.Instance.KasizukiMgr.GetNowManType();
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve Kasizuki man type failed: " + ex.GetType().Name);
                }
            }

            return ManDataType.主人公;
        }

        private static void ShowKasizukiBlockDialog()
        {
            try
            {
                if (GameMain.Instance != null && GameMain.Instance.SysDlg != null)
                {
                    GameMain.Instance.SysDlg.Show(
                        "专属契约女仆不能接待主人公以外的顾客。",
                        SystemDialog.TYPE.OK,
                        null,
                        null);
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Show Kasizuki block dialog failed: " + ex.GetType().Name);
                }
            }
        }

        private static Maid ResolveScheduleViewerMaid(ScheduleTaskViewer viewer)
        {
            try
            {
                if (viewer != null && viewer.taskCtrl != null && viewer.taskCtrl.ScheduleCtrl != null)
                {
                    return viewer.taskCtrl.ScheduleCtrl.SelectedMaid;
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve schedule maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static Maid ResolveScheduleSceneMaid(ScheduleScene scene, int slotId)
        {
            try
            {
                if (scene == null || scene.slot == null || slotId < 0 || slotId >= scene.slot.Length)
                {
                    return null;
                }

                Slot slot = scene.slot[slotId];
                return slot != null ? slot.maid : null;
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("Resolve schedule scene maid failed: " + ex.GetType().Name);
                }
            }

            return null;
        }

        private static void ClearScheduleWorkId(Maid maid, ScheduleMgr.ScheduleTime time)
        {
            if (maid == null || maid.status == null)
            {
                return;
            }

            switch (time)
            {
                case ScheduleMgr.ScheduleTime.DayTime:
                    maid.status.noonWorkId = 0;
                    break;
                case ScheduleMgr.ScheduleTime.Night:
                    maid.status.nightWorkId = 0;
                    break;
            }
        }

        private static void UpdateFacilityAssignedMaidData()
        {
            try
            {
                if (GameMain.Instance != null && GameMain.Instance.FacilityMgr != null)
                {
                    GameMain.Instance.FacilityMgr.UpdateFacilityAssignedMaidData();
                }
            }
            catch (Exception ex)
            {
                if (ExclusiveMaidNTRBlockPlugin.Log != null)
                {
                    ExclusiveMaidNTRBlockPlugin.Log.LogWarning("UpdateFacilityAssignedMaidData failed: " + ex.GetType().Name);
                }
            }
        }

        private static HashSet<int> CurrentScenarioContext
        {
            get
            {
                if (_scenarioSkipStack == null || _scenarioSkipStack.Count == 0)
                {
                    return null;
                }

                return _scenarioSkipStack.Peek();
            }
        }
    }
}

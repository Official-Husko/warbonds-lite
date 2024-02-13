using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace rimstocks;

[StaticConstructorOnStartup]
public static class Harmony_SomeNamespace
{
    public const int modularTicksUnit = 60000; //하루의 길이, 하루에 두번이상 저장할시 그날 데이터 덮어씀

    public static readonly MethodInfo ExtraTabTranspilerCall =
        AccessTools.Method(typeof(Harmony_SomeNamespace), "ExtraTabFunc");

    public static readonly FieldInfo
        CurTabAccessor = AccessTools.DeclaredField(typeof(MainTabWindow_History), "curTab");

    public static readonly FieldInfo TabsAccessor = AccessTools.DeclaredField(typeof(MainTabWindow_History), "tabs");

    private static List<CurveMark> marks = new List<CurveMark>();

    public static readonly CustomGraphGroup customGraphGroup = new CustomGraphGroup();

    //WorldComponent_PriceSaveLoad.savePrice 로 가격 저장, 
    //WorldComponent_PriceSaveLoad.loadPrice 로 가격 불러오기

    static Harmony_SomeNamespace()
    {
        var harmony = new Harmony("yayo.rimstocks.2");

        harmony.Patch(
            AccessTools.Method(typeof(MainTabWindow_History), "PreOpen"),
            null,
            new HarmonyMethod(typeof(Harmony_SomeNamespace), nameof(patch_preOpen1))
        );
        harmony.Patch(
            AccessTools.Method(typeof(MainTabWindow_History), "PreOpen"),
            null,
            null,
            new HarmonyMethod(typeof(Harmony_SomeNamespace), nameof(graphTranspiler))
        );

        harmony.Patch(
            AccessTools.Method(typeof(MainTabWindow_History), "DoWindowContents"),
            null,
            new HarmonyMethod(typeof(Harmony_SomeNamespace), nameof(DoWindowContentsPostFix))
        );
    }

    public static void ExtraTabFunc(List<TabRecord> list)
    {
        list.Add(new TabRecord("Statistics".Translate(),
            delegate
            {
                CurTabAccessor.SetValue(null, (byte)2, BindingFlags.NonPublic | BindingFlags.Static, null, null);
            }, () => (byte)CurTabAccessor.GetValue(null) == 2));
        list.Add(new TabRecord("warbond_graphTitle".Translate(),
            delegate
            {
                CurTabAccessor.SetValue(null, (byte)3, BindingFlags.NonPublic | BindingFlags.Static, null, null);
            }, () => (byte)CurTabAccessor.GetValue(null) == 3));
        CurTabAccessor.SetValue(null, (byte)3, BindingFlags.NonPublic | BindingFlags.Static, null, null);
    }

    public static void DoWindowContentsPostFix(Rect rect, HistoryAutoRecorderGroup ___historyAutoRecorderGroup,
        ref FloatRange ___graphSection)
    {
        if ((byte)CurTabAccessor.GetValue(null) != 3)
        {
            return;
        }

        rect.yMin += 45f;
        rect.yMin += 17f;
        GUI.BeginGroup(rect);
        var graphRect = new Rect(0f, 0f, rect.width, 450f);
        var legendRect = new Rect(0f, graphRect.yMax, rect.width - 200, rect.height - graphRect.yMax);
        customGraphGroup.DrawGraph(graphRect, legendRect, ___graphSection);
        Text.Font = GameFont.Small;
        var num = (float)Core.AbsTickGame / GenDate.TicksPerDay;
        if (Widgets.ButtonText(new Rect(graphRect.xMin + graphRect.width - 200, graphRect.yMax, 100f, 40f),
                "Last30Days".Translate()))
        {
            ___graphSection = new FloatRange(Mathf.Max(0f, num - 30f), num);
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

        if (Widgets.ButtonText(new Rect(graphRect.xMin + graphRect.width - 100, graphRect.yMax, 100f, 40f),
                "Last100Days".Translate()))
        {
            ___graphSection = new FloatRange(Mathf.Max(0f, num - 100f), num);
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

        if (Widgets.ButtonText(new Rect(graphRect.xMin + graphRect.width - 200, graphRect.yMax + 40, 100f, 40f),
                "Last300Days".Translate()))
        {
            ___graphSection = new FloatRange(Mathf.Max(0f, num - 300f), num);
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

        if (Widgets.ButtonText(new Rect(graphRect.xMin + graphRect.width - 100, graphRect.yMax + 40, 100f, 40f),
                "AllDays".Translate()))
        {
            ___graphSection = new FloatRange(0f, num);
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

        GUI.EndGroup();
    }

    public static void patch_preOpen1(ref FloatRange ___graphSection)
    {
        var num = (float)Core.AbsTickGame / GenDate.TicksPerDay;
        ___graphSection = new FloatRange(Mathf.Max(0f, num - 30f), num);
    }

    public static IEnumerable<CodeInstruction> graphTranspiler(ILGenerator generator,
        IEnumerable<CodeInstruction> instructions)
    {
        var callVirt4 = 0;
        var nopStack = 0;
        var voidStack = 0;
        foreach (var ci in instructions)
        {
            if (voidStack > 0)
            {
                voidStack -= 1;
            }
            else if (nopStack > 0)
            {
                nopStack -= 1;
                yield return new CodeInstruction(OpCodes.Nop);
            }
            else
            {
                yield return ci;
                if (ci.opcode == OpCodes.Callvirt)
                {
                    callVirt4 += 1;
                }

                if (callVirt4 != 3)
                {
                    continue;
                }

                callVirt4 = -9999;
                voidStack = 3;
                nopStack = 25 - voidStack;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, TabsAccessor);
                yield return new CodeInstruction(OpCodes.Call, ExtraTabTranspilerCall);
            }
        }
    }
}
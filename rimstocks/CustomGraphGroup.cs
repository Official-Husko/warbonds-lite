using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace rimstocks;

public class CustomGraphGroup
{
    private readonly List<SimpleCurveDrawInfo> curves = new List<SimpleCurveDrawInfo>();
    private int cachedGraphTickCount = -1;

    public void DrawGraph(Rect graphRect, Rect legendRect, FloatRange section)
    {
        var ticksGame = Core.AbsTickGame;
        if (ticksGame != cachedGraphTickCount)
        {
            cachedGraphTickCount = ticksGame;
            curves.Clear();
            //foreach (string faction in WorldComponent_PriceSaveLoad.staticInstance.factionToPriceData.Keys)
            foreach (var f in Core.ar_faction)
            {
                var key = util.factionDefNameToKey(f.defName);
                if (!WorldComponent_PriceSaveLoad.staticInstance.factionToPriceData.ContainsKey(key))
                {
                    continue;
                }

                var rs = WorldComponent_PriceSaveLoad.staticInstance.func_289013(key);
                if (!rs.graphEnabled)
                {
                    continue;
                }

                var simpleCurveDrawInfo = new SimpleCurveDrawInfo
                {
                    color = rs.color,
                    label = rs.label,
                    curve = new SimpleCurve()
                };
                foreach (var kvp in rs.timeToPriceData)
                {
                    simpleCurveDrawInfo.curve.Add(new CurvePoint(kvp.Key, kvp.Value), false);
                }

                simpleCurveDrawInfo.curve.SortPoints();
                curves.Add(simpleCurveDrawInfo);
            }
        }

        if (Mathf.Approximately(section.min, section.max))
        {
            section.max += 1.66666669E-05f;
        }

        var curveDrawerStyle = Find.History.curveDrawerStyle;
        curveDrawerStyle.FixedSection = section;
        curveDrawerStyle.UseFixedScale = false;
        curveDrawerStyle.FixedScale = default;
        curveDrawerStyle.YIntegersOnly = false;
        curveDrawerStyle.OnlyPositiveValues = true;
        DrawCurves(graphRect, curves, curveDrawerStyle, legendRect);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    public static void DrawCurves(Rect rect, List<SimpleCurveDrawInfo> curves, SimpleCurveDrawerStyle style = null,
        Rect legendRect = default)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (style == null)
        {
            style = new SimpleCurveDrawerStyle();
        }

        var flipper = true;
        var viewRect = default(Rect);
        foreach (var simpleCurveDrawInfo in curves)
        {
            if (simpleCurveDrawInfo.curve == null)
            {
                continue;
            }

            if (flipper)
            {
                flipper = false;
                viewRect = simpleCurveDrawInfo.curve.View.rect;
            }
            else
            {
                viewRect.xMin = Mathf.Min(viewRect.xMin, simpleCurveDrawInfo.curve.View.rect.xMin);
                viewRect.xMax = Mathf.Max(viewRect.xMax, simpleCurveDrawInfo.curve.View.rect.xMax);
                viewRect.yMin = Mathf.Min(viewRect.yMin, simpleCurveDrawInfo.curve.View.rect.yMin);
                viewRect.yMax = Mathf.Max(viewRect.yMax, simpleCurveDrawInfo.curve.View.rect.yMax);
            }
        }

        if (style.UseFixedScale)
        {
            viewRect.yMin = style.FixedScale.x;
            viewRect.yMax = style.FixedScale.y;
        }

        if (style.OnlyPositiveValues)
        {
            if (viewRect.xMin < 0f)
            {
                viewRect.xMin = 0f;
            }

            if (viewRect.yMin < 0f)
            {
                viewRect.yMin = 0f;
            }
        }

        if (style.UseFixedSection)
        {
            viewRect.xMin = style.FixedSection.min;
            viewRect.xMax = style.FixedSection.max;
        }

        if (style.DrawLegend)
        {
            DrawCurvesLegend(legendRect);
        }

        if (Mathf.Approximately(viewRect.width, 0f) || Mathf.Approximately(viewRect.height, 0f))
        {
            return;
        }

        var rect2 = rect;
        if (style.DrawMeasures)
        {
            rect2.xMin += 60f;
            rect2.yMax -= 30f;
        }

        if (style.DrawBackground)
        {
            GUI.color = new Color(0.1f, 0.1f, 0.1f);
            GUI.DrawTexture(rect2, BaseContent.WhiteTex);
        }

        if (style.DrawBackgroundLines)
        {
            SimpleCurveDrawer.DrawGraphBackgroundLines(rect2, viewRect);
        }

        if (style.DrawMeasures)
        {
            SimpleCurveDrawer.DrawCurveMeasures(rect, viewRect, rect2, style.MeasureLabelsXCount,
                style.MeasureLabelsYCount, style.XIntegersOnly, style.YIntegersOnly);
        }

        foreach (var curve in curves)
        {
            SimpleCurveDrawer.DrawCurveLines(rect2, curve, style.DrawPoints, viewRect, style.UseAntiAliasedLines,
                style.PointsRemoveOptimization);
        }

        if (style.DrawCurveMousePoint)
        {
            SimpleCurveDrawer.DrawCurveMousePoint(curves, rect2, viewRect, style.LabelX);
        }
    }

    public static void DrawCurvesLegend(Rect rect)
    {
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        Text.WordWrap = false;
        GUI.BeginGroup(rect);
        var num = 0f;
        var num2 = 0f;
        var num3 = (int)(rect.width / 140f);
        var num4 = 0;
        foreach (var fd in Core.ar_faction)
        {
            var key = util.factionDefNameToKey(fd.defName);
            if (!WorldComponent_PriceSaveLoad.staticInstance.factionToPriceData.ContainsKey(key))
            {
                continue;
            }

            var rs = WorldComponent_PriceSaveLoad.staticInstance.func_289013(key);
            GUI.color = rs.color;
            GUI.DrawTexture(new Rect(num, num2 + 18.0f, 130f, 3f), BaseContent.WhiteTex);
            GUI.color = Color.white;
            Widgets.Checkbox(new Vector2(num, num2), ref rs.graphEnabled);


            var f = FactionDef.Named(rs.defname);
            if (f != null && f.FactionIcon != null)
            {
                var tex = f.FactionIcon;
                GUI.color = f.colorSpectrum is { Count: > 0 } ? f.colorSpectrum[0] : Color.white;

                GUI.DrawTexture(new Rect(num + 20, num2 - 1f, 25f, 25f), tex); // rect 2번째값 작을수록 y축 위쪽으로 이동
            }

            GUI.color = Color.white;

            if (rs.label != null)
            {
                Widgets.Label(new Rect(num + 45, num2, 85f, 100f), rs.label);
            }

            num4++;
            if (num4 == num3)
            {
                num4 = 0;
                num = 0f;
                num2 += 20f;
            }
            else
            {
                num += 140f;
            }
        }

        GUI.EndGroup();
        GUI.color = Color.white;
        Text.WordWrap = true;
    }
}
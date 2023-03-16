using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace nuff.Ideology_Faction_Icon
{
    /*
     * Code used and modified from Minify Everything by erdelf, under MIT License

        MIT License

        Copyright (c) 2017 

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
     */

    public class Listing_StandardListControl
    {
        base.DoSettingsWindowContents(inRect: inRect);
        Text.Font = GameFont.Medium;
        Rect topRect = inRect.TopPart(pct: 0.05f);
        this.searchTerm = Widgets.TextField(rect: topRect.RightPart(pct: 0.95f).LeftPart(pct: 0.95f), text: this.searchTerm);
        Rect labelRect = inRect.TopPart(pct: 0.1f).BottomHalf();
        Rect bottomRect = inRect.BottomPart(pct: 0.9f);
        #region leftSide

        Rect leftRect = bottomRect.LeftHalf().RightPart(pct: 0.9f).LeftPart(pct: 0.9f);
            GUI.BeginGroup(position: leftRect, style: new GUIStyle(other: GUI.skin.box));
                List<ThingDef> found = DefDatabase<ThingDef>.AllDefs.Where(predicate: td =>
                                                                                          td.Minifiable &&
                                                                                          (td.defName.Contains(value: this.searchTerm) || td.label.Contains(value: this.searchTerm)) &&
                                                                                          !this.Settings.disabledDefList.Contains(item: td)).OrderBy(keySelector: td => td.LabelCap.RawText ?? td.defName)
                                                         .ToList();
            float num = 3f;
            Widgets.BeginScrollView(outRect: leftRect.AtZero(), scrollPosition: ref this.leftScrollPosition,
                                    viewRect: new Rect(x: 0f, y: 0f, width: leftRect.width / 10 * 9, height: found.Count* 32f));
                if (!found.NullOrEmpty())
                {
                    foreach (ThingDef def in found)
                    {
                        Rect rowRect = new Rect(x: 5, y: num, width: leftRect.width - 6, height: 30);
            Widgets.DrawHighlightIfMouseover(rect: rowRect);
                        if (def == this.leftSelectedDef)
                            Widgets.DrawHighlightSelected(rect: rowRect);
                        Widgets.Label(rect: rowRect, label: def.LabelCap.RawText ?? def.defName);
                        if (Widgets.ButtonInvisible(butRect: rowRect))
                            this.leftSelectedDef = def;

                        num += 32f;
                    }
    }

    Widgets.EndScrollView();
    GUI.EndGroup();

    #endregion


    #region rightSide

    Widgets.Label(rect: labelRect.RightHalf().RightPart(pct: 0.9f), label: "Disabled Minifying for:");
    Rect rightRect = bottomRect.RightHalf().RightPart(pct: 0.9f).LeftPart(pct: 0.9f);
    GUI.BeginGroup(position: rightRect, style: GUI.skin.box);
    num = 6f;
    Widgets.BeginScrollView(outRect: rightRect.AtZero(), scrollPosition: ref this.rightScrollPosition,
                            viewRect: new Rect(x: 0f, y: 0f, width: rightRect.width / 5 * 4, height: this.Settings.disabledDefList.Count * 32f));
    if (!this.Settings.disabledDefList.NullOrEmpty())
    {
        foreach (ThingDef def in this.Settings.disabledDefList.Where(predicate: def => (def.defName.Contains(value: this.searchTerm) || def.label.Contains(value: this.searchTerm))))
        {
            Rect rowRect = new Rect(x: 5, y: num, width: leftRect.width - 6, height: 30);
            Widgets.DrawHighlightIfMouseover(rect: rowRect);
            if (def == this.rightSelectedDef)
                Widgets.DrawHighlightSelected(rect: rowRect);
            Widgets.Label(rect: rowRect, label: def.LabelCap.RawText ?? def.defName);
            if (Widgets.ButtonInvisible(butRect: rowRect))
                this.rightSelectedDef = def;

            num += 32f;
        }
    }

    Widgets.EndScrollView();
    GUI.EndGroup();

    #endregion


    #region buttons

    if (Widgets.ButtonImage(butRect: bottomRect.BottomPart(pct: 0.6f).TopPart(pct: 0.1f).RightPart(pct: 0.525f).LeftPart(pct: 0.1f), tex: TexUI.ArrowTexRight) &&
        this.leftSelectedDef != null)
    {
        this.Settings.disabledDefList.Add(item: this.leftSelectedDef);
        this.Settings.disabledDefList = this.Settings.disabledDefList.OrderBy(keySelector: td => td.LabelCap.RawText ?? td.defName).ToList();
        this.rightSelectedDef = this.leftSelectedDef;
        this.leftSelectedDef = null;
        MinifyEverything.RemoveMinifiedFor(def: this.rightSelectedDef);
    }

    if (Widgets.ButtonImage(butRect: bottomRect.BottomPart(pct: 0.4f).TopPart(pct: 0.15f).RightPart(pct: 0.525f).LeftPart(pct: 0.1f), tex: TexUI.ArrowTexLeft) &&
        this.rightSelectedDef != null)
    {
        this.Settings.disabledDefList.Remove(item: this.rightSelectedDef);
        this.leftSelectedDef = this.rightSelectedDef;
        this.rightSelectedDef = null;
        MinifyEverything.AddMinifiedFor(def: this.leftSelectedDef);
    }

            #endregion
    }
}

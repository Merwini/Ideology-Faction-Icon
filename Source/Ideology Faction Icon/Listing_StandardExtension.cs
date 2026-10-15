using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace nuff.Ideology_Faction_Icon
{
    public static class Listing_StandardExtensions
    {

        #region EnumLicense
        /*
		    Code used and modified from Compact Hediffs by PeteTimesSix, under MIT License
		 
		    MIT License

			Copyright (c) 2020 PeteTimesSix

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

        #endregion

        public static float ButtonTextPadding = 5f;
        public static float AfterLabelMinGap = 10f;

        public static readonly Color SelectedButtonColor = new Color(.65f, 1f, .65f);

        public static void EnumSelector<T>(this Listing_Standard listing, ref T value, string label, string valueLabelPrefix, string valueTooltipPostfix = "_tooltip", string tooltip = null) where T : Enum
        {
            string[] names = Enum.GetNames(value.GetType());

            float lineHeight = Text.LineHeight;
            float labelWidth = Text.CalcSize(label).x + AfterLabelMinGap;

            var tempWidth = listing.ColumnWidth;

            float buttonsWidth = 0f;
            foreach (var name in names)
            {
                string text = (valueLabelPrefix + name).Translate();
                float width = Text.CalcSize(text).x + ButtonTextPadding * 2f;
                if (buttonsWidth < width)
                    buttonsWidth = width;
            }

            bool fitsOnLabelRow = (((buttonsWidth * names.Length) + labelWidth) < tempWidth);
            float buttonsRectWidth = fitsOnLabelRow ?
                listing.ColumnWidth - (labelWidth) :
                listing.ColumnWidth;

            int rowNum = 0;
            int columnNum = 0;
            int maxColumnNum = 0;
            foreach (var name in names)
            {
                if ((columnNum + 1) * buttonsWidth > buttonsRectWidth)
                {
                    columnNum = 0;
                    rowNum++;
                }
                float x = (columnNum * buttonsWidth);
                float y = rowNum * lineHeight;
                columnNum++;
                if (rowNum == 0 && maxColumnNum < columnNum)
                    maxColumnNum = columnNum;
            }
            rowNum++; //label row
            if (!fitsOnLabelRow)
                rowNum++;

            Rect wholeRect = listing.GetRect((float)rowNum * lineHeight);

            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(wholeRect))
                {
                    Widgets.DrawHighlight(wholeRect);
                }
                TooltipHandler.TipRegion(wholeRect, tooltip);
            }

            Rect labelRect = wholeRect.TopPartPixels(lineHeight).LeftPartPixels(labelWidth);
            GUI.color = Color.white;
            Widgets.Label(labelRect, label);

            Rect buttonsRect = fitsOnLabelRow ?
                wholeRect.RightPartPixels(buttonsRectWidth) :
                wholeRect.BottomPartPixels(wholeRect.height - lineHeight);

            buttonsWidth = buttonsRectWidth / (float)maxColumnNum;

            rowNum = 0;
            columnNum = 0;
            foreach (var name in names)
            {
                if ((columnNum + 1) * buttonsWidth > buttonsRectWidth)
                {
                    columnNum = 0;
                    rowNum++;
                }
                float x = (columnNum * buttonsWidth);
                float y = rowNum * lineHeight;
                columnNum++;
                string buttonText = (valueLabelPrefix + name).Translate();
                var enumValue = (T)Enum.Parse(value.GetType(), name);
                GUI.color = value.Equals(enumValue) ? SelectedButtonColor : Color.white;
                var buttonRect = new Rect(buttonsRect.x + x, buttonsRect.y + y, buttonsWidth, lineHeight);
                if (valueTooltipPostfix != null)
                    TooltipHandler.TipRegion(buttonRect, (valueLabelPrefix + name + valueTooltipPostfix).Translate());
                bool clicked = Widgets.ButtonText(buttonRect, buttonText);
                if (clicked)
                    value = enumValue;
            }

            listing.Gap(listing.verticalSpacing);
            GUI.color = Color.white;
        }
    }
}
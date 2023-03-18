using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace Ideology_Faction_Icon
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
				//string buttonText = (valueLabelPrefix + name).Translate(); //TODO translations?
				string buttonText = (valueLabelPrefix + name);
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

		#region ListLicense
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
		#endregion
		public static void ListControl(this Listing_Standard listingStandard, Rect inRect, ref List<Faction> leftList, ref List<Faction> rightList, ref string searchTerm, 
										ref Vector2 leftScrollPosition, ref Vector2 rightScrollPosition, ref Faction leftSelectedObject, ref Faction rightSelectedObject)
		{
			string tempString = searchTerm;
			
			

			//Text.Font = GameFont.Medium;
			Rect topRect = inRect.TopPart(pct: 0.05f);
			searchTerm = Widgets.TextField(rect: topRect.RightPart(pct: 0.95f).LeftPart(pct: 0.95f), text: searchTerm);
			Rect labelRect = inRect.TopPart(pct: 0.1f).BottomHalf();
			Rect bottomRect = inRect.BottomPart(pct: 0.9f);

			#region leftSide

			Rect leftRect = bottomRect.LeftHalf().RightPart(pct: 0.9f).LeftPart(pct: 0.9f);
			GUI.BeginGroup(position: leftRect, style: new GUIStyle(other: GUI.skin.box));
			List<Faction> tempList2 = rightList;
			List<Faction> tempList = leftList.Where(predicate: fact => fact.Name.ToLower().Contains(tempString.ToLower())
																		&& !tempList2.Contains(fact)).ToList();
			float num = 3f;
			Widgets.BeginScrollView(outRect: leftRect.AtZero(), scrollPosition: ref leftScrollPosition,
									viewRect: new Rect(x: 0f, y: 0f, width: leftRect.width / 10 * 9, height: tempList.Count * 32f));
			if (!tempList.NullOrEmpty())
			{
				foreach (Faction fact in tempList)
				{
					Rect rowRect = new Rect(x: 5, y: num, width: leftRect.width - 6, height: 30);
					Widgets.DrawHighlightIfMouseover(rect: rowRect);
					if (fact == leftSelectedObject)
						Widgets.DrawHighlightSelected(rect: rowRect);
					Widgets.Label(rect: rowRect, label: fact.Name ?? fact.def.label);
					if (Widgets.ButtonInvisible(butRect: rowRect))
						leftSelectedObject = fact;

					num += 32f;
				}
			}
			Widgets.EndScrollView();
			GUI.EndGroup();

			#endregion


			#region rightSide

			Widgets.Label(rect: labelRect.RightHalf().RightPart(pct: 0.9f), label: "Enable Icon Changing for:");
			Rect rightRect = bottomRect.RightHalf().RightPart(pct: 0.9f).LeftPart(pct: 0.9f);
			GUI.BeginGroup(position: rightRect, style: GUI.skin.box);
			num = 6f;
			Widgets.BeginScrollView(outRect: rightRect.AtZero(), scrollPosition: ref rightScrollPosition,
									viewRect: new Rect(x: 0f, y: 0f, width: rightRect.width / 5 * 4, height: rightList.Count * 32f));
			if (!rightList.NullOrEmpty())
			{
				foreach (Faction fact in rightList.Where(predicate: fact => (fact.Name.Contains(value: tempString))))
				{
					Rect rowRect = new Rect(x: 5, y: num, width: leftRect.width - 6, height: 30);
					Widgets.DrawHighlightIfMouseover(rect: rowRect);
					if (fact == rightSelectedObject)
						Widgets.DrawHighlightSelected(rect: rowRect);
					Widgets.Label(rect: rowRect, label: fact.Name ?? fact.def.label);
					if (Widgets.ButtonInvisible(butRect: rowRect))
						rightSelectedObject = fact;

					num += 32f;
				}
			}
			Widgets.EndScrollView();
			GUI.EndGroup();

			#endregion


			#region buttons

			if (Widgets.ButtonImage(butRect: bottomRect.BottomPart(pct: 0.6f).TopPart(pct: 0.1f).RightPart(pct: 0.525f).LeftPart(pct: 0.1f), tex: TexUI.ArrowTexRight) &&
				leftSelectedObject != null)
			{
				rightList.Add(item: leftSelectedObject);
				rightList = rightList.OrderBy(keySelector: fact => fact.Name ?? fact.def.label).ToList();
				rightSelectedObject = leftSelectedObject;
				leftSelectedObject = null;
			}

			if (Widgets.ButtonImage(butRect: bottomRect.BottomPart(pct: 0.4f).TopPart(pct: 0.15f).RightPart(pct: 0.525f).LeftPart(pct: 0.1f), tex: TexUI.ArrowTexLeft) &&
				rightSelectedObject != null)
			{
				rightList.Remove(item: rightSelectedObject);
				leftSelectedObject = rightSelectedObject;
				rightSelectedObject = null;
			}

			#endregion

		}

	}
}
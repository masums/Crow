﻿//
//  Spliter.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	public class Splitter : GraphicObject
	{
		#region CTOR
		public Splitter (): base(){}
		#endregion

		int thickness;

		[XmlAttributeAttribute][DefaultValue(1)]
		public virtual int Thickness {
			get { return thickness; }
			set {
				if (thickness == value)
					return;
				thickness = value; 
				NotifyValueChanged ("Thickness", thickness);
				RegisterForLayouting (LayoutingType.Sizing);
				RegisterForGraphicUpdate ();
			}
		}

		Unit u1, u2;
		int init1 = -1, init2 = -1, delta = 0, min1, min2, max1 , max2;
		GraphicObject go1 = null, go2 = null;

		void initSplit(Measure m1, int size1, Measure m2, int size2){
			if (m1 != Measure.Stretched) {
				init1 = size1;
				u1 = m1.Units;
			}
			if (m2 != Measure.Stretched) {
				init2 = size2;
				u2 = m2.Units;
			}
		}
		void convertSizeInPix(GraphicObject g1){

		}

		#region GraphicObject override
		public override ILayoutable Parent {
			get { return base.Parent; }
			set {
				if (value != null) {			
					GenericStack gs = value as GenericStack;
					if (gs == null)
						throw new Exception ("Splitter may only be chil of stack");
					
				}
				base.Parent = value;
			}
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if ((Parent as GenericStack).Orientation == Orientation.Horizontal)
				CurrentInterface.MouseCursor = XCursor.H;
			else
				CurrentInterface.MouseCursor = XCursor.V;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			CurrentInterface.MouseCursor = XCursor.Default;
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);
			go1 = go2 = null;
			init1 = init2 = -1;
			delta = 0;

			GenericStack gs = Parent as GenericStack;
			int ptrSplit = gs.Children.IndexOf (this);
			if (ptrSplit == 0 || ptrSplit == gs.Children.Count - 1)
				return;

			go1 = gs.Children [ptrSplit - 1];
			go2 = gs.Children [ptrSplit + 1];

			if (gs.Orientation == Orientation.Horizontal) {
				initSplit (go1.Width, go1.Slot.Width, go2.Width, go2.Slot.Width);
				min1 = go1.MinimumSize.Width;
				min2 = go2.MinimumSize.Width;
				max1 = go1.MaximumSize.Width;
				max2 = go2.MaximumSize.Width;
				if (init1 >= 0)
					go1.Width = init1;
				if (init2 >= 0)
					go2.Width = init2;
			} else {
				initSplit (go1.Height, go1.Slot.Height, go2.Height, go2.Slot.Height);
				min1 = go1.MinimumSize.Height;
				min2 = go2.MinimumSize.Height;
				max1 = go1.MaximumSize.Height;
				max2 = go2.MaximumSize.Height;
				if (init1 >= 0)
					go1.Height = init1;
				if (init2 >= 0)
					go2.Height = init2;
			}
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (!IsActive)
				return;

			GenericStack gs = Parent as GenericStack;
			int newDelta = delta, size1 = init1 , size2 = init2;
			if (gs.Orientation == Orientation.Horizontal) {
				newDelta -= e.XDelta;
				if (size1 < 0)
					size1 = go1.Slot.Width + delta;
				if (size2 < 0)
					size2 = go2.Slot.Width - delta;
			} else {
				newDelta -= e.YDelta;
				if (size1 < 0)
					size1 = go1.Slot.Height + delta;
				if (size2 < 0)
					size2 = go2.Slot.Height - delta;
			}

			if (size1 - newDelta < min1 || (max1 > 0 && size1 - newDelta > max1) ||
				size2 + newDelta < min2 || (max2 > 0 && size2 + newDelta > max2))
				return;

			delta = newDelta;

			if (gs.Orientation == Orientation.Horizontal) {
				if (init1 >= 0)
					go1.Width = init1 - delta;
				if (init2 >= 0)
					go2.Width = init2 + delta;
			} else {
				if (init1 >= 0)
					go1.Height = init1 - delta;
				if (init2 >= 0)
					go2.Height = init2 + delta;
			}
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			GenericStack gs = Parent as GenericStack;

			if (init1 >= 0 && u1 == Unit.Percent) {
				if (gs.Orientation == Orientation.Horizontal)
					go1.Width = new Measure ((int)Math.Ceiling (
						go1.Width.Value * 100.0 / (double)gs.Slot.Width), Unit.Percent);
				else
					go1.Height = new Measure ((int)Math.Ceiling (
						go1.Height.Value * 100.0 / (double)gs.Slot.Height), Unit.Percent);
			}
			if (init2 >= 0 && u2 == Unit.Percent) {
				if (gs.Orientation == Orientation.Horizontal)
					go2.Width = new Measure ((int)Math.Floor (
						go2.Width.Value * 100.0 / (double)gs.Slot.Width), Unit.Percent);
				else
					go2.Height = new Measure ((int)Math.Floor (
						go2.Height.Value * 100.0 / (double)gs.Slot.Height), Unit.Percent);
			}
		}
		public override bool UpdateLayout (LayoutingType layoutType)
		{
			GenericStack gs = Parent as GenericStack;
			if (layoutType == LayoutingType.Width){
				if (gs.Orientation == Orientation.Horizontal)
					Width = thickness;
				else
					Width = Measure.Stretched;
			} else if (layoutType == LayoutingType.Height){
				if (gs.Orientation == Orientation.Vertical)
					Height = thickness;
				else
					Height = Measure.Stretched;
			}
			return base.UpdateLayout (layoutType);
		}
		#endregion
	}
}


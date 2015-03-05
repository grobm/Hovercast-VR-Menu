﻿using System;
using System.Collections.Generic;
using System.Linq;
using Hoverboard.Core.Custom;
using Hoverboard.Core.Input;
using Hoverboard.Core.Navigation;
using UnityEngine;

namespace Hoverboard.Core.State {

	/*================================================================================================*/
	public class ButtonState : IHovercastItemState {

		public NavItem NavItem { get; private set; }

		public bool IsSelectionPrevented { get; private set; }

		private readonly InteractionSettings vSettings;
		private readonly IDictionary<CursorType, float> vHighlightDistanceMap;
		private readonly IDictionary<CursorType, float> vHighlightProgressMap;
		private readonly IDictionary<CursorType, bool> vIsNearestHighlightMap;

		private Func<Vector3, float> vCursorDistanceFunc;
		private DateTime? vSelectionStart;
		private bool vIsAnimating;
		private float vDistanceUponSelection;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public ButtonState(NavItem pNavItem, InteractionSettings pSettings) {
			NavItem = pNavItem;
			vSettings = pSettings;

			vHighlightDistanceMap = new Dictionary<CursorType, float>();
			vHighlightProgressMap = new Dictionary<CursorType, float>();
			vIsNearestHighlightMap = new Dictionary<CursorType, bool>();

			foreach ( CursorType cursorType in Enum.GetValues(typeof(CursorType)) ) {
				vHighlightDistanceMap[cursorType] = float.MaxValue;
				vHighlightProgressMap[cursorType] = 0;
				vIsNearestHighlightMap[cursorType] = false;
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public float GetHighlightDistance(CursorType pCursorType) {
			return vHighlightDistanceMap[pCursorType];
		}

		/*--------------------------------------------------------------------------------------------*/
		public bool GetIsNearestHighlight(CursorType pCursorType) {
			return vIsNearestHighlightMap[pCursorType];
		}

		/*--------------------------------------------------------------------------------------------*/
		public float MinHighlightDistance {
			get {
				return vHighlightDistanceMap.Min(x => x.Value);
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		public float MaxHighlightProgress {
			get {
				return vHighlightProgressMap.Max(x => x.Value);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		public bool IsNearestHighlight {
			get {
				return vIsNearestHighlightMap.Any(x => x.Value);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		public float SelectionProgress {
			get {
				if ( vSelectionStart == null ) {
					if ( !NavItem.IsStickySelected ) {
						return 0;
					}

					return Mathf.InverseLerp(vSettings.StickyReleaseDistance,
						vDistanceUponSelection, MinHighlightDistance);
				}

				float ms = (float)(DateTime.UtcNow-(DateTime)vSelectionStart).TotalMilliseconds;
				return Math.Min(1, ms/vSettings.SelectionMilliseconds);
			}
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public void SetCursorDistanceFunction(Func<Vector3, float> pFunc) {
			vCursorDistanceFunc = pFunc;
		}

		/*--------------------------------------------------------------------------------------------*/
		public void SetIsAnimating(bool pIsAnimating) {
			vIsAnimating = pIsAnimating;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		internal void UpdateWithCursor(CursorType pType, Vector3? pCursorPosition) {
			if ( pCursorPosition == null || vIsAnimating || !NavItem.IsEnabled ) {
				vHighlightDistanceMap[pType] = float.MaxValue;
				vHighlightProgressMap[pType] = 0;
				return;
			}

			if ( vCursorDistanceFunc == null ) {
				throw new Exception("No CursorDistanceFunction has been set.");
			}

			float dist = vCursorDistanceFunc((Vector3)pCursorPosition);
			float prog = Mathf.InverseLerp(vSettings.HighlightDistanceMax,
				vSettings.HighlightDistanceMin, dist);

			vHighlightDistanceMap[pType] = dist;
			vHighlightProgressMap[pType] = prog;
		}

		/*--------------------------------------------------------------------------------------------*/
		internal void SetAsNearestButton(CursorType pCursorType, bool pIsNearest) {
			vIsNearestHighlightMap[pCursorType] = pIsNearest;
		}

		/*--------------------------------------------------------------------------------------------*/
		internal bool UpdateSelectionProcess() {
			bool isNearest = IsNearestHighlight;

			if ( !isNearest || SelectionProgress <= 0 ) {
				NavItem.DeselectStickySelections();
			}

			if ( !isNearest || MaxHighlightProgress < 1 ) {
				vSelectionStart = null;
				IsSelectionPrevented = false;
				return false;
			}

			if ( IsSelectionPrevented || !NavItem.AllowSelection ) {
				vSelectionStart = null;
				return false;
			}

			if ( vSelectionStart == null ) {
				vSelectionStart = DateTime.UtcNow;
				return false;
			}

			if ( SelectionProgress < 1 ) {
				return false;
			}

			vSelectionStart = null;
			IsSelectionPrevented = true;
			vDistanceUponSelection = MinHighlightDistance;
			NavItem.Select();
			return true;
		}

	}

}
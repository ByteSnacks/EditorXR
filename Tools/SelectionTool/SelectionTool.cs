using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	public class SelectionTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap, ISetHighlight, IGameObjectLocking
	{
		static HashSet<GameObject> s_SelectedObjects = new HashSet<GameObject>(); // Selection set is static because multiple selection tools can simulataneously add and remove objects from a shared selection

		GameObject m_HoverGameObject;
		GameObject m_PressedObject;
		DateTime m_LastSelectTime;

		// The prefab (if any) that was double clicked, whose individual pieces can be selected
		static GameObject s_CurrentPrefabOpened;

		public ActionMap actionMap { get { return m_ActionMap; } }
		[SerializeField]
		ActionMap m_ActionMap;

		public ActionMapInput actionMapInput
		{
			get { return m_SelectionInput; }
			set { m_SelectionInput = (SelectionInput)value; }
		}
		SelectionInput m_SelectionInput;

		public Node selfNode { get; set; }

		public Func<Transform, GameObject> getFirstGameObject { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action<GameObject, bool> setHighlight { private get; set; }
		public Action<GameObject, bool> setLocked { get; set; }
		public Func<GameObject, bool> isLocked { get; set; }
		public Action<GameObject, Transform> checkHover { get; set; }

		public event Action<GameObject, Transform> hovered;
		public event Action<Transform> selected;

		public Func<Transform, bool> isRayActive = delegate { return true; };

		void Update()
		{
			if (rayOrigin == null)
				return;

			if (!isRayActive(rayOrigin))
				return;

			var newHoverGameObject = getFirstGameObject(rayOrigin);
			GameObject newPrefabRoot = null;
			if (newHoverGameObject != null)
			{
				// If gameObject is within a prefab and not the current prefab, choose prefab root
				newPrefabRoot = PrefabUtility.FindPrefabRoot(newHoverGameObject);
				if (newPrefabRoot)
				{
					if (newPrefabRoot != s_CurrentPrefabOpened)
						newHoverGameObject = newPrefabRoot;
				}
				if (newHoverGameObject.isStatic)
					return;
			}

			if (hovered != null)
				hovered(newHoverGameObject, rayOrigin);

			if (isLocked(newHoverGameObject))
				return;


			// Handle changing highlight
			if (newHoverGameObject != m_HoverGameObject)
			{
				if (m_HoverGameObject != null)
					setHighlight(m_HoverGameObject, false);

				if (newHoverGameObject != null)
					setHighlight(newHoverGameObject, true);
			}

			m_HoverGameObject = newHoverGameObject;

			if (m_SelectionInput.select.wasJustPressed && m_HoverGameObject)
				m_PressedObject = m_HoverGameObject;

			// Handle select button press
			if (m_SelectionInput.select.wasJustReleased)
			{
				if (m_PressedObject == m_HoverGameObject)
				{
					s_CurrentPrefabOpened = newPrefabRoot;

					// Multi-Select
					if (m_SelectionInput.multiSelect.isHeld)
					{
						if (s_SelectedObjects.Contains(m_HoverGameObject))
						{
							// Already selected, so remove from selection
							s_SelectedObjects.Remove(m_HoverGameObject);
						}
						else
						{
							// Add to selection
							s_SelectedObjects.Add(m_HoverGameObject);
							Selection.activeGameObject = m_HoverGameObject;
						}
					}
					else
					{
						if (s_CurrentPrefabOpened && s_CurrentPrefabOpened != m_HoverGameObject)
							s_SelectedObjects.Remove(s_CurrentPrefabOpened);

						s_SelectedObjects.Clear();
						Selection.activeGameObject = m_HoverGameObject;
						s_SelectedObjects.Add(m_HoverGameObject);
					}

					setHighlight(m_HoverGameObject, false);

					Selection.objects = s_SelectedObjects.ToArray();
					if (selected != null)
						selected(rayOrigin);
				}

				m_PressedObject = null;
			}
		}

		void OnDisable()
		{
			if (m_HoverGameObject != null)
			{
				setHighlight(m_HoverGameObject, false);
				m_HoverGameObject = null;
			}
		}
	}
}

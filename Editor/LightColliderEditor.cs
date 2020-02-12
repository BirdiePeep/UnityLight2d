using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Bird.Light2D
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LightCollider))]
	public class LightColliderEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.DrawDefaultInspector();

			var light = target as LightCollider;
			light.selfShadow = EditorGUILayout.Toggle("Self Shadow", light.selfShadow);
		}
	}
}


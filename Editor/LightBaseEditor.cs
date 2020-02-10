using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Bird.Light2D
{
	[CustomEditor(typeof(LightBase))]
	public class LightBaseEditor : Editor
	{
		private void OnSceneGUI()
		{
			var light = target as LightBase;

			var center = light.transform.position;
			var forward = light.transform.forward;
			var up = light.transform.up;
			var right = light.transform.right;
			var spread = light.spread * 0.5f;

			//Outline
			Handles.color = Color.green;
			if(light.spread >= 360f)
			{
				//Outer
				Handles.DrawWireDisc(center, forward, light.radius);

				//Inner
				Handles.DrawWireDisc(center, forward, light.radius*light.innerRadius);
			}
			else
			{
				//Draw arc
				float radius = light.radius;
				Handles.DrawWireArc(center, forward, right, spread, radius);
				Handles.DrawWireArc(center, forward, right, -spread, radius);
				Handles.DrawLine(center, light.transform.position + Quaternion.AngleAxis(spread, forward) * right * radius);
				Handles.DrawLine(center, light.transform.position + Quaternion.AngleAxis(-spread, forward) * right * radius);

				//Draw inner arc
				radius = light.radius * light.innerRadius;
				Handles.DrawWireArc(center, forward, right, spread, radius);
				Handles.DrawWireArc(center, forward, right, -spread, radius);
				Handles.DrawLine(center, light.transform.position + Quaternion.AngleAxis(spread, forward) * right * radius);
				Handles.DrawLine(center, light.transform.position + Quaternion.AngleAxis(-spread, forward) * right * radius);
			}

			//Inner angle
			Handles.DrawLine(center, light.transform.position + Quaternion.AngleAxis(spread - (spread * light.innerAngle), forward) * right * light.radius);
			Handles.DrawLine(center, light.transform.position + Quaternion.AngleAxis(-spread - (-spread * light.innerAngle), forward) * right * light.radius);

			//Radius
			{
				var pos =  center + right * light.radius;
				var size = HandleUtility.GetHandleSize(pos) * 0.1f;

				EditorGUI.BeginChangeCheck();
				pos = Handles.Slider2D(pos, Vector3.forward, up, right, size, Handles.RectangleHandleCap, Vector3.zero);
				if(EditorGUI.EndChangeCheck())
				{
					light.radius = Vector3.Distance(center, pos);

					var dir = Vector3.Normalize(pos - center);
					float newAngle = AngleBetweenTwoUnitVectors(Vector3.forward, Vector3.right, dir) * Mathf.Rad2Deg;
					light.transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.forward);
				}
			}
		}
		public float AngleBetweenTwoUnitVectors(Vector3 planeNormal, Vector3 vectorA, Vector3 vectorB)
		{
			//Find the cross product
			var cross = Vector3.Cross(vectorA, planeNormal);
			if (Vector3.Dot(cross, vectorB) > 0.0f)
				return (Mathf.PI*2.0f) - Mathf.Acos(Mathf.Clamp(Vector3.Dot(vectorA, vectorB), -1.0f, 1.0f));
			else
				return Mathf.Acos(Mathf.Clamp(Vector3.Dot(vectorA, vectorB), -1.0f, 1.0f));
		}
	}
}


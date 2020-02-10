using UnityEngine;
using UnityEditor;

namespace Bird.Light2D
{
	public static class Helper
	{
		[MenuItem("GameObject/Bird/Point Light", false, 15)]
		static void CreatePointLight()
		{
			var obj = new GameObject();
			obj.AddComponent<LightBase>();
			obj.name = "Point Light";
			obj.transform.parent = Selection.activeTransform;
		}
	}
}


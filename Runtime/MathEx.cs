using UnityEngine;

namespace Bird.Light2D
{
	public static class MathEx
	{
		public const float PI = 3.14159265358979338f;
		public const float TWO_PI = (PI * 2);
		public const float HALF_PI = 1.570796326796f;
		public const float QUARTER_PI = 0.785398f;

		public static float AngleBetweenTwoUnitVectors(Vector3 planeNormal, Vector3 vectorA, Vector3 vectorB)
		{
			//Find the cross product
			var cross = Vector3.Cross(vectorA, planeNormal);
			if (Vector3.Dot(cross, vectorB) > 0.0f)
				return TWO_PI - Mathf.Acos(Mathf.Clamp(Vector3.Dot(vectorA, vectorB), -1.0f, 1.0f));
			else
				return Mathf.Acos(Mathf.Clamp(Vector3.Dot(vectorA, vectorB), -1.0f, 1.0f));
		}
	}
}

using UnityEngine;

using System.Collections.Generic;

public static class Helper
{
	public static readonly float InscribedBox = 1.0f / Mathf.Sqrt(2.0f);

	public static bool Enabled(Behaviour b)
	{
		return b != null && b.enabled == true && b.gameObject.activeInHierarchy == true;
	}

	public static T Destroy<T>(T o)
		where T : Object
	{
		if (o != null)
		{
			Object.DestroyImmediate(o);
		}

		return null;
	}

	public static bool Zero(float v)
	{
		return Mathf.Approximately(v, 0.0f);
	}

	public static float Reciprocal(float v)
	{
		return Zero(v) == false ? 1.0f / v : 0.0f;
	}

	public static float Acos(float v)
	{
		if (v >= -1.0f && v <= 1.0f)
		{
			return Mathf.Acos(v);
		}

		return 0.0f;
	}

	public static Vector3 Reciprocal3(Vector3 xyz)
	{
		xyz.x = Reciprocal(xyz.x);
		xyz.y = Reciprocal(xyz.y);
		xyz.z = Reciprocal(xyz.z);
		return xyz;
	}

	public static float Divide(float a, float b)
	{
		return b != 0.0f ? a / b : 0.0f;
	}

	public static float DampenFactor(float dampening, float elapsed)
	{
		return 1.0f - Mathf.Pow((float)System.Math.E, -dampening * elapsed);
	}

	public static float Dampen(float current, float target, float dampening, float elapsed, float minStep = 0.0f)
	{
		var factor = DampenFactor(dampening, elapsed);
		var maxDelta = Mathf.Abs(target - current) * factor + minStep * elapsed;

		return MoveTowards(current, target, maxDelta);
	}

	public static Quaternion Dampen(Quaternion current, Quaternion target, float dampening, float elapsed, float minStep = 0.0f)
	{
		var factor = DampenFactor(dampening, elapsed);
		var maxDelta = Quaternion.Angle(current, target) * factor + minStep * elapsed;

		return MoveTowards(current, target, maxDelta);
	}

	public static Vector3 Dampen3(Vector3 current, Vector3 target, float dampening, float elapsed, float minStep = 0.0f)
	{
		var factor = DampenFactor(dampening, elapsed);
		var maxDelta = Mathf.Abs((target - current).magnitude) * factor + minStep * elapsed;

		return Vector3.MoveTowards(current, target, maxDelta);
	}

	public static Quaternion MoveTowards(Quaternion current, Quaternion target, float maxDelta)
	{
		var delta = Quaternion.Angle(current, target);

		return Quaternion.Slerp(current, target, Divide(maxDelta, delta));
	}

	public static float MoveTowards(float current, float target, float maxDelta)
	{
		if (target > current)
		{
			current = System.Math.Min(target, current + maxDelta);
		}
		else
		{
			current = System.Math.Max(target, current - maxDelta);
		}

		return current;
	}

	public static void SetLocalRotation(Transform t, Quaternion q)
	{
		if (t != null)
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false && t.localRotation == q) return;
#endif
			t.localRotation = q;
		}
	}

	public static Bounds NewBoundsFromMinMax(Vector3 min, Vector3 max)
	{
		var bounds = default(Bounds);

		bounds.SetMinMax(min, max);

		return bounds;
	}

	public static Bounds NewBoundsCenter(Bounds b, Vector3 c)
	{
		var x = Mathf.Max(Mathf.Abs(c.x - b.min.x), Mathf.Abs(c.x - b.max.x));
		var y = Mathf.Max(Mathf.Abs(c.y - b.min.z), Mathf.Abs(c.y - b.max.y));
		var z = Mathf.Max(Mathf.Abs(c.z - b.min.z), Mathf.Abs(c.z - b.max.z));

		return new Bounds(c, new Vector3(x, y, z) * 2.0f);
	}

	public static void ResizeArrayTo<T>(ref List<T> array, int size, System.Func<int, T> newT, System.Action<T> removeT)
	{
		if (array != null)
		{
			while (array.Count < size)
			{
				array.Add(newT != null ? newT(array.Count) : default(T));
			}

			while (array.Count > size)
			{
				if (removeT != null)
				{
					removeT(array[array.Count - 1]);
				}

				array.RemoveAt(array.Count - 1);
			}
		}
	}

	public static Material CreateTempMaterial(string shaderName)
	{
		var shader = Shader.Find(shaderName);

		if (shader == null)
		{
			Debug.LogError("Failed to find shader: " + shaderName); return null;
		}

		var material = new Material(shader);

		material.hideFlags = HideFlags.DontSave | HideFlags.HideInInspector;

		return material;
	}

	public static GameObject CloneGameObject(GameObject source, Transform parent, bool keepName = false)
	{
		return CloneGameObject(source, parent, source.transform.localPosition, source.transform.localRotation, keepName);
	}

	public static GameObject CloneGameObject(GameObject source, Transform parent, Vector3 localPosition, Quaternion localRotation, bool keepName = false)
	{
		if (source != null)
		{
			var clone = default(GameObject);

			if (parent != null)
			{
				clone = (GameObject)GameObject.Instantiate(source);

				clone.transform.parent = parent;
				clone.transform.localPosition = localPosition;
				clone.transform.localRotation = localRotation;
				clone.transform.localScale = source.transform.localScale;
			}
			else
			{
				clone = (GameObject)GameObject.Instantiate(source, localPosition, localRotation);
			}

			if (keepName == true) clone.name = source.name;

			return clone;
		}

		return source;
	}

	public static GameObject CreateGameObject(string name = "", Transform parent = null)
	{
		return CreateGameObject(name, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static GameObject CreateGameObject(string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = new GameObject(name);

		gameObject.transform.parent = parent;
		gameObject.transform.localPosition = localPosition;
		gameObject.transform.localRotation = localRotation;
		gameObject.transform.localScale = localScale;

		return gameObject;
	}

	public static float DistanceToHorizon(float radius, float distanceToCenter)
	{
		if (distanceToCenter > radius)
		{
			return Mathf.Sqrt(distanceToCenter * distanceToCenter - radius * radius);
		}

		return 0.0f;
	}

	// return.x = -PI   .. +PI
	// return.y = -PI/2 .. +PI/2
	public static Vector2 CartesianToPolar(Vector3 xyz)
	{
		var longitude = Mathf.Atan2(xyz.x, xyz.z);
		var latitude = Mathf.Asin(xyz.y / xyz.magnitude);

		return new Vector2(longitude, latitude);
	}

	// return.x = 0 .. 1
	// return.y = 0 .. 1
	public static Vector2 CartesianToPolarUV(Vector3 xyz)
	{
		var uv = CartesianToPolar(xyz);

		uv.x = Mathf.Repeat(0.5f - uv.x / (Mathf.PI * 2.0f), 1.0f);
		uv.y = 0.5f + uv.y / Mathf.PI;

		return uv;
	}

	public static Vector4 CalculateSpriteUV(Sprite s)
	{
		var uv = default(Vector4);

		if (s != null)
		{
			var r = s.textureRect;
			var t = s.texture;

			uv.x = Divide(r.xMin, t.width);
			uv.y = Divide(r.yMin, t.height);
			uv.z = Divide(r.xMax, t.width);
			uv.w = Divide(r.yMax, t.height);
		}

		return uv;
	}

	public static void SetKeywords(Material m, List<string> keywords)
	{
		if (m != null && ArraysEqual(m.shaderKeywords, keywords) == false)
		{
			m.shaderKeywords = keywords.ToArray();
		}
	}

	public static void SetKeywords(Material m, List<string> keywords, bool checkShaderKeywords)
	{
		if (checkShaderKeywords)
		{
			if (m != null && ArraysEqual(m.shaderKeywords, keywords) == false)
			{
				m.shaderKeywords = keywords.ToArray();
			}
		}
		else
		{
			m.shaderKeywords = keywords.ToArray();
		}
	}

	public static bool ArraysEqual<T>(T[] a, List<T> b)
	{
		if (a == null || b == null) return false;

		if (a.Length != b.Count) return false;

		var comparer = EqualityComparer<T>.Default;

		for (var i = 0; i < a.Length; i++)
		{
			if (comparer.Equals(a[i], b[i]) == false)
			{
				return false;
			}
		}

		return true;
	}

	public static Color Brighten(Color color, float brightness)
	{
		color.r *= brightness;
		color.g *= brightness;
		color.b *= brightness;

		return color;
	}

	public static Color Premultiply(Color color)
	{
		color.r *= color.a;
		color.g *= color.a;
		color.b *= color.a;

		return color;
	}

	public static void CalculateLight(Light light, Vector3 center, Transform directionTransform, Transform positionTransform, ref Vector3 position, ref Vector3 direction, ref Color color)
	{
		if (light != null)
		{
			direction = Vector3.Normalize(position - center);
			position = light.transform.position;
			color = Brighten(light.color, light.intensity * 2.0f);

			switch (light.type)
			{
				case LightType.Point: direction = Vector3.Normalize(position - center); break;
				//distances fix.
				case LightType.Directional: position = center + direction * (position - center).magnitude; break;
			}

			// Transform into local space?
			if (directionTransform != null)
			{
				direction = directionTransform.InverseTransformDirection(direction);
			}

			if (positionTransform != null)
			{
				position = positionTransform.InverseTransformPoint(position);
			}
		}
	}

	public static int WriteLights(List<Light> lights, int maxLights, Vector3 center, Transform directionTransform, Transform positionTransform, params Material[] materials)
	{
		var lightCount = 0;

		if (lights != null)
		{
			for (var i = 1; i <= lights.Count; i++)
			{
				var index = i - 1;
				var light = lights[index];

				if (Enabled(light) == true && light.intensity > 0.0f && lightCount < maxLights)
				{
					var prefix = "_Light" + (++lightCount);
					var direction = default(Vector3);
					var position = default(Vector3);
					var color = default(Color);

					CalculateLight(light, center, directionTransform, positionTransform, ref position, ref direction, ref color);

					for (var j = materials.Length - 1; j >= 0; j--)
					{
						var material = materials[j];

						if (material != null)
						{
							material.SetVector(prefix + "Direction", direction);
							material.SetVector(prefix + "Position", VectorHelper.MakeFrom(position, 1.0f));
							material.SetColor(prefix + "Color", color);

							//Debug.Log(string.Format("{0} | {1} | {2}", prefix + "Direction", prefix + "Position", prefix + "Color"));
						}
					}
				}
			}
		}

		return lightCount;
	}

	public static int WriteShadows(List<Shadow> shadows, int maxShadows, params Material[] materials)
	{
		var shadowCount = 0;

		if (shadows != null)
		{
			for (var i = 1; i <= shadows.Count; i++)
			{
				var index = i - 1;
				var shadow = shadows[index];

				if (Enabled(shadow) == true && shadow.CalculateShadow() == true && shadowCount < maxShadows)
				{
					var prefix = "_Shadow" + (++shadowCount);

					for (var j = materials.Length - 1; j >= 0; j--)
					{
						var material = materials[j];

						if (material != null)
						{
							material.SetTexture(prefix + "Texture", shadow.GetTexture());
							material.SetMatrix(prefix + "Matrix", shadow.Matrix);
							material.SetFloat(prefix + "Ratio", shadow.Ratio);

							//Debug.Log(string.Format("{0} | {1} | {2}", prefix + "Texture", prefix + "Matrix", prefix + "Ratio"));
						}
					}
				}
			}
		}

		return shadowCount;
	}

	public static Matrix4x4 Scaling(Vector3 xyz)
	{
		var matrix = Matrix4x4.identity;

		matrix.m00 = xyz.x;
		matrix.m11 = xyz.y;
		matrix.m22 = xyz.z;

		return matrix;
	}

	public static Matrix4x4 Rotation(Quaternion q)
	{
		var matrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

		return matrix;
	}

	public static Matrix4x4 Translation(Vector3 xyz)
	{
		var matrix = Matrix4x4.identity;

		matrix.m03 = xyz.x;
		matrix.m13 = xyz.y;
		matrix.m23 = xyz.z;

		return matrix;
	}


	public static void WriteLightKeywords(bool lit, int lightCount, params List<string>[] keywordLists)
	{
		if (lit == true)
		{
			var keyword = "LIGHT_" + lightCount;

			for (var i = keywordLists.Length - 1; i >= 0; i--)
			{
				var keywordList = keywordLists[i];

				if (keywordList != null)
				{
					keywordList.Add(keyword);
				}
			}
		}
	}

	public static void WriteShadowKeywords(int shadowCount, params List<string>[] keywordLists)
	{
		if (shadowCount > 0)
		{
			var keyword = "SHADOW_" + shadowCount;

			for (var i = keywordLists.Length - 1; i >= 0; i--)
			{
				var keywordList = keywordLists[i];

				if (keywordList != null)
				{
					keywordList.Add(keyword);
				}
			}
		}
	}

	public static void WriteMie(float sharpness, float strength, params Material[] materials)
	{
		sharpness = Mathf.Pow(10.0f, sharpness);
		strength *= (Mathf.Log10(sharpness) + 1) * 0.75f;

		//var mie  = -(1.0f - 1.0f / Mathf.Pow(10.0f, sharpness));
		//var mie4 = new Vector4(mie * 2.0f, 1.0f - mie * mie, 1.0f + mie * mie, mie / strength);

		var mie = -(1.0f - 1.0f / sharpness);
		var mie4 = new Vector4(mie * 2.0f, 1.0f - mie * mie, 1.0f + mie * mie, strength);

		for (var j = materials.Length - 1; j >= 0; j--)
		{
			var material = materials[j];

			if (material != null)
			{
				material.SetVector("_Mie", mie4);
			}
		}
	}

	public static void WriteRayleigh(float strength, params Material[] materials)
	{
		for (var j = materials.Length - 1; j >= 0; j--)
		{
			var material = materials[j];

			if (material != null)
			{
				material.SetFloat("_Rayleigh", strength);
			}
		}
	}

	public static Texture2D CreateTempTeture2D(int width, int height, TextureFormat format = TextureFormat.ARGB32, bool mips = false, bool linear = false, bool recordUndo = true)
	{
		var texture2D = new Texture2D(width, height, format, mips, linear);

		texture2D.hideFlags = HideFlags.DontSave;

		return texture2D;
	}

#if UNITY_EDITOR
	public static void DrawSphere(Vector3 center, Vector3 right, Vector3 up, Vector3 forward, int resolution = 32)
	{
		DrawCircle(center, right, up, resolution);
		DrawCircle(center, right, forward, resolution);
		DrawCircle(center, forward, up, resolution);
	}

	public static void DrawCircle(Vector3 center, Vector3 right, Vector3 up, int resolution = 32)
	{
		var step = Reciprocal(resolution);

		for (var i = 0; i < resolution; i++)
		{
			var a = i * step;
			var b = a + step;

			a = a * Mathf.PI * 2.0f;
			b = b * Mathf.PI * 2.0f;

			Gizmos.DrawLine(center + right * Mathf.Sin(a) + up * Mathf.Cos(a), center + right * Mathf.Sin(b) + up * Mathf.Cos(b));
		}
	}

	public static void DrawCircle(Vector3 center, Vector3 axis, float radius, int resolution = 32)
	{
		var rotation = Quaternion.FromToRotation(Vector3.up, axis);
		var right = rotation * Vector3.right * radius;
		var forward = rotation * Vector3.forward * radius;

		DrawCircle(center, right, forward, resolution);
	}
#endif
}
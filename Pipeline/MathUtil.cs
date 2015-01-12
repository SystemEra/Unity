using UnityEngine;
using System.Collections;

public static class MathUtil
{
	public static Vector2 Multiply(Vector2 a, Vector2 b)
	{
		return new Vector2(a.x * b.x, a.y * b.y);
	}

	public static Vector3 Multiply(Vector3 a, Vector3 b)
	{
		return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
	}

	public static Vector2 Divide(Vector2 a, Vector2 b)
	{
		return new Vector2(a.x / b.x, a.y / b.y);
	}

	public static Vector3 Divide(Vector3 a, Vector3 b)
	{
		return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
	}

	public static float MaxComponent(Vector3 v)
	{
		return Mathf.Max(Mathf.Max(v.x, v.y), v.z);
	}

	public static float MinComponent(Vector3 v)
	{
		return Mathf.Min(Mathf.Min(v.x, v.y), v.z);
	}

	public static float LineSegmentPointDistance2(Vector2 v, Vector2 w, Vector2 p)
	{
		Vector2 projection;
		return LineSegmentPointDistance2(v, w, p, out projection);
	}
	public static float LineSegmentPointDistance2(Vector2 v, Vector2 w, Vector2 p, out Vector2 projection ) 
	{
		// Return minimum distance between line segment vw and point p
		float l2 = (v - w).sqrMagnitude;  // i.e. |w-v|^2 -  avoid a sqrt
		if (l2 == 0.0)
		{
			projection = v;
			return Vector2.Distance(p, v);   // v == w case
		}

		// Consider the line extending the segment, parameterized as v + t (w - v).
		// We find projection of point p onto the line. 
		// It falls where t = [(p-v) . (w-v)] / |w-v|^2
		float t = Vector2.Dot(p - v, w - v) / l2;
		if (t < 0.0) { projection = v; return Vector2.Distance(p, v); }       // Beyond the 'v' end of the segment
		else if (t > 1.0) { projection = w; return Vector2.Distance(p, w); }  // Beyond the 'w' end of the segment
		projection = v + t * (w - v);  // Projection falls on the segment
		return Vector2.Distance(p, projection);
	}

	public static float LineSegmentPointDistance3(Vector2 v, Vector2 w, Vector2 p)
	{
		Vector3 projection;
		return LineSegmentPointDistance3(v, w, p, out projection);
	}
	public static float LineSegmentPointDistance3(Vector3 v, Vector3 w, Vector3 p, out Vector3 projection)
	{
		// Return minimum distance between line segment vw and point p
		float l2 = (v - w).sqrMagnitude;  // i.e. |w-v|^2 -  avoid a sqrt
		if (l2 == 0.0)
		{
			projection = v;
			return Vector3.Distance(p, v);   // v == w case
		}

		// Consider the line extending the segment, parameterized as v + t (w - v).
		// We find projection of point p onto the line. 
		// It falls where t = [(p-v) . (w-v)] / |w-v|^2
		float t = Vector3.Dot(p - v, w - v) / l2;
		if (t < 0.0) { projection = v; return Vector3.Distance(p, v); }       // Beyond the 'v' end of the segment
		else if (t > 1.0) { projection = w; return Vector3.Distance(p, w); }  // Beyond the 'w' end of the segment
		projection = v + t * (w - v);  // Projection falls on the segment
		
		return Vector3.Distance(p, projection);
	}

	public static Vector3 Abs(Vector3 v)
	{
		return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
	}

	public static Vector3 ClosestPointBounds(Bounds bounds, Vector3 point)
	{
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		
		Vector3 q = Vector3.zero;
		float v = 0;
		
		v = point.x;
		v = Mathf.Max(v, min.x);
		v = Mathf.Min(v, max.x);
		q.x = v;
		
		v = point.y;
		v = Mathf.Max(v, min.y);
		v = Mathf.Min(v, max.y);
		q.y = v;
		
		v = point.z;
		v = Mathf.Max(v, min.z);
		v = Mathf.Min(v, max.z);
		q.z = v;
		
		return q;
	}

	public static Vector3 FarthestPointBounds(Bounds bounds, Vector3 point)
	{
		point = new Vector3(point.x < bounds.center.x ? float.PositiveInfinity : float.NegativeInfinity,
		                    point.y < bounds.center.y ? float.PositiveInfinity : float.NegativeInfinity,
		                    point.z < bounds.center.z ? float.PositiveInfinity : float.NegativeInfinity);
		
		return ClosestPointBounds(bounds, point);
	}
	
	public static bool BoundsIntersectSphere(Bounds bounds, Vector3 center, float radius)
	{
		// Find point on bounds closest to sphere center
		Vector3 p = ClosestPointBounds(bounds, center);
		
		// Sphere and AABB intersect if the (squared) distance from sphere center to point (p)
		// is less than the (squared) sphere radius
		Vector3 v = p - center;
		
		return Vector3.Dot(v, v) <= radius * radius;
	}

	public static bool BoundsInsideSphere(Bounds bounds, Vector3 center, float radius)
	{
		// Find point on bounds closest to sphere center
		Vector3 p = FarthestPointBounds(bounds, center);
		
		// AABB inside sphere if the (squared) distance from sphere center to point (p)
		// is less than the (squared) sphere radius
		Vector3 v = p - center;
		
		return Vector3.Dot(v, v) <= radius * radius;
	}

	public static T Clamp<T>(T val, T min, T max) where T : System.IComparable<T>
	{
		if (val.CompareTo(min) < 0) return min;
		else if(val.CompareTo(max) > 0) return max;
		else return val;
	}

	public static bool Intersect(this Rect a, Rect b ) {
		
		FlipNegative( ref a );
		FlipNegative( ref b );
		bool c1 = a.xMin < b.xMax;
		bool c2 = a.xMax > b.xMin;
		bool c3 = a.yMin < b.yMax;
		bool c4 = a.yMax > b.yMin;
		return c1 && c2 && c3 && c4;
		
	}
	
	private static void FlipNegative(ref Rect r) 
	{
		if( r.width < 0 ) 
			r.x -= ( r.width *= -1 );
		if( r.height < 0 )
			r.y -= ( r.height *= -1 );
	}

	public static readonly UnityEngine.Random Random = new UnityEngine.Random();
	public static Vector3 RandomUniformCube(float halfEdgeLength)
	{
		return new Vector3(Random.Range(-halfEdgeLength, halfEdgeLength), Random.Range(-halfEdgeLength, halfEdgeLength), Random.Range(-halfEdgeLength, halfEdgeLength));
	}

	public static Vector3 RandomUniformSphere()
	{
		float u = UnityEngine.Random.Range(0.0f, 1.0f);
		float v = UnityEngine.Random.Range(0.0f, 1.0f);
		
		float theta = 2.0f * Mathf.PI * u;
		float phi = Mathf.Acos(2.0f * v - 1.0f);

		return new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(phi));
	}

	public static int Mod(int a, int b)
	{
		int m = a % b;
		if (m < 0)
			return m + b;
		else
			return m;
	}

	public static float Mod(float a, float b)
	{
		float m = a % b;
		if (m < 0.0f)
			return m + b;
		else
			return m;
	}
}

using System;
using System.Linq;
using UnityEngine;

public static class Stat
{
	/// @return the average value of array a
	public static float Avg(float[] a)
	{
		float sum = 0;
		foreach (float n in a)
			sum += n;
		return sum / a.Length;
	}

	/// @param avg output the average value of array a
	/// @param std output the standard deviation value of array a
	public static void AvgStd(float[] a, out float avg, out float std)
	{
		avg = Stat.Avg(a);
		std = 0;

		foreach (float n in a)
			std += (n - avg) * (n - avg);
		std = Mathf.Sqrt(std / (a.Length - 1));
	}

	/// similar to AvgStd, except for that this function try to remove some outliers in a.
	/// see http://www.netmba.com/statistics/plot/box/
	public static void AvgStdRobust(float[] a, out float avg, out float std)
	{
		Array.Sort(a);
		int len = a.Length;
		float Q1 = a[len / 4], Q3 = a[len * 3 / 4];
		float minLimit = Q1 - (Q3 - Q1) * 1.5f, maxLimit = Q3 + (Q3 - Q1) * 1.5f;
		float[] b = (from a1 in a
				where a1 >= minLimit && a1 <= maxLimit
				select a1).ToArray();

		Stat.AvgStd(b, out avg, out std);
	}

	/// @return the average value of each component of array a
	public static Vector3 Avg(Vector3[] a)
	{
		Vector3 avg = Vector3.zero;
		foreach (Vector3 n in a) // sum of vectors
			avg += n;
		avg /= a.Length;
		return avg;
	}

	/// @param avg output the average value of each component of array a
	/// @param std output the standard deviation value of the angle of each Vector3 in array a and avg
	public static void AvgStdAngle(Vector3[] a, out Vector3 avg, out float std)
	{
		avg = Avg(a);
		std = 0;
		foreach (Vector3 n in a)
			std += Mathf.Pow(Vector3.Angle(n, avg), 2);
		std = Mathf.Sqrt(std / (a.Length - 1));
	}

	/// similar to AvgStdAngle, but the std are angles in three axes
	public static void AvgStd3Axis(Vector3[] a, out Vector3 avg, out Vector3 std)
	{
		avg = Avg(a);
		std = Vector3.zero;

		foreach (Vector3 n in a)
		{
			Quaternion q = Quaternion.FromToRotation(n, avg);
			std += q.eulerAngles.MinAngle().Square(); // eulerAngles transfer an angle into three angles around 3 axes
		}
		std = (std / (a.Length - 1)).Sqrt();
	}

	/// similar to AvgStdAngle, but the std are computed in each component of a Vector3
	public static void AvgStd3Coord(Vector3[] a, out Vector3 avg, out Vector3 std)
	{
		avg = Avg(a);
		std = Vector3.zero;

		foreach (Vector3 n in a)
		{
			std += (n - avg).Square();
		}
		std = (std / (a.Length - 1)).Sqrt();
	}

}

/// some extension methods
public static class V3Extension
{
	// UnityEngine.Vector3 is a struct only containing value type members, thus v2=v1 is equivalent to v2=v1.Clone();
	//public static Vector3 Clone(this Vector3 v)
	//{
	//	return new Vector3(v.x, v.y, v.z);
	//}

	public static float Min(this Vector3 v)
	{
		return Mathf.Min(v.x, v.y, v.z);
	}

	public static float Max(this Vector3 v)
	{
		return Mathf.Max(v.x, v.y, v.z);
	}

	public static float Sum(this Vector3 v)
	{
		return v.x + v.y + v.z;
	}

	public static Vector3 Abs(this Vector3 v)
	{
		return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
	}

	public static Vector3 DotMul(Vector3 v1, Vector3 v2)
	{
		return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
	}

	public static Vector3 DotDiv(Vector3 v1, Vector3 v2)
	{
		return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
	}
	
	public static float L1Norm(this Vector3 v)
	{
		return v.Abs().Sum()/3;
	}
	
	public static Vector3 Square(this Vector3 v)
	{
		return V3Extension.DotMul(v, v);
	}

	public static Vector3 Sqrt(this Vector3 v)
	{
		return new Vector3(Mathf.Sqrt(v.x), Mathf.Sqrt(v.y), Mathf.Sqrt(v.z));
	}

	public static bool NotLargerThan(this Vector3 v, Vector3 v1)
	{
		return (v.x <= v1.x && v.y <= v1.y && v.z <= v1.z);
	}

	public static bool AbsNotLargerThan(this Vector3 v, Vector3 v1)
	{
		return v.Abs().NotLargerThan(v1.Abs());
	}

	/// if any v[i] is 180~360, change it to 360-v[i]. else, keep it unchanged.
	/// each component of v should be -180~360
	public static Vector3 MinAngle(this Vector3 v)
	{
		for (int i = 0; i < 3; i++)
		{
			if (v[i] < -180 || v[i] > 360)
				Debug.LogException(new Exception("not in -180~360"));
			v[i] = Mathf.Min(v[i], 360 - v[i]);
		}
		return v;
	}
}

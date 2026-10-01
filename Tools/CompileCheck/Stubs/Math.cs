using System;
namespace UnityEngine
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float this[int i] { get => i == 0 ? x : y; set { if (i == 0) x = value; else y = value; } }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 up => new Vector2(0, 1);
        public static Vector2 down => new Vector2(0, -1);
        public static Vector2 left => new Vector2(-1, 0);
        public static Vector2 right => new Vector2(1, 0);
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public Vector2 normalized { get { var m = magnitude; return m > 1e-5f ? new Vector2(x / m, y / m) : zero; } }
        public void Normalize() { this = normalized; }
        public void Set(float nx, float ny) { x = nx; y = ny; }
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
        public static Vector2 MoveTowards(Vector2 c, Vector2 t, float d) { var v = t - c; var m = v.magnitude; return m <= d || m == 0 ? t : c + v / m * d; }
        public static Vector2 ClampMagnitude(Vector2 v, float max) => v.sqrMagnitude > max * max ? v.normalized * max : v;
        public static float Angle(Vector2 a, Vector2 b) { var d = (float)Math.Sqrt(a.sqrMagnitude * b.sqrMagnitude); if (d < 1e-15f) return 0; return (float)Math.Acos(Mathf.Clamp(Dot(a, b) / d, -1, 1)) * Mathf.Rad2Deg; }
        public static float SignedAngle(Vector2 a, Vector2 b) => Angle(a, b) * Mathf.Sign(a.x * b.y - a.y * b.x);
        public static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x);
        public static Vector2 Reflect(Vector2 d, Vector2 n) => -2f * Dot(n, d) * n + d;
        public static Vector2 Scale(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);
        public static Vector2 Min(Vector2 a, Vector2 b) => new Vector2(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
        public static Vector2 Max(Vector2 a, Vector2 b) => new Vector2(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        public static Vector2 SmoothDamp(Vector2 c, Vector2 t, ref Vector2 v, float s, float max, float dt) => t;
        public static Vector2 SmoothDamp(Vector2 c, Vector2 t, ref Vector2 v, float s) => t;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public static Vector2 operator /(Vector2 a, Vector2 b) => new Vector2(a.x / b.x, a.y / b.y);
        public static bool operator ==(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public bool Equals(Vector2 o) => x == o.x && y == o.y;
        public override bool Equals(object o) => o is Vector2 v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);
        public override string ToString() => $"({x:F2}, {y:F2})";
        public string ToString(string f) => $"({x.ToString(f)}, {y.ToString(f)})";
    }

    public struct Vector3 : IEquatable<Vector3>
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public float this[int i] { get => i == 0 ? x : i == 1 ? y : z; set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; } }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized { get { var m = magnitude; return m > 1e-5f ? this / m : zero; } }
        public void Normalize() { this = normalized; }
        public void Set(float a, float b, float c) { x = a; y = b; z = c; }
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
        public static Vector3 MoveTowards(Vector3 c, Vector3 t, float d) { var v = t - c; var m = v.magnitude; return m <= d || m == 0 ? t : c + v / m * d; }
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 Min(Vector3 a, Vector3 b) => new Vector3(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));
        public static Vector3 Max(Vector3 a, Vector3 b) => new Vector3(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) => v - n * Dot(v, n) / Math.Max(1e-6f, n.sqrMagnitude);
        public static Vector3 Project(Vector3 v, Vector3 n) => n * Dot(v, n) / Math.Max(1e-6f, n.sqrMagnitude);
        public static float Angle(Vector3 a, Vector3 b) { var d = (float)Math.Sqrt(a.sqrMagnitude * b.sqrMagnitude); if (d < 1e-15f) return 0; return (float)Math.Acos(Mathf.Clamp(Dot(a, b) / d, -1, 1)) * Mathf.Rad2Deg; }
        public static float SignedAngle(Vector3 a, Vector3 b, Vector3 axis) => Angle(a, b) * Math.Sign(Dot(axis, Cross(a, b)));
        public static Vector3 ClampMagnitude(Vector3 v, float max) => v.sqrMagnitude > max * max ? v.normalized * max : v;
        public static Vector3 SmoothDamp(Vector3 c, Vector3 t, ref Vector3 v, float s, float max, float dt) => t;
        public static Vector3 SmoothDamp(Vector3 c, Vector3 t, ref Vector3 v, float s) => t;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => a * d;
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public bool Equals(Vector3 o) => x == o.x && y == o.y && z == o.z;
        public override bool Equals(object o) => o is Vector3 v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
        public string ToString(string f) => $"({x.ToString(f)}, {y.ToString(f)}, {z.ToString(f)})";
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Vector4 zero => new Vector4(0, 0, 0, 0);
        public static Vector4 one => new Vector4(1, 1, 1, 1);
        public static implicit operator Vector4(Vector3 v) => new Vector4(v.x, v.y, v.z, 0);
        public static implicit operator Vector4(Vector2 v) => new Vector4(v.x, v.y, 0, 0);
        public static implicit operator Vector4(Color c) => new Vector4(c.r, c.g, c.b, c.a);
    }

    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public int this[int i] { get => i == 0 ? x : y; set { if (i == 0) x = value; else y = value; } }
        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int one => new Vector2Int(1, 1);
        public static Vector2Int up => new Vector2Int(0, 1);
        public static Vector2Int down => new Vector2Int(0, -1);
        public static Vector2Int left => new Vector2Int(-1, 0);
        public static Vector2Int right => new Vector2Int(1, 0);
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public int sqrMagnitude => x * x + y * y;
        public void Set(int a, int b) { x = a; y = b; }
        public static float Distance(Vector2Int a, Vector2Int b) => (a - b).magnitude;
        public static Vector2Int Min(Vector2Int a, Vector2Int b) => new Vector2Int(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
        public static Vector2Int Max(Vector2Int a, Vector2Int b) => new Vector2Int(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        public static Vector2Int RoundToInt(Vector2 v) => new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        public static Vector2Int FloorToInt(Vector2 v) => new Vector2Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        public static Vector2Int CeilToInt(Vector2 v) => new Vector2Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);
        public static Vector2Int operator -(Vector2Int a) => new Vector2Int(-a.x, -a.y);
        public static Vector2Int operator *(Vector2Int a, int d) => new Vector2Int(a.x * d, a.y * d);
        public static Vector2Int operator *(int d, Vector2Int a) => new Vector2Int(a.x * d, a.y * d);
        public static Vector2Int operator *(Vector2Int a, Vector2Int b) => new Vector2Int(a.x * b.x, a.y * b.y);
        public static Vector2Int operator /(Vector2Int a, int d) => new Vector2Int(a.x / d, a.y / d);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public static implicit operator Vector2(Vector2Int v) => new Vector2(v.x, v.y);
        public static explicit operator Vector3Int(Vector2Int v) => new Vector3Int(v.x, v.y, 0);
        public bool Equals(Vector2Int o) => x == o.x && y == o.y;
        public override bool Equals(object o) => o is Vector2Int v && Equals(v);
        public override int GetHashCode() => x * 73856093 ^ y * 19349663;
        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public Vector3Int(int x, int y) { this.x = x; this.y = y; z = 0; }
        public static Vector3Int zero => new Vector3Int(0, 0, 0);
        public static Vector3Int one => new Vector3Int(1, 1, 1);
        public static Vector3Int up => new Vector3Int(0, 1, 0);
        public static Vector3Int right => new Vector3Int(1, 0, 0);
        public static Vector3Int operator +(Vector3Int a, Vector3Int b) => new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) => new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
        public static bool operator ==(Vector3Int a, Vector3Int b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3Int a, Vector3Int b) => !(a == b);
        public static implicit operator Vector3(Vector3Int v) => new Vector3(v.x, v.y, v.z);
        public static explicit operator Vector2Int(Vector3Int v) => new Vector2Int(v.x, v.y);
        public bool Equals(Vector3Int o) => this == o;
        public override bool Equals(object o) => o is Vector3Int v && Equals(v);
        public override int GetHashCode() => x * 73856093 ^ y * 19349663 ^ z * 83492791;
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        public Vector3 eulerAngles { get => Vector3.zero; set { } }
        // Unity's order: z, then x, then y.
        public static Quaternion Euler(float x, float y, float z) => AngleAxis(y, Vector3.up) * AngleAxis(x, Vector3.right) * AngleAxis(z, Vector3.forward);
        public static Quaternion Euler(Vector3 e) => Euler(e.x, e.y, e.z);
        public static Quaternion AngleAxis(float a, Vector3 axis) { var n = axis.normalized; var h = a * Mathf.Deg2Rad * 0.5f; var s = Mathf.Sin(h); return new Quaternion(n.x * s, n.y * s, n.z * s, Mathf.Cos(h)); }
        public static Quaternion LookRotation(Vector3 f) => identity;
        public static Quaternion LookRotation(Vector3 f, Vector3 up) => identity;
        public static Quaternion FromToRotation(Vector3 a, Vector3 b) => identity;
        public static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => b;
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => b;
        public static float Angle(Quaternion a, Quaternion b) => 0;
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            var p = q * new Quaternion(v.x, v.y, v.z, 0) * Inverse(q);
            return new Vector3(p.x, p.y, p.z);
        }
    }

    public struct Matrix4x4
    {
        public static Matrix4x4 identity => default;
        public static Matrix4x4 TRS(Vector3 p, Quaternion q, Vector3 s) => default;
        public static Matrix4x4 Scale(Vector3 s) => default;
        public static Matrix4x4 Translate(Vector3 p) => default;
        public static Matrix4x4 Rotate(Quaternion q) => default;
        public Matrix4x4 inverse => this;
        public Vector3 MultiplyPoint(Vector3 p) => p;
        public Vector3 MultiplyPoint3x4(Vector3 p) => p;
        public Vector3 MultiplyVector(Vector3 p) => p;
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => a;
        public static Matrix4x4 Ortho(float l, float r, float b, float t, float n, float f) => default;
    }

    public struct Color : IEquatable<Color>
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public float this[int i] { get => i == 0 ? r : i == 1 ? g : i == 2 ? b : a; set { if (i == 0) r = value; else if (i == 1) g = value; else if (i == 2) b = value; else a = value; } }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color red => new Color(1, 0, 0, 1);
        public static Color green => new Color(0, 1, 0, 1);
        public static Color blue => new Color(0, 0, 1, 1);
        public static Color yellow => new Color(1, 0.92f, 0.016f, 1);
        public static Color cyan => new Color(0, 1, 1, 1);
        public static Color magenta => new Color(1, 0, 1, 1);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f, 1);
        public static Color grey => gray;
        public float grayscale => 0.299f * r + 0.587f * g + 0.114f * b;
        public float maxColorComponent => Math.Max(r, Math.Max(g, b));
        public Color linear => this;
        public Color gamma => this;
        public static Color Lerp(Color x, Color y, float t) { t = Mathf.Clamp01(t); return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t); }
        public static Color LerpUnclamped(Color x, Color y, float t) => new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t);
        public static Color HSVToRGB(float h, float s, float v) => white;
        public static Color HSVToRGB(float h, float s, float v, bool hdr) => white;
        public static void RGBToHSV(Color c, out float h, out float s, out float v) { h = s = v = 0; }
        public static Color operator *(Color x, Color y) => new Color(x.r * y.r, x.g * y.g, x.b * y.b, x.a * y.a);
        public static Color operator *(Color x, float d) => new Color(x.r * d, x.g * d, x.b * d, x.a * d);
        public static Color operator *(float d, Color x) => x * d;
        public static Color operator /(Color x, float d) => new Color(x.r / d, x.g / d, x.b / d, x.a / d);
        public static Color operator +(Color x, Color y) => new Color(x.r + y.r, x.g + y.g, x.b + y.b, x.a + y.a);
        public static Color operator -(Color x, Color y) => new Color(x.r - y.r, x.g - y.g, x.b - y.b, x.a - y.a);
        public static bool operator ==(Color x, Color y) => x.Equals(y);
        public static bool operator !=(Color x, Color y) => !x.Equals(y);
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        public static implicit operator Color(Vector4 v) => new Color(v.x, v.y, v.z, v.w);
        public bool Equals(Color o) => r == o.r && g == o.g && b == o.b && a == o.a;
        public override bool Equals(object o) => o is Color c && Equals(c);
        public override int GetHashCode() => r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode() ^ a.GetHashCode();
        public override string ToString() => $"RGBA({r:F3}, {g:F3}, {b:F3}, {a:F3})";
    }

    public struct Color32 : IEquatable<Color32>
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) => new Color32((byte)Math.Round(Mathf.Clamp01(c.r) * 255), (byte)Math.Round(Mathf.Clamp01(c.g) * 255), (byte)Math.Round(Mathf.Clamp01(c.b) * 255), (byte)Math.Round(Mathf.Clamp01(c.a) * 255));
        public static Color32 Lerp(Color32 x, Color32 y, float t) => Color.Lerp(x, y, t);
        public bool Equals(Color32 o) => r == o.r && g == o.g && b == o.b && a == o.a;
        public override bool Equals(object o) => o is Color32 c && Equals(c);
        public override int GetHashCode() => r | g << 8 | b << 16 | a << 24;
        public override string ToString() => $"RGBA({r}, {g}, {b}, {a})";
    }

    public static class ColorUtility
    {
        public static string ToHtmlStringRGB(Color c) { Color32 x = c; return $"{x.r:X2}{x.g:X2}{x.b:X2}"; }
        public static string ToHtmlStringRGBA(Color c) { Color32 x = c; return $"{x.r:X2}{x.g:X2}{x.b:X2}{x.a:X2}"; }
        public static bool TryParseHtmlString(string s, out Color c) { c = Color.white; return true; }
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public Rect(Vector2 p, Vector2 s) { x = p.x; y = p.y; width = s.x; height = s.y; }
        public Vector2 position { get => new Vector2(x, y); set { x = value.x; y = value.y; } }
        public Vector2 size { get => new Vector2(width, height); set { width = value.x; height = value.y; } }
        public Vector2 center { get => new Vector2(x + width / 2, y + height / 2); set { x = value.x - width / 2; y = value.y - height / 2; } }
        public Vector2 min => new Vector2(xMin, yMin);
        public Vector2 max => new Vector2(xMax, yMax);
        public float xMin { get => x; set { width += x - value; x = value; } }
        public float yMin { get => y; set { height += y - value; y = value; } }
        public float xMax { get => x + width; set => width = value - x; }
        public float yMax { get => y + height; set => height = value - y; }
        public bool Contains(Vector2 p) => p.x >= xMin && p.x < xMax && p.y >= yMin && p.y < yMax;
        public bool Contains(Vector3 p) => Contains((Vector2)p);
        public bool Overlaps(Rect o) => o.xMax > xMin && o.xMin < xMax && o.yMax > yMin && o.yMin < yMax;
        public static Rect zero => default;
        public static Rect MinMaxRect(float a, float b, float c, float d) => new Rect(a, b, c - a, d - b);
    }

    public struct RectInt
    {
        public int x, y, width, height;
        public RectInt(int x, int y, int w, int h) { this.x = x; this.y = y; width = w; height = h; }
        public RectInt(Vector2Int p, Vector2Int s) { x = p.x; y = p.y; width = s.x; height = s.y; }
        public int xMin { get => Math.Min(x, x + width); set { var m = xMax; x = value; width = m - x; } }
        public int yMin { get => Math.Min(y, y + height); set { var m = yMax; y = value; height = m - y; } }
        public int xMax { get => Math.Max(x, x + width); set => width = value - x; }
        public int yMax { get => Math.Max(y, y + height); set => height = value - y; }
        public Vector2Int position { get => new Vector2Int(x, y); set { x = value.x; y = value.y; } }
        public Vector2Int size { get => new Vector2Int(width, height); set { width = value.x; height = value.y; } }
        public Vector2Int min { get => new Vector2Int(xMin, yMin); set { xMin = value.x; yMin = value.y; } }
        public Vector2Int max { get => new Vector2Int(xMax, yMax); set { xMax = value.x; yMax = value.y; } }
        public Vector2 center => new Vector2(x + width / 2f, y + height / 2f);
        public bool Contains(Vector2Int p) => p.x >= xMin && p.y >= yMin && p.x < xMax && p.y < yMax;
        public bool Overlaps(RectInt o) => o.xMin < xMax && o.xMax > xMin && o.yMin < yMax && o.yMax > yMin;
        public void SetMinMax(Vector2Int a, Vector2Int b) { min = a; max = b; }
        public void ClampToBounds(RectInt b) { }
        public PositionEnumerator allPositionsWithin => new PositionEnumerator(min, max);
        public struct PositionEnumerator : System.Collections.Generic.IEnumerator<Vector2Int>
        {
            readonly Vector2Int min, max; Vector2Int cur;
            public PositionEnumerator(Vector2Int min, Vector2Int max) { this.min = min; this.max = max; cur = new Vector2Int(min.x - 1, min.y); }
            public PositionEnumerator GetEnumerator() => this;
            public bool MoveNext() { if (cur.y >= max.y) return false; cur.x++; if (cur.x >= max.x) { cur.x = min.x; cur.y++; if (cur.y >= max.y) return false; } return true; }
            public Vector2Int Current => cur;
            object System.Collections.IEnumerator.Current => cur;
            public void Reset() { cur = new Vector2Int(min.x - 1, min.y); }
            public void Dispose() { }
        }
        public override string ToString() => $"(x:{x}, y:{y}, width:{width}, height:{height})";
    }

    public struct Bounds
    {
        public Bounds(Vector3 c, Vector3 s) { center = c; size = s; }
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
        public Vector3 extents { get => size / 2; set => size = value * 2; }
        public Vector3 min { get => center - extents; set { } }
        public Vector3 max { get => center + extents; set { } }
        public void Encapsulate(Vector3 p) { }
        public void Encapsulate(Bounds b) { }
        public bool Contains(Vector3 p) => true;
        public void SetMinMax(Vector3 a, Vector3 b) { center = (a + b) / 2; size = b - a; }
        public bool Intersects(Bounds b) => true;
        public void Expand(float a) { }
    }

    public struct BoundsInt
    {
        public BoundsInt(Vector3Int p, Vector3Int s) { position = p; size = s; }
        public BoundsInt(int x, int y, int z, int sx, int sy, int sz) { position = new Vector3Int(x, y, z); size = new Vector3Int(sx, sy, sz); }
        public Vector3Int position { get; set; }
        public Vector3Int size { get; set; }
        public int x => position.x; public int y => position.y; public int z => position.z;
        public int xMin => position.x; public int yMin => position.y; public int zMin => position.z;
        public int xMax => position.x + size.x; public int yMax => position.y + size.y; public int zMax => position.z + size.z;
        public Vector3Int min => position;
        public Vector3Int max => position + size;
        public bool Contains(Vector3Int p) => p.x >= xMin && p.y >= yMin && p.z >= zMin && p.x < xMax && p.y < yMax && p.z < zMax;
        public System.Collections.Generic.IEnumerable<Vector3Int> allPositionsWithin
        {
            get { for (var z = zMin; z < zMax; z++) for (var y = yMin; y < yMax; y++) for (var x = xMin; x < xMax; x++) yield return new Vector3Int(x, y, z); }
        }
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public const float Epsilon = float.Epsilon;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Tan(float f) => (float)Math.Tan(f);
        public static float Asin(float f) => (float)Math.Asin(f);
        public static float Acos(float f) => (float)Math.Acos(f);
        public static float Atan(float f) => (float)Math.Atan(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int v) => Math.Abs(v);
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Min(params float[] v) { var m = v[0]; foreach (var x in v) if (x < m) m = x; return m; }
        public static int Min(int a, int b) => a < b ? a : b;
        public static int Min(params int[] v) { var m = v[0]; foreach (var x in v) if (x < m) m = x; return m; }
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Max(params float[] v) { var m = v[0]; foreach (var x in v) if (x > m) m = x; return m; }
        public static int Max(int a, int b) => a > b ? a : b;
        public static int Max(params int[] v) { var m = v[0]; foreach (var x in v) if (x > m) m = x; return m; }
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Exp(float p) => (float)Math.Exp(p);
        public static float Log(float f, float p) => (float)Math.Log(f, p);
        public static float Log(float f) => (float)Math.Log(f);
        public static float Log10(float f) => (float)Math.Log10(f);
        public static float Ceil(float f) => (float)Math.Ceiling(f);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static float Round(float f) => (float)Math.Round(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int RoundToInt(float f) => (int)Math.Round(f);
        public static float Sign(float f) => f >= 0f ? 1f : -1f;
        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float LerpAngle(float a, float b, float t) { var d = Repeat(b - a, 360); if (d > 180) d -= 360; return a + d * Clamp01(t); }
        public static float MoveTowards(float c, float t, float d) => Math.Abs(t - c) <= d ? t : c + Sign(t - c) * d;
        public static float MoveTowardsAngle(float c, float t, float d) { var dl = DeltaAngle(c, t); if (-d < dl && dl < d) return t; return MoveTowards(c, c + dl, d); }
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = -2f * t * t * t + 3f * t * t; return b * t + a * (1f - t); }
        public static float SmoothDamp(float c, float t, ref float v, float s, float max, float dt) => t;
        public static float SmoothDamp(float c, float t, ref float v, float s) => t;
        public static float Repeat(float t, float l) => Clamp(t - Floor(t / l) * l, 0f, l);
        public static float PingPong(float t, float l) { t = Repeat(t, l * 2f); return l - Abs(t - l); }
        public static float InverseLerp(float a, float b, float v) => a != b ? Clamp01((v - a) / (b - a)) : 0f;
        public static float DeltaAngle(float c, float t) { var d = Repeat(t - c, 360f); if (d > 180f) d -= 360f; return d; }
        public static bool Approximately(float a, float b) => Abs(b - a) < Max(1e-6f * Max(Abs(a), Abs(b)), Epsilon * 8);
        public static float PerlinNoise(float x, float y) => 0.5f;
        public static bool IsPowerOfTwo(int v) => (v & (v - 1)) == 0;
        public static int NextPowerOfTwo(int v) { var p = 1; while (p < v) p <<= 1; return p; }
        public static float GammaToLinearSpace(float v) => (float)Math.Pow(v, 2.2);
        public static float LinearToGammaSpace(float v) => (float)Math.Pow(v, 1 / 2.2);
    }

    public static class Random
    {
        static System.Random r = new System.Random(1);
        public static float value => (float)r.NextDouble();
        public static void InitState(int seed) { r = new System.Random(seed); }
        public static float Range(float a, float b) => a + (float)r.NextDouble() * (b - a);
        public static int Range(int a, int b) => b <= a ? a : r.Next(a, b);
        public static Vector2 insideUnitCircle { get { var a = value * Mathf.PI * 2; var m = Mathf.Sqrt(value); return new Vector2(Mathf.Cos(a) * m, Mathf.Sin(a) * m); } }
        public static Vector3 insideUnitSphere => insideUnitCircle;
        public static Vector3 onUnitSphere => Vector3.up;
        public static Quaternion rotation => Quaternion.identity;
        public static Color ColorHSV() => Color.white;
        public struct State { }
        public static State state { get; set; }
    }
}

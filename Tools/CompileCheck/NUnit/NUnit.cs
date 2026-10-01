using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute { public string Description { get; set; } }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)] public sealed class TestCaseAttribute : Attribute { public object[] Arguments { get; } public object ExpectedResult { get; set; } public string TestName { get; set; } public TestCaseAttribute(params object[] args) { Arguments = args ?? new object[] { null }; } }
    [AttributeUsage(AttributeTargets.Method)] public sealed class SetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class TearDownAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class OneTimeSetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class OneTimeTearDownAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class TestFixtureAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)] public sealed class IgnoreAttribute : Attribute { public IgnoreAttribute(string r) { } }
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)] public sealed class CategoryAttribute : Attribute { public CategoryAttribute(string r) { } }
    public delegate void TestDelegate();
    public class AssertionException : Exception { public AssertionException(string m) : base(m) { } }

    static class Eq
    {
        static bool IsNumeric(object o) => o is sbyte || o is byte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong || o is float || o is double || o is decimal;
        public static bool Equal(object a, object b)
        {
            if (a == null || b == null) return a == null && b == null;
            if (IsNumeric(a) && IsNumeric(b))
            {
                if ((a is float || a is double || b is float || b is double)) return Convert.ToDouble(a) == Convert.ToDouble(b) || (double.IsNaN(Convert.ToDouble(a)) && double.IsNaN(Convert.ToDouble(b)));
                return Convert.ToDecimal(a) == Convert.ToDecimal(b);
            }
            if (!(a is string) && !(b is string) && a is IEnumerable ea && b is IEnumerable eb && !(a.GetType().IsValueType && a.Equals(b)))
            {
                var la = ea.Cast<object>().ToList(); var lb = eb.Cast<object>().ToList();
                if (la.Count != lb.Count) return false;
                for (var i = 0; i < la.Count; i++) if (!Equal(la[i], lb[i])) return false;
                return true;
            }
            return a.Equals(b);
        }
        public static string Show(object o) => o == null ? "null" : o is string s ? "\"" + s + "\"" : o is IEnumerable e && !(o is string) ? "[" + string.Join(", ", e.Cast<object>().Take(12).Select(Show)) + "]" : o.ToString();
        public static int Compare(object a, object b)
        {
            if (IsNumeric(a) && IsNumeric(b)) return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));
            return ((IComparable)a).CompareTo(b);
        }
        public static string Msg(string m, object[] args) => m == null ? "" : (args != null && args.Length > 0 ? string.Format(m, args) : m);
    }

    public static class Assert
    {
        static void Fail(string what, string m, object[] a) => throw new AssertionException(what + (string.IsNullOrEmpty(Eq.Msg(m, a)) ? "" : "  -- " + Eq.Msg(m, a)));
        public static void Fail(string m = null, params object[] a) => Fail("Assert.Fail", m, a);
        public static void Pass(string m = null) { }
        public static void AreEqual(object expected, object actual) => AreEqual(expected, actual, null);
        public static void AreEqual(object expected, object actual, string m, params object[] a) { if (!Eq.Equal(expected, actual)) Fail($"Expected {Eq.Show(expected)} but was {Eq.Show(actual)}", m, a); }
        public static void AreEqual(double expected, double actual, double delta) => AreEqual(expected, actual, delta, null);
        public static void AreEqual(double expected, double actual, double delta, string m, params object[] a) { if (!(Math.Abs(expected - actual) <= delta) && !(double.IsInfinity(expected) && expected == actual)) Fail($"Expected {expected} +/- {delta} but was {actual}", m, a); }
        public static void AreEqual(double expected, double? actual, double delta, string m = null, params object[] a) => AreEqual(expected, actual ?? double.NaN, delta, m, a);
        public static void AreNotEqual(object expected, object actual, string m = null, params object[] a) { if (Eq.Equal(expected, actual)) Fail($"Expected not {Eq.Show(expected)}", m, a); }
        public static void AreSame(object expected, object actual, string m = null, params object[] a) { if (!ReferenceEquals(expected, actual)) Fail($"Expected same object {Eq.Show(expected)} but was {Eq.Show(actual)}", m, a); }
        public static void AreNotSame(object expected, object actual, string m = null, params object[] a) { if (ReferenceEquals(expected, actual)) Fail("Expected different objects", m, a); }
        public static void IsTrue(bool c, string m = null, params object[] a) { if (!c) Fail("Expected True", m, a); }
        public static void IsTrue(bool? c, string m = null, params object[] a) { if (c != true) Fail("Expected True", m, a); }
        public static void True(bool c, string m = null, params object[] a) => IsTrue(c, m, a);
        public static void IsFalse(bool c, string m = null, params object[] a) { if (c) Fail("Expected False", m, a); }
        public static void IsFalse(bool? c, string m = null, params object[] a) { if (c != false) Fail("Expected False", m, a); }
        public static void False(bool c, string m = null, params object[] a) => IsFalse(c, m, a);
        public static void IsNull(object o, string m = null, params object[] a) { if (o != null && !(o is UnityEngine.Object u && !u)) Fail($"Expected null but was {Eq.Show(o)}", m, a); }
        public static void Null(object o, string m = null, params object[] a) => IsNull(o, m, a);
        public static void IsNotNull(object o, string m = null, params object[] a) { if (o == null) Fail("Expected not null", m, a); }
        public static void NotNull(object o, string m = null, params object[] a) => IsNotNull(o, m, a);
        public static void IsEmpty(object o, string m = null, params object[] a) { var empty = o is string s ? s.Length == 0 : o is IEnumerable e && !e.Cast<object>().Any(); if (!empty) Fail($"Expected empty but was {Eq.Show(o)}", m, a); }
        public static void IsNotEmpty(object o, string m = null, params object[] a) { var empty = o is string s ? s.Length == 0 : o is IEnumerable e && !e.Cast<object>().Any(); if (empty) Fail("Expected not empty", m, a); }
        public static void Greater(object x, object y, string m = null, params object[] a) { if (!(Eq.Compare(x, y) > 0)) Fail($"Expected {x} > {y}", m, a); }
        public static void GreaterOrEqual(object x, object y, string m = null, params object[] a) { if (!(Eq.Compare(x, y) >= 0)) Fail($"Expected {x} >= {y}", m, a); }
        public static void Less(object x, object y, string m = null, params object[] a) { if (!(Eq.Compare(x, y) < 0)) Fail($"Expected {x} < {y}", m, a); }
        public static void LessOrEqual(object x, object y, string m = null, params object[] a) { if (!(Eq.Compare(x, y) <= 0)) Fail($"Expected {x} <= {y}", m, a); }
        public static T Throws<T>(TestDelegate d, string m = null, params object[] a) where T : Exception
        {
            try { d(); } catch (T e) when (e.GetType() == typeof(T)) { return e; } catch (Exception e) { Fail($"Expected {typeof(T).Name} but got {e.GetType().Name}: {e.Message}", m, a); }
            Fail($"Expected {typeof(T).Name} but nothing was thrown", m, a); return null;
        }
        public static T Catch<T>(TestDelegate d) where T : Exception { try { d(); } catch (T e) { return e; } Fail("Expected exception", null, null); return null; }
        public static void DoesNotThrow(TestDelegate d, string m = null, params object[] a) { try { d(); } catch (Exception e) { Fail($"Unexpected {e.GetType().Name}: {e.Message}", m, a); } }
        public static void That(object actual, IResolveConstraint c, string m = null, params object[] a) { var r = c.Resolve(); if (!r.Matches(actual)) Fail($"Expected {r.Description} but was {Eq.Show(actual)}", m, a); }
        public static void That(bool c, string m = null, params object[] a) => IsTrue(c, m, a);
        public static void Contains(object expected, ICollection actual, string m = null, params object[] a) { if (!actual.Cast<object>().Any(x => Eq.Equal(x, expected))) Fail($"Expected collection containing {Eq.Show(expected)}", m, a); }
        public static void Ignore(string m = null) => throw new IgnoreException(m);
        public static void Inconclusive(string m = null) => throw new IgnoreException(m);
    }
    public class IgnoreException : Exception { public IgnoreException(string m) : base(m) { } }
    public interface IResolveConstraint { Constraint Resolve(); }
    public abstract class Constraint : IResolveConstraint { public abstract bool Matches(object a); public abstract string Description { get; } public Constraint Resolve() => this; public Constraint Within(double d) => new Within(this, d); public OrBuilder Or => new OrBuilder(this); }
    public sealed class OrBuilder { readonly Constraint left; public OrBuilder(Constraint l) { left = l; } public Constraint EqualTo(object e) => new OrC(left, Is.EqualTo(e)); }
    class OrC : Constraint { readonly Constraint a, b; public OrC(Constraint a, Constraint b) { this.a = a; this.b = b; } public override bool Matches(object x) => a.Matches(x) || b.Matches(x); public override string Description => a.Description + " or " + b.Description; }
    class Within : Constraint { readonly Constraint inner; readonly double d; public Within(Constraint i, double d) { inner = i; this.d = d; } public override bool Matches(object a) => inner is EqualC e ? Math.Abs(Convert.ToDouble(e.Expected) - Convert.ToDouble(a)) <= d : inner.Matches(a); public override string Description => inner.Description + " within " + d; }
    class EqualC : Constraint { public object Expected; public override bool Matches(object a) => Eq.Equal(Expected, a); public override string Description => Eq.Show(Expected); }
    class RangeC : Constraint { public object Min, Max; public override bool Matches(object a) => Eq.Compare(a, Min) >= 0 && Eq.Compare(a, Max) <= 0; public override string Description => $"in range ({Min}, {Max})"; }
    class Pred : Constraint { readonly Func<object, bool> f; readonly string d; public Pred(Func<object, bool> f, string d) { this.f = f; this.d = d; } public override bool Matches(object a) => f(a); public override string Description => d; }
    public static class Is
    {
        public static Constraint InRange(object min, object max) => new RangeC { Min = min, Max = max };
        public static Constraint EqualTo(object e) => new EqualC { Expected = e };
        public static Constraint GreaterThan(object e) => new Pred(a => Eq.Compare(a, e) > 0, "greater than " + e);
        public static Constraint LessThan(object e) => new Pred(a => Eq.Compare(a, e) < 0, "less than " + e);
        public static Constraint GreaterThanOrEqualTo(object e) => new Pred(a => Eq.Compare(a, e) >= 0, ">= " + e);
        public static Constraint LessThanOrEqualTo(object e) => new Pred(a => Eq.Compare(a, e) <= 0, "<= " + e);
        public static Constraint Null => new Pred(a => a == null, "null");
        public static Constraint True => new Pred(a => a is bool b && b, "True");
        public static Constraint False => new Pred(a => a is bool b && !b, "False");
        public static Constraint Empty => new Pred(a => a is string s ? s.Length == 0 : !((IEnumerable)a).Cast<object>().Any(), "empty");
        public static class Not { public static Constraint Null => new Pred(a => a != null, "not null"); public static Constraint Empty => new Pred(a => a is string s ? s.Length > 0 : ((IEnumerable)a).Cast<object>().Any(), "not empty"); public static Constraint EqualTo(object e) => new Pred(a => !Eq.Equal(e, a), "not " + e); }
    }
    public static class CollectionAssert
    {
        public static void AreEqual(IEnumerable expected, IEnumerable actual, string m = null, params object[] a) { if (!Eq.Equal(expected, actual)) throw new AssertionException($"Expected {Eq.Show(expected)} but was {Eq.Show(actual)} {Eq.Msg(m, a)}"); }
        public static void AreEquivalent(IEnumerable expected, IEnumerable actual, string m = null, params object[] a)
        {
            var la = expected.Cast<object>().ToList(); var lb = actual.Cast<object>().ToList();
            var ok = la.Count == lb.Count; foreach (var x in la) { var i = lb.FindIndex(y => Eq.Equal(x, y)); if (i < 0) { ok = false; break; } lb.RemoveAt(i); }
            if (!ok) throw new AssertionException($"Expected equivalent to {Eq.Show(expected)} but was {Eq.Show(actual)} {Eq.Msg(m, a)}");
        }
        public static void IsEmpty(IEnumerable c, string m = null, params object[] a) { if (c.Cast<object>().Any()) throw new AssertionException($"Expected empty but was {Eq.Show(c)} {Eq.Msg(m, a)}"); }
        public static void IsNotEmpty(IEnumerable c, string m = null, params object[] a) { if (!c.Cast<object>().Any()) throw new AssertionException($"Expected not empty {Eq.Msg(m, a)}"); }
        public static void Contains(IEnumerable c, object e, string m = null, params object[] a) { if (!c.Cast<object>().Any(x => Eq.Equal(x, e))) throw new AssertionException($"Expected {Eq.Show(c)} to contain {Eq.Show(e)} {Eq.Msg(m, a)}"); }
        public static void DoesNotContain(IEnumerable c, object e, string m = null, params object[] a) { if (c.Cast<object>().Any(x => Eq.Equal(x, e))) throw new AssertionException($"Expected {Eq.Show(c)} not to contain {Eq.Show(e)} {Eq.Msg(m, a)}"); }
        public static void AllItemsAreUnique(IEnumerable c, string m = null, params object[] a) { var l = c.Cast<object>().ToList(); if (l.Distinct().Count() != l.Count) throw new AssertionException("Items not unique " + Eq.Msg(m, a)); }
    }
    public static class StringAssert
    {
        public static void Contains(string expected, string actual, string m = null, params object[] a) { if (actual == null || !actual.Contains(expected)) throw new AssertionException($"Expected string containing \"{expected}\" but was \"{actual}\" {Eq.Msg(m, a)}"); }
        public static void DoesNotContain(string expected, string actual, string m = null, params object[] a) { if (actual != null && actual.Contains(expected)) throw new AssertionException($"Expected string not containing \"{expected}\" but was \"{actual}\" {Eq.Msg(m, a)}"); }
        public static void StartsWith(string expected, string actual, string m = null, params object[] a) { if (actual == null || !actual.StartsWith(expected)) throw new AssertionException($"Expected string starting \"{expected}\" but was \"{actual}\""); }
    }
}
namespace UnityEngine.TestTools { public static class LogAssert { public static void Expect(UnityEngine.LogType t, string m) { } public static void Expect(UnityEngine.LogType t, System.Text.RegularExpressions.Regex m) { } public static void NoUnexpectedReceived() { } public static bool ignoreFailingMessages { get; set; } } public sealed class UnityTestAttribute : System.Attribute { } }

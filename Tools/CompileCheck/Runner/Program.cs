using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
static class Program
{
    static int Main(string[] args)
    {
        var asm = typeof(ARPG.Tests.LegendaryTests).Assembly;
        int pass = 0, fail = 0, skip = 0;
        var filter = args.Length > 0 ? args[0] : null;
        foreach (var type in asm.GetTypes().Where(t => t.IsClass && !t.IsAbstract).OrderBy(t => t.FullName))
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.IsDefined(typeof(TestAttribute)) || m.IsDefined(typeof(TestCaseAttribute))).ToList();
            if (methods.Count == 0) continue;
            var setUp = type.GetMethods().FirstOrDefault(m => m.IsDefined(typeof(SetUpAttribute)));
            var tearDown = type.GetMethods().FirstOrDefault(m => m.IsDefined(typeof(TearDownAttribute)));
            foreach (var method in methods)
            {
                var cases = method.GetCustomAttributes<TestCaseAttribute>().Select(c => c.Arguments).ToList();
                if (cases.Count == 0) cases.Add(new object[0]);
                foreach (var raw in cases)
                {
                    var name = $"{type.Name}.{method.Name}" + (raw.Length > 0 ? "(" + string.Join(",", raw) + ")" : "");
                    if (filter != null && !name.Contains(filter)) continue;
                    try
                    {
                        var ps = method.GetParameters();
                        var callArgs = ps.Select((p, i) => i < raw.Length ? Convert(raw[i], p.ParameterType) : p.DefaultValue).ToArray();
                        var instance = Activator.CreateInstance(type);
                        setUp?.Invoke(instance, null);
                        try { method.Invoke(instance, callArgs); }
                        finally { tearDown?.Invoke(instance, null); }
                        pass++;
                    }
                    catch (Exception e)
                    {
                        var inner = e is TargetInvocationException t && t.InnerException != null ? t.InnerException : e;
                        if (inner is IgnoreException) { skip++; continue; }
                        fail++;
                        var where = inner.StackTrace?.Split('\n').FirstOrDefault(l => l.Contains("/Assets/_Project/"))?.Trim() ?? inner.StackTrace?.Split('\n').FirstOrDefault()?.Trim();
                        Console.WriteLine($"FAIL {name}\n     {inner.GetType().Name}: {inner.Message.Replace("\n", " ")}\n     {where}");
                    }
                }
            }
        }
        Console.WriteLine($"\n{pass} passed, {fail} failed, {skip} skipped");
        return fail == 0 ? 0 : 1;
    }
    static object Convert(object v, Type t)
    {
        if (v == null) return null;
        if (t.IsInstanceOfType(v)) return v;
        if (t.IsEnum) return Enum.ToObject(t, v);
        return System.Convert.ChangeType(v, t);
    }
}

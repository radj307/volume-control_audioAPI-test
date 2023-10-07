using System;
using System.Collections.Generic;
using System.Diagnostics;
using VolumeControl.Log;

namespace volume_control_audioAPI_test
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            //var t = new Stopwatch();
            //var profile = (Action action) =>
            //{
            //    if (t.Elapsed != TimeSpan.Zero) t.Reset();

            //    t.Start();
            //    action.Invoke();
            //    t.Stop();

            //    var elapsed = t.Elapsed;

            //    t.Reset();

            //    return elapsed;
            //};

            //List<int> l = new();
            //for (int i = 0; i < 1000; ++i)
            //{
            //    l.Add((i + 1) * Random.Shared.Next(1, 5) - 1);
            //}

            //var byIndex = () =>
            //{
            //    for (int i = 0; i < l.Count; ++i)
            //    {
            //        _ = l[i];
            //    }
            //};
            //var byIndexWithCachedMax = () =>
            //{
            //    for (int i = 0, max = l.Count; i < max; ++i)
            //    {
            //        _ = l[i];
            //    }
            //};
            //var byEnumerable = () =>
            //{
            //    l.ForEach(i => _ = i);
            //};

            //var elapsed1 = profile(byIndex);
            //var elapsed2 = profile(byIndexWithCachedMax);
            //var elapsed3 = profile(byEnumerable);

            //return;

            Config settings = new();

            App app = new();

            int rc = app.Run(new MainWindow());
            //try
            //{
            //}
            //catch (Exception ex)
            //{
            //    FLog.Log.Fatal("Application exited because of an unhandled exception:", ex);
            //}
        }
    }
}

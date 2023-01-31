using System;
using System.Linq;
using System.Collections.Generic;

namespace landrope.common{
  public static class SpannerFactory{
    public static Func<bool,bool,TimeSpan[]> spanner =>
      ((Func<Func<bool,bool,TimeSpan[]>>)(() => { 
        DateTime last = DateTime.Now; 
        List<TimeSpan> recs = new List<TimeSpan>();
        Func<bool,bool,TimeSpan[]> run = (returns, accumulate) => { 
          var xlast = DateTime.Now; 
          var span = xlast - last;
          last = xlast; 
          recs.Add(span);
          if (!returns)
          return new TimeSpan[0];
          
          var arr = recs.ToArray();
          recs.Clear();
          if (accumulate)
          {
            for (int i=1;i<arr.Length;i++)
              arr[i] += arr[i-1];
          }
          return arr; 
          
        }; 
        return run; 
      })).Invoke();
  }
}

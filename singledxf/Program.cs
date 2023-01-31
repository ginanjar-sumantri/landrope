using CadLoader;
using System;
using System.Linq;

namespace singledxf
{
	class Program
	{
		static void Main(string[] args)
		{
			string fname = args[0];
			DxfFile dxf = new DxfFile(fname);
			
			var shape = dxf.singleplane;

			shape.coords.Select((s, i) => (s, i)).ToList().ForEach(x => {
					Console.WriteLine($"shape {x.i}");
					x.s.ForEach(v => Console.WriteLine($"{v.x},{v.y}"));
				Console.WriteLine("");
				});
		}
	}
}

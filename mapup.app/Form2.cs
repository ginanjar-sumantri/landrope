using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mapup
{
	public partial class Form2 : Form
	{
		string listfname;
		public Form2()
		{
			InitializeComponent();
		}

		public Form2(string listfname)
			:this()
		{
			this.listfname = listfname;
			process();
		}

		void process()
		{
			Console.WriteLine($"Batch processing map files based on \"{listfname}\"...");
			var context = Form1.CreateContext();
			var proc = new processor(context);
			string result = proc.BatchProcess(fname);
			Console.WriteLine(result);
			Console.Write("Press any key to close...");

		}
	}

	public class MyConsole : TextWriter
	{
		TextBox txtbox;
		public MyConsole(TextBox txtbox)
			:base()
		{
			this.txtbox = txtbox;
		}

		public override Encoding Encoding => Encoding.ASCII;

		public override void Write(string value)
		{
			
		}
	}
}

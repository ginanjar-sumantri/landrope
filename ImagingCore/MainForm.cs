using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
#if _IRON_PDF_
using IronPdf;
#endif
#if _ITEXT_SHARP_
using iText;
using iText.Pdfa;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Kernel.Pdf.Xobject;
using iText.IO.Image;
using iText.Layout.Properties;
using iText.Layout.Element;
using iText.Kernel.Geom;
#endif

namespace ImagingCore
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		public PdfDocument ShowAndScan()
		{
			var form = new ScanForm();
			var res = form.Execute();
			return res;
		}

		public string[] Printers => PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
		public void SelectPrinter(string printer)
		{
			printDocument1.PrinterSettings.PrinterName = printer;
		}

		public (string name, decimal width, decimal height)[] Papers => 
				printDocument1.PrinterSettings.PaperSizes.Cast<PaperSize>()
			.Select(p => (name:p.PaperName,width:p.Width/100m,height:p.Height/100m))
			.ToArray();

		public bool Print(PdfDocument doc, string printer, string paper, bool portrait)
		{
			return false;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{

		}

		private void button1_Click(object sender, EventArgs e)
		{
			new ScanForm().Execute();
		}
	}
}

/*#define _ITEXT_SHARP_
#define _SELECT_PDF_
#define _IRON_PDF*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TwainDotNet;
using System.IO;
using TwainDotNet.WinFroms;
using TwainDotNet.TwainNative;
/*using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Utils;
using PdfSharpCore;
using PdfSharpCore.Pdf.Advanced;*/
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing.Imaging;
#if _IRON_PDF_
using IronPdf;
#endif
#if _ITEXT_SHARP_
using iText.Kernel.Pdf;
using iText.Layout;
using iText.IO.Image;
using iText.Layout.Properties;
using iText.Layout.Element;
using iText.Kernel.Geom;
#endif

namespace ImagingCore
{

	public partial class ScanForm : Form
	{
		Twain _twain;
		ScanSettings _settings;

		public ScanForm()
		{
			InitializeComponent();

			_twain = new Twain(new WinFormsWindowMessageHook(this));
			_twain.TransferImage += delegate (Object sender, TransferImageEventArgs args)
			{
				if (args.Image != null)
				{
					var imgstrm = args.Image.ToJpegStrm(80);
					imlist.Add(imgstrm);
					itemScroll1.Limit = imlist.Count;
					itemScroll1.MoveLast();
					//itemScroll1.ValueChange
					if (imlist.Count >= 1)
						Zooming();
					//pictureBox1.Image = args.Image;

					//widthLabel.Text = "Width: " + pictureBox1.Image.Width;
					//heightLabel.Text = "Height: " + pictureBox1.Image.Height;
				}
			};
			_twain.ScanningComplete += delegate
			{
				Enabled = true;
			};
		}

		private void selectSource_Click(object sender, EventArgs e)
		{
			_twain.SelectSource();
		}

		private void scan_Click(object sender, EventArgs e)
		{
			Enabled = false;

			_settings = new ScanSettings();
			_settings.UseDocumentFeeder = useAdfCheckBox.Checked;
			_settings.ShowTwainUI = useUICheckBox.Checked;
			_settings.ShowProgressIndicatorUI = showProgressIndicatorUICheckBox.Checked;
			_settings.UseDuplex = useDuplexCheckBox.Checked;
			_settings.Resolution =
					//                blackAndWhiteCheckBox.Checked
					//? ResolutionSettings.Fax : 
					ResolutionSettings.Photocopier;
			//_settings.Area = !checkBoxArea.Checked ? null : AreaSettings;
			_settings.ShouldTransferAllPages = true;

			//_settings.Rotation = new RotationSettings()
			//{
			//	/*               AutomaticRotate = autoRotateCheckBox.Checked,
			//								 AutomaticBorderDetection = autoDetectBorderCheckBox.Checked
			// */
			//};

			try
			{
				_twain.StartScanning(_settings);
			}
			catch (TwainException ex)
			{
				MessageBox.Show(ex.Message);
				Enabled = true;
			}
		}

		ObservableCollection<(int width, int height, float res, MemoryStream strm)> imlist = new ObservableCollection<(int, int, float, MemoryStream)>();
		System.Drawing.Image selimg = null;
		int imgidx = -1;
		int ImageIdx
		{
			get => imgidx;
			set
			{
				imgidx = value;
				var strm = imlist[imgidx].strm;
				strm.Seek(0, SeekOrigin.Begin);
				selimg = (imgidx >= 0 && imgidx < imlist.Count) ? System.Drawing.Image.FromStream(strm) : null;
				Zooming();
			}
		}

		enum ZoomMode
		{
			None,
			Width,
			Height,
			Auto,
			Percentage
		}

		(ZoomMode mode, int percent) Zoom = (ZoomMode.None, 100);

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			var txt = comboBox1.SelectedItem as string;
			var st = txt.Trim();
			Zoom = st switch
			{
				"Original" => (ZoomMode.None, 0),
				"Width" => (ZoomMode.Width, 0),
				"Height" => (ZoomMode.Height, 0),
				"Auto" => (ZoomMode.Auto, 0),
				_ => (ZoomMode.Percentage, int.Parse(st.Substring(0, st.Length - 1)))
			};
			Zooming();
		}

		private void Zooming()
		{
			switch (Zoom.mode)
			{
				case ZoomMode.Auto:
					pictureBox1.Image = selimg;
					panel3.AutoScroll = false;
					pictureBox1.Left = 0;
					pictureBox1.Top = 0;
					pictureBox1.Width = panel3.ClientSize.Width;
					pictureBox1.Height = panel3.ClientSize.Height;
					pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
					break;
				default:
					pictureBox1.Image = FitImage();
					if (pictureBox1.Image == null)
						return;

					pictureBox1.Width = pictureBox1.Image.Width;
					pictureBox1.Height = pictureBox1.Image.Height;
					pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
					pictureBox1.Left = Math.Max(0, (panel3.ClientSize.Width - pictureBox1.Width) / 2);
					pictureBox1.Top = 0;
					panel3.AutoScroll = true;
					break;
			}
		}

		System.Drawing.Image FitImage()
		{
			if (Zoom.percent == 100d || Zoom.mode == ZoomMode.None || Zoom.mode == ZoomMode.Auto)
				return selimg;

			var wi = panel3.Width;// - SystemInformation.VerticalScrollBarWidth;
			var hi = panel3.Height;// - SystemInformation.HorizontalScrollBarHeight;
			var fact = Zoom.mode switch
			{
				ZoomMode.Width => (mul: wi, div: selimg.Width),
				ZoomMode.Height => (mul: hi, div: selimg.Height),
				_ => (mul: Zoom.percent, div: 100)
			};
			var newsize = new Size(selimg.Width * fact.mul / fact.div, selimg.Height * fact.mul / fact.div);
			var bmp = new Bitmap(selimg, newsize);
			using (var g = Graphics.FromImage(bmp))
			{
				g.DrawImage(selimg, new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), newsize),
											new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), selimg.Size), GraphicsUnit.Pixel);
			}
			return bmp;
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			Zooming();
		}

		private void itemScroll1_ValueChange(object sender, mycomponents.ScrollValueChangeArgs e)
		{
			ImageIdx = e.Value - 1;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			comboBox1.SelectedIndex = 0;
			imlist.CollectionChanged += Imlist_CollectionChanged;
		}

		private void Imlist_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
				case NotifyCollectionChangedAction.Remove: itemScroll1.Limit = imlist.Count; break;
				case NotifyCollectionChangedAction.Reset: itemScroll1.Limit = 0; break;
			}
		}

		static string[] pgsizes = new []{
			nameof(PageSize.LETTER), nameof(PageSize.LEGAL),nameof(PageSize.A2),nameof(PageSize.A3),
			nameof(PageSize.A4),nameof(PageSize.A5),nameof(PageSize.A6)
		};

		PdfDocument result = null;

#if _ITEXT_SHARP_
		PdfDocument MakePDF()
		{
			if (imlist.Count == 0)
				return null;

			var dimensions = imlist.Select(i => (width: i.width / i.res, height: i.height / i.res))
				.ToArray();

			var maxwi = dimensions.Max(d => d.width);
			var maxhi = dimensions.Max(d => d.height);

			var papers = pgsizes.Select(p => (name: p, rect: PageSizeHelper.FindPage(p)))
									.Where(x=>x.rect!=null)
									.Select(x=>(x.name, size: (width:x.rect.GetWidth(),height:x.rect.GetHeight())))
									.ToArray();
			var xpages_p = papers.Select(x => (x.name, dx: x.size.width / 72d - maxwi, dy: x.size.height - maxhi, or: 'p'));
			var xpages_l = papers.Select(x => (x.name, dx: x.size.width / 72d - maxhi, dy: x.size.height - maxwi, or: 'l'));
			var xpage = xpages_p.Union(xpages_l).Where(x => x.dx >= 0 && x.dy >= 0)
										.OrderBy(x => (x.dx * x.dx + x.dy * x.dy))
										.FirstOrDefault();
			
			var paper= (xpage.name==null) ? PageSize.A4 : PageSizeHelper.FindPage(xpage.name);
			if (xpage.name != null && xpage.or == 'l')
				paper = paper.Rotate();

			var dstrm = new MemoryStream();
			var wrt = new PdfWriter(dstrm);
			var pdoc = new PdfDocument(wrt);
			var doc = new Document(pdoc, paper);
			for (int i=0;i<imlist.Count;i++)
			{
				var strm = imlist[i].strm;
				if (i > 0)
				{
					var abtype = i < imlist.Count - 1 ? AreaBreakType.NEXT_PAGE : AreaBreakType.LAST_PAGE;
					doc.Add(new AreaBreak(abtype));
				}
				doc.Add(new iText.Layout.Element.Image(ImportImg(strm)));
			}

			return doc.GetPdfDocument().;

			ImageData ImportImg(Stream strm)
			{
				strm.Seek(0, SeekOrigin.Begin);
				var data = new byte[strm.Length];
				strm.Read(data, 0, data.Length);
				return ImageDataFactory.Create(data);
			}
		}

#elif _SELECT_PDF_
		PdfDocument MakePDF()
		{
			if (imlist.Count == 0)
				return null;

			/*			var dimensions = imlist.Select(i => (img:i, width: i.Width  / i.HorizontalResolution, height: i.Height / i.VerticalResolution))
							.ToArray();
						var strm = new MemoryStream();
						var doc = new PdfDocument(strm);
						//doc. ColorMode = PdfColorMode.Cmyk;
			*/

			var doc = ImageToPdfConverter.ImageToPdf(imlist, ImageBehavior.TopLeftCornerOfPage);
			/*			foreach (var ximg in dimensions)
						{
							var xpages = pgsizes.Select(x => (x.name, dx: x.size.Width / 72d - ximg.width, dy: x.size.Height - ximg.height, or:x.size.Rotation==90? 'p' : 'l'));
							var xpage = xpages.Where(x => x.dx >= 0 && x.dy >= 0)
													.OrderBy(x=>(x.dx*x.dx+x.dy*x.dy))
													.FirstOrDefault();
							var page = ImageToPdfConverter.ImageToPdf( new PdfPage();// { PageOrientation=xpage.or=='p'?PageOrientation.Portrait: PageOrientation.Landscape, };
							using (var istream = new MemoryStream())
							{
								ximg.img.Save(istream, ImageFormat.Jpeg);
								istream.Seek(0, SeekOrigin.Begin);
								var item = new PdfImage(doc, XImage.FromStream(() => istream));
								page.Elements.Add(item.First());
							}
							doc.AddPage(page);
						}*/
			return doc;
		}
#else
		PdfDocument MakePDF() =>
			imlist.Any() ? ImageToPdfConverter.ImageToPdf(imlist, ImageBehavior.TopLeftCornerOfPage) : null;
#endif

		private void ScanForm_Shown(object sender, EventArgs e)
		{
			this.TopMost = true;
			this.Activate();
		}

		private void ScanForm_Activated(object sender, EventArgs e)
		{
			this.TopMost = false;
		}

		public PdfDocument Execute()
		{
			result = null;
			imlist.Clear();
			ShowDialog();
			return DialogResult == DialogResult.OK ? result : null;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (imlist.Count == 0)
			{
				MessageBox.Show("Belum ada dokumen yang di-scan. Klik 'Tutup' untuk membatalkan...");
				return;
			}

			this.Cursor = Cursors.WaitCursor;
			try
			{
				result = MakePDF();
				result.SaveAs("test.pdf");
			}
			finally
			{
				this.Cursor = this.DefaultCursor;
			}
			DialogResult = DialogResult.OK;
		}

		private void button3_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void button4_Click(object sender, EventArgs e)
		{
			if (imlist.Count > 0 && imgidx > 0)
				imlist.RemoveAt(imgidx);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			imlist.Clear();
		}
	}
}

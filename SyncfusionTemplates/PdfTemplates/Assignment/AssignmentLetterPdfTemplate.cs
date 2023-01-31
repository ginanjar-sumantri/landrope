using landrope.common;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SyncfusionTemplates.PdfTemplates.Assignment
{
	public class AssignmentLetterPdfTemplate
	{
		private RptSuratTugas _data { get; set; }
		private byte[] bytesData { get; set; }

		public AssignmentLetterPdfTemplate(RptSuratTugas data)
		{
			_data = data;
		}

		public async Task<string> Generate()
		{
			try
			{
				bytesData = await CreatePdf();

				return Convert.ToBase64String(bytesData);
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public async Task<byte[]> CreatePdf()
		{
			byte[] bytes;
			PdfDocument doc = new PdfDocument();
			doc.PageSettings.Size = PdfPageSize.A4;
			//doc.PageSettings.Size = new SizeF(2480, 3296);

			PdfPage page = doc.Pages.Add();

			page.Graphics.DrawString("SURAT TUGAS", PdfHelpers.LargeBoldFont, PdfHelpers.BlackBrush, 260, 20, PdfHelpers.TextAlignCenter);
			page.Graphics.DrawString(_data.tanggalPenugasan.GetValueOrDefault().ToString("dd MMMM yyyy", new CultureInfo("id-ID")), PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 50, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString(_data.nomorPenugasan, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 503, 50, PdfHelpers.TextAlignRight);
			page.Graphics.DrawLine(PdfHelpers.BlackPen, new PointF(10, 60), new PointF(505, 60));

			page.Graphics.DrawString($"Kepada   : Pensertifikatan BPN", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 80, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString(_data.penerima, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 61, 93, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString($"Cc           : {_data.tembusan[0]}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 115, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString($"Jenis Penugasan : {_data.jenisPenugasan}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 150, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString($"Bidang yang akan diproses :", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 200, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString($"Terlampir, dengan summary penjelasan sbb :", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 230, PdfHelpers.TextAlignLeft);

			//Set layout properties
			PdfLayoutFormat format = new PdfLayoutFormat();
			format.Break = PdfLayoutBreakType.FitPage;
			format.Layout = PdfLayoutType.Paginate;

			//Specify the style for PdfGridcell 
			PdfGridCellStyle headerstyle = new PdfGridCellStyle();
			headerstyle.Font = PdfHelpers.SmallFont;
			headerstyle.StringFormat = PdfHelpers.TextAlignCenter;

			PdfGrid grid = new PdfGrid();
			grid.DataSource = await CreateListBidang();
			//grid.ApplyBuiltinStyle(style, setting);
			grid.Style.Font = PdfHelpers.SmallestFont;
			grid.Style.CellPadding = new PdfPaddings(2, 2, 2, 2);
			grid.Headers.ApplyStyle(headerstyle);

			// Set Column Alignment
			for (int i = 0; i < grid.Columns.Count; i++)
				if (new int[] { 3, 4 }.Contains(i))
					grid.Columns[i].Format = PdfHelpers.TextAlignRight;
				else if (new int[] { 1, 2 }.Contains(i))
					grid.Columns[i].Format = PdfHelpers.TextAlignLeft;
				else
					grid.Columns[i].Format = PdfHelpers.TextAlignCenter;

			// Set Column Width
			for (int i = 0; i < grid.Columns.Count; i++)
				if (new int[] { 0 }.Contains(i))
					grid.Columns[i].Width = 25;
				else if (new int[] { 1 }.Contains(i))
					grid.Columns[i].Width = 100;
				else if (new int[] { 2 }.Contains(i))
					grid.Columns[i].Width = 180;
				else if (new int[] { 4 }.Contains(i))
					grid.Columns[i].Width = 80;
				else
					grid.Columns[i].Width = 50;

			grid.Rows[1].Cells[2].StringFormat = PdfHelpers.TextAlignRight;

			//Draw table
			grid.Draw(page, 12, 250, format);

			page.Graphics.DrawString("Demikian Surat Tugas ini disampaikan, untuk dapat dikerjakan dengan segera.", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 350, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString("Hormat kami,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 420, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString(_data.signs[0].signName, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 12, 500, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString("Menerima Tugas,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 400, 420, PdfHelpers.TextAlignLeft);
			page.Graphics.DrawString(_data.signs[1].signName, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 400, 500, PdfHelpers.TextAlignLeft);

			//Add the footer template at the bottom.
			doc.Template.Bottom = await PdfHelpers.CreateFooterPagination(doc, "Page {0} of {1}");

			MemoryStream stream = new MemoryStream();

			//Save the PDF document 
			doc.Save(stream);

			//Close the PDF document
			doc.Close(true);

			stream.Position = 0;
			return stream.ToArray();
		}

		private async Task<DataTable> CreateListBidang()
		{
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("NO.", typeof(string));
            dataTable.Columns.Add("PROJECT", typeof(string));
            dataTable.Columns.Add("DESA", typeof(string));
            dataTable.Columns.Add("JUMLAH BIDANG", typeof(string));
            dataTable.Columns.Add("LUAS SURAT M2", typeof(string));

            dataTable.Rows.Add("1.", _data.project, _data.desa, _data.jumlahBidang.ToString(), $"{_data.luasSurat:#,##0}");
            dataTable.Rows.Add("", "", "Total", _data.jumlahBidang.ToString(), $"{_data.luasSurat:#,##0}");

            return dataTable;
        }
    }
}

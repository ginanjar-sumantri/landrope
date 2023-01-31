using System;
using System.Linq;
using System.Threading.Tasks;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.IO;
using Syncfusion.Pdf.Grid;
using landrope.common;
using System.Data;
using Syncfusion.Drawing;
using System.Globalization;
using System.Drawing.Printing;

namespace SyncfusionTemplates.PdfTemplates.PraPembebasan
{
    public class MeasuringRequestPdfTemplate
    {

        MeasurementRequestFormView _data;
        

        public MeasuringRequestPdfTemplate(MeasurementRequestFormView data)
        {
            _data = data;
        }

        public async Task<string> Generate()
        {
            try
            {
                byte[] result;
                result = await CreatePdf();
                return Convert.ToBase64String(result);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        async Task<byte[]> CreatePdf()
        {
            PdfDocument doc = new PdfDocument();
            //doc.PageSettings.Orientation = PdfPageOrientation.Landscape;
            doc.PageSettings.Size = PdfPageSize.A4;
            //doc.PageSettings.Size = new SizeF(2480, 3296);

            PdfPage page = doc.Pages.Add();

            page.Graphics.DrawString("FORM PERMINTAAN PENGUKURAN DAN PERMINTAAN PETLOK", PdfHelpers.SmallBoldFont, PdfHelpers.BlackBrush, 250, 10, PdfHelpers.TextAlignCenter);
            page.Graphics.DrawString("Tanggal :", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 45, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString($"Alasan Pengajuan : {_data.Reason}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 60, PdfHelpers.TextAlignLeft);

            #region PdfGridBuiltinStyleSettings
            PdfGridBuiltinStyleSettings setting = new PdfGridBuiltinStyleSettings();
            setting.ApplyStyleForHeaderRow = true;
            //setting.ApplyStyleForHeaderRow = Header != null ? true : false;
            //setting.ApplyStyleForBandedRows = Bandedrow != null ? true : false;
            //setting.ApplyStyleForBandedColumns = Bandedcolumn != null ? true : false;
            //setting.ApplyStyleForFirstColumn = Firstcolumn != null ? true : false;
            //setting.ApplyStyleForLastColumn = Lastcolumn != null ? true : false;
            //setting.ApplyStyleForLastRow = Lastrow != null ? true : false;
            #endregion

            //Set layout properties
            PdfLayoutFormat format = new PdfLayoutFormat();
            format.Break = PdfLayoutBreakType.FitPage;
            format.Layout = PdfLayoutType.Paginate;

            //Specify the style for PdfGridcell 
            PdfGridCellStyle headerstyle = new PdfGridCellStyle();
            headerstyle.Font = PdfHelpers.SmallBoldFont;
            headerstyle.StringFormat = PdfHelpers.TextAlignCenter;

            PdfGridBuiltinStyle style = (PdfGridBuiltinStyle)Enum.Parse(typeof(PdfGridBuiltinStyle), "TableGrid");

            PdfGrid grid = new PdfGrid();
            grid.DataSource = GetDataTable();
            grid.ApplyBuiltinStyle(style, setting);
            grid.Style.Font = PdfHelpers.SmallestFont;
            grid.Style.CellPadding = new PdfPaddings(2, 2, 2, 2);
            grid.RepeatHeader = true;
            grid.Headers.ApplyStyle(headerstyle);

            // Set Column Alignment
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 0 }.Contains(i))
                    grid.Columns[i].Format = PdfHelpers.TextAlignCenter;
                else
                    grid.Columns[i].Format = PdfHelpers.TextAlignLeft;

            // Set Column Width
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 0 }.Contains(i))
                    grid.Columns[i].Width = 20;
                else if (new int[] { 1, 4, 5 }.Contains(i))
                    grid.Columns[i].Width = 80;
                else if (new int[] { 2, 3 }.Contains(i))
                    grid.Columns[i].Width = 130;
                else
                    grid.Columns[i].Width = 70;

            //Draw table
            PdfLayoutResult result = grid.Draw(page, 0, 80, format);

            //Draw Footer
            DrawFooter(result);

            MemoryStream stream = new MemoryStream();

            //Save the PDF document 
            doc.Save(stream);

            //Close the PDF document
            doc.Close(true);

            stream.Position = 0;

            return stream.ToArray();
        }

        DataTable GetDataTable()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("No", typeof(string));
            dataTable.Columns.Add("Nomor Request", typeof(string));
            dataTable.Columns.Add("Pemilik", typeof(string));
            dataTable.Columns.Add("Alas Hak", typeof(string));
            dataTable.Columns.Add("Desa", typeof(string));
            dataTable.Columns.Add("Grup", typeof(string));

            int i = 1;
            foreach (var x in _data.table)
            {
                dataTable.Rows.Add($"{i}.", x.nomorRequest, x.pemilik, x.alasHak, x.desa, x.group);
                i++;
            };

            return dataTable;
        }

        void DrawFooter(PdfLayoutResult res)
        {
            PdfPage _page = res.Page;

            float signRow = res.Bounds.Bottom + 30;
            if ((_page.Size.Height - signRow) < 60)
            {
                _page = _page.Section.Pages.Add();
                signRow = 15;
            }

            _page.Graphics.DrawString("Pemohon,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, signRow, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(_data.requestor != null ? _data.requestor.ToUpper() : ". . . . . . . . . . . .", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, signRow + 60, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString("Mengetahui,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 360, signRow, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(_data.mengetahui != null ? _data.mengetahui.ToUpper() : ". . . . . . . . . . . .", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 360, signRow + 60, PdfHelpers.TextAlignLeft);
        }
    }
}

using landrope.common;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Lists;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SyncfusionTemplates.PdfTemplates.DocumentBundle
{
    public class BundleDetailReportPdfTemplate
    {
        private List<RegisteredDocView> _data { get; set; }
        private byte[] bytesData { get; set; }
        private string _idBidang {get; set;}

        public BundleDetailReportPdfTemplate(string idBidang, List<RegisteredDocView> data)
        {
            _data = data;
            _idBidang = idBidang;
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

            PdfPage page = doc.Pages.Add();

            //Add the header at the top.
            doc.Template.Top = await PdfHelpers.CreateHeaderPagination(doc, "{0}/{1}");

            PdfLayoutResult resTable = await DrawTable(page);

            float boundsEndTable = resTable.Bounds.Bottom;
            PdfPage _page = resTable.Page;

            float signWord = resTable.Bounds.Bottom + 40;
            if ((_page.Size.Height - signWord) < 120)
            {
                _page = _page.Section.Pages.Add();
                signWord = 20;
            }

            _page.Graphics.DrawString("Tanggal,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 20, signWord - 15, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString("Dibuat Oleh", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 20, signWord, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(". . . . . . . . . . . . . . .", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 20, signWord + 60, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString("Diserahkan Oleh", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 300, signWord, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(". . . . . . . . . . . . . . .", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 300, signWord + 60, PdfHelpers.TextAlignLeft);

            MemoryStream stream = new MemoryStream();

            //Save the PDF document 
            doc.Save(stream);

            //Close the PDF document
            doc.Close(true);

            stream.Position = 0;
            return stream.ToArray();
        }

        private async Task<PdfLayoutResult> DrawTable(PdfPage page)
        {
            //Set layout properties
            PdfLayoutFormat format = new PdfLayoutFormat();
            format.Break = PdfLayoutBreakType.FitPage;
            format.Layout = PdfLayoutType.Paginate;

            PdfGrid grid = new PdfGrid();
            //grid.DataSource = await CreateListBidang();
            grid.Style.Font = PdfHelpers.SmallestFont;
            grid.Style.CellPadding = new PdfPaddings(2, 2, 2, 2);
            grid.Columns.Add(6);

            // Set Column Alignment
            for (int i = 0; i < grid.Columns.Count; i++)
            //     if (new int[] { }.Contains(i))
            //         grid.Columns[i].Format = PdfHelpers.TextAlignRight;
            //     else if (new int[] { 1, 2}.Contains(i))
                    grid.Columns[i].Format = PdfHelpers.TextAlignLeft;
            //     else
            //         grid.Columns[i].Format = PdfHelpers.TextAlignCenter;

            // Set Column Width
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 0 }.Contains(i))
                    grid.Columns[i].Width = 20;
                // else if (new int[] { 1, 3, 5 }.Contains(i))
                //     grid.Columns[i].Width = 105;
                // else if (new int[] { 6, 7 }.Contains(i))
                //     grid.Columns[i].Width = 40;
                else if (new int[] { 2 }.Contains(i)) // Meta Data
                    grid.Columns[i].Width = 160;
                else if (new int[] { 3, 4, 5 }.Contains(i)) // scan, asli, copy
                    grid.Columns[i].Width = 35;
                // else
                //     grid.Columns[i].Width = 50;

            PdfGridRow headerRow = grid.Rows.Add();
            headerRow.Cells[0].Value = "REPORT DETAIL BUNDLES";
            headerRow.Cells[0].ColumnSpan = 6;
            headerRow.Cells[0].Style.Font = PdfHelpers.NormalBoldFont;
            headerRow.Cells[0].StringFormat = PdfHelpers.TextAlignCenter;

            // Bidang
            PdfGridRow bidangRow = grid.Rows.Add();
            bidangRow.Cells[0].Value = $"ID BIDANG : {_idBidang}";
            bidangRow.Cells[0].ColumnSpan = 6;
            bidangRow.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;

            // header Doc
            PdfGridRow headerDocRow = grid.Rows.Add();
            headerDocRow.Cells[0].Value = "No.";
            headerDocRow.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;
            headerDocRow.Cells[1].Value = "JENIS DOKUMEN";
            headerDocRow.Cells[1].StringFormat = PdfHelpers.TextAlignLeft;
            headerDocRow.Cells[2].Value = "METADATA";
            headerDocRow.Cells[2].StringFormat = PdfHelpers.TextAlignCenter;
            headerDocRow.Cells[3].Value = "SCAN";
            headerDocRow.Cells[3].StringFormat = PdfHelpers.TextAlignCenter;
            headerDocRow.Cells[4].Value = "ASLI";
            headerDocRow.Cells[4].StringFormat = PdfHelpers.TextAlignCenter;
            headerDocRow.Cells[5].Value = "COPY";
            headerDocRow.Cells[5].StringFormat = PdfHelpers.TextAlignCenter;

            var regenerateData = _data.GroupBy( g => g.keyDocType)
                        .ToDictionary( x => x.Key, y => y.Select( s => s).ToList());

            int n = 1;
            foreach(KeyValuePair<string, List<RegisteredDocView>> docs in regenerateData)
            {
                int rowspan = docs.Value.Count();
                int index = 0;
                foreach(var doc in docs.Value)
                {
                    PdfGridRow row = grid.Rows.Add();
                    row.Cells[0].Value = n.ToString();
                    row.Cells[0].StringFormat = PdfHelpers.TextAlignCenter;

                    row.Cells[1].Value = doc.docType;
                    row.Cells[1].StringFormat = PdfHelpers.TextAlignLeft;
                    
                    //metadata
                    row.Cells[2].Value = doc.properties;
                    row.Cells[2].StringFormat = PdfHelpers.TextAlignLeft;
                    row.Cells[2].Style.Font = new PdfStandardFont(PdfFontFamily.TimesRoman, 8);

                    // scan
                    row.Cells[3].Value = doc.Soft_Copy ? "V" : "";
                    row.Cells[3].StringFormat = PdfHelpers.TextAlignCenter;

                    //asli
                    row.Cells[4].Value = doc.Asli ? "V" : "";
                    row.Cells[4].StringFormat = PdfHelpers.TextAlignCenter;

                    //copy
                    row.Cells[5].Value = doc.Copy > 0 ? doc.Copy.ToString() : "";
                    row.Cells[5].StringFormat = PdfHelpers.TextAlignCenter;

                    if(rowspan > 1 && index == 0)
                    {
                        row.Cells[0].RowSpan = rowspan;
                        row.Cells[1].RowSpan = rowspan;
                        row.Cells[3].RowSpan = rowspan;
                    }

                    index++;
                }
                n++;
            }

            //Specify the style for PdfGridcell 
            PdfGridCellStyle headerstyle = new PdfGridCellStyle();
            headerstyle.Font = PdfHelpers.SmallBoldFont;
            headerstyle.StringFormat = PdfHelpers.TextAlignCenter;
            grid.Headers.ApplyStyle(headerstyle);
            grid.RepeatHeader = true;

            //Draw table
            PdfLayoutResult result = grid.Draw(page, 0, 0, format);
            return result;
        }
    }
}

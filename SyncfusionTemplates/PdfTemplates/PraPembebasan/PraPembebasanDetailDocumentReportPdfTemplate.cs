using landrope.common;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Tables;
using Syncfusion.Drawing;

namespace SyncfusionTemplates.PdfTemplates.PraPembebasan
{
    public class PraPembebasanDetailDocumentReportPdfTemplate
    {
        IEnumerable<PraBebasDetailDocumentReportViewModel> _data { get; set; }
        PraBebasDetailDocumentReportViewModel dataTmp { get; set; }

        public PraPembebasanDetailDocumentReportPdfTemplate(IEnumerable<PraBebasDetailDocumentReportViewModel> data)
        {
            _data = data;
        }

        public async Task<byte[]> Generate()
        {
            MemoryStream[] streams = new MemoryStream[_data.Count()];

            int i = 0;
            foreach (var x in _data)
            {
                dataTmp = x;
                streams[i] = await CreatePdf();
                i++;
            }

            MemoryStream streamResult = await MergeDocuments(streams);

            return streamResult.ToArray();
        }

        async Task<MemoryStream> CreatePdf()
        {
            PdfDocument doc = new PdfDocument();
            doc.PageSettings.Size = PdfPageSize.A4;

            PdfPage page = doc.Pages.Add();

            //Add the header at the top.
            doc.Template.Top = await PdfHelpers.CreateHeaderPagination(doc, "{0}/{1}");

            PdfLayoutResult resTable = await DrawTable(page);

            CreateFooter(resTable);

            MemoryStream stream = new MemoryStream();
            doc.Save(stream);
            doc.Close(true);
            stream.Position = 0;

            return stream;
        }

        async Task<PdfLayoutResult> DrawTable(PdfPage page)
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
                grid.Columns[i].Format = PdfHelpers.TextAlignLeft;

            // Set Column Width
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 0 }.Contains(i))
                    grid.Columns[i].Width = 20;
                else if (new int[] { 2 }.Contains(i)) // Meta Data
                    grid.Columns[i].Width = 160;
                else if (new int[] { 3, 4, 5 }.Contains(i)) // scan, asli, copy
                    grid.Columns[i].Width = 35;

            PdfGridRow headerRow = grid.Rows.Add();
            headerRow.Cells[0].Value = "REPORT DETAIL BUNDLES";
            headerRow.Cells[0].ColumnSpan = 6;
            headerRow.Cells[0].Style.Font = PdfHelpers.NormalBoldFont;
            headerRow.Cells[0].StringFormat = PdfHelpers.TextAlignCenter;

            PdfGridCellStyle lStyle = new PdfGridCellStyle();
            lStyle.Borders.Top.Color = new PdfColor(Color.Transparent);
            lStyle.Borders.Bottom.Color = new PdfColor(Color.Transparent);
            lStyle.Borders.Right.Color = new PdfColor(Color.Transparent);
            PdfGridCellStyle rStyle = new PdfGridCellStyle();
            rStyle.Borders.Top.Color = new PdfColor(Color.Transparent);
            rStyle.Borders.Bottom.Color = new PdfColor(Color.Transparent);
            rStyle.Borders.Left.Color = new PdfColor(Color.Transparent);

            // Bidang
            PdfGridRow row1 = grid.Rows.Add();
            row1.Cells[0].Value = $"ID BIDANG : {dataTmp.IdBidang}";
            row1.Cells[0].ColumnSpan = 2;
            row1.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;
            row1.Cells[0].Style = lStyle;
            row1.Cells[2].Value = $"GROUP : {dataTmp.Group}";
            row1.Cells[2].ColumnSpan = 4;
            row1.Cells[2].StringFormat = PdfHelpers.TextAlignLeft;
            row1.Cells[2].Style = rStyle;
            PdfGridRow row2 = grid.Rows.Add();
            row2.Cells[0].Value = $"ALAS HAK : {dataTmp.AlasHak}";
            row2.Cells[0].ColumnSpan = 2;
            row2.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;
            row2.Cells[0].Style = lStyle;
            row2.Cells[2].Value = $"PROJECT : {dataTmp.Project}";
            row2.Cells[2].ColumnSpan = 4;
            row2.Cells[2].StringFormat = PdfHelpers.TextAlignLeft;
            row2.Cells[2].Style = rStyle;
            PdfGridRow row3 = grid.Rows.Add();
            row3.Cells[0].Value = $"LUAS : {dataTmp.LuasSurat}";
            row3.Cells[0].ColumnSpan = 2;
            row3.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;
            row3.Cells[0].Style = lStyle;
            row3.Cells[2].Value = $"DESA : {dataTmp.Desa}";
            row3.Cells[2].ColumnSpan = 4;
            row3.Cells[2].StringFormat = PdfHelpers.TextAlignLeft;
            row3.Cells[2].Style = rStyle;
            PdfGridRow row4 = grid.Rows.Add();
            row4.Cells[0].Value = $"PEMILIK : {dataTmp.Pemilik}";
            row4.Cells[0].ColumnSpan = 2;
            row4.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;
            row4.Cells[0].Style = lStyle;
            row4.Cells[2].ColumnSpan = 4;
            row4.Cells[2].StringFormat = PdfHelpers.TextAlignLeft;
            row4.Cells[2].Style = rStyle;
            PdfGridRow row5 = grid.Rows.Add();
            row5.Cells[0].Value = $"ALIAS : {dataTmp.Alias}";
            row5.Cells[0].ColumnSpan = 2;
            row5.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;
            row5.Cells[0].Style = lStyle;
            row5.Cells[2].ColumnSpan = 4;
            row5.Cells[2].StringFormat = PdfHelpers.TextAlignLeft;
            row5.Cells[2].Style = rStyle;

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

            var regenerateData = dataTmp.Docs.GroupBy(g => g.keyDocType)
                        .ToDictionary(x => x.Key, y => y.Select(s => s).ToList());

            int n = 1;
            foreach (var docs in regenerateData)
            {
                int rowspan = docs.Value.Count();
                int index = 0;
                foreach (var doc in docs.Value)
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

                    if (rowspan > 1 && index == 0)
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

        void CreateFooter(PdfLayoutResult resTable)
        {
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
        }

        async Task<MemoryStream> MergeDocuments(MemoryStream[] streams) {
            PdfDocument doc = new PdfDocument();

            PdfDocumentBase.Merge(doc, streams);

            MemoryStream stream = new MemoryStream();
            doc.Save(stream);
            doc.Close(true);
            stream.Position = 0;

            return stream;
        }
    }
}

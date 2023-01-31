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
using System.Threading.Tasks;

namespace SyncfusionTemplates.PdfTemplates.DocumentBundle
{
    public class ReceiptNotaryReportPdfTemplate
    {
        private List<RptTandaTerimaNotaris> _data { get; set; }
        private byte[] bytesData { get; set; }

        public ReceiptNotaryReportPdfTemplate(List<RptTandaTerimaNotaris> data)
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
            grid.Columns.Add(8);

            // Set Column Alignment
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { }.Contains(i))
                    grid.Columns[i].Format = PdfHelpers.TextAlignRight;
                else if (new int[] { 1, 2, 3, 4, 5 }.Contains(i))
                    grid.Columns[i].Format = PdfHelpers.TextAlignLeft;
                else
                    grid.Columns[i].Format = PdfHelpers.TextAlignCenter;

            // Set Column Width
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 0 }.Contains(i))
                    grid.Columns[i].Width = 20;
                else if (new int[] { 1, 3, 5 }.Contains(i))
                    grid.Columns[i].Width = 105;
                else if (new int[] { 6, 7 }.Contains(i))
                    grid.Columns[i].Width = 40;
                else
                    grid.Columns[i].Width = 50;

            PdfGridRow headerRow = grid.Rows.Add();
            headerRow.Cells[0].Value = "TANDA TERIMA";
            headerRow.Cells[0].ColumnSpan = 8;
            headerRow.Cells[0].Style.Font = PdfHelpers.NormalBoldFont;

            RptTandaTerimaNotaris firstData = _data.FirstOrDefault();
            _data.RemoveAt(0);
            PdfGridRow rowIdBidang = grid.Rows.Add();
            rowIdBidang.Cells[0].Value = firstData.Identity.ToUpper();
            rowIdBidang.Cells[0].ColumnSpan = 2;
            rowIdBidang.Cells[0].Style.Font = PdfHelpers.SmallBoldFont;
            rowIdBidang.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;
            rowIdBidang.Cells[2].Value = firstData.Details[0].value.ToUpper();
            rowIdBidang.Cells[2].ColumnSpan = 6;

            PdfGridRow rowChecklist = grid.Rows.Add();
            rowChecklist.Cells[0].ColumnSpan = 6;
            rowChecklist.Cells[6].Value = "Asli";
            rowChecklist.Cells[6].Style.Font = PdfHelpers.SmallBoldFont;
            rowChecklist.Cells[7].Value = "Copy";
            rowChecklist.Cells[7].Style.Font = PdfHelpers.SmallBoldFont;

            int n = 1;
            foreach (RptTandaTerimaNotaris x in _data)
            {
                PdfGridRow row = grid.Rows.Add();
                row.Cells[0].Value = x.Identity;
                row.Cells[0].ColumnSpan = 8;
                row.Cells[0].Style.Font = PdfHelpers.SmallBoldFont;
                row.Cells[0].StringFormat = PdfHelpers.TextAlignLeft;

                foreach (ReportDetail y in x.Details)
                {
                    PdfGridRow subRow1 = grid.Rows.Add();
                    PdfGridRow subRow2 = null;
                    subRow1.Cells[0].Value = $"{n}";
                    subRow1.Cells[1].Value = y.Identity;

                    if(new[] { "kartu keluarga", "akta nikah" }.Any(f => y.Identity.ToLower().StartsWith(f)))
                    {
                        var test = y.Details.ToList();
                        test.Insert(1, new ReportDetail("", ""));
                        y.Details = test.ToArray();
                    }

                    if (y.Details.Length > 3)
                    {
                        if (y.Details.Length.Equals(6))
                        {
                            subRow2 = grid.Rows.Add();
                            subRow1.Cells[0].RowSpan = 2;
                            subRow1.Cells[1].RowSpan = 2;
                            subRow1.Cells[6].RowSpan = 2;
                            subRow1.Cells[7].RowSpan = 2;
                        }

                        bool rowEnd = false;
                        for(int c = 0; c < y.Details.Length - 2; c++)
                        //foreach (ReportDetail z in y.Details)
                        {
                            if ((c % 2).Equals(0)) {
                                if (subRow2 != null && rowEnd)
                                {
                                    subRow2.Cells[2].Value = y.Details[c].Identity;
                                    subRow2.Cells[3].Value = y.Details[c].value;
                                }
                                else
                                {
                                    subRow1.Cells[2].Value = y.Details[c].Identity;
                                    subRow1.Cells[3].Value = y.Details[c].value;
                                }
                                rowEnd = false;
                            }
                            else
                            {
                                if (subRow2 != null && c > 1)
                                {
                                    subRow2.Cells[4].Value = y.Details[c].Identity;
                                    subRow2.Cells[5].Value = y.Details[c].value;
                                }
                                else
                                {
                                    subRow1.Cells[4].Value = y.Details[c].Identity;
                                    subRow1.Cells[5].Value = y.Details[c].value;
                                }
                                rowEnd = true;
                            }
                        }
                    }
                    else
                    {
                        row.Cells[1].ColumnSpan = 3;

                        for (int c = 0; c < y.Details.Length - 2; c++)
                        {
                            subRow1.Cells[4].Value = y.Details[c].Identity;
                            subRow1.Cells[5].Value = y.Details[c].value;
                        }
                    }

                    subRow1.Cells[6].Style.Font = PdfHelpers.SmallBoldItalicFont;
                    subRow1.Cells[6].Value = bool.Parse(y.Details[y.Details.Length - 2].value) ? "V" : "";
                    subRow1.Cells[7].Style.Font = PdfHelpers.SmallBoldItalicFont;
                    subRow1.Cells[7].Value = bool.Parse(y.Details[y.Details.Length - 1].value) ? "V" : "";
                    n++;
                }
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

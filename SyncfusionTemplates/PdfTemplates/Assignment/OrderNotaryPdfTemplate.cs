using landrope.common;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf.Lists;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SyncfusionTemplates.PdfTemplates.Assignment
{
    public class OrderNotaryPdfTemplate
    {
        private RptOrderNotary _data { get; set; }
        private byte[] bytesData { get; set; }

        public OrderNotaryPdfTemplate(RptOrderNotary data)
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
            if (_data.lampiran.Length > 24)
                doc.PageSettings.Margins.Bottom = 70;

            PdfPage page = doc.Pages.Add();

            //Add the header at the top.
            doc.Template.Top = await PdfHelpers.CreateLetterHeadWithPaging(doc, "{0}/{1}");

            page.Graphics.DrawString("SURAT ORDER NOTARIS", PdfHelpers.LargeBoldFont, PdfHelpers.BlackBrush, 260, 10, PdfHelpers.TextAlignCenter);
            page.Graphics.DrawString(_data.pengirim, PdfHelpers.SmallBoldFont, PdfHelpers.BlackBrush, 0, 50, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString("Nomor", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 63, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString($": {_data.nomorPenugasan}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 50, 63, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString("Tanggal", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 76, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString($": {_data.tanggalPenugasan.GetValueOrDefault().ToString("dd MMMM yyyy", new CultureInfo("id-ID"))}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 50, 76, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString("Perihal", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 89, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString($": {_data.perihal}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 50, 89, PdfHelpers.TextAlignLeft);

            page.Graphics.DrawString($"Kepada Yth,{Environment.NewLine}" +
                $"Notaris/PPAT{Environment.NewLine}" +
                $"{_data.notaris}{Environment.NewLine}" +
                $"Di tempat", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 140, PdfHelpers.TextAlignLeft);

            page.Graphics.DrawString($"Mohon agar disiapkan sesuai denegan data-data terlampir :{Environment.NewLine}" +
                $"Desa : {_data.desa}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 190, PdfHelpers.TextAlignLeft);

            PdfLayoutResult resTable = await DrawTable(page);

            List<string> notes = new List<string> { { "Akta Jual Beli berdasarkan Akta PPJB." }, { "Target penyelesaian Akta Jual Beli 7 (tujuh) hari sejak penyerahan Validasi Pajak." } };

            PdfOrderedMarker list = new PdfOrderedMarker(PdfNumberStyle.Numeric, PdfHelpers.SmallFont);
            //Create Ordered list as sublist of parent list
            PdfOrderedList subList = new PdfOrderedList(PdfHelpers.SmallFont);
            subList.Marker = list;
            //Add items to the list
            float boundBotOrderedList = 0;
            foreach (string x in notes)
            {
                subList.Items.Add(x);
                boundBotOrderedList += 15;
            }

            float boundsEndTable = resTable.Bounds.Bottom;
            Console.WriteLine(resTable.Page.Size.Height - boundsEndTable);
            PdfPage _pageList = resTable.Page;
            float boundsBotOrderedList = resTable.Bounds.Bottom + 15;
            if ((resTable.Page.Size.Height - boundsEndTable) < 180)
            {
                _pageList = resTable.Page.Section.Pages.Add();
                boundsBotOrderedList = 10;
            }

            //Draw list
            PdfLayoutResult resList = subList.Draw(_pageList, 30, boundsBotOrderedList);
            resList.Page.Graphics.DrawString("Note :", PdfHelpers.SmallBoldFont, PdfHelpers.BlackBrush, 0, resList.Bounds.Top + 5, PdfHelpers.TextAlignLeft);

            PdfPage _page = resList.Page;
            float endWord = resList.Bounds.Bottom + boundBotOrderedList + 20;
            if ((_page.Size.Height - endWord) < 235)
            {
                _page = _page.Section.Pages.Add();
                endWord = 10;
            }

            _page.Graphics.DrawString("Demikian surat order ini disampaikan, terima kasih atas perhatian dan kerjasama yang telah terjalin baik selama ini.", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, endWord, PdfHelpers.TextAlignLeft);
            float signWord = endWord + 30;
            _page.Graphics.DrawString("Dibuat oleh,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, signWord, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(_data.signs[0].signName, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, signWord + 60, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString("Diketahui oleh,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 130, signWord, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(_data.signs[1].signName, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 130, signWord + 60, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString("Disetujui oleh,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 260, signWord, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(_data.signs[2].signName, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 260, signWord + 60, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString("Diterima oleh,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 400, signWord, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(_data.signs[3].signName, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 400, signWord + 60, PdfHelpers.TextAlignLeft);

            //Add the footer template at the bottom.
            //doc.Template.Bottom = await PdfHelpers.CreateFooterPagination(doc, "Page {0} of {1}");

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
            grid.Columns.Add(9);

            // Set Column Alignment
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 4, 6, 8 }.Contains(i))
                    grid.Columns[i].Format = PdfHelpers.TextAlignRight;
                else if (new int[] { 1 }.Contains(i))
                    grid.Columns[i].Format = PdfHelpers.TextAlignLeft;
                else
                    grid.Columns[i].Format = PdfHelpers.TextAlignCenter;

            // Set Column Width
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 0 }.Contains(i))
                    grid.Columns[i].Width = 20;
                else if (new int[] { 5, 7 }.Contains(i))
                    grid.Columns[i].Width = 15;
                else if (new int[] { 1 }.Contains(i))
                    grid.Columns[i].Width = 160;
                else if (new int[] { 8 }.Contains(i))
                    grid.Columns[i].Width = 80;
                else if (new int[] { 4, 6 }.Contains(i))
                    grid.Columns[i].Width = 60;
                else
                    grid.Columns[i].Width = 50;

            PdfGridRow[] headerRow = grid.Headers.Add(2);
            headerRow[0].Cells[0].RowSpan = 2;
            headerRow[0].Cells[0].Value = "No";
            headerRow[0].Cells[1].RowSpan = 2;
            headerRow[0].Cells[1].Value = "Nama";
            headerRow[0].Cells[2].ColumnSpan = 3;
            headerRow[0].Cells[2].Value = "Obyek Transaksi Jual Beli";
            headerRow[0].Cells[5].RowSpan = 2;
            headerRow[0].Cells[5].ColumnSpan = 2;
            headerRow[0].Cells[5].Value = "Harga Jual/M2";
            headerRow[0].Cells[7].RowSpan = 2;
            headerRow[0].Cells[7].ColumnSpan = 2;
            headerRow[0].Cells[7].Value = "Nilai Transaksi";
            headerRow[1].Cells[2].Value = "EX.SHM";
            headerRow[1].Cells[3].Value = "No.SHGB";
            headerRow[1].Cells[4].Value = "Luas (M2)";

            int n = 1;
            foreach (lampiranSurat x in _data.lampiran)
            {
                PdfGridRow row = grid.Rows.Add();
                row.Cells[0].Value = $"{n}";
                row.Cells[1].Value = x.namaPemilik;
                row.Cells[2].Value = x.exSHM;
                row.Cells[3].Value = x.noShgb;
                row.Cells[4].Value = $"{x.luas:#,##0}";
                row.Cells[5].Value = "Rp";
                row.Cells[5].Style.Borders.Right = PdfPens.Transparent;
                row.Cells[6].Value = $"{x.hargaJual:#,##0}";
                row.Cells[6].Style.Borders.Left = PdfPens.Transparent;
                row.Cells[7].Value = "Rp";
                row.Cells[7].Style.Borders.Right = PdfPens.Transparent;
                row.Cells[8].Value = $"{x.nilaiTransaksi:#,##0}";
                row.Cells[8].Style.Borders.Left = PdfPens.Transparent;
                n++;
            }

            PdfGridRow rowTotal = grid.Rows.Add();
            rowTotal.Cells[0].ColumnSpan = 4;
            rowTotal.Cells[5].ColumnSpan = 2;
            rowTotal.Cells[4].Value = $"{_data.lampiran.Sum(x => x.luas):#,##0}";
            rowTotal.Cells[4].Style.Font = PdfHelpers.SmallestBoldFont;
            rowTotal.Cells[7].Value = "Rp";
            rowTotal.Cells[7].Style.Borders.Right = PdfPens.Transparent;
            rowTotal.Cells[8].Value = $"{_data.lampiran.Sum(x => x.nilaiTransaksi):#,##0}";
            rowTotal.Cells[8].Style.Font = PdfHelpers.SmallestBoldFont;
            rowTotal.Cells[8].Style.Borders.Left = PdfPens.Transparent;

            //Specify the style for PdfGridcell 
            PdfGridCellStyle headerstyle = new PdfGridCellStyle();
            headerstyle.Font = PdfHelpers.SmallBoldFont;
            headerstyle.StringFormat = PdfHelpers.TextAlignCenter;
            grid.Headers.ApplyStyle(headerstyle);
            grid.RepeatHeader = true;

            //Draw table
            PdfLayoutResult result = grid.Draw(page, 0, 220, format);
            return result;
        }
    }
}

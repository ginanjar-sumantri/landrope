using System;
using System.Linq;
using System.Threading.Tasks;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.IO;
using Syncfusion.Pdf.Grid;
using landrope.common;
using System.Data;
using System.Globalization;
using Syncfusion.Drawing;

namespace SyncfusionTemplates.PdfTemplates.PaymentRequest
{
    public class MemoPelunasanPdfTemplate
    {
        private MemoPembayaranView _data { get; set; }

        public MemoPelunasanPdfTemplate(MemoPembayaranView data)
        {
            _data = data;
        }

        public async Task<string> Generate()
        {
            try
            {
                byte[] data = await CreatePdf();

                return Convert.ToBase64String(data);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task<byte[]> CreatePdf()
        {
            byte[] bytes;
            PdfDocument doc = new PdfDocument();
            doc.PageSettings.Orientation = PdfPageOrientation.Landscape;
            doc.PageSettings.Size = PdfPageSize.A4;
            //doc.PageSettings.Size = new SizeF(2480, 3296);

            PdfPage page = doc.Pages.Add();

            page.Graphics.DrawString($"Jakarta, {DateTime.UtcNow.AddHours(7).ToString("dd MMMM yyyy", new CultureInfo("id-ID"))}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 760, 10, PdfHelpers.TextAlignRight);
            page.Graphics.DrawString($"No. {_data.NoMemo}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 10, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString($"Kepada Yth. {_data.Kepada}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, 25, PdfHelpers.TextAlignLeft);
            page.Graphics.DrawString(_data.TahapProject, PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 760, 42, PdfHelpers.TextAlignRight);

            //page.Graphics.DrawRectangle(PdfHelpers.YellowBrush, 399, 30, 232, 20);
            //page.Graphics.DrawString("OVERLAP (BINTANG/DAMAI)", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 520, 42, PdfHelpers.TextAlignCenter);

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
            format.Break = PdfLayoutBreakType.FitElement;
            format.Layout = PdfLayoutType.Paginate;

            //Specify the style for PdfGridcell 
            PdfGridCellStyle headerstyle = new PdfGridCellStyle();
            headerstyle.Font = PdfHelpers.SmallFont;
            headerstyle.StringFormat = PdfHelpers.TextAlignCenter;

            PdfGridBuiltinStyle style = (PdfGridBuiltinStyle)Enum.Parse(typeof(PdfGridBuiltinStyle), "GridTable4");

            PdfGrid grid = new PdfGrid();
            grid.DataSource = await DetailsPembayaranDataTable();
            grid.ApplyBuiltinStyle(style, setting);
            grid.Style.Font = PdfHelpers.SmallestFont;
            grid.Style.CellPadding = new PdfPaddings(2, 2, 2, 2);
            grid.Headers.ApplyStyle(headerstyle);

            // Set Column Alignment
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 7, 8, 9, 10, 12, 13 }.Contains(i))
                    grid.Columns[i].Format = PdfHelpers.TextAlignRight;
                else if (new int[] { 2 }.Contains(i))
                    grid.Columns[i].Format = PdfHelpers.TextAlignLeft;
                else
                    grid.Columns[i].Format = PdfHelpers.TextAlignCenter;

            // Set Column Width
            for (int i = 0; i < grid.Columns.Count; i++)
                if (new int[] { 0 }.Contains(i))
                    grid.Columns[i].Width = 20;
                else if (new int[] { 6, 12 }.Contains(i))
                    grid.Columns[i].Width = 60;
                else if (new int[] { 2, 3, 4, 13 }.Contains(i))
                    grid.Columns[i].Width = 80;
                else if (new int[] { 5, 7, 8, 9, 10 }.Contains(i))
                    grid.Columns[i].Width = 40;
                else
                    grid.Columns[i].Width = 50;

            grid.Rows[0].Cells[2].RowSpan = _data.detailBidangs.Count();
            grid.Rows[0].Cells[2].StringFormat = PdfHelpers.TextAlignCenter;
            grid.Rows[0].Cells[4].RowSpan = _data.detailBidangs.Count();
            grid.Rows[_data.detailBidangs.Count()].Style = new PdfGridRowStyle { Font = PdfHelpers.SmallestBoldFont };
            var row = grid.Rows.Add();
            row.Cells[0].ColumnSpan = grid.Columns.Count;
            row.Cells[0].Style = new PdfGridCellStyle { CellPadding = new PdfPaddings(0, 0, 7, 7) };

            //Add pembayaran ext
            await AddPembayaranSummary(grid);

            //Draw table
            PdfLayoutResult result = grid.Draw(page, 0, 50, format);

            //Draw Footer
            await AddFooterContent(result);

            MemoryStream stream = new MemoryStream();

            //Save the PDF document 
            doc.Save(stream);

            //Close the PDF document
            doc.Close(true);

            stream.Position = 0;

            return stream.ToArray();
        }

        private async Task<DataTable> DetailsPembayaranDataTable()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("No", typeof(string));
            dataTable.Columns.Add("Tanggal", typeof(string));
            dataTable.Columns.Add("Alias", typeof(string));
            dataTable.Columns.Add("Pemilik", typeof(string));
            dataTable.Columns.Add("Lokasi", typeof(string));
            dataTable.Columns.Add("No. Peta", typeof(string));
            dataTable.Columns.Add("Surat Asal", typeof(string));
            dataTable.Columns.Add("Luas Surat", typeof(string));
            dataTable.Columns.Add("Rev. Luas Surat", typeof(string));
            dataTable.Columns.Add("Luas Ukur Internal", typeof(string));
            dataTable.Columns.Add("Luas Bayar", typeof(string));
            dataTable.Columns.Add("ID Bidang", typeof(string));
            dataTable.Columns.Add("Harga", typeof(string));
            dataTable.Columns.Add("Jumlah", typeof(string));

            int i = 1;
            foreach (var x in _data.detailBidangs)
            {
                dataTable.Rows.Add(i.ToString() + ".", "", x.Alias, x.Pemilik, $"{x.Desa} - {x.Project} - {x.Perusahaan}", x.NomorPeta, x.SuratAsal, $"{x.LuasSurat:#,##0}", $"{x.LuasBayar:#,##0}", $"{x.LuasUkurInternal:#,##0}", $"{x.LuasBayar:#,##0}", x.id_bidang, $"{x.Harga:#,##0}", $"{x.Jumlah:#,##0}");
                i++;
            };
            dataTable.Rows.Add("", "", "", "", "", "", "", "", "", "", "", "", "", $"{_data.TotalJumlah:#,##0}");

            return dataTable;
        }

        private async Task AddPembayaranSummary(PdfGrid grid)
        {
            foreach (var x in _data.detailPembayaran.Where(x => x.nilai.GetValueOrDefault() > 0))
            {
                var row = grid.Rows.Add();
                row.Style = new PdfGridRowStyle { Font = PdfHelpers.SmallestBoldFont };
                row.Cells[2].ColumnSpan = 3;

                string date = x.Tanggal.HasValue ? x.Tanggal.GetValueOrDefault().ToString("dd/MM/yyyy", new CultureInfo("id-ID")) : "";
                string beban = x.fglainnya.GetValueOrDefault() ? "(Beban Penjual)" : "(Beban Pembeli)";
                row.Cells[1].Value = date;
                row.Cells[13].Value = $"{x.nilai.GetValueOrDefault():#,##0}";

                string val = x.identity.ToLower();
                switch (val)
                {
                    case "utj":
                        row.Cells[2].Value = x.identity;
                        break;
                    case "pelunasan":
                        row.Cells[2].Value = x.identity;
                        break;
                    default:
                        row.Cells[2].Value = val.StartsWith("dp") ? x.identity : $"{x.identity} {beban}";
                        break;
                }
            }
        }

        private async Task AddFooterContent(PdfLayoutResult res)
        {
            PdfPage _page = res.Page;

            float noteRow = res.Bounds.Bottom + 15;
            _page.Graphics.DrawString($"Note : {_data.Note}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, noteRow, PdfHelpers.TextAlignLeft);

            float aktaRow = noteRow + 15;
            _page.Graphics.DrawString($"Nilai Akte Rp {_data.NilaiAkte:#,##0},-/m2", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, aktaRow, PdfHelpers.TextAlignLeft);

            float rencanaTransaksiRow = aktaRow + 15;
            float areaNeeded = 285 + (40 * _data.Giro.Count());
            if ((_page.Size.Height - aktaRow) < areaNeeded)
            {
                _page = _page.Section.Pages.Add();
                rencanaTransaksiRow = 15;
            }

            string tanggalPenyerahan = _data.TanggalPenyerahan.HasValue ? _data.TanggalPenyerahan.GetValueOrDefault().ToString("dd/MMM/yyyy", new CultureInfo("id-ID")) : "";
            _page.Graphics.DrawString($"Rencana transaksi tanggal {tanggalPenyerahan} di Kantor Notaris {_data.Notaris}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, rencanaTransaksiRow, PdfHelpers.TextAlignLeft);

            float titleGiroRow = rencanaTransaksiRow + 15;
            _page.Graphics.DrawString("Tolong disiapkan :", PdfHelpers.SmallBoldFont, PdfHelpers.BlackBrush, 0, titleGiroRow, PdfHelpers.TextAlignLeft);

            float bodyGiroRow = titleGiroRow + 15;
            int i = 1;
            foreach (var x in _data.Giro)
            {
                res.Page.Graphics.DrawString($"{i}. {x.Jenis.ToString().ToUpper()} Senilai Rp. {x.Nominal:#,##0},- an {x.NamaPenerima.ToUpper()}, {x.BankPenerima} {x.AccountPenerima}", PdfHelpers.NormalBoldFont, PdfHelpers.BlackBrush, 0, bodyGiroRow, PdfHelpers.TextAlignLeft);
                bodyGiroRow += 15; i++;
            }

            //float cpRow = bodyGiroRow + 15;
            _page.Graphics.DrawString($"Bilamana BG & Cek sudah disiapkan tolong agar dapat menghubungi {_data.ContactPerson} di {_data.ContactPersonPhone}", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, bodyGiroRow, PdfHelpers.TextAlignLeft);

            float thanksRow = bodyGiroRow + 15;
            _page.Graphics.DrawString("Terima kasih atas kerjasama yang baik", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, thanksRow, PdfHelpers.TextAlignLeft);

            float signRow = thanksRow + 25;
            _page.Graphics.DrawString("Hormat kami,", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, signRow, PdfHelpers.TextAlignLeft);
            _page.Graphics.DrawString(_data.MemoSigns ?? "", PdfHelpers.SmallFont, PdfHelpers.BlackBrush, 0, signRow + 80, PdfHelpers.TextAlignLeft);
        }
    }
}

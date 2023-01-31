using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using landrope.common;
using System.IO;
using Syncfusion.XlsIO;
using System.Globalization;

namespace SyncfusionTemplates.ExcelTemplates.PaymentRequest
{
    public class MemoPelunasanXlsTemplate
    {
        private MemoPembayaranView _data { get; set; }
        private bool isOverlap;

        public MemoPelunasanXlsTemplate(MemoPembayaranView data)
        {
            _data = data;

            IEnumerable<string> distinct = _data.detailBidangs.Where(x => x.AlasHak != null).Select(x => x.AlasHak).Distinct(StringComparer.OrdinalIgnoreCase);
            //isOverlap = _data.detailBidangs.Count(x => x.AlasHak != null) != distinct.Count();
            isOverlap = (_data.detailBidangs.Count(x => x.AlasHak != null) == 1) ? true 
                        : (_data.detailBidangs.Count(x => x.AlasHak != null) != distinct.Count());
        }

        public async Task<byte[]> Generate()
        {
            try
            {
                ExcelEngine excelEngine = new ExcelEngine();
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;
                IWorkbook workbook = application.Workbooks.Create(3);
                IWorksheet sheet = workbook.Worksheets[0];

                await CreateHeader(sheet);
                if (isOverlap)
                    await CreateOverlapReport(sheet);
                else
                    await CreateNotOverlapReport(sheet);

                MemoryStream result = new MemoryStream();
                workbook.SaveAs(result);
                return result.ToArray();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task CreateHeader(IWorksheet sheet)
        {
            sheet.Range["A1"].Text = $"No.                        {_data.NoMemo}";
            sheet.Range["A2"].Text = $"Kepada Yth. {_data.Kepada  }";
            sheet.Range[isOverlap ? "S1" : "M1"].Text = $"Jakarta, {DateTime.UtcNow.AddHours(7).ToString("dd MMMM yyyy", new CultureInfo("id-ID"))}";
            sheet.Range[isOverlap ? "S1" : "M1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            sheet.Range[isOverlap ? "S3" : "M3"].Text = _data.TahapProject;
            sheet.Range[isOverlap ? "S3" : "M3"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

            if (isOverlap)
            {
                sheet.Range["K3"].Text = "OVERLAP(BINTANG / DAMAI)";
                sheet.Range["K3:P3"].BorderAround();
                sheet.Range["K3"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                sheet.Range["K3"].CellStyle.FillBackground = ExcelKnownColors.Yellow;
                sheet["K3:P3"].Merge();
            }
        }

        private async Task CreateNotOverlapReport(IWorksheet sheet)
        {
            int i = 4;
            int totalDetail = _data.detailBidangs.Count();
            int totalDetailExt = _data.detailPembayaran.Count(x => x.nilai > 0);

            sheet[$"A4:M{i + totalDetail + 2 + totalDetailExt}"].BorderInside();
            sheet[$"A4:M{i + totalDetail + 2 + totalDetailExt}"].BorderAround();
            sheet[$"A4:M{i + totalDetail + 2 + totalDetailExt}"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            sheet[$"A4:M{i + totalDetail + 2 + totalDetailExt}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

            sheet[i, 1].Text = "No.";
            sheet[i, 2].Text = "Tanggal";
            sheet[i, 3].Text = "Alias";
            sheet[i, 4].Text = "Pemilik";
            sheet[i, 5].Text = "Lokasi";
            sheet[i, 6].Text = "No Peta";
            sheet[i, 7].Text = "Surat asal";
            sheet[i, 8].Text = "Luas Surat";
            sheet[i, 9].Text = "Luas Ukur Internal";
            sheet[i, 10].Text = "Luas Bayar";
            sheet[i, 11].Text = "Id Bidang";
            sheet[i, 12].Text = "Harga /m2";
            sheet[i, 13].Text = "Jumlah";

            sheet.SetColumnWidth(1, 3.80);
            sheet.SetColumnWidth(2, 12);
            sheet.SetColumnWidth(3, 14);
            sheet.SetColumnWidth(4, 14);
            sheet.SetColumnWidth(5, 22);
            sheet.SetColumnWidth(7, 16);
            sheet.SetColumnWidth(9, 8.4);
            sheet.SetColumnWidth(11, 14);
            sheet.SetColumnWidth(12, 14);
            sheet.SetColumnWidth(13, 18);
            sheet[$"A{i}:M{i}"].CellStyle.Font.Bold = true;
            i++;

            foreach (DetailBidangs x in _data.detailBidangs)
            {
                sheet[i, 1].Text = $"{i - 4}.";
                //sheet[i, 2].Text = "Tanggal";
                sheet[i, 3].Text = x.Alias;
                sheet[i, 4].Text = x.Pemilik;
                sheet[i, 5].Text = $"{x.Desa} - {x.Project} - {x.Perusahaan}";
                sheet[i, 6].Text = x.NomorPeta;
                sheet[i, 7].Text = x.SuratAsal;
                sheet[i, 8].Number = x.LuasSurat.GetValueOrDefault();
                sheet[i, 9].Number = x.LuasUkurInternal.GetValueOrDefault();
                sheet[i, 10].Number = x.LuasBayar.GetValueOrDefault();
                sheet[i, 11].Text = x.id_bidang;
                sheet[i, 12].Number = x.Harga;
                sheet[i, 13].Formula = $"J{i}*L{i}";
                sheet[$"A{i}:M{i}"].CellStyle.Font.Bold = x.IsBayar;
                i++;
            }

            sheet[$"A4:M{i - 1}"].WrapText = true;

            sheet[$"H5:K{totalDetail + 5}"].NumberFormat = "#,##0";
            sheet[$"H5:K{totalDetail + 5}"].HorizontalAlignment = ExcelHAlign.HAlignRight;
            sheet[$"L5:M{totalDetail + 5}"].NumberFormat = "#,##0";
            sheet[$"L5:M{totalDetail + 5}"].HorizontalAlignment = ExcelHAlign.HAlignRight;

            sheet[i, 13].Formula = $"SUM(M5:M{i - 1})";
            sheet[i, 13].HorizontalAlignment = ExcelHAlign.HAlignRight;
            sheet[i, 13].CellStyle.Font.Bold = true;
            i += 2;

            int tempCount = i - 1;
            foreach (DetailPembayaran x in _data.detailPembayaran.Where(x => x.nilai > 0))
            {
                sheet[i, 2].Text = x.Tanggal.HasValue ? x.Tanggal.GetValueOrDefault().ToString("dd/MMM/yy", new CultureInfo("id-ID")) : "";
                sheet[i, 13].Number = x.nilai.GetValueOrDefault();
                sheet[i, 13].NumberFormat = "#,##0";
                sheet[i, 13].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                string beban = x.fglainnya.GetValueOrDefault() ? "(Beban Penjual)" : "(Beban Pembeli)";

                string val = x.identity.ToLower();
                switch (val)
                {
                    case "utj":
                        sheet[i, 3].Text = x.identity;
                        break;
                    case "pelunasan":
                        sheet[i, 3].Text = x.identity;
                        break;
                    default:
                        sheet[i, 3].Text = val.StartsWith("dp") ? x.identity : $"{x.identity} {beban}";
                        break;
                }
                sheet[i, 3].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;

                if ((i - tempCount) == totalDetailExt)
                {
                    sheet[i, 2].CellStyle.Font.Bold = true;
                    sheet[i, 3].CellStyle.Font.Bold = true;
                    sheet[i, 13].CellStyle.Font.Bold = true;
                }

                i++;
            }
            sheet[$"A1:M{i - 1}"].CellStyle.Font.Size = 14;

            await CreateFooter(sheet, i);
        }

        private async Task CreateOverlapReport(IWorksheet sheet)
        {
            int i = 4;
            int totalDetail = _data.detailBidangs.Count();
            int totalDetailExt = _data.detailPembayaran.Count(x => x.nilai > 0);

            sheet[$"A4:S{i + totalDetail + 2 + totalDetailExt}"].BorderInside();
            sheet[$"A4:S{i + totalDetail + 2 + totalDetailExt}"].BorderAround();
            sheet[$"A4:S{i + totalDetail + 2 + totalDetailExt}"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            sheet[$"A4:S{i + totalDetail + 2 + totalDetailExt}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

            sheet[i, 1].Text = "No.";
            sheet[i, 2].Text = "Tanggal";
            sheet[i, 3].Text = "Alias";
            sheet[i, 4].Text = "Pemilik";
            sheet[i, 5].Text = "Lokasi";
            sheet[i, 6].Text = "No Peta";
            sheet[i, 7].Text = "Surat asal";
            sheet[i, 8].Text = "Luas Surat";
            sheet[i, 9].Text = "L.Ukur Internal";
            sheet[i, 10].Text = "Luas Bayar";
            sheet[i, 11].Text = "Nama";
            sheet[i, 12].Text = "Alas Hak";
            sheet[i, 13].Text = "Tahap";
            sheet[i, 14].Text = "L.BTG";
            sheet[i, 15].Text = "L.O";
            sheet[i, 16].Text = "No. NIB";
            sheet[i, 17].Text = "Id Bid";
            sheet[i, 18].Text = "Harga /m2";
            sheet[i, 19].Text = "Jumlah";

            sheet.SetColumnWidth(1, 3.80);
            sheet.SetColumnWidth(2, 12);
            sheet.SetColumnWidth(3, 14);
            sheet.SetColumnWidth(4, 14);
            sheet.SetColumnWidth(5, 16);
            sheet.SetColumnWidth(7, 14);
            sheet.SetColumnWidth(9, 8.4);
            sheet.SetColumnWidth(11, 14);
            sheet.SetColumnWidth(12, 14);
            sheet.SetColumnWidth(18, 14);
            sheet.SetColumnWidth(19, 16);
            sheet[$"A{i}:S{i}"].CellStyle.Font.Bold = true;
            i++;

            int index = 0;
            DetailBidangs[] details = _data.detailBidangs.OrderBy(x => x.AlasHak).ThenBy(x => x.id_bidang).ToArray();
            foreach (DetailBidangs x in details)
            {
                sheet[i, 1].Text = $"{i - 4}.";
                sheet[i, 3].Text = x.Alias;
                sheet[i, 4].Text = x.Pemilik;
                sheet[i, 5].Text = $"{x.Desa} - {x.Project} - {x.Perusahaan}";
                sheet[i, 6].Text = x.NomorPeta;
                sheet[i, 7].Text = x.SuratAsal;
                sheet[i, 8].Number = x.LuasSurat.GetValueOrDefault();
                sheet[i, 9].Number = x.LuasUkurInternal.GetValueOrDefault();
                sheet[i, 10].Number = x.LuasBayar.GetValueOrDefault();
                sheet[i, 11].Text = x.NamaSurat;
                sheet[i, 12].Text = x.AlasHak;
                sheet[i, 13].Text = x.Tahap;
                sheet[i, 14].Number = x.luasBintang.GetValueOrDefault();
                sheet[i, 15].Number = x.luasOverlap.GetValueOrDefault();
                sheet[i, 16].Text = x.NIB;
                sheet[i, 17].Text = x.id_bidang;
                sheet[i, 18].Number = x.Harga;
                sheet[i, 19].Formula = $"J{i}*R{i}";
                sheet[$"A{i}:S{i}"].CellStyle.Font.Bold = x.IsBayar;

                bool isAnyIdBidang = details.Where(y => y.id_bidang.Equals(x.id_bidang)).Count() > 1;
                bool isSameIdBidang = index < details.Length - 1 &&
                    x.id_bidang.Equals(details[index + 1].id_bidang, StringComparison.OrdinalIgnoreCase);
                bool isNextIdBidangNotSame = index < details.Length - 2 &&
                    details[index + 1].id_bidang.Equals(details[index + 2].id_bidang, StringComparison.OrdinalIgnoreCase);
                bool isSameAlasHak = x.AlasHak != null &&
                    index < details.Length - 1 &&
                    x.AlasHak.Equals(details[index + 1].AlasHak, StringComparison.OrdinalIgnoreCase);

                if (isAnyIdBidang)
                {
                    if (isSameIdBidang)
                    {
                        sheet[$"A{i}:A{i + 1}"].Merge();
                        sheet[$"B{i}:B{i + 1}"].Merge();
                        sheet[$"C{i}:C{i + 1}"].Merge();
                        sheet[$"D{i}:D{i + 1}"].Merge();
                        sheet[$"E{i}:E{i + 1}"].Merge();
                        sheet[$"F{i}:F{i + 1}"].Merge();
                        sheet[$"G{i}:G{i + 1}"].Merge();
                        sheet[$"H{i}:H{i + 1}"].Merge();
                        sheet[$"I{i}:I{i + 1}"].Merge();
                        sheet[$"J{i}:J{i + 1}"].Merge();
                        sheet[$"M{i}:M{i + 1}"].Merge();
                        sheet[$"Q{i}:Q{i + 1}"].Merge();
                        sheet[$"R{i}:R{i + 1}"].Merge();
                        sheet[$"S{i}:S{i + 1}"].Merge();
                    }
                }
                else
                    if (isSameAlasHak && !isNextIdBidangNotSame)
                {
                    sheet[$"K{i}:K{i + 1}"].Merge();
                    sheet[$"L{i}:L{i + 1}"].Merge();
                    sheet[$"M{i}:M{i + 1}"].Merge();
                    sheet[$"N{i}:N{i + 1}"].Merge();
                    sheet[$"P{i}:P{i + 1}"].Merge();
                }

                i++; index++;
            }

            sheet[$"A4:Q{i - 1}"].WrapText = true;

            sheet[$"H5:J{totalDetail + 5}"].NumberFormat = "#,##0";
            sheet[$"H5:J{totalDetail + 5}"].HorizontalAlignment = ExcelHAlign.HAlignRight;
            sheet[$"N5:O{totalDetail + 5}"].NumberFormat = "#,##0";
            sheet[$"N5:O{totalDetail + 5}"].HorizontalAlignment = ExcelHAlign.HAlignRight;
            sheet[$"R5:S{totalDetail + 5}"].NumberFormat = "#,##0";
            sheet[$"R5:S{totalDetail + 5}"].HorizontalAlignment = ExcelHAlign.HAlignRight;

            sheet.Range[$"S{i}"].Number = _data.TotalJumlah.GetValueOrDefault();
            sheet[i, 19].HorizontalAlignment = ExcelHAlign.HAlignRight;
            sheet[i, 19].CellStyle.Font.Bold = true;
            i += 2;

            int tempCount = i - 1;
            foreach (DetailPembayaran x in _data.detailPembayaran.Where(x => x.nilai > 0))
            {
                sheet[i, 2].Text = x.Tanggal.HasValue ? x.Tanggal.GetValueOrDefault().ToString("dd/MMM/yy", new CultureInfo("id-ID")) : "";
                sheet[i, 19].Number = x.nilai.GetValueOrDefault();
                sheet[i, 19].NumberFormat = "#,##0";
                sheet[i, 19].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                string beban = x.fglainnya.GetValueOrDefault() ? "(Beban Penjual)" : "(Beban Pembeli)";

                string val = x.identity.ToLower();
                switch (val)
                {
                    case "utj":
                        sheet[i, 3].Text = x.identity;
                        break;
                    case "pelunasan":
                        sheet[i, 3].Text = x.identity;
                        break;
                    default:
                        sheet[i, 3].Text = val.StartsWith("dp") ? x.identity : $"{x.identity} {beban}";
                        break;
                }
                sheet[i, 3].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;

                if ((i - tempCount) == totalDetailExt)
                {
                    sheet[i, 2].CellStyle.Font.Bold = true;
                    sheet[i, 3].CellStyle.Font.Bold = true;
                    sheet[i, 19].CellStyle.Font.Bold = true;
                }
                i++;
            }
            sheet[$"A1:S{i - 1}"].CellStyle.Font.Size = 14;

            await CreateFooter(sheet, i);
        }

        private async Task CreateFooter(IWorksheet sheet, int i)
        {
            int col = isOverlap ? 18 : 12;

            sheet.Range[isOverlap ? $"A{i}:S{i + 4}" : $"A{i}:M{i + 4}"].CellStyle.Font.Size = 14;
            sheet.Range[$"A{i}"].Text = $"Note : {_data.Note}";
            i++;

            sheet.Range[$"A{i}"].Text = $"Nilai Akte Rp {_data.NilaiAkte:#,##0},-/m2";
            if (isOverlap) sheet[i, 13].Text = "KATEGORI :";
            sheet.Range[i, col].Text = $"MNG : {_data.Mng ?? "-"}";
            i++;

            string tanggalPenyerahan = _data.TanggalPenyerahan.HasValue ? _data.TanggalPenyerahan.GetValueOrDefault().ToString("dd/MMM/yyyy", new CultureInfo("id-ID")) : "";
            sheet.Range[$"A{i}"].Text = $"Rencana transaksi tanggal {tanggalPenyerahan} di Kantor Notaris {_data.Notaris}";
            sheet.Range[i, col].Text = $"SALES : {_data.Sales ?? "-"}";
            i++;

            sheet.Range[$"A{i}"].Text = "Tolong disiapkan :";
            sheet.Range[$"A{i}"].CellStyle.Font.Bold = true;
            sheet.Range[i, col].Text = $"MED : {_data.Mediator ?? "-"}";
            i++;

            int no = 1;
            foreach (var x in _data.Giro)
            {
                sheet.Range[$"A{i}"].Text = $"{no}. {x.Jenis.ToString().ToUpper()} Senilai Rp. {x.Nominal:#,##0},- an {x.NamaPenerima.ToUpper()}, {x.BankPenerima} {x.AccountPenerima}";
                sheet.Range[$"A{i}"].CellStyle.Font.Size = 20;
                sheet.Range[$"A{i}"].CellStyle.Font.Bold = true;
                no++; i++;
            }
            int iBg = i;
            sheet.Range[$"A{i}"].Text = $"Bilamana BG & Cek sudah disiapkan tolong agar dapat menghubungi {_data.ContactPerson} di {_data.ContactPersonPhone}"; i++;
            sheet.Range[$"A{i}"].Text = "Terima kasih atas kerjasama yang baik"; i += 2;
            sheet.Range[$"A{i}"].Text = "Hormat kami,"; i += 3;
            sheet.Range[$"A{i}"].Text = _data.MemoSigns; i++;

            foreach (var x in _data.Tembusan)
            {
                if (_data.Tembusan.First() == x)
                    sheet.Range[$"A{i}"].Text = "Cc";
                sheet.Range[$"B{i}"].Text = $": {x}";
                i++;
            }
            sheet.Range[isOverlap ? $"A{iBg}:S{i}" : $"A{iBg}:M{i}"].CellStyle.Font.Size = 14;
        }
    }
}

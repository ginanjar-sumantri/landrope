using landrope.common;
using Syncfusion.XlsIO;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Syncfusion.Drawing;

namespace SyncfusionTemplates.ExcelTemplates.PaymentRequest
{
    public static class PaymentMemoDetailXlsTemplate
    {
        static MemoTanahView _data { get; set; }
        static bool isOverlap = false;

        public static async Task<byte[]> Generate(MemoTanahView data)
        {
            try
            {
                _data = data;
                ExcelEngine excelEngine = new ExcelEngine();
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                IWorkbook workbook = application.Workbooks.Create(3);
                IWorksheet sheet = workbook.Worksheets[0];

                await CreateHeader(sheet);

                await CreateReport(sheet);

                MemoryStream result = new MemoryStream();
                workbook.SaveAs(result);
                return result.ToArray();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        static async Task CreateHeader(IWorksheet sheet)
        {
            sheet.Range["A1"].Text = _data.Title;
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Underline = ExcelUnderline.Single;
            sheet.Range["A1"].CellStyle.Font.Size = 16;

            sheet.Range["A2"].Text = "No                :";
            sheet.Range["C2"].Text = _data.NoMemo;
            sheet.Range["A3"].Text = "Tanggal      :";
            sheet.Range["C3"].Text = _data.TanggalMemo;
            sheet.Range["K3"].Text = _data.BiayaLain;
            sheet.Range["O3"].Text = _data.TahapProject;
        }

        static async Task CreateReport(IWorksheet sheet)
        {
            int i = 4,
                totalDetail = _data.BidangDetails.Length,
                lastRowColumn = i + totalDetail + 1;

            List<BayarBidangDetails> additionalColumns = _data.BayarBidangDetails.GroupBy(x => x.ColumnName).Select(x => x.FirstOrDefault()).OrderBy(x => x.order).ToList();
            string lastColumn = XlsHelpers.GetExcelColumnName(additionalColumns.Count() + 15);

            sheet[$"A4:{lastColumn}{lastRowColumn}"].BorderInside();
            sheet[$"A4:{lastColumn}{lastRowColumn}"].BorderAround();
            sheet[$"A4:{lastColumn}{lastRowColumn}"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            sheet[$"A4:{lastColumn}{lastRowColumn}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

            sheet[i, 1].Text = "NO";
            sheet[i, 2].Text = "ID BID";
            sheet[i, 3].Text = "ALIAS";
            sheet[i, 4].Text = "DESA";
            sheet[$"E{i}:F{i}"].Merge();
            sheet[i, 5].Text = "KODE LEGAL";
            sheet[i, 7].Text = "PEMILIK";
            sheet[i, 8].Text = "SURAT ASAL";
            sheet[i, 9].Text = "L SURAT";
            sheet[i, 10].Text = "L UKUR INT";
            sheet[i, 11].Text = "L PBT";
            sheet[i, 12].Text = "L BAYAR";
            sheet[i, 13].Text = "NO PETA";
            sheet[i, 14].Text = "HARGA";
            sheet[i, 15].Text = "TOTAL BELI";

            int z = 16;
            foreach (var v in additionalColumns)
            {
                sheet[i, z].Text = v.ColumnName;
                z++;
            }

            sheet.SetColumnWidth(1, 3.80);
            sheet.SetColumnWidth(2, 14);
            sheet.SetColumnWidth(3, 14);
            sheet.SetColumnWidth(4, 18);
            sheet.SetColumnWidth(5, 14);
            sheet.SetColumnWidth(6, 18);
            sheet.SetColumnWidth(7, 14);
            sheet.SetColumnWidth(8, 16);
            sheet.SetColumnWidth(9, 10);
            sheet.SetColumnWidth(10, 10);
            sheet.SetColumnWidth(11, 10);
            sheet.SetColumnWidth(12, 10);
            sheet.SetColumnWidth(14, 14);
            sheet.SetColumnWidth(15, 18);

            sheet[$"A{i}:{lastColumn}{i}"].CellStyle.Font.Bold = true;
            sheet[$"A{i}:{lastColumn}{i}"].CellStyle.Color = Color.Yellow;
            i++;

            foreach (var (x, t) in _data.BidangDetails.OrderBy(x => x.Project).ThenBy(x => x.Ptsk).Select((value, t) => (value, t)))
            {
                sheet[i, 1].Text = $"{i - 4}.";
                sheet[i, 2].Text = x.IdBidang;
                sheet[i, 3].Text = x.Alias;
                sheet[i, 4].Text = x.Desa;
                sheet[i, 5].Text = x.Project;
                sheet[i, 6].Text = x.Ptsk;
                sheet[i, 7].Text = x.Pemilik;
                sheet[i, 8].Text = x.SuratAsal;
                sheet[i, 9].Number = x.LuasSurat.GetValueOrDefault();
                sheet[i, 10].Number = x.LuasInternal.GetValueOrDefault();
                sheet[i, 11].Number = x.LuasPBT.GetValueOrDefault();
                sheet[i, 12].Number = x.LuasBayar.GetValueOrDefault();
                sheet[i, 13].Text = x.NoPeta;
                sheet[i, 14].Number = x.Harga.GetValueOrDefault();
                sheet[i, 15].Number = x.Totalbeli.GetValueOrDefault();

                int countSameHeader = 1;
                bool isCountMergeRow = (t + countSameHeader < totalDetail - 1);

                while (isCountMergeRow)
                {
                    countSameHeader += 1;
                    isCountMergeRow = (t + countSameHeader < totalDetail - 1) &&
                        (x.Project == _data.BidangDetails[t + countSameHeader].Project) &&
                        (x.Ptsk == _data.BidangDetails[t + countSameHeader].Ptsk);
                }
                    

                if (countSameHeader > 1)
                {
                    sheet[$"E{i}:E{i + countSameHeader}"].Merge();
                    sheet[$"F{i}:F{i + countSameHeader}"].Merge();
                }

                z = 16;
                foreach (var v in additionalColumns)
                {
                    var dataBayar = _data.BayarBidangDetails.Where(y => y.ColumnName == v.ColumnName).FirstOrDefault(y => y.keyPersil == x.keyPersil);

                    if (dataBayar != null)
                    {
                        if (double.TryParse(Convert.ToString(dataBayar.Value), out double val))
                        {
                            sheet[i, z].Number = val;
                            sheet[i, z].NumberFormat = "#,##0";
                            sheet[i, z].HorizontalAlignment = ExcelHAlign.HAlignRight;
                        }
                        else
                            sheet[i, z].Text = dataBayar.Value;

                        sheet[i, z].AutofitColumns();

                        // Transaction Sorting Order
                        // 1 UTJ
                        // 2 - dst : DP
                        // 55: biayalain
                        // 99 : Pelunasan
                        // 100 Rekening
                        string latestTransactionHeader = _data.BayarBidangDetails.Where(y => y.keyPersil == x.keyPersil && (y.order < 100 && y.order != 55)) // 100 is Sorting Transaction Rekening
                            .OrderByDescending(y => y.order).Select(y => y.ColumnName).FirstOrDefault();

                        if (dataBayar.order < 100 && (!x.IsSelected || dataBayar.ColumnName != latestTransactionHeader))
                            sheet[i, z].CellStyle.Color = Color.LightGray;
                    }
                    z++;
                }
                i++;
            }
            i++;
            sheet[$"A4:{lastColumn}{lastRowColumn}"].WrapText = true;

            sheet[$"I5:L{totalDetail + 5}"].NumberFormat = "#,##0";
            sheet[$"I5:L{totalDetail + 5}"].HorizontalAlignment = ExcelHAlign.HAlignRight;
            sheet[$"N5:O{totalDetail + 5}"].NumberFormat = "#,##0";
            sheet[$"N5:O{totalDetail + 5}"].HorizontalAlignment = ExcelHAlign.HAlignRight;

            await CreateFooter(sheet, i);
        }

        static async Task CreateFooter(IWorksheet sheet, int i)
        {
            int col = isOverlap ? 18 : 15;

            sheet.Range[isOverlap ? $"A{i}:S{i + 4}" : $"A{i}:Q{i + 4}"].CellStyle.Font.Size = 14;
            sheet.Range[$"A{i}"].Text = "Note : ";
            sheet.Range[i, col].Text = $"MNG : {_data.Mng ?? "-"}";
            i++;

            sheet.Range[i, col].Text = $"SALES : {_data.Sales ?? "-"}";
            if (!string.IsNullOrEmpty(_data.Note))
            {
                sheet.Range[$"A{i}"].Text = $"> {_data.Note}";
                i++;
            }

            sheet.Range[$"A{i}"].Text = $"> NILAI AKTE RP. {_data.NilaiAkte:#,##0},-/M2";
            if (isOverlap) sheet[i, 13].Text = "KATEGORI :";
            i++;

            sheet.Range[i, col].Text = $"MED : {_data.Mediator ?? "-"}";
            if (!string.IsNullOrEmpty(_data.BiayaLain))
            {
                sheet.Range[$"A{i}"].Text = $"> {_data.BiayaLain}";
                i++;
            }

            if (_data.Giro.Length > 0)
            {
                sheet.Range[$"A{i}"].Text = "Tolong disiapkan :";
                sheet.Range[$"A{i}"].CellStyle.Font.Bold = true;
                i++;

                int no = 1;
                foreach (var x in _data.Giro)
                {
                    sheet.Range[$"A{i}"].Text = $"{no}. {x.Jenis.ToString().ToUpper()} Senilai Rp. {x.Nominal:#,##0},- an {x.NamaPenerima.ToUpper()}, {x.BankPenerima} {x.AccountPenerima}";
                    sheet.Range[$"A{i}"].CellStyle.Font.Size = 20;
                    sheet.Range[$"A{i}"].CellStyle.Font.Bold = true;
                    no++; i++;
                }
            }
            
            int iBg = i; i++;
            string tanggalPenyerahan = _data.TanggalPenyerahan.HasValue ? _data.TanggalPenyerahan.GetValueOrDefault().ToString("dddd, dd MMM yyyy", new CultureInfo("id-ID")) : "";
            sheet.Range[$"A{i}"].Text = $"Rencana transaksi tanggal {tanggalPenyerahan} di Kantor Notaris {_data.Notaris}";
            i++;
            sheet.Range[$"A{i}"].Text = "TERIMA KASIH";
            i++;
            sheet.Range[$"A{i}"].RowHeight = 60;
            i++;

            var dataSign = _data.MemoSigns.Split(", ");
            sheet.Range[$"A{i}"].Text = dataSign[0];
            i++;
            sheet.Range[$"A{i}"].Text = dataSign[1] ?? "";

            sheet.Range[isOverlap ? $"A{iBg}:S{i}" : $"A{iBg}:Q{i}"].CellStyle.Font.Size = 14;
        }
    }
}

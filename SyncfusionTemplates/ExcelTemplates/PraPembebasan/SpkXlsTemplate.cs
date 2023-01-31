using landrope.common;
using Syncfusion.XlsIO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SyncfusionTemplates.ExcelTemplates.PraPembebasan
{
    public class SpkXlsTemplate
    {
        ViewSpkReport _data;
        string type;

        public SpkXlsTemplate(ViewSpkReport data, string status)
        {
            _data = data;

            switch (status.ToLower())
            {
                case string x when x.Contains("approval spk dp"):
                    type = "DP";
                    break;
                case string x when x.Contains("approval spk pelunasan"):
                    type = "PELUNASAN";
                    break;
            }
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

        async Task CreateReport(IWorksheet sheet)
        {
            sheet["A1"].ColumnWidth = 18;
            sheet["B1"].ColumnWidth = 18;
            sheet["C1"].ColumnWidth = 18;
            sheet["D1"].ColumnWidth = 20;
            sheet["E1"].ColumnWidth = 20;

            sheet["A2:E2"].Merge();
            sheet["A2"].Text = $"PERMOHONAN TRANSAKSI {type}";
            sheet["A2"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            sheet["A2"].CellStyle.Font.Bold = true;
            sheet["A2"].CellStyle.Font.Size = 12;

            sheet["A4"].Text = "GROUP";
            sheet["A5"].Text = "Nama";
            sheet["A6"].Text = "Alas Hak";
            sheet["A7"].Text = "Desa";
            sheet["A8"].Text = "Luas Surat";
            sheet["A9"].Text = "Luas Ukur";
            sheet["A9"].VerticalAlignment = ExcelVAlign.VAlignTop;
            sheet["D4"].Text = "ID BIDANG";
            sheet["D5"].Text = "NO. PETA";
            sheet["D6"].Text = "NOTARIS";
            sheet["D7"].Text = "PROJECT";

            sheet["B4"].Text = $": {_data.Group?.ToUpper()}";
            sheet["B5"].Text = $": {_data.Nama?.ToUpper()}";
            sheet["B6"].Text = $": {_data.AlasHak?.ToUpper()}";
            sheet["B7"].Text = $": {_data.Desa?.ToUpper()}";
            sheet["B8"].Text = $": {_data.LuasSurat:#,##0} M2";
            sheet["B9"].Text = $": {_data.LuasUkur:#,##0} M2";

            int i;
            for (i = 4; i < 6 + 4; i++)
                sheet[$"B{i}:C{i}"].Merge();
            sheet["B9"].VerticalAlignment = ExcelVAlign.VAlignTop;

            sheet["E4"].Text = $": {_data.IdBidang?.ToUpper()}";
            sheet["E5"].Text = $": {_data.NoPeta?.ToUpper()}";
            sheet["E6"].Text = $": {_data.Notaris?.ToUpper()}";
            sheet["E7"].Text = $": {_data.Project?.ToUpper()}";

            sheet["D8:E8"].Merge();
            sheet["D8"].Text = _data.Ptsk?.ToUpper();
            sheet["D9:E9"].Merge();
            sheet["D9"].Text = string.Join("\n", _data.Notes).ToUpper();
            sheet["D9"].WrapText = true;
            sheet["D9:E9"].AutofitRows();

            sheet["A11"].Text = "NO";
            sheet["B11"].Text = "TEMUAN LEGAL PUSAT";
            sheet["D11"].Text = "TANGGAPAN";

            int totalSampleRow = 6;
            for (i = 11; i < totalSampleRow + 11; i++)
            {
                sheet[$"B{i}:C{i}"].Merge();
                sheet[$"D{i}:E{i}"].Merge();
            }

            sheet["A11:E11"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            sheet["A11:E11"].CellStyle.ColorIndex = ExcelKnownColors.Grey_25_percent;
            sheet[$"A11:E{i - 1}"].BorderAround();
            sheet[$"A11:E{i - 1}"].BorderInside();

            sheet["A18"].Text = "DIMOHON OLEH,";
            sheet["B18"].Text = "DIPERIKSA OLEH,";
            sheet["B18:C18"].Merge();
            sheet["D18"].Text = "DISETUJUI OLEH,";
            sheet["D18:E18"].Merge();
            sheet["A19"].RowHeight = 75;
            //sheet["A20"].Text = $"({Constants.FullName.ToUpper()})";
            sheet["B20"].Text = "(E. SULAEMAN)";
            sheet["C20"].Text = "(DENNIS ARYANTO)";
            sheet["D20"].Text = "(DENNIS P. WANGSYA)";
            sheet["E20"].Text = "(LILIYANTI PRAWIRA)";
            sheet["A20:E20"].CellStyle.Font.Bold = true;
            sheet["A18:E20"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            sheet[$"A18:E20"].BorderAround();
            sheet[$"A18:E20"].BorderInside();

            sheet["A23"].Text = "DIVISI FINANCE, MOHON PROSES GIRO";
            sheet["A23:B23"].Merge();
            sheet["A24"].Text = "DIKETAHUI OLEH,";
            sheet["A24:B24"].Merge();
            sheet["A25"].RowHeight = 75;
            sheet["A26"].Text = "(YAYA)";
            sheet["B26"].Text = "(HELSA)";
            sheet["A26:B26"].CellStyle.Font.Bold = true;
            sheet["A23:B26"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            sheet[$"A23:B26"].BorderAround();
            sheet[$"A23:B26"].BorderInside();

            sheet["A28"].Text = "KOMENTAR :";
            sheet["A28"].CellStyle.Font.Bold = true;

            int commentRow = 15;
            for (i = 28; i <= commentRow + 28; i++)
                sheet[$"A{i}:E{i}"].Merge();
            sheet[$"A28:E{i - 1}"].BorderAround();
            sheet[$"A28:E{i - 1}"].BorderInside();
        }
    }
}

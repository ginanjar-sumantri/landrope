using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;

namespace landrope.common
{
    public static class DataSetHelper
    {
        public static DataSet ToDataSet<T>(this IList<T> list, string[] skipField = null)
        {
            skipField = skipField == null ? skipField = new string[]{""} : skipField;
            Type elementType = typeof(T);
            DataSet ds = new DataSet();
            System.Data.DataTable t = new System.Data.DataTable();
            ds.Tables.Add(t);

            //add a column to table for each public property on T
            foreach (var propInfo in elementType.GetProperties())
            {
                if (!skipField.Contains(propInfo.Name.ToLower()))
                    t.Columns.Add(propInfo.Name, Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType);
            }

            //go through each property on T and add each value to the table
            foreach (T item in list)
            {
                DataRow row = t.NewRow();
                foreach (var propInfo in elementType.GetProperties())
                {
                    if (!skipField.Contains(propInfo.Name.ToLower()))
                        row[propInfo.Name] = propInfo.GetValue(item) ?? DBNull.Value;
                }

                //This line was missing:
                t.Rows.Add(row);
            }


            return ds;
        }

        public static void ExportDataSetToExcel(DataSet ds, string file)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(ds.Tables[0]);
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Font.Bold = true;
                wb.SaveAs(file);
            }
        }

        public static void ExportMultipleDataSetToExcel(DataSet[] ds, string file)
        {
            XLWorkbook wb = new XLWorkbook();
            var count = 1;
            foreach (DataSet ds2 in ds)
            {
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.General;
                wb.Style.Font.Bold = false;
                wb.Worksheets.Add(ds2.Tables[0], count.ToString());
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Font.Bold = true;
                count++;
            }

            wb.SaveAs(file);
        }

        public static string ExportMultipleDataSetToExcelBase64(DataSet[] ds)
        {
            XLWorkbook wb = new XLWorkbook();
            var stream = new MemoryStream();
            var count = 1;
            foreach (DataSet ds2 in ds)
            {
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.General;
                wb.Style.Font.Bold = false;
                wb.Worksheets.Add(ds2.Tables[0], count.ToString());
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Font.Bold = true;
                count++;
            }

            wb.SaveAs(stream);

            var s = stream.ToArray();
            var b64 = Convert.ToBase64String(s);

            return b64;
        }

        public static byte[] ExportMultipleDataSetToExcelByte(DataSExcelSheet[] ds)
        {
            XLWorkbook wb = new XLWorkbook();
            var stream = new MemoryStream();
            var count = 1;
            foreach (var ds2 in ds)
            {
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.General;
                wb.Style.Font.Bold = false;
                wb.Worksheets.Add(ds2.dataS.Tables[0], ds2.sheetName);
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Font.Bold = true;
                wb.Worksheet(count).Columns().AdjustToContents();
                count++;
            }

            wb.SaveAs(stream);

            var s = stream.ToArray();

            return s;
        }
    }

    public class DataSExcelSheet
    {
        public DataSet dataS { get; set; }
        public string sheetName { get; set; }
    }
}

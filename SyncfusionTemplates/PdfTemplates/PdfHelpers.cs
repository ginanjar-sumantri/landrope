using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace SyncfusionTemplates.PdfTemplates
{
    public static class PdfHelpers
    {
        
        public static string Company = "Agung Sedayu Group";
        public static string Address = "Jl. Pantai Indah Kapuk Boulevard, Kamal Muara, Penjaringan Jakarta Utara (14470)";
        public static string Telephone = "(021) 50282888. 50525999";
        private static PdfFont LargeTitleFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 11, PdfFontStyle.Bold);
        private static PdfFont SmallestTitleFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 5);


        public static PdfSolidBrush BlackBrush = new PdfSolidBrush(Color.Black);
        public static PdfSolidBrush WhiteBrush = new PdfSolidBrush(Color.White);
        public static PdfSolidBrush YellowBrush = new PdfSolidBrush(Color.Yellow);

        public static PdfPen BlackPen = new PdfPen(Color.Black);
        public static PdfPen YellowPen = new PdfPen(Color.Yellow);

        public static PdfFont SmallestFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
        public static PdfFont SmallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        public static PdfFont NormalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12);
        public static PdfFont LargeFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14);

        public static PdfFont SmallestBoldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
        public static PdfFont SmallBoldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
        public static PdfFont NormalBoldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        public static PdfFont LargeBoldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
        public static PdfFont SmallBoldItalicFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold | PdfFontStyle.Italic);

        public static PdfStringFormat TextAlignLeft = new PdfStringFormat { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Middle };
        public static PdfStringFormat TextAlignRight = new PdfStringFormat { Alignment = PdfTextAlignment.Right, LineAlignment = PdfVerticalAlignment.Middle };
        public static PdfStringFormat TextAlignCenter = new PdfStringFormat { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle };

        public async static Task<byte[]> GetLogo()
        {
            var buildDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = buildDir + @"\logoASG.png";
            return File.ReadAllBytes(filePath);
        }

        public async static Task<PdfPageTemplateElement> CreateFooterPagination(PdfDocument doc, string pattern)
        {
            float width = doc.Pages[0].GetClientSize().Width;
            RectangleF bounds = new RectangleF(0, 0, width, 50);
            PdfPageTemplateElement footer = new PdfPageTemplateElement(bounds);

            //Create page number field.
            PdfPageNumberField pageNumber = new PdfPageNumberField(SmallFont, BlackBrush);

            //Create page count field.
            PdfPageCountField count = new PdfPageCountField(SmallFont, BlackBrush);

            //Add the fields in composite fields.
            PdfCompositeField compositeField = new PdfCompositeField(SmallestFont, BlackBrush, pattern, pageNumber, count);
            //ex for patter : "Page {0} of {1}"

            compositeField.Bounds = footer.Bounds;

            //Draw the composite field in footer.
            compositeField.Draw(footer.Graphics, new PointF((width / 2) - 10, 35));

            return footer;
        }

        public async static Task<PdfPageTemplateElement> CreateHeaderPagination(PdfDocument doc, string pattern)
        {
            float width = doc.Pages[0].GetClientSize().Width - 60;
            RectangleF bounds = new RectangleF(0, 0, width, 20);
            PdfPageTemplateElement header = new PdfPageTemplateElement(bounds);

            PdfPageNumberField pageNumber = new PdfPageNumberField(SmallFont, BlackBrush);

            //Create page count field.
            PdfPageCountField count = new PdfPageCountField(SmallFont, BlackBrush);

            //Add the fields in composite fields.
            PdfCompositeField compositeField = new PdfCompositeField(SmallestFont, BlackBrush, pattern, pageNumber, count);
            //ex for patter : "Page {0} of {1}"

            compositeField.Bounds = header.Bounds;

            //Draw the composite field in header.
            compositeField.Draw(header.Graphics, new PointF(width + 35, 0));

            return header;
        }

        public async static Task<PdfPageTemplateElement> CreateLetterHead(PdfDocument doc)
        {
            RectangleF bounds = new RectangleF(0, 0, 300, 50);
            PdfPageTemplateElement header = new PdfPageTemplateElement(bounds);

            //Draw the image in the header.
            Stream stream = new MemoryStream(await GetLogo());
            PdfImage image = new PdfBitmap(stream);
            header.Graphics.DrawImage(image, new PointF(0, 0), new SizeF(22, 30));

            header.Graphics.DrawString(Company.ToUpper(), LargeTitleFont, BlackBrush, 30, 5, TextAlignLeft);

            RectangleF addressBounds = new RectangleF(30, -5, 130, 50);
            PdfStringFormat format = TextAlignLeft;
            //Set left-to-right text direction for RTL text
            format.TextDirection = PdfTextDirection.LeftToRight;
            //Draw string with left-to-right format
            header.Graphics.DrawString($"{Address}{Environment.NewLine}{Telephone}", SmallestTitleFont, BlackBrush, addressBounds, format);

            return header;
        }

        public async static Task<PdfPageTemplateElement> CreateLetterHeadWithPaging(PdfDocument doc, string pattern)
        {
            float width = doc.Pages[0].GetClientSize().Width - 50;
            RectangleF bounds = new RectangleF(0, 0, width, 50);
            PdfPageTemplateElement header = new PdfPageTemplateElement(bounds);

            //Draw the image in the header.
            Stream stream = new MemoryStream(await GetLogo());
            PdfImage image = new PdfBitmap(stream);
            header.Graphics.DrawImage(image, new PointF(0, 0), new SizeF(30, 30));

            header.Graphics.DrawString(Company.ToUpper(), LargeTitleFont, BlackBrush, 40, 5, TextAlignLeft);

            RectangleF addressBounds = new RectangleF(40, -5, 130, 50);
            PdfStringFormat format = TextAlignLeft;
            //Set left-to-right text direction for RTL text
            format.TextDirection = PdfTextDirection.LeftToRight;
            //Draw string with left-to-right format
            header.Graphics.DrawString($"{Address}{Environment.NewLine}{Telephone}", SmallestTitleFont, BlackBrush, addressBounds, format);

            //Create page number field.
            PdfPageNumberField pageNumber = new PdfPageNumberField(SmallFont, BlackBrush);

            //Create page count field.
            PdfPageCountField count = new PdfPageCountField(SmallFont, BlackBrush);

            //Add the fields in composite fields.
            PdfCompositeField compositeField = new PdfCompositeField(SmallestFont, BlackBrush, pattern, pageNumber, count);
            //ex for patter : "Page {0} of {1}"

            compositeField.Bounds = header.Bounds;

            //Draw the composite field in footer.
            compositeField.Draw(header.Graphics, new PointF(width + 35, 0));

            return header;
        }
    }
}

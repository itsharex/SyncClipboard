﻿using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SyncClipboard.Utility;
using static SyncClipboard.Service.ProfileType;

namespace SyncClipboard.Service
{
    class ImageProfile : FileProfile
    {
        public ImageProfile(string filepath) : base(filepath)
        {
        }

        public ImageProfile(JsonProfile jsonProfile) : base(jsonProfile)
        {
        }

        public override ClipboardType GetProfileType()
        {
            return ClipboardType.Image;
        }

        private void SetBitmap(DataObject dataObject, string imagePath)
        {
            FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, bytes.Length);
            fileStream.Close();

            Stream stream = new MemoryStream(bytes);

            var bitmap = new Bitmap(stream);
            stream.Close();

            dataObject.SetData(DataFormats.Bitmap, bitmap);
        }

        private void SetHtml(DataObject dataObject, string imagePath)
        {
            string html = $@"<img src=""file:///{imagePath}"">";
            string clipboardHtml = ClipboardHtmlBuilder.GetClipboardHtml(html);
            dataObject.SetData(DataFormats.Html, clipboardHtml);
        }

        private static string clipboardQqFormat = @"<QQRichEditFormat>
<Info version=""1001"">
</Info>
<EditElement type=""1"" imagebiztype=""0"" textsummary="""" filepath=""<<<<<<"" shortcut="""">
</EditElement>
</QQRichEditFormat>";

        private void SetQqFormat(DataObject dataObject, string imagePath)
        {
            string clipboardQq = clipboardQqFormat.Replace("<<<<<<", imagePath);
            MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(clipboardQq));
            dataObject.SetData("QQ_Unicode_RichEdit_Format", ms);
        }

        protected override DataObject CreateDataObject()
        {
            var dataObject = base.CreateDataObject();
            if (dataObject is null)
            {
                return null;
            }

            string imagePath = GetTempLocalFilePath();

            SetHtml(dataObject, imagePath);
            SetQqFormat(dataObject, imagePath);
            SetBitmap(dataObject, imagePath);

            return dataObject;
        }
    }
}
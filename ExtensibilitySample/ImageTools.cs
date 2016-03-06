using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ExtensibilitySample
{
    class ImageTools
    {
        // adds data uri header to the encoded string for png image
        public static string AddDataURIHeader(string encodedString)
        {
            return "data:image/png;base64," + encodedString;
        }

        // converts data URL into the raw string to convert
        // this just strips the data URI
        public static string StripDataURIHeader(string url)
        {
            StringBuilder sb = new StringBuilder(url, url.Length);
            sb.Replace("data:image/png;base64,", String.Empty);

            // todo: better error checking

            return sb.ToString();
        }

        // Converts file to b64 string encoded as a PNG
        // regardless of what the input is, it is converted to a PNG in this process :)
        public static async Task<string> FileToString(StorageFile source)
        {
            using (IRandomAccessStream fileStream = await source.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                var destStream = new InMemoryRandomAccessStream();

                // decode the file stream
                var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(fileStream);
                BitmapTransform transform = new BitmapTransform();
                var pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.DoNotColorManage
                    );

                // encode the bytes
                return await EncodeBytesToPNGString(pixelData.DetachPixelData(), decoder.PixelWidth, decoder.PixelHeight);
            }
        }

        // gets a byte array representing the pixels in a writeable bitmap
        public static byte[] GetBitmapBytes(WriteableBitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // copy pixel buffer to the memory buffer
                bitmap.PixelBuffer.AsStream().CopyTo(ms);

                // return buffer as byte array
                return ms.ToArray();
            }
        }

        // encodes bytes into a b64 string in png format, must also specify height and width
        public static async Task<string> EncodeBytesToPNGString(byte[] bytes, uint width, uint height)
        {
            using (IRandomAccessStream destStream = new InMemoryRandomAccessStream())
            {
                // encoder, assumes input bytes are Bgra8
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, destStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    width,
                    height,
                    96,
                    96,
                    bytes
                    );

                // encode
                await encoder.FlushAsync();

                // convert the stream to bytes
                byte[] encodedBytes = new byte[destStream.Size];
                destStream.AsStreamForRead().Read(encodedBytes, 0, (int)destStream.Size);

                // and finally encode the bytes to a string
                return Convert.ToBase64String(encodedBytes.ToArray());
            }
        }

        // decode base 64 string into a bitmap
        // assumes the string represents an image
        public static IRandomAccessStream DecodeStringToBitmapSource(string base64String)
        {
            var rawBytes = Convert.FromBase64String(base64String);
            MemoryStream inputStream = new MemoryStream(rawBytes, 0, rawBytes.Length, false);
            return inputStream.AsRandomAccessStream();
        }
    }
}

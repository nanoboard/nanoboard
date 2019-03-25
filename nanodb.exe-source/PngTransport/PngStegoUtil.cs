using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;	//regex to check dataURL
 
namespace nboard
{
    class PngStegoUtil
    {
        public void HideBytesInPng(Image innocuousBmp, string outputImageFileName, byte[] hiddenBytes)
        {
            byte[] hiddenLengthBytes = BitConverter.GetBytes(hiddenBytes.Length);
            byte[] hiddenCombinedBytes = PngUtils.Combine(hiddenLengthBytes, hiddenBytes);
            byte[] rgbComponents = PngUtils.RgbComponentsToBytes(innocuousBmp);
            byte[] encodedRgbComponents = EncodeBytes(hiddenCombinedBytes, rgbComponents);
            Bitmap encodedBmp = PngUtils.ByteArrayToBitmap(encodedRgbComponents, innocuousBmp.Width, innocuousBmp.Height);
            encodedBmp.Save(outputImageFileName, ImageFormat.Png);
            encodedBmp.Dispose();
            innocuousBmp.Dispose();
			Console.WriteLine("Saved as "+outputImageFileName);
			return;
        }

        public void HideBytesInPng(string inputImageFileName, string outputImageFileName, byte[] hiddenBytes)
        {
            byte[] hiddenLengthBytes = BitConverter.GetBytes(hiddenBytes.Length);
            byte[] hiddenCombinedBytes = PngUtils.Combine(hiddenLengthBytes, hiddenBytes);
            Image innocuousBmp = Image.FromFile(inputImageFileName);
            byte[] rgbComponents = PngUtils.RgbComponentsToBytes(innocuousBmp);
            byte[] encodedRgbComponents = EncodeBytes(hiddenCombinedBytes, rgbComponents);
            Bitmap encodedBmp = PngUtils.ByteArrayToBitmap(encodedRgbComponents, innocuousBmp.Width, innocuousBmp.Height);
            encodedBmp.Save(outputImageFileName, ImageFormat.Png);
            encodedBmp.Dispose();
            innocuousBmp.Dispose();
        }
 
        private static byte[] EncodeBytes(byte[] hiddenBytes, byte[] innocuousBytes)
        {
            BitArray hiddenBits = new BitArray(hiddenBytes);
            byte[] encodedBitmapRgbComponents = new byte[innocuousBytes.Length];

            for (int i = 0; i < innocuousBytes.Length; i++)
            {
                if (i < hiddenBits.Length)
                {
                    byte evenByte = (byte)(innocuousBytes[i] - innocuousBytes[i] % 2);
                    encodedBitmapRgbComponents[i] = (byte)(evenByte + (hiddenBits[i] ? 1 : 0));
                }

                else
                {
                    encodedBitmapRgbComponents[i] = innocuousBytes[i];
                }
            }
            return encodedBitmapRgbComponents;
        }
 
        public byte[] ReadHiddenBytesFromPng(string imageFileName)								//pathway
        {
			Bitmap loadedEncodedBmp;
			if(																					//if dataURL
						imageFileName.IndexOf("data:") != -1
					&& 	imageFileName.IndexOf("base64,")!= -1
					&&	nbpack.NBPackMain.IsBase64Encoded(imageFileName.Split(',')[1])
			){
				//create bitmap from dataURL, and save this as PNG-file to Upload folder.
				var base64Data = Regex.Match(imageFileName, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
				var binData = Convert.FromBase64String(base64Data);

				using (var stream = new MemoryStream(binData))
				{
					loadedEncodedBmp = new Bitmap(stream);		//create image from dataURL
				}
			}else{
/*				//loadedEncodedBmp = new Bitmap(imageFileName);				
				Bitmap temp = new Bitmap(imageFileName);	//file will be locked if incorrect
				loadedEncodedBmp = (Bitmap)temp.Clone();	//clone bitmap
				temp.Dispose(); 							//now file can be deleted.
*/

				byte[] bytes = System.IO.File.ReadAllBytes(imageFileName);
				System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes);
				//Image img = Image.FromStream(ms);
				//loadedEncodedBmp = img as Bitmap;
				loadedEncodedBmp = new Bitmap(ms);


			}
			
            byte[] loadedEncodedRgbComponents = PngUtils.RgbComponentsToBytes(loadedEncodedBmp);
            const int bytesInInt = 4;
            byte[] loadedHiddenLengthBytes = DecodeBytes(loadedEncodedRgbComponents, 0, bytesInInt);
            int loadedHiddenLength = BitConverter.ToInt32(loadedHiddenLengthBytes, 0);
            byte[] loadedHiddenBytes = DecodeBytes(loadedEncodedRgbComponents, bytesInInt, loadedHiddenLength);
            loadedEncodedBmp.Dispose();
            return loadedHiddenBytes;
        }
 
        public byte[] ReadHiddenBytesFromPng(Image container)										//image from RAM
        {
			Bitmap loadedEncodedBmp = container as Bitmap;												//RGB bitmap from Image
			byte[] loadedEncodedRgbComponents = PngUtils.RgbComponentsToBytes(loadedEncodedBmp);
            loadedEncodedBmp.Dispose();																	//no need dispose temp bitmap, and this can be disposed now.
            const int bytesInInt = 4;
            byte[] loadedHiddenLengthBytes = DecodeBytes(loadedEncodedRgbComponents, 0, bytesInInt);
            int loadedHiddenLength = BitConverter.ToInt32(loadedHiddenLengthBytes, 0);
            byte[] loadedHiddenBytes = DecodeBytes(loadedEncodedRgbComponents, bytesInInt, loadedHiddenLength);
            return loadedHiddenBytes;
        }

        private static byte[] DecodeBytes(byte[] innocuousLookingData, int byteIndex, int byteCount)
        {
            const int bitsInBytes = 8;
            int bitCount = byteCount * bitsInBytes;
            int bitIndex = byteIndex * bitsInBytes;
            bool[] loadedHiddenBools = new bool[bitCount];

            for (int i = 0; i < bitCount; i++)
            {
                loadedHiddenBools[i] = innocuousLookingData[i + bitIndex] % 2 == 1;
            }

            BitArray loadedHiddenBits = new BitArray(loadedHiddenBools);
            byte[] loadedHiddenBytes = new byte[loadedHiddenBits.Length / bitsInBytes];
            loadedHiddenBits.CopyTo(loadedHiddenBytes, 0);
            return loadedHiddenBytes;
        }
    }
}
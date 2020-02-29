using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using Chaos.NaCl;
using NDB;
using nboard;
using NServer;
using System.Text.RegularExpressions;
//using System.Net;
//wanted to download captcha pack from here:
//https://github.com/Karasiq/nanoboard/releases/download/v1.2.0/ffeaeb19.nbc
//but github using redirect, and no any proxy added, for downloading this anonymously.

namespace captcha
{
    /*
        Class encapsulates captcha retriveal (from pack) and verification.

        Each post has it's designated captcha. 
        It is determined by post's SHA256 hash (signature tag and it's contents
        are excluded from hash calculation).
        This hash should match POW filter - at least three consecutive bytes
        with values from zero to one, starting from fourth byte of a hash - 
        otherwise post is considered to be invalid (without POW).
        First three bytes of hash is captcha index (wrapped by amount of captchas
        in the pack).
        The code extracts relevant captcha by index extracted from post's hash,
        and captcha is public ed25519 key, 32-byte seed encrypted by XORing with
        SHA512(UTF-8(captcha answer + public key in hexstring form))
        and captcha image (1-bit) 50x20 pixels (column by column, each bit 
        represents a pixel (1 - black, 0 - white).
    */
    class Captcha
    {
        private static SHA256 _sha;
        private const int PowByteOffset = 3;
        private const int PowLength = 3;
        private const int PowTreshold = 1;
        private const int CaptchaBlockLength = 32 + 32 + 125;
        private const string DaraUriPngPrefix = "data:image/png;base64,";
        private const string CaptchaImageFileSuffix = ".png";
        public const string SignatureTag = "sign";
        private const string PowTag = "pow";
		
		public static string captcha_file = "captcha.nbc";
		public static string original_captcha_file_sha256_hash = "0732888283037E2B17FFF361EAB73BEC26F7D2505CDB98C83C80EC14B9680413";
		public static string captcha_downloading_url = "http://some_url_to_download_captcha/";	//This value can be customized in config-3.json, without hardcoding this.

		public static bool captcha_checked = false;		public static bool bypassValidation = false;
		public static bool IsCaptchaValid = false;
		public static bool captcha_found = false;

        private static string _packFile;

		private static string SHA256CheckSum(string filePath)
		{
			Console.Write("Wait calculating SHA256-hash for \""+filePath+"\"... ");
			using (SHA256 SHA256 = SHA256Managed.Create())
			{
				using (FileStream fileStream = File.OpenRead(filePath)){
					string sha256hash = ToHex(SHA256.ComputeHash(fileStream), true);
					Console.Write("Done!\n");
					//Console.WriteLine(sha256hash);	//show hash
					return sha256hash;
				}
			}
		}

		private static string ToHex(byte[] bytes, bool upperCase)
		{
			StringBuilder result = new StringBuilder(bytes.Length * 2);
			for (int i = 0; i < bytes.Length; i++)
				result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
			return result.ToString();
		}
		
//		private static void download_captcha_file(string url, string filename){
			/*
			using (WebClient wc = new WebClient())
			{
				//wc.DownloadProgressChanged += wc_DownloadProgressChanged;
				wc.DownloadFileAsync (
					new System.Uri(url),	// Param1 = Link of file
					filename				// Param2 = Path to save
				);
			}
			*/
			
//			Console.WriteLine("url: "+url+", filename: "+filename);
/*
	long fileSize = 0;
    int bufferSize = 1024;
    bufferSize *= 1000;
    long existLen = 0;
    
    System.IO.FileStream saveFileStream;
    if (System.IO.File.Exists(filename))
    {
        System.IO.FileInfo destinationFileInfo = new System.IO.FileInfo(filename);
        existLen = destinationFileInfo.Length;
    }

    if (existLen > 0)
        saveFileStream = new System.IO.FileStream(filename,
                                                  System.IO.FileMode.Append,
                                                  System.IO.FileAccess.Write,
                                                  System.IO.FileShare.ReadWrite);
    else
        saveFileStream = new System.IO.FileStream(filename,
                                                  System.IO.FileMode.Create,
                                                  System.IO.FileAccess.Write,
                                                  System.IO.FileShare.ReadWrite);
 
    System.Net.HttpWebRequest httpReq;
    System.Net.HttpWebResponse httpRes;
    httpReq = (System.Net.HttpWebRequest) System.Net.HttpWebRequest.Create(url);
    httpReq.AddRange((int) existLen);
    System.IO.Stream resStream;
    httpRes = (System.Net.HttpWebResponse) httpReq.GetResponse();
    resStream = httpRes.GetResponseStream();
 
    fileSize = httpRes.ContentLength;
 
    int byteSize;
    byte[] downBuffer = new byte[bufferSize];
 
    while ((byteSize = resStream.Read(downBuffer, 0, downBuffer.Length)) > 0)
    {
        saveFileStream.Write(downBuffer, 0, byteSize);
    }
*/

/*
            bool _collectAvail = false;
            AggregatorMain.Run(new string[0], url, filename);
//            Aggregator.ParseImage(new string[0], url, filename);
            ThreadPool.QueueUserWorkItem(o => 
            {
                while(AggregatorMain.Running) 
                {
                    Thread.Sleep(1000);
                }

                _collectAvail = true;
            });
            //return _collectAvail;
*/			
/*
		DateTime startTime = DateTime.UtcNow;
        WebRequest request = WebRequest.Create(url);
        WebResponse response = request.GetResponse();
        using (Stream responseStream = response.GetResponseStream()) {
            using (Stream fileStream = File.OpenWrite(filename)) { 
                byte[] buffer = new byte[4096];
                int bytesRead = responseStream.Read(buffer, 0, 4096);
                while (bytesRead > 0) {       
                    fileStream.Write(buffer, 0, bytesRead);
                    DateTime nowTime = DateTime.UtcNow;
                    if ((nowTime - startTime).TotalMinutes > 5) {
                        throw new ApplicationException(
                            "Download timed out");
                    }
                    bytesRead = responseStream.Read(buffer, 0, 4096);
                }
            }
        }
*/
		
/*
			WebClient webClient = new WebClient();
			webClient.DownloadFile(url, filename);			
*/
/*
			WebClient webClient = new WebClient();
			//webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
			//webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
			webClient.DownloadFileAsync(new Uri(url), filename);
*/
  
/*
			using (var client = new WebClient())
			{
				client.DownloadFile(new System.Uri(url), filename);
			}			
*/

//}

		
		public static bool verify_captcha_hash(){
            _packFile = Configurator.Instance.GetValue("captcha_pack_file", captcha_file);
			captcha_downloading_url = Uri.UnescapeDataString(Configurator.Instance.GetValue("captcha_url", captcha_downloading_url));
			
			string captcha_file_hash = "";
			if(!File.Exists(_packFile)){
				Console.WriteLine(_packFile+" does not exists.");

				//download_captcha_file(captcha_downloading_url, captcha_file);	//github using redirect with temporary links, so cann't download. Method was been removed.

				return false;
			}
			else{
				captcha_file_hash = SHA256CheckSum(_packFile);
				if(captcha_file_hash!=original_captcha_file_sha256_hash){
					Console.WriteLine(
								"(captcha_file_hash == original_captcha_file_sha256_hash): "+(captcha_file_hash==original_captcha_file_sha256_hash)
						+"\n"+	"captcha_file_hash:\n"+captcha_file_hash
						+"\n"+	"original_captcha_file_sha256_hash:\n"+original_captcha_file_sha256_hash
					);

					//download_captcha_file(captcha_downloading_url, captcha_file);	//just leave this here.

					return false;
				}else{
					Console.Write("Hash OK? "+(captcha_file_hash==original_captcha_file_sha256_hash)+". ");
					return true;
				}
			}
		}
		
        static Captcha()
        {
            //_packFile = Configurator.Instance.GetValue("captcha_pack_file", "captcha.nbc");
            _packFile = Configurator.Instance.GetValue("captcha_pack_file", captcha_file);
            _sha = SHA256.Create();
			captcha_found = File.Exists(_packFile);			
        }

        public string ImageDataUri
        {
            get
            {
                return LoadImageAsDataUri();
            }
        }

        private readonly byte[] _publicKey;
        private readonly byte[] _encryptedSeed;
        private readonly byte[] _imageBits;
        private string _imageDataUri = null;

        public Captcha(byte[] publicKey, byte[] encryptedSeed, byte[] imageBits)
        {
            _publicKey = publicKey;
            _encryptedSeed = encryptedSeed;
            _imageBits = imageBits;
        }

        public string LoadImageAsDataUri()
        {
            if (_imageDataUri != null) return _imageDataUri;
            var bitmap = BitmapConvert.Convert(_imageBits);
            var imageFile = Guid.NewGuid().ToString() + CaptchaImageFileSuffix;
            bitmap.Save(imageFile);
            var uri = DaraUriPngPrefix + Convert.ToBase64String(File.ReadAllBytes(imageFile));
            File.Delete(imageFile);
            _imageDataUri = uri;
            return _imageDataUri;
        }

        public string AddSignatureToThePost(string post, string guess)
        {
            var dec_seed = ByteEncryptionUtil.WrappedXor(_encryptedSeed, guess + _publicKey.Stringify());
            var privateKey = Ed25519.ExpandedPrivateKeyFromSeed(dec_seed);
            var signature = Ed25519.Sign(Encoding.UTF8.GetBytes(post), privateKey);
            return post + "[" + SignatureTag + "=" + signature.Stringify() + "]";
        }

        public bool CheckSignature(string postWithSignature)
        {
            var post = Encoding.UTF8.GetBytes(postWithSignature.ExceptSignature());
            var sign = postWithSignature.Signature();
            return Ed25519.Verify(sign, post, _publicKey);
        }

        public bool CheckGuess(string guess)
        {
            var dec_seed = ByteEncryptionUtil.WrappedXor(_encryptedSeed, guess + _publicKey.Stringify());
            var privateKey = Ed25519.ExpandedPrivateKeyFromSeed(dec_seed);
            var dummyMessage = new byte[]{(byte)0};
            var signature = Ed25519.Sign(dummyMessage, privateKey);
            return Ed25519.Verify(signature, dummyMessage, _publicKey);
        }

        public static Captcha GetCaptchaForPost(string post)
        {
            string captchaPackFilename = _packFile;
            var size = new FileInfo(captchaPackFilename).Length;
            int count = (int) (size / CaptchaBlockLength);
            return GetCaptchaForIndex(captchaPackFilename, CaptchaIndex(post, count));
        }

        public static Captcha GetCaptchaForIndex(string captchaPackFilename, int captchaIndex)
        {
            if (captchaIndex == -1) return null;
            var publicKey = FileUtil.Read(captchaPackFilename, captchaIndex * CaptchaBlockLength, 32);
            var encryptedSeed = FileUtil.Read(captchaPackFilename, captchaIndex * CaptchaBlockLength + 32, 32);
            var image = FileUtil.Read(captchaPackFilename, captchaIndex * CaptchaBlockLength + 32 + 32, 125);
            return new Captcha(publicKey, encryptedSeed, image);
        }

        public static bool PostHasSolvedCaptcha(string post, bool bypassValidation = false)
        {
			if(bypassValidation) {
				//Console.WriteLine("Captcha.cs: PostHasSolvedCaptcha - bypassValidation = "+bypassValidation+" now.");
				return true;
			}
            var captcha = GetCaptchaForPost(post);
            if (captcha == null) return false;
            return captcha.CheckSignature(post);
        }

        private static string ExceptXmg(string post)
        {
            var matches = Regex.Matches(post, "\\[xmg=[^\\]]*\\]");
            foreach (Match m in matches)
            {
                post = post.Replace(m.Value, _sha.ComputeHash(Encoding.UTF8.GetBytes(m.Value)).Stringify());
            }
            return post;
        }

        private static byte[] ComputeHash(string post)
        {
            return _sha.ComputeHash(Encoding.UTF8.GetBytes(post));
        }

        public static bool PostHasValidPOW(string post)
        {
            post = post.ExceptSignature();
            var hash = ComputeHash(ExceptXmg(post));
            return hash.MaxConsecZeros(PowByteOffset, PowTreshold) >= PowLength;
        }

        public static int CaptchaIndex(string post, int max)
        {
			if(max==0){return 0;}						//% max, when max = 0 return error
            post = post.ExceptSignature();
            var hash = ComputeHash(ExceptXmg(post));
            if (hash.MaxConsecZeros(PowByteOffset, PowTreshold) < PowLength) return -1;
            return (hash[0] + hash[1] * 256 + hash[2] * 256 * 256) % max;
        }

        public static string AddPow(string post)
        {
            post = post.ExceptSignature();
            var xpost = ExceptXmg(post);
            byte[] hash = null;
            var buffer = new byte[128];
            var rand = new RNGCryptoServiceProvider();
            var trash = "";

            while (hash == null || hash.MaxConsecZeros(PowByteOffset, PowTreshold) < PowLength)
            {
                rand.GetBytes(buffer);
                trash = "["+PowTag+"=" + buffer.Stringify() + "]";
                hash = ComputeHash(xpost + trash);
            }

//			Console.WriteLine("Catcha.cs. AddPow. trash: "+trash+", hash: "+BitConverter.ToString(hash).Replace("-", ""));
            return post + trash;
        }
    }
}
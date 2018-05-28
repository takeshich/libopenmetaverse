using System;
using System.IO;
using Android.Graphics;

namespace OpenMetaverse.Imaging
{
	class BitmapDataExA:System.IO.MemoryStream
	{
		public int Width;
		public int Height;
		public byte PixelDepth;

		//変換前のデータがどういうデータの格納方法かを保持
		public bool ButtomDown;
		public bool RightLeft;

		//カラーマップ持っているか
		public bool hasColorMap = false;

		public BitmapDataExA():base()
		{
		}

		public MemoryStream CombineBitmapHeader (byte[] colorMap)
		{
			//ヘッダ情報の準備
			int rgbpallet = 0;
			int bitCount = 8;
			int usedColor = 0;
			switch (this.PixelDepth) {
			case 8:
				rgbpallet = 4;
				usedColor = 256;
				break;
			case 16:
				bitCount = 16;
				break;
			case 24://24bit
				bitCount = 24;
				break;
			case 32://32bit
				bitCount = 32;
				break;

			}

			//ヘッダサイズ
			int headerSize = 54;
			//bitmapのdata部のサイズ
			//int bodySize = (bd.Height * bd.Width * 4) + (bd.Height * zerocount);
			int bodySize = (int)this.Length;
			//fileサイズ
			int fileSize = bodySize + headerSize;


			//bitmapの全体の箱の準備
			byte[] rawdata = new byte[fileSize];

			//bitmapのヘッダ
			//BITMAPFILEHEADER
			//        bfType			2byte
			//        bfSize			4byte
			//        bfReserved1		2byte
			//        bfReserved2		2byte
			//        bfOffBits			4byte

			//BITMAPINFOHEADER
			//        biSize          =  0+14, // (4)
			//        biWidth         =  4+14, // (4)
			//        biHeight        =  8+14, // (4)
			//        biPlanes        = 12+14, // (2)
			//        biBitCount      = 14+14, // (2)
			//        biCompression   = 16+14, // (4)
			//        biSizeImage     = 20+14, // (4)
			//        biXPelsPerMeter = 24+14, // (4)
			//        biYPelsPerMeter = 28+14, // (4)
			//        biClrUsed       = 32+14, // (4)
			//        biClrImportant  = 36+14, // (4)

			//カラーパレット
			//1,4,8ビットの時は作るが、24,32ビットの時は画像データ内にあるので作らない
			//4byte

			//ヘッダの準備
			char
			// member of BITMAPFILEHEADER
			bfType0= 'B',
			bfType1= 'M';         // (2) Type of object; set to "BM"


			int
			bfSize = fileSize,
			bfReserved1 = 0,     // (2) Reserved, set to zero.
			bfReserved2 = 0,     // (2) Reserved, set to zero.
			bfOffBits = headerSize,  // (4) Offset, in bytes, from this structure


			// member of BITMAPINFOHEADER
			biSize = 40 ,          // (4) # of bytes required by this structure.
			biWidth = this.Width,         // (4) Width of the image, in pixels.
			biHeight = this.Height,        // (4) Height of the image, in pixels.
			biPlanes = 1,        // (2) # of planes for the target device.
			biBitCount = bitCount,      // (2) # of bits per pixel. Must be 1, 4, 8, or 24.
			biCompression = 0,   // (4) Specifies the compression used.
			biSizeImage = bodySize,     // (4) Size of the image, in bytes.
			biXPelsPerMeter = 3780, // (4) Horizontal resolution of the image.
			biYPelsPerMeter = 3780, // (4) Vertical resolution of the image.
			biClrUsed = usedColor,       // (4) # of colors in the color table used by the image.
			biClrImportant= 0  // (4) # of important colors.
			                    ;
			if(rgbpallet != 0){
				bfOffBits += 1024;
				fileSize += 256;
			}

			//操作用の箱の準備
			//全体用
			MemoryStream bobitmap = new MemoryStream(fileSize);

			//header用
			MemoryStream bo = new MemoryStream(headerSize);
			//body用
//			MemoryStream bobody = new MemoryStream(bodySize);
			byte[] bobodyComp = new byte[bodySize];
			//byte[] bobodyDump = new byte[bodySize];



			//ヘッダ情報書き込み
			//writeBITMAPFILEHEADER
			// bfType
			bo.WriteByte((byte)bfType0);
			bo.WriteByte((byte)bfType1);


//			bo.WriteByte((byte)(bfSize));
//			bo.WriteByte((byte)(bfSize/ 0x100));
//			bo.WriteByte((byte)(bfSize/ 0x10000));
//			bo.WriteByte((byte)(bfSize/ 0x1000000));
			//bfSize
			bo.Write (BitConverter.GetBytes (bfSize), 0, 4);

//			bo.WriteByte((byte)(bfReserved1));
//			bo.WriteByte((byte)(bfReserved1/ 0x100));
			//bfReserved1
			bo.Write(BitConverter.GetBytes(bfReserved1),0,2);

//			bo.WriteByte((byte)(bfReserved2));
//			bo.WriteByte((byte)(bfReserved2/ 0x100));
			//bfReserved2
			bo.Write(BitConverter.GetBytes(bfReserved2),0,2);

//			bo.WriteByte((byte)(bfOffBits));
//			bo.WriteByte((byte)(bfOffBits/ 0x100));
//			bo.WriteByte((byte)(bfOffBits/ 0x10000));
//			bo.WriteByte((byte)(bfOffBits/ 0x1000000));
			//bfOffBits
			bo.Write(BitConverter.GetBytes(bfOffBits),0,4);

			//writeBITMAPINFOHEADER
//			bo.WriteByte((byte)(biSize));
//			bo.WriteByte((byte)(biSize/ 0x100));
//			bo.WriteByte((byte)(biSize/ 0x10000));
//			bo.WriteByte((byte)(biSize/ 0x1000000));
			//biSize
			bo.Write(BitConverter.GetBytes(biSize),0,4);

//			bo.WriteByte((byte)(biWidth));
//			bo.WriteByte((byte)(biWidth/ 0x100));
//			bo.WriteByte((byte)(biWidth/ 0x10000));
//			bo.WriteByte((byte)(biWidth/ 0x1000000));
			//biWidth
			bo.Write(BitConverter.GetBytes(biWidth),0,4);

//			bo.WriteByte((byte)(biHeight));
//			bo.WriteByte((byte)(biHeight/ 0x100));
//			bo.WriteByte((byte)(biHeight/ 0x10000));
//			bo.WriteByte((byte)(biHeight/ 0x1000000));
			//biHeight
			bo.Write(BitConverter.GetBytes(biHeight),0,4);

//			bo.WriteByte((byte)(biPlanes));
//			bo.WriteByte((byte)(biPlanes/ 0x100));
			//biPlanes
			bo.Write(BitConverter.GetBytes(biPlanes),0,2);

//			bo.WriteByte((byte)(biBitCount));
//			bo.WriteByte((byte)(biBitCount/ 0x100));
			//biBitCount
			bo.Write(BitConverter.GetBytes(biBitCount),0,2);

//			bo.WriteByte((byte)(biCompression));
//			bo.WriteByte((byte)(biCompression/ 0x100));
//			bo.WriteByte((byte)(biCompression/ 0x10000));
//			bo.WriteByte((byte)(biCompression/ 0x1000000));
			//biCompression
			bo.Write(BitConverter.GetBytes(biCompression),0,4);

//			bo.WriteByte((byte)(biSizeImage));
//			bo.WriteByte((byte)(biSizeImage/ 0x100));
//			bo.WriteByte((byte)(biSizeImage/ 0x10000));
//			bo.WriteByte((byte)(biSizeImage/ 0x1000000));
			//biSizeImage
			bo.Write(BitConverter.GetBytes(biSizeImage),0,4);

//			bo.WriteByte((byte)(biXPelsPerMeter));
//			bo.WriteByte((byte)(biXPelsPerMeter/ 0x100));
//			bo.WriteByte((byte)(biXPelsPerMeter/ 0x10000));
//			bo.WriteByte((byte)(biXPelsPerMeter/ 0x1000000));
			//biXPelsPerMeter
			bo.Write(BitConverter.GetBytes(biXPelsPerMeter),0,4);

//			bo.WriteByte((byte)(biYPelsPerMeter));
//			bo.WriteByte((byte)(biYPelsPerMeter/ 0x100));
//			bo.WriteByte((byte)(biYPelsPerMeter/ 0x10000));
//			bo.WriteByte((byte)(biYPelsPerMeter/ 0x1000000));
			//biYPelsPerMeter
			bo.Write(BitConverter.GetBytes(biYPelsPerMeter),0,4);

//			bo.WriteByte((byte)(biClrUsed));
//			bo.WriteByte((byte)(biClrUsed/ 0x100));
//			bo.WriteByte((byte)(biClrUsed/ 0x10000));
//			bo.WriteByte((byte)(biClrUsed/ 0x1000000));
			//biClrUsed
			bo.Write(BitConverter.GetBytes(biClrUsed),0,4);

//			bo.WriteByte((byte)(biClrImportant));
//			bo.WriteByte((byte)(biClrImportant/ 0x100));
//			bo.WriteByte((byte)(biClrImportant/ 0x10000));
//			bo.WriteByte((byte)(biClrImportant/ 0x1000000));
			//biClrImportant
			bo.Write(BitConverter.GetBytes(biClrImportant),0,4);


			//カラーパレットがある場合は、書き込み
			//カラーパレットのサイズは？
			//グレースケール？(grayscaleの場合は、0~255までBGRに入れていく)
			//それ以外は、パレットをコピーする。
			if(rgbpallet != 0){
			//カラーパレットをどう取ればいいのかわかっていないので、テキトー
			//カラーパレットがない場合は、0うめかグレースケール
			//byte[256] no hakoni BGR0を入れていく

				bo.Write(colorMap,0,colorMap.Length);


			}

			bobitmap.Write(bo.ToArray(),0,(int)bo.Length);
			bobitmap.Write(this.ToArray(),0,(int)this.Length);
			bo.Dispose ();

			return bobitmap;

		}
	}
}


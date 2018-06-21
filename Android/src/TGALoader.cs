/*
 * Copyright (c) 2006-2016, openmetaverse.co
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.co nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using Android.Graphics;

namespace OpenMetaverse.Imaging
{
    /// <summary>
    /// Capability to load TGAs to Bitmap 
    /// </summary>
    public class LoadTGAClass
    {
        struct tgaColorMap
        {
            public ushort FirstEntryIndex;
            public ushort Length;
            public byte EntrySize;

            public void Read(System.IO.BinaryReader br)
            {
                FirstEntryIndex = br.ReadUInt16();
                Length = br.ReadUInt16();
                EntrySize = br.ReadByte();
            }
        }

        struct tgaImageSpec
        {
            public ushort XOrigin;
            public ushort YOrigin;
            public ushort Width;
            public ushort Height;
            public byte PixelDepth;
            public byte Descriptor;

            public void Read(System.IO.BinaryReader br)
            {
                XOrigin = br.ReadUInt16();
                YOrigin = br.ReadUInt16();
                Width = br.ReadUInt16();
                Height = br.ReadUInt16();
                PixelDepth = br.ReadByte();
                Descriptor = br.ReadByte();
            }

            public byte AlphaBits
            {
                get
                {
                    return (byte)(Descriptor & 0xF);
                }
                set
                {
                    Descriptor = (byte)((Descriptor & ~0xF) | (value & 0xF));
                }
            }

            public bool BottomDown
            {
                get
                {
                    return (Descriptor & 0x20) == 0x20;
                }
                set
                {
                    Descriptor = (byte)((Descriptor & ~0x20) | (value ? 0x20 : 0));
                }
            }
			public bool RightLeft
			{
				get
				{
					return (Descriptor & 0x10) == 0x10;
				}
				set
				{
					Descriptor = (byte)((Descriptor & ~0x10) | (value ? 0x10 : 0));
				}
			}
        }

        struct tgaHeader
        {
            public byte IdLength;
            public byte ColorMapType;
            public byte ImageType;

            public tgaColorMap ColorMap;
            public tgaImageSpec ImageSpec;

            public void Read(System.IO.BinaryReader br)
            {

				this.IdLength = br.ReadByte();
				this.ColorMapType = br.ReadByte();
				this.ImageType = br.ReadByte();
                this.ColorMap = new tgaColorMap();
                this.ImageSpec = new tgaImageSpec();
                this.ColorMap.Read(br);
                this.ImageSpec.Read(br);
            }

            public bool RleEncoded
            {
                get
                {
                    return ImageType >= 9;
                }
            }

        }

		/// <summary>
		/// Gets the color map.
		/// </summary>
		/// <returns>
		/// The color map.
		/// </returns>
		/// <param name='hdr'>
		/// Hdr.
		/// </param>
		/// <param name='br'>
		/// Br.
		/// </param>
		static byte[] GetColorMap (tgaHeader hdr, System.IO.BinaryReader br)
		{
			byte size = hdr.ColorMap.EntrySize;
			byte[] colorMap = new byte[256*4];
			byte[] tgacolorMap = new byte[256*4];

			//colormapも持っているか？
			if (hdr.ColorMapType == 0x01) {
				//ヘッダより先のカラーマップの読み取り
				//br.Read (tgacolorMap, 18, (int)size * 8);
				br.Read (tgacolorMap, 0, (int)size*32);

				switch (size) {
				case 0x20:
					//32
					colorMap = tgacolorMap;
					break;
				case 0x18:
					//24
					for (int i= 0; i< 256; i++) {
						colorMap [i * 4] = tgacolorMap [i * 3];
						colorMap [i * 4 + 1] = tgacolorMap [i * 3 + 1];
						colorMap [i * 4 + 2] = tgacolorMap [i * 3 + 2];
						colorMap [i * 4 + 3] = 0;
					}
					break;
				case 0x10:
					colorMap = tgacolorMap;
					break;
				case 0x09:
					//not implimented
					break;
				default:
					break;
				}
			} else {
				//grayscale
				if (size == 0x00) {
					for (int j=0; j<256; j++) {
						colorMap [j * 4] = (byte)(j + (j << 8) + (j << 16) + (j << 24));
						colorMap [j * 4 + 1] = (byte)(j + (j << 8) + (j << 16) + (j << 24));
						colorMap [j * 4 + 2] = (byte)(j + (j << 8) + (j << 16) + (j << 24));
						colorMap [j * 4 + 3] = 0;
					}
				}
			}

			return colorMap ;

		}

		/// <summary>
		/// Decodes the line.
		/// 列の構成要素変換について
		/// 右から左の時の処理、左から右の処理
		/// </summary>
		/// <param name='b'>
		/// bitmapのデータ部の配列を格納
		/// </param>
		/// <param name='line'>
		/// 列の長さ
		/// </param>
		/// <param name='byp'>
		/// 構成ビット(1,2,3)8bit,16bit,32bit
		/// </param>
		/// <param name='data'>
		/// 列の構成要素配列
		/// </param>
		/// <param name='cd'>
		/// Cd.
		/// </param>
		static void decodeLine(BitmapDataExA b,int line,int byp,byte[] data)
        {
			//bitmap作成時にLineの値が4の倍数でない場合に4の倍数になるように0のデータを追加するため
			int mod = data.Length % 4;
			MemoryStream zerofill = new MemoryStream();
			switch(mod){
			case 3:
				zerofill.WriteByte(0);
				break;
			case 2:
				zerofill.WriteByte(0);
				zerofill.WriteByte(0);
				break;
			case 1:
				zerofill.WriteByte(0);
				zerofill.WriteByte(0);
				zerofill.WriteByte(0);
				break;
			}


            if (b.RightLeft)
            {
				//未検証
				//右から左の時
				//dataを逆に入れなおして、bitmapdataに格納
				int jd = 0;
				byte[] bdata = new byte[data.Length];
				for(int i = data.Length-1;i >= 0; i--)
				{
					bdata[jd] = data[i];
					jd++;
				}
				
				b.Write(bdata,0,bdata.Length);
				b.Write(zerofill.ToArray(),0,(int)zerofill.Length);
            }
            else
            {
				//左から右の時
				b.Write(data,0,data.Length);
				b.Write(zerofill.ToArray(),0,(int)zerofill.Length);
            }
        }

		/// <summary>
		/// Decodes the rle.
		/// </summary>
		/// <param name='b'>
		/// The blue component.
		/// </param>
		/// <param name='byp'>
		/// Byp.
		/// </param>
		/// <param name='cd'>
		/// Cd.
		/// </param>
		/// <param name='br'>
		/// Br.
		/// </param>
		/// <param name='bottomUp'>
		/// If set to <c>true</c> bottom up.
		/// </param>
		static void decodeRle(BitmapDataExA b,int byp, System.IO.BinaryReader br)
        {
            try
            {
                int w = b.Width;
                // make buffer larger, so in case of emergency I can decode 
                // over line ends.
                byte[] linebuffer = new byte[(w + 128) * byp];
                int maxindex = w * byp;
                int index = 0;

				//bitmapはlineが4の倍数である必要があるので、調整処理
				int zerocount = 0;
				int mod = maxindex % 4;
				switch (mod) {
				case 3:
					zerocount = 1;
					break;
				case 2:
					zerocount = 2;
					break;
				case 1:
					zerocount = 3;
					break;
				case 0:
					zerocount = 0;
					break;
				default:
					break;
				}
				int bitmapwidth = maxindex + zerocount;


                for (int j = 0; j < b.Height; ++j)
                {
                    while (index < maxindex)
                    {
						//連続するデータの数か連続しないデータの数を得る。最初の1バイトを
                        byte blocktype = br.ReadByte();

                        int bytestoread;
                        int bytestocopy;

                        if (blocktype >= 0x80)
                        {
							//圧縮されている場合
                            bytestoread = byp;
                            bytestocopy = byp * (blocktype - 0x80);
                        }
                        else
                        {
							//圧縮なし
                            bytestoread = byp * (blocktype + 1);
                            bytestocopy = 0;
                        }

                        //if (index + bytestoread > maxindex)
                        //	throw new System.ArgumentException ("Corrupt TGA");

						//linebufferに格納し、indexをすすめる
                        br.Read(linebuffer, index, bytestoread);

						index = index + bytestoread;

						//連続している回数同じデータを格納
                        for (int i = 0; i != bytestocopy; ++i)
                        {
                            linebuffer[index + i] = linebuffer[index + i - bytestoread];
                        }
						index = index + bytestocopy;
                    }
					MemoryStream ms = new MemoryStream(maxindex);
					ms.Write(linebuffer,0,maxindex);

                    if (b.ButtomDown)
					{
                        //decodeLine(b, b.Height - j - 1, byp, linebuffer, ref cd);
						//上から下に書く場合
						//逆順になるのでポジションを指定してあげる。
						b.Position = (b.Height - j - 1) * bitmapwidth;
						decodeLine(b, b.Height - j - 1, byp, ms.ToArray());
					}
                    else{
                        //decodeLine(b, j, byp, linebuffer, ref cd);
						decodeLine(b, j, byp, ms.ToArray());
					}
					ms.Dispose();
					
                    if (index > maxindex)
                    {
                        Array.Copy(linebuffer, maxindex, linebuffer, 0, index - maxindex);
                        index -= maxindex;
                    }
                    else
                        index = 0;

                }

            }
            catch (System.IO.EndOfStreamException)
            {
            }
        }

		/// <summary>
		/// Decodes the plain.
		/// </summary>
		/// <param name='b'>
		/// The blue component.
		/// </param>
		/// <param name='byp'>
		/// Byp.
		/// </param>
		/// <param name='cd'>
		/// Cd.
		/// </param>
		/// <param name='br'>
		/// Br.
		/// </param>
		/// <param name='bottomUp'>
		/// If set to <c>true</c> bottom up.
		/// </param>
		static void decodePlain (BitmapDataExA b, int byp, System.IO.BinaryReader br)
		{
			int w = b.Width;
			byte[] linebuffer = new byte[w * byp];

			//bitmapはlineが4の倍数である必要があるので、調整処理
			int zerocount = 0;
			int mod = linebuffer.Length % 4;
			switch (mod) {
			case 3:
				zerocount = 1;
				break;
			case 2:
				zerocount = 2;
				break;
			case 1:
				zerocount = 3;
				break;
			case 0:
				zerocount = 0;
				break;
			default:
				break;
			}
			int bitmapwidth = linebuffer.Length + zerocount;

			//１ラインずつの処理
			for (int j = 0; j < b.Height; ++j) {
				br.Read (linebuffer, 0, w * byp);

				if (b.ButtomDown)
				{
					//上から下に書く場合
					//逆順になるのでポジションを指定してあげる。
					b.Position = (b.Height - j - 1) * bitmapwidth;
					decodeLine (b, b.Height - j - 1, byp, linebuffer);
				}
				else{
					//下から上に書く場合
					decodeLine (b, j, byp, linebuffer);
				}
			}
        }

		/// <summary>
		/// Decodes the standard8.
		/// </summary>
		/// <param name='b'>
		/// The blue component.
		/// </param>
		/// <param name='hdr'>
		/// Hdr.
		/// </param>
		/// <param name='br'>
		/// Br.
		/// </param>
		static void decodeStandard8 (BitmapDataExA b, tgaHeader hdr, System.IO.BinaryReader br)
		{

			if (hdr.RleEncoded) {
				decodeRle(b, 1, br);
			} else {
				decodePlain (b, 1, br);
			}
        }

		/// <summary>
		/// Decodes the special16.
		/// </summary>
		/// <param name='b'>
		/// The blue component.
		/// </param>
		/// <param name='hdr'>
		/// Hdr.
		/// </param>
		/// <param name='br'>
		/// Br.
		/// </param>
		static void decodeSpecial16 (BitmapDataExA b, tgaHeader hdr, System.IO.BinaryReader br)
		{
			if (hdr.RleEncoded) {
				decodeRle(b, 2, br);
			} else {
				decodePlain (b, 2, br);
			}
        }

		/// <summary>
		/// Decodes the standard16.
		/// </summary>
		/// <param name='b'>
		/// The blue component.
		/// </param>
		/// <param name='hdr'>
		/// Hdr.
		/// </param>
		/// <param name='br'>
		/// Br.
		/// </param>
		static void decodeStandard16 (BitmapDataExA b, tgaHeader hdr, System.IO.BinaryReader br)
		{
			if (hdr.RleEncoded) {
				decodeRle(b, 2, br);
			} else {
				decodePlain (b, 2, br);
			}
        }

		/// <summary>
		/// Decodes the special24.
		/// </summary>
		/// <param name='b'>
		/// The blue component.
		/// </param>
		/// <param name='hdr'>
		/// Hdr.
		/// </param>
		/// <param name='br'>
		/// Br.
		/// </param>
		static void decodeSpecial24 (BitmapDataExA b, tgaHeader hdr, System.IO.BinaryReader br)
		{
			if (hdr.RleEncoded) {
				decodeRle(b, 3, br);
			} else {
				decodePlain (b, 3, br);
			}
        }

		static void decodeStandard24 (BitmapDataExA b, tgaHeader hdr, System.IO.BinaryReader br)
		{

			if (hdr.RleEncoded) {
				decodeRle(b, 3, br);
			} else {
				decodePlain (b, 3, br);
			}
        }

		static void decodeStandard32 (BitmapDataExA b, tgaHeader hdr, System.IO.BinaryReader br)
		{

			if (hdr.RleEncoded) {
				decodeRle(b, 4, br);
			} else {
				decodePlain (b, 4, br);
			}
        }


//        public static System.Drawing.Size GetTGASize(string filename)
//        {
//            System.IO.FileStream f = System.IO.File.OpenRead(filename);
//
//            System.IO.BinaryReader br = new System.IO.BinaryReader(f);
//
//            tgaHeader header = new tgaHeader();
//            header.Read(br);
//            br.Close();
//
//            return new System.Drawing.Size(header.ImageSpec.Width, header.ImageSpec.Height);
//
//        }

        public static Bitmap LoadTGA(System.IO.Stream source)
        {

			System.IO.MemoryStream mms = new System.IO.MemoryStream();

			source.CopyTo(mms);
			byte[] buffer = new byte[(int)mms.Length];
			buffer = mms.ToArray();
			mms.Dispose ();

			System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer);

			byte[] colorMap = new byte[256*4];

            using (System.IO.BinaryReader br = new System.IO.BinaryReader(ms))
            {
                tgaHeader header = new tgaHeader();
                header.Read(br);

                if (header.ImageSpec.PixelDepth != 8 &&
                    header.ImageSpec.PixelDepth != 16 &&
                    header.ImageSpec.PixelDepth != 24 &&
                    header.ImageSpec.PixelDepth != 32)
                    throw new ArgumentException("Not a supported tga file.");

                if (header.ImageSpec.AlphaBits > 8)
                    throw new ArgumentException("Not a supported tga file.");

                if (header.ImageSpec.Width > 4096 ||
                    header.ImageSpec.Height > 4096)
                    throw new ArgumentException("Image too large.");

				//ヘッダに画像のwidth,height情報がない場合
				if (header.ImageSpec.Width == 0 ||
				    header.ImageSpec.Height == 0)
					throw new ArgumentException("Not implemented");

//				if (header.ImageSpec.PixelDepth == 8){
//					throw new ArgumentException("Not a supported tga file.");
//				}

				BitmapDataExA bd = new BitmapDataExA ();

				bd.Width = header.ImageSpec.Width;
				bd.Height = header.ImageSpec.Height;
				bd.PixelDepth = header.ImageSpec.PixelDepth;
				bd.ButtomDown = header.ImageSpec.BottomDown;
				bd.RightLeft = header.ImageSpec.RightLeft;

				switch (header.ImageSpec.PixelDepth) {
				case 8:
					//colormapの処理
					colorMap = GetColorMap (header, br);
					decodeStandard8 (bd, header, br);
					break;
				case 16:
					decodeStandard16 (bd, header, br);
					break;
				case 24:
					decodeStandard24 (bd, header, br);
					break;
				case 32:
					decodeStandard32 (bd, header, br);
					break;
				default:
					return null;
				}

					//bitmapのヘッダ情報の結合
				MemoryStream bms = new MemoryStream ();
	
				bms = bd.CombineBitmapHeader (colorMap);


//				Bitmap bitmap = Bitmap.CreateBitmap (header.ImageSpec.Width, header.ImageSpec.Height, Bitmap.Config.Argb8888);
//				Canvas canvas = new Canvas (bitmap);
//				//				canvas.DrawBitmap (data, 0, header.ImageSpec.Width, 0, 0, header.ImageSpec.Width, header.ImageSpec.Height, true, (Paint)null);
//				bitmap.EraseColor( Color.Transparent );
//				canvas.DrawBitmap (BitmapFactory.DecodeByteArray (bms.ToArray (), 0, (int)bms.Length), 0, 0, (Paint)null);


				bd.Dispose ();
				BitmapFactory.Options options = new BitmapFactory.Options();

				options.InPreferredConfig = Bitmap.Config.Argb8888;
				options.InScaled = true;

//				FileStream fs = new FileStream(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)+"/ume.bmp",FileMode.CreateNew);
//				fs.Write(bms.ToArray(),0,(int)bms.Length);

				return BitmapFactory.DecodeByteArray (bms.ToArray (), 0, (int)bms.Length,options);
				//return bitmap;

            }
        }
		
        public static ManagedImage LoadTGAImage(System.IO.Stream source)
        {
            return LoadTGAImage(source, false);
        }
        
        public static ManagedImage LoadTGAImage(System.IO.Stream source, bool mask)
        {
            byte[] buffer = new byte[source.Length];
            source.Read(buffer, 0, buffer.Length);

            System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer);

            using (System.IO.BinaryReader br = new System.IO.BinaryReader(ms))
            {
                tgaHeader header = new tgaHeader();
                header.Read(br);

                if (header.ImageSpec.PixelDepth != 8 &&
                    header.ImageSpec.PixelDepth != 16 &&
                    header.ImageSpec.PixelDepth != 24 &&
                    header.ImageSpec.PixelDepth != 32)
                    throw new ArgumentException("Not a supported tga file.");

                if (header.ImageSpec.AlphaBits > 8)
                    throw new ArgumentException("Not a supported tga file.");

                if (header.ImageSpec.Width > 4096 ||
                    header.ImageSpec.Height > 4096)
                    throw new ArgumentException("Image too large.");

				//ヘッダに画像のwidth,height情報がない場合
				if (header.ImageSpec.Width == 0 ||
				    header.ImageSpec.Height == 0)
					throw new ArgumentException("Not implemented");

                byte[] decoded = new byte[header.ImageSpec.Width * header.ImageSpec.Height * 4];
				BitmapDataExA bd = new BitmapDataExA();

                bd.Width = header.ImageSpec.Width;
                bd.Height = header.ImageSpec.Height;
				bd.PixelDepth = header.ImageSpec.PixelDepth;
				bd.ButtomDown = header.ImageSpec.BottomDown;
				bd.RightLeft = header.ImageSpec.RightLeft;
			

                switch (header.ImageSpec.PixelDepth)
                {
                    case 8:
                        decodeStandard8(bd, header, br);
                        break;
                    case 16:
						decodeStandard16(bd, header, br);
                        break;
                    case 24:
                        decodeStandard24(bd, header, br);
                        break;
                    case 32:
                        decodeStandard32(bd, header, br);
                        break;
                    default:
                        return null;
                }


                int n = header.ImageSpec.Width * header.ImageSpec.Height;
                ManagedImage image;

                if (mask && header.ImageSpec.AlphaBits == 0 && header.ImageSpec.PixelDepth == 8)
                {
                    image = new ManagedImage(header.ImageSpec.Width, header.ImageSpec.Height,
                        ManagedImage.ImageChannels.Alpha);
                    int p = 3;

                    for (int i = 0; i < n; i++)
                    {
                        image.Alpha[i] = decoded[p];
                        p += 4;
                    }
                }
                else
                {
                    image = new ManagedImage(header.ImageSpec.Width, header.ImageSpec.Height,
                        ManagedImage.ImageChannels.Color | ManagedImage.ImageChannels.Alpha);
                    int p = 0;

                    for (int i = 0; i < n; i++)
                    {
                        image.Blue[i] = decoded[p++];
                        image.Green[i] = decoded[p++];
                        image.Red[i] = decoded[p++];
                        image.Alpha[i] = decoded[p++];
                    }
                }

                br.Close();
                return image;
            }
        }

        public static Bitmap LoadTGA(string filename)
        {
            try
            {
                using (System.IO.FileStream f = System.IO.File.OpenRead(filename))
                {
                    return LoadTGA(f);
                }
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                return null;	// file not found
            }
            catch (System.IO.FileNotFoundException)
            {
                return null; // file not found
            }
        }
    }
}

//Fractalgen_1.3 + fractalgen_words.txt
using System;
using System.Drawing;					//draw Image
using System.Collections.Generic;		//to using "List"
using System.IO;						//using "File" to check fractalgen_words.txt.

namespace fractalgen
{
	public class Program
	{
		//change ( ("string Main_" -> "void Main") and ("return string" -> "Console.ReadKey()") ) -> to compile this cs-file in standalone program.
		//public static void Main(string[] args)	//two arguments can be specified - PNG width and height
		public static string Main_(string[] args)		//return string, after:
													//using fractalgen;
													//string fractalgen_result = fractalgen.Program.Main_(new string[]{splitted[2], splitted[3]});
		{
			//display info with "first three arguments setting width and height for PNG"
			//This is usage for standalone exe.
			System.Console.WriteLine(	"\n=================================================================\n"	+
										">fractalgen.exe [pathway] [PNG-width number, pixels] [PNG-height number, pixels]\n");
			System.Console.WriteLine(	">fractalgen.exe \"My_folder_for_images/\" 1024 768"								+
										"\n=================================================================\n");
			
			//by default, PNG resolution is 1920x1080 (FullHD), 	if not specified in arguments...
			int width 	= 	1920;
			int height 	= 	1080;
			string pathway = "";
			string result = "";
			
			int skip = 0;
			
			//display arguments	to test
			//Console.WriteLine("arguments\n");
			//for(int i=0; i<args.Length; i++){
			//	Console.Write(args[i]+" ");
			//}
			//Console.WriteLine("end arguments");
			
			if(args.Length>3){ //if more than two arguments specified...
				result = "Too many arguments specified... Only width and height allowed... \nSTOP!";
				System.Console.WriteLine(result);	//show error and stop program...
			}
			else{
				int temp_integer;													//define temp integer
				
				if(args.Length==0){}
				else if(
						( args.Length>=2 )
					|| 	(
								args[0]!=""
							&& 	!int.TryParse(args[0], out temp_integer)
					)
				){						//if more than 1 arguments specified this containing width
					if(args[0]!="" && !int.TryParse(args[0], out temp_integer)){	//first argument can be a pathway to saving PNG.
						pathway = args[0];
						skip = 1;
					}
				}
				if(args.Length>=1 || args.Length>=2){	//if more than 1 arguments specified this containing width
					//check is next arguments an integers?..
					bool isNumericWidth = int.TryParse(args[0+( (args.Length>=2)?skip:0 )], out temp_integer);		//is numeric? true, false
			
					if(isNumericWidth==true){											//if numeric
//						width = int.Parse(args[0+( (args.Length>=2)?skip:0 )] );										//to int
						width = nbpack.NBPackMain.parse_number( args[0+( (args.Length>=2)?skip:0 )] );										//to int
					}
					else{
						//leave default value and show error...
						System.Console.WriteLine(	"Width argument - not a number, this was been specified incorrectly. \n"+
													"PNG width will be default: {0}",
													width
						);
					}
			
					if(args.Length==2 || args.Length==3){ //if second argument specified
						bool isNumericheight = int.TryParse(args[1+ ( (args.Length==2)?skip:0 )], out temp_integer);	//is numeric? true, false
						if(isNumericheight==true){										//check this
//							height = int.Parse(args[1+ ( (args.Length==3)?skip:0) ]);	 							//write as integer
							height = nbpack.NBPackMain.parse_number(args[1+ ( (args.Length==3)?skip:0) ]);	 							//write as integer
						}
						else{//leave default value end show error...
							System.Console.WriteLine(
														"Height argument - not a number, this was been specified incorrectly. \n"+
														"PNG height will be default: {0}",
														height
							);
						}
					}
				}
			
				//generate fractal PNG, with this width and height
				System.Console.WriteLine("Please, wait, generating fractal...");
				result = new FractalGen().GenerateToFile(ref pathway, ref width, ref height);
				System.Console.WriteLine("Done!");
			}
			//Console.WriteLine("Press any key to exit...");
			//Console.ReadKey();		//if "void Main" - don't close window, for standalone exe
			return result;			//if "string Main_" - return string after including
		}
	}

	public class TextGen
	{
		//who, which, how, [do it]
		public static List<string> who = new List<string>(){};						//define empty lists
		public static List<string> which = new List<string>(){};
		public static List<string> how = new List<string>(){};
		public static List<string> doit = new List<string>(){};
		
		//Defatul values
		public static string[] who_default = 	new string[] {						//who
			"хомячина", "рассвет", "ропот", "рот", "пенис", "суд", "день", "фрактал", "паскудник", "дебил", 
			"обоссан", "паршивец", "тварь", "дизайнер", "экстремист", "наркоман",
			"слегка", "порноактёр", "чебурек", "человек", "бетон", "волюнтарист", "щелчок", "рубака",
			"групповод", "медвежоночек", "шахматист", "децибел", "яблочник", "плотоход",
			"биточек", "профсоюз", "самолёт-цистерна", "гигантоман", "авиасекстант", "химизатор", "синтез",
			"сахарин", "мудило",
			"там", "где-то", "как же", "но,", "почему", "это же"
		};
		public static string[] which_default =	new string[] {						//which
			"c лезвием", "травленый", "ярмарочный",
			"двадцатиградусный", "горько-солёный", "садочный", "заседательский", "просветительский",
			"безмятежный", "неистовый", "паршивый", "обоссаный", "грядущий", "бредущий", "милый",
			"усталый", "нормальный", "паскудный", "растрёпанный", "библиотечный", "убийственный", "позорный",
			"безмозглый", "настроенный", "настороженный", "подлинный", "остающийся", "несмеявшийся", "отсталый", "просящий",
			"восьмилетний","частнопрактикующий","грудастый","взлетающий","упёртый","всратый","задрыпанный"
		};
		public static string[] how_default =	new string[] {						//how
			"на рассвете", "неистово",  "пожевывая", "устало", "подлинно", "нормально", "просветительски", "безмозгло",
			"размыто", "скомканно", "дёрганно", "головкой", "неловко", "зашкварно", "тайно", "открыто",
			"угарно", "попарно", "топорно", "чопорно", "нанотехнологично", "прилично", "лично", "отлично",
			"закрыто",  "говнисто", "с лопаты", "нормально", "вовек", "троллируя", 
			"равноправно","электротехнично","псевдоморфозно","как ниндзя","забористо","упёрто", "неохотно"
		};
		public static string[] doit_default =	new string[] {						//doit
			"проткнул", "причалил",
			"обоссал", "лизал", "топтал", "таскал", "желал", "бежал", "накалывал", "ловил", "гнобил", "топил",
			"душил", "ушел", "настал", "хочет", "боится", "ест", "живёт", "умрёт", "омичевал", "напугал", "клонировал",
			"разыскивал", "пришел", "обблевал", "насвистывает", "отстал", "целовал", "спит", "убит", "продернулся",
			"самоутешился","поделился","раскрасил","вмешался","надкусил", "умотал", "ужрался","жиреет",
			"упрыгивает", "долбится", "выпендривается"
		};
		
		public static void set_default_words(){
			//Add default words
			who.InsertRange		(0, who_default);
			which.InsertRange	(0, which_default);
			how.InsertRange		(0,	how_default);
			doit.InsertRange	(0,	doit_default);			
		}
		
		public static void save_to_file(){
			//strings array
			string [] wordsList = new string[]
			{
				"who: "+String.Join("; ", who.ToArray()),
				"which: "+String.Join("; ", which.ToArray()),
				"how: "+String.Join("; ", how.ToArray()),
				"doit: "+String.Join("; ", doit.ToArray())
			};

			try{
				System.IO.File.WriteAllLines("fractalgen_words.txt", wordsList);
				Console.WriteLine("fractalgen_words.txt successfully saved.");
			}catch (Exception e){
				Console.WriteLine("Error saving fractalgen_words.txt: "+e.ToString());
			}			
		}

		public string Generate(int seed)
		{
			bool save = false;
			if (File.Exists("fractalgen_words.txt")) {												//if exists - take words from this file
				Console.WriteLine("fractalgen_words.txt - The file exists. Using words from this file...");
				string[] readText = File.ReadAllLines("fractalgen_words.txt");
				if(readText.Length==0){
					set_default_words();
					save = true;
				}
				else{
					for(int str = 0; str<readText.Length; str++){
						int len = readText[str].Length;
					
						//add words from the file to empty lists
						if(readText[str].StartsWith("#")){continue;}
						else if(readText[str].StartsWith("who: ")){
							who.InsertRange(0, readText[str].Substring(5, len-5).Split(new string[] {"; "}, StringSplitOptions.None));
						}
						else if(readText[str].StartsWith("which: ")){
							which.InsertRange(0, readText[str].Substring(7, len-7).Split(new string[] {"; "}, StringSplitOptions.None));
						}
						else if(readText[str].StartsWith("how: ")){
							how.InsertRange(0, readText[str].Substring(5, len-5).Split(new string[] {"; "}, StringSplitOptions.None));
						}
						else if(readText[str].StartsWith("doit: ")){
							doit.InsertRange(0, readText[str].Substring(6, len-6).Split(new string[] {"; "}, StringSplitOptions.None));
						}
						//set default values, if strings is invalid, and save this then
						else if(str==0){
							who.InsertRange		(0, who_default);
							save = true;
						}else if(str==1){
							which.InsertRange	(0, which_default);
							save = true;
						}else if(str==2){
							how.InsertRange		(0,	how_default);
							save = true;
						}else if(str==3){
							doit.InsertRange	(0,	doit_default);
							save = true;
						}
						else{
							Console.WriteLine("Invalid line. Rename fractalgen_words.txt and restart this program to see format of this file. ");
						}
					}
				}
			}else{																		//else - use default words and save this to file.
				Console.WriteLine("fractalgen_words.txt - file not exists. Generate this...");
				set_default_words();
				save = true;
			}
			if(save == true){save_to_file();}

			var r = new Random(seed);
			return 	(
							who[r.Next()%who.Count]	+	" "	+	which[r.Next()%which.Count]	+	" "
						+ 	how[r.Next()%how.Count]	+	" "	+	doit[r.Next()%doit.Count]
					);		//generate text string
		}
	}
	
	class FractalGen
	{
		const int cnt = 3;
		const int maxd = 10;
		float[] len = new float[cnt];
		float[] ang = new float[cnt];
		Color col;
		Color[] colors;

		public string GenerateToFile(ref string path, ref int w, ref int h)//PNG width and height from main_ function, picture size, bytes, filesize, bitlength
		{
			//System.Console.WriteLine("Generating PNG: path: {0}, width {1}, height {2}", path, w, h);

			Random rnd = new Random();
			Byte[] clr = new Byte[28];
			rnd.NextBytes(clr);
		
			var r = new Random();
			int seed = r.Next();
			r = new Random(seed);

			for (int i = 0; i < cnt; i++)
			{
				len[i] = (float)r.NextDouble()/4 + 0.5f;
				ang[i] = ((float)r.NextDouble() - 0.5f) * 360;
			}

			var b = new Bitmap(w, h);
			var g = Graphics.FromImage(b);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			var square_side = (r.Next()%64)+1;									//random side of square from 1 to 64
			for(
				int i = 0-(r.Next()%square_side)+1;	//random shifting first square
				i<=w;									//up to width
				i = i+square_side						//add square
			){
				for(
					int j=0-(r.Next()%square_side)+1;	//random shifting first square
					j<=h;								//up to height
					j = j+square_side					//add square
				){
					//filling squares for each (j column)
					g.FillRectangle(
						new SolidBrush(
							Color.FromArgb(				//random RGBA-color
								//r.Next()%255/8,		//high dynamic transparency
								32,						//static transparent value (87.5% transparency)
								//r.Next()%255,			//random 	r
								//r.Next()%255,			//ranodom 	g
								//r.Next()%255			//random 	b
															//Make colors lighter
								255-r.Next()%255/4,		//light R
								255-r.Next()%255/4,		//light G
								255-r.Next()%255/4		//light B
							)
						),
						i,							//start width
						j,							//start height
						square_side,				//fill square width	
						square_side					//fill square height
					);
				}
			}
			//transparent background filled...

			//fill fractal
			
			colors = new Color[2];

			//random two rgba colors - for fractal
			colors[0] = Color.FromArgb(
				//r.Next()%255,
				255-r.Next()%255/8,							//low transparency
				r.Next()%255, r.Next()%255, r.Next()%255	//R, G, B
			);	
			colors[1] = Color.FromArgb(
				255-r.Next()%255/8,							//low transparency
				r.Next()%255, r.Next()%255, r.Next()%255	//rgb
			);

			//draw fractal
			Frac(g, w/2, h/2, h/5, 0, 0);
			//fractal filled.
			
			//generate text
			var top_text = seed.ToString() + " fractalgen v1.3";			//text in the top
			var random_string = new TextGen().Generate(seed);				//text in the bottom

			//fill top and bottom text...
			int text_size = 22; 											//by default fontsize for text size is 22
			
			//change font size, by PNG width and height
			text_size = (w+h) >> 7; 			// divide by 128 via shifting
			if (text_size < 8)	text_size = 0;


		
			var strings1 = 1;	//calculate strings for top text			//for test, generate PNG 150x150
			var strings2 = 1;	//calculate strings for bottom text
			
			if(text_size!=0){
				using(var sf = new StringFormat()				//using alignment for the top text
				{
					Alignment = StringAlignment.Center,			//center
					LineAlignment = StringAlignment.Near,		//top alignment - in the top
				})
				{
					if((double)top_text.Length*text_size/w > 1){
						strings1 = System.Convert.ToInt32(System.Math.Ceiling((double)top_text.Length*text_size/w));
					}

					//Fill the background for text in the top...
					g.FillRectangle(
						//new SolidBrush(Color.White),			//color - white, by default
						new SolidBrush(
							Color.FromArgb(						//color
								128,							//half-transparency
								255-r.Next()%255/8,				//random colors is lighter
								255-r.Next()%255/8,
								255-r.Next()%255/8
							)
						),
						(w-top_text.Length*text_size)/2,		//start position for width
						text_size,								//start position for height
						(top_text.Length*text_size),			//width, pixels
						(text_size*2)*strings1					//background height, pixels.
					);				

					g.DrawString(											//draw top text
						top_text,											//text
						new Font(FontFamily.GenericMonospace, text_size),	//font and fontsize
						new SolidBrush(
							Color.FromArgb(									//color
								255,										//no trancparency
								(r.Next()%255*8)%255,						//random colors for text will be shadow
								(r.Next()%255*8)%255,
								(r.Next()%255*8)%255
							)
						),
						new Rectangle(0, text_size, w, h),					//area
						sf													//alignment in area
					);
				}

				using(var sf = new StringFormat()				//using alignment for bottom text
				{
					Alignment = StringAlignment.Center,			//center
					LineAlignment = StringAlignment.Far,		//top alignment - in the bottom
				}){
					if(((double)(random_string.Length*text_size)/w)>1){
						strings2 = System.Convert.ToInt32(System.Math.Ceiling((double)random_string.Length*text_size/w));
					}

					//Fill the background for text in the bottom...
					g.FillRectangle(
						//lighter color with half transparency
						new SolidBrush(Color.FromArgb(128, 255-r.Next()%255/8, 255-r.Next()%255/8, 255-r.Next()%255/8)),
						(w-random_string.Length*text_size)/2,								//start width
						h - text_size*(strings2+4),											//start position for height
						random_string.Length*text_size,										//width pixels
						(text_size*(strings2+1))*2											//height pixels.
					);

					g.DrawString(															//bottom text
						random_string, 														//text
						new Font(FontFamily.GenericMonospace, text_size),					//font and fontsize
						new SolidBrush(Color.FromArgb(255, (r.Next()%255*8)%255, (r.Next()%255*8)%255, (r.Next()%255*8)%255)), //shadow color without trancparency
						new Rectangle(0, 0, w, h-text_size*2),								//area
						sf																	//alignment in area
					);
				}
			}//else if font size = 0, don't fill top and bottom text...
			//If image size low, like 150x150, and text is multistring, background for text will be multistring too.
			//top and bottom text filled with background.

		
		//Emulate LSB noise on bitmap - for prevent "frequency analysis" by the colors in PNG.
			Bitmap newBitmap = new Bitmap(b, b.Width, b.Height);
			Color actualColor;
			var newA = 0;				//define new argb colors
			var newR = 0;
			var newG = 0;
			var newB = 0;
			var randvalue = 0;			//define randvalue

			int half_byte = 0;
			for (int i = 0; i < b.Width; i++)		//for each line
			{
				for (int j = 0; j < b.Height; j++)	//and each pixel in line
				{
					if(half_byte>=8){								//int is int32 and have 4 bytes (32 bits) = 8 half-bytes
						half_byte=1;
						randvalue = r.Next();
					}
					else{
						randvalue = randvalue>>4;
						half_byte++;
					}
					//get the pixel from the b image
					actualColor = b.GetPixel(i, j);			//get ARGB color for this pixel

					//change 1 last bit in the byte for each color
					newA = (actualColor.A!=255) ? actualColor.A+( randvalue			%2 ) : actualColor.A-( randvalue		%2 ); //add one bit, if color is lesser than 255 or subtract this bit
					newR = (actualColor.R!=255) ? actualColor.R+( ( randvalue>>1 )	%2 ) : actualColor.R-( ( randvalue>>1 )	%2 );
					newG = (actualColor.G!=255) ? actualColor.G+( ( randvalue>>2 )	%2 ) : actualColor.G-( ( randvalue>>2 )	%2 );
					newB = (actualColor.B!=255) ? actualColor.B+( ( randvalue>>3 )	%2 ) : actualColor.B-( ( randvalue>>3 )	%2 );

					newBitmap.SetPixel(i, j, Color.FromArgb(newA, newR, newG, newB));	//set pixel in bitmap
				}
			}
			//bits for colors in bitmap was been changed randomly...
			
			try{
				if( !Directory.Exists(path+Path.DirectorySeparatorChar) ){
					DirectoryInfo di = Directory.CreateDirectory(path+Path.DirectorySeparatorChar);	//Try to create the directory if this does not exists
					Console.WriteLine("Directory at path not found and created: "+di.FullName);
				} 
			}
			catch (IOException ioex){
				Console.WriteLine(ioex.Message);
			}
			string pathway = ((path=="")?"":path+Path.DirectorySeparatorChar)+Guid.NewGuid().ToString();
			
			//b.Save(pathway+"_orig.png"); 																			//save previous not modified picture, as png-file
			newBitmap.Save(pathway+".png");																			//save modified bitmap with LSB noise, as png-file.
			//now, this avaliable for comparison

			string saved = "(width = "+w+", height = "+h+")\n"+"Saved as: "+pathway+".png";
			Console.WriteLine(saved);
			return saved;
		}

		static float[] Rotate(float[] p, float a)
		{
			float[] n = new float[2];
			float dtr = (float)(Math.PI / 180);
			a *= dtr;
			float ca = (float) Math.Cos(a);
			float sa = (float) Math.Sin(a);
			n[0] = p[0] * ca - p[1] * sa;
			n[1] = p[1] * ca + p[0] * sa;
			return n;
		}

		static float fLerp(float a, float b, float t)
		{
			return (a * (1-t) + b * t);
		}

		static int iLerp(int a, int b, float t)
		{
			return (int)(a * (1-t) + b * t);
		}

		static Color Lerp(Color a, Color b, float t)
		{
			return Color.FromArgb(iLerp(a.A, b.A, t), iLerp(a.R, b.R, t), iLerp(a.G, b.G, t), iLerp(a.B, b.B, t));
		}

		void Frac(
			Graphics g, 
			float x, float y, float s, float a, int d)
		{
			if (d > maxd)
				return;
			float t0 = d / (float)(maxd + 1);
			int it = (int)(t0*200);

			if (d < 4) it = 180;

			for (int i = 0; i < cnt; i++)
			{
				float[] p = new float[] { s * len[i], s * len[i] };
				p = Rotate(p, ang[i] + a);
				p[0] += x;
				p[1] += y;

				col = Lerp(colors[0], colors[1], d/(float)maxd);

				g.DrawLine(new Pen(Color.FromArgb(202-it, col), 1), x, y, p[0], p[1]);
				Frac(g, p[0], p[1], s * len[i], a + ang[i], d + 1);
			}
		}
	}
}

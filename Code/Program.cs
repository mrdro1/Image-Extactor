using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Threading.Tasks;
using System.IO;

namespace ImgExtr
{
    class Program
    {
        public static int GetMostUsedColor(Bitmap theBitMap)
        {

            var MostUsedColor = Color.Empty;
            var MostUsedColorIncidence = 0;
            var dctColorIncidence = new Dictionary<int, int>();
            int counter_color = 0;
            for (int row = 0; row < theBitMap.Size.Width; row++)
            {
                for (int col = 0; col < theBitMap.Size.Height; col++)
                {
                    var pixelColor = theBitMap.GetPixel(row, col).ToArgb();

                    if (dctColorIncidence.Keys.Contains(pixelColor))
                    {
                        dctColorIncidence[pixelColor]++;
                    }
                    else
                    {
                        dctColorIncidence.Add(pixelColor, 1);
                        counter_color++;
                    }
                }
            }

            var dctSortedByValueHighToLow = dctColorIncidence.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            MostUsedColor = Color.FromArgb(dctSortedByValueHighToLow.First().Key);
            MostUsedColorIncidence = dctSortedByValueHighToLow.First().Value;
            return counter_color;
        }

    static void Main(string[] args)
        {
            if (args.Count() != 2)
            {
                Console.WriteLine("Count argument '{0}', but need 2: '/dir_in' '/dir_out'", args.Count());
                return;
            }

            string[] list_fn;
            if (Directory.Exists(args[0]))
                list_fn = Directory.GetFiles(args[0]);
            else
            {
                Console.WriteLine("Input directory '{0}' doesn't exist.", args[0]);
                return;
            }
            string out_dir;
            if (Directory.Exists(args[1]))
                out_dir = System.IO.Path.GetFullPath(args[1]) + "//";
            else
            {
                Console.WriteLine("Output directory '{0}' doesn't exist.", args[1]);
                return;
            }

            foreach (string fn in list_fn)
            {
                if (System.IO.Path.GetExtension(fn) != ".pdf")
                    return;
                Console.WriteLine("Processed file '{0}'.", fn);
                PdfReader reader = new PdfReader(fn);
                PRStream pst;
                PdfImageObject pio;
                PdfObject po;
                int n = reader.XrefSize; //number of objects in pdf document
                FileStream fs = null;
                try
                {
                    int image_counter = 1;
                    for(int i = 0; i < n; i++)
                    {   
                        po = reader.GetPdfObject(i); //get the object at the index i in the objects collection
                        if(po == null || !po.IsStream() ) //object not found so continue
                            continue;
                        pst = (PRStream)po; //cast object to stream
                        PdfObject type = pst.Get(PdfName.SUBTYPE); //get the object type
                                                                   //check if the object is the image type object
                        if (type != null && type.ToString().Equals(PdfName.IMAGE.ToString()))
                        {
                            string fn_without_ext = System.IO.Path.GetFileNameWithoutExtension(fn);
                            pio = new PdfImageObject(pst); //get the image
                            //read bytes of image in to an array
                            int width = System.Convert.ToInt32( pio.Get(PdfName.WIDTH).ToString());
                            int height = System.Convert.ToInt32(pio.Get(PdfName.HEIGHT).ToString());
                            if(width < 100 || height < 100)
                                continue;
                            byte[] imgdata = pio.GetImageAsBytes();
                            var ms = new MemoryStream(imgdata);
                            Bitmap bmp = new Bitmap(ms);
                            if (GetMostUsedColor(bmp) == 1)
                                continue;
                            //write the bytes array to file
                            fs = new FileStream(out_dir + fn_without_ext + "_" + image_counter + ".png", FileMode.Create);
                            fs.Write(imgdata, 0, imgdata.Length);
                            fs.Flush();
                            fs.Close();
                            image_counter++;
                        }
                    }


                }
                catch (Exception e)
                { 
                    //Console.WriteLine(e.Message); 
                }
            }
        }
    }
}

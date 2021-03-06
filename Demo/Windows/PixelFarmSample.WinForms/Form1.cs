﻿//MIT, 2016-present, WinterDev
using System;
using System.Collections.Generic;
using System.Drawing;

using System.IO;
using System.Windows.Forms;

using PixelFarm.CpuBlit;
using PixelFarm.Drawing.Fonts;

using Typography.OpenFont;
using Typography.TextLayout;
using Typography.Rendering;
using Typography.Contours;



namespace SampleWinForms
{
    public partial class Form1 : Form
    {
        Graphics g;
        AggPainter painter;
        ActualBitmap destImg;
        Bitmap winBmp;

        TextPrinterBase selectedTextPrinter = null;
        VxsTextPrinter _devVxsTextPrinter = null;

        UI.DebugGlyphVisualizer debugGlyphVisualizer = new UI.DebugGlyphVisualizer();
        TypographyTest.BasicFontOptions _basicOptions;
        TypographyTest.GlyphRenderOptions _glyphRenderOptions;
        TypographyTest.ContourAnalysisOptions _contourAnalysisOpts;

        public Form1()
        {
            InitializeComponent();

            //
            _basicOptions = openFontOptions1.Options;
            _basicOptions.TypefaceChanged += (s, e) =>
            {
                if (e.SelectedTypeface == null) return;
                //
                if (_devVxsTextPrinter != null)
                {
                    _devVxsTextPrinter.Typeface = e.SelectedTypeface;
                    var reqFont = new PixelFarm.Drawing.RequestFont(e.SelectedTypeface.Name, _basicOptions.FontSizeInPoints);
                    _devVxsTextPrinter.ChangeFont(reqFont);
                    painter.CurrentFont = reqFont;
                }


                this.glyphNameListUserControl1.Typeface = e.SelectedTypeface;
            };

            _basicOptions.UpdateRenderOutput += (s, e) => UpdateRenderOutput();
            //
            _glyphRenderOptions = glyphRenderOptionsUserControl1.Options;
            _glyphRenderOptions.UpdateRenderOutput += (s, e) => UpdateRenderOutput();
            //
            _contourAnalysisOpts = glyphContourAnalysisOptionsUserControl1.Options;
            _contourAnalysisOpts.UpdateRenderOutput += (s, e) => UpdateRenderOutput();



            txtInputChar.TextChanged += (s, e) => UpdateRenderOutput();
            button1.Click += (s, e) => UpdateRenderOutput();

            //
            this.glyphNameListUserControl1.GlyphNameChanged += (s, e) =>
            {
                //test render 
                //just our convention by add & and ;
                RenderByGlyphName(glyphNameListUserControl1.SelectedGlyphName);
            };
            //----------------
            //string inputstr = "ก้า";
            //string inputstr = "น้ำน้ำ";
            //string inputstr = "example";
            //string inputstr = "lllll";
            //string inputstr = "e";
            //string inputstr = "T";
            //string inputstr = "u";
            //string inputstr = "t";
            //string inputstr = "2";
            //string inputstr = "3";
            //string inputstr = "o";
            //string inputstr = "l";
            //string inputstr = "k";
            //string inputstr = "8";
            //string inputstr = "#";
            //string inputstr = "a";
            string inputstr = "0";
            //string inputstr = "e";
            //string inputstr = "l";
            //string inputstr = "t";
            //string inputstr = "i";
            //string inputstr = "ma"; 
            //string inputstr = "po";
            //string inputstr = "Å";
            //string inputstr = "fi";
            //string inputstr = "ก่นกิ่น";
            //string inputstr = "ญญู";
            //string inputstr = "ป่า"; //for gpos test 
            //string inputstr = "快速上手";
            //string inputstr = "啊";

            //----------------
            this.txtInputChar.Text = inputstr;
            _readyToRender = true;
        }
        void RenderByGlyphName(string selectedGlyphName)
        {
            //---------------------------------------------
            //this version only render with MiniAgg**
            //---------------------------------------------

            painter.Clear(PixelFarm.Drawing.Color.White);
            painter.UseSubPixelLcdEffect = _contourAnalysisOpts.LcdTechnique;
            painter.FillColor = PixelFarm.Drawing.Color.Black;

            selectedTextPrinter = _devVxsTextPrinter;
            selectedTextPrinter.Typeface = _basicOptions.Typeface;
            selectedTextPrinter.FontSizeInPoints = _basicOptions.FontSizeInPoints;
            selectedTextPrinter.ScriptLang = _basicOptions.ScriptLang;
            selectedTextPrinter.PositionTechnique = _basicOptions.PositionTech;

            selectedTextPrinter.HintTechnique = _glyphRenderOptions.HintTechnique;
            selectedTextPrinter.EnableLigature = _glyphRenderOptions.EnableLigature;

            //test print 3 lines
#if DEBUG
            GlyphDynamicOutline.dbugTestNewGridFitting = _contourAnalysisOpts.EnableGridFit;
            GlyphDynamicOutline.dbugActualPosToConsole = _contourAnalysisOpts.WriteFitOutputToConsole;
            GlyphDynamicOutline.dbugUseHorizontalFitValue = _contourAnalysisOpts.UseHorizontalFitAlignment;
#endif


            float x_pos = 0, y_pos = 100;
            var glyphPlanList = new Typography.TextLayout.UnscaledGlyphPlanList();


            //in this version
            //create a glyph-plan manully
            ushort selectedGlyphIndex =
                glyphNameListUserControl1.Typeface.GetGlyphIndexByName(selectedGlyphName);

            glyphPlanList.Append(
                new Typography.TextLayout.UnscaledGlyphPlan(0, selectedGlyphIndex, 0, 0, 0));

            var seq = new Typography.TextLayout.GlyphPlanSequence(
                glyphPlanList,
                0, 1);
            selectedTextPrinter.DrawFromGlyphPlans(seq, x_pos, y_pos);

            char[] printTextBuffer = this.txtInputChar.Text.ToCharArray();
            float lineSpacingPx = selectedTextPrinter.FontLineSpacingPx;
            for (int i = 0; i < 1; ++i)
            {
                selectedTextPrinter.DrawString(printTextBuffer, x_pos, y_pos);
                y_pos -= lineSpacingPx;
            }


            //copy from Agg's memory buffer to gdi 
            PixelFarm.CpuBlit.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
            g.Clear(System.Drawing.Color.White);
            g.DrawImage(winBmp, new System.Drawing.Point(10, 0));
        }

        bool _readyToRender;

        LayoutFarm.OpenFontTextService _textService;
        void UpdateRenderOutput()
        {
            if (!_readyToRender) return;
            //
            if (g == null)
            {
                destImg = new ActualBitmap(800, 600);
                painter = AggPainter.Create(destImg);
                winBmp = new Bitmap(destImg.Width, destImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                g = this.CreateGraphics();

                painter.CurrentFont = new PixelFarm.Drawing.RequestFont("tahoma", 14);


                _textService = new LayoutFarm.OpenFontTextService();
                _textService.LoadFontsFromFolder("../../../TestFonts");

                _devVxsTextPrinter = new VxsTextPrinter(painter, _textService);
                _devVxsTextPrinter.ScriptLang = _basicOptions.ScriptLang;
                _devVxsTextPrinter.PositionTechnique = Typography.TextLayout.PositionTechnique.OpenFont;

            }

            if (string.IsNullOrEmpty(this.txtInputChar.Text))
            {
                return;
            }

            //test option use be used with lcd subpixel rendering.
            //this demonstrate how we shift a pixel for subpixel rendering tech

            if (_contourAnalysisOpts.SetupPrinterLayoutForLcdSubPix)
            {
                //TODO: set lcd or not here
            }
            else
            {
                //TODO: set lcd or not here

            }

            //1. read typeface from font file 
            TypographyTest.RenderChoice renderChoice = _basicOptions.RenderChoice;
            switch (renderChoice)
            {

                case TypographyTest.RenderChoice.RenderWithGdiPlusPath:
                    //not render in this example
                    //see more at ...
                    break;
                case TypographyTest.RenderChoice.RenderWithTextPrinterAndMiniAgg:
                    {
                        //clear previous draw
                        painter.Clear(PixelFarm.Drawing.Color.White);
                        painter.UseSubPixelLcdEffect = _contourAnalysisOpts.LcdTechnique;
                        painter.FillColor = PixelFarm.Drawing.Color.Black;

                        selectedTextPrinter = _devVxsTextPrinter;
                        selectedTextPrinter.Typeface = _basicOptions.Typeface;
                        selectedTextPrinter.FontSizeInPoints = _basicOptions.FontSizeInPoints;
                        selectedTextPrinter.ScriptLang = _basicOptions.ScriptLang;
                        selectedTextPrinter.PositionTechnique = _basicOptions.PositionTech;

                        selectedTextPrinter.HintTechnique = _glyphRenderOptions.HintTechnique;
                        selectedTextPrinter.EnableLigature = _glyphRenderOptions.EnableLigature;
                        selectedTextPrinter.SimulateSlant = _contourAnalysisOpts.SimulateSlant;

                        //test print 3 lines
#if DEBUG
                        GlyphDynamicOutline.dbugTestNewGridFitting = _contourAnalysisOpts.EnableGridFit;
                        GlyphDynamicOutline.dbugActualPosToConsole = _contourAnalysisOpts.WriteFitOutputToConsole;
                        GlyphDynamicOutline.dbugUseHorizontalFitValue = _contourAnalysisOpts.UseHorizontalFitAlignment;
#endif

                        char[] printTextBuffer = this.txtInputChar.Text.ToCharArray();
                        float x_pos = 0, y_pos = 50;
                        float lineSpacingPx = selectedTextPrinter.FontLineSpacingPx;
                        for (int i = 0; i < 1; ++i)
                        {
                            selectedTextPrinter.DrawString(printTextBuffer, x_pos, y_pos);
                            y_pos -= lineSpacingPx;
                        }


                        //copy from Agg's memory buffer to gdi 
                        PixelFarm.CpuBlit.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
                        g.Clear(Color.White);
                        g.DrawImage(winBmp, new Point(10, 0));

                    }
                    break;

                //==============================================
                //render 1 glyph for debug and test
                case TypographyTest.RenderChoice.RenderWithMsdfGen:
                case TypographyTest.RenderChoice.RenderWithSdfGen:
                    {
                        char testChar = this.txtInputChar.Text[0];
                        Typeface typeFace = _basicOptions.Typeface;
                        RenderWithMsdfImg(typeFace, testChar, _basicOptions.FontSizeInPoints);

                    }
                    break;
                case TypographyTest.RenderChoice.RenderWithMiniAgg_SingleGlyph:
                    {
                        selectedTextPrinter = _devVxsTextPrinter;
                        //for test only 1 char 
                        RenderSingleCharWithMiniAgg(
                             _basicOptions.Typeface,
                            this.txtInputChar.Text[0],
                            _basicOptions.FontSizeInPoints);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        void RenderSingleCharWithMiniAgg(Typeface typeface, char testChar, float sizeInPoint)
        {

            //---------------
            //set up vinfo
            UI.DebugGlyphVisualizerInfoView vinfo = debugGlyphVisualizer.VisualizeInfoView;

            if (vinfo == null)
            {
                vinfo = new UI.DebugGlyphVisualizerInfoView();
                vinfo.SetTreeView(glyphContourAnalysisOptionsUserControl1.DebugTreeView);
                vinfo.SetFlushOutputHander(() =>
                {
                    painter.SetOrigin(0, 0);
                    //6. use this util to copy image from Agg actual image to System.Drawing.Bitmap
                    PixelFarm.CpuBlit.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
                    //--------------- 
                    //7. just render our bitmap
                    g.Clear(Color.White);
                    g.DrawImage(winBmp, new Point(30, 100));

                });
                debugGlyphVisualizer.VisualizeInfoView = vinfo;
            }

            //---------------
            //we use the debugGlyphVisualize the render it
            this.debugGlyphVisualizer.SetFont(typeface, sizeInPoint);
            debugGlyphVisualizer.CanvasPainter = painter;
            debugGlyphVisualizer.UseLcdTechnique = _contourAnalysisOpts.LcdTechnique;
            debugGlyphVisualizer.FillBackGround = _glyphRenderOptions.FillBackground;
            debugGlyphVisualizer.DrawBorder = _glyphRenderOptions.DrawBorder;

            debugGlyphVisualizer.ShowTess = _contourAnalysisOpts.ShowTess;
            debugGlyphVisualizer.WalkTrianglesAndEdges = _contourAnalysisOpts.ShowTriangle;
            debugGlyphVisualizer.DrawEndLineHub = _contourAnalysisOpts.DrawLineHubConn;
            debugGlyphVisualizer.DrawPerpendicularLine = _contourAnalysisOpts.DrawPerpendicularLine;
            debugGlyphVisualizer.WalkCentroidBone = _contourAnalysisOpts.DrawCentroidBone;
            debugGlyphVisualizer.WalkGlyphBone = _contourAnalysisOpts.DrawGlyphBone;

            debugGlyphVisualizer.GlyphEdgeOffset = _contourAnalysisOpts.EdgeOffset;

            debugGlyphVisualizer.DrawDynamicOutline = _contourAnalysisOpts.DynamicOutline;
            debugGlyphVisualizer.DrawRegenerateOutline = _contourAnalysisOpts.DrawRegenerationOutline;
            debugGlyphVisualizer.DrawGlyphPoint = _contourAnalysisOpts.DrawGlyphPoint;

#if DEBUG
            GlyphDynamicOutline.dbugTestNewGridFitting = _contourAnalysisOpts.EnableGridFit;
            GlyphDynamicOutline.dbugActualPosToConsole = _contourAnalysisOpts.WriteFitOutputToConsole;
            GlyphDynamicOutline.dbugUseHorizontalFitValue = _contourAnalysisOpts.UseHorizontalFitAlignment;
#endif


            //------------------------------------------------------

            debugGlyphVisualizer.RenderChar(testChar, _glyphRenderOptions.HintTechnique);
            //---------------------------------------------------- 

            //--------------------------
            if (_contourAnalysisOpts.ShowGrid)
            {
                //render grid
                RenderGrids(800, 600, _gridSize, painter);
            }
            painter.SetOrigin(0, 0);
            //6. use this util to copy image from Agg actual image to System.Drawing.Bitmap
            PixelFarm.CpuBlit.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
            //--------------- 
            //7. just render our bitmap
            g.Clear(Color.White);
            g.DrawImage(winBmp, new Point(30, 100));
            //g.DrawRectangle(Pens.White, new System.Drawing.Rectangle(30, 20, winBmp.Width, winBmp.Height));
        }

        void RenderWithMsdfImg(Typeface typeface, char testChar, float sizeInPoint)
        {
            painter.FillColor = PixelFarm.Drawing.Color.Black;
            //p.UseSubPixelRendering = chkLcdTechnique.Checked;
            painter.Clear(PixelFarm.Drawing.Color.White);
            //----------------------------------------------------
            var builder = new GlyphPathBuilder(typeface);
            builder.SetHintTechnique(_glyphRenderOptions.HintTechnique);

            //----------------------------------------------------
            builder.Build(testChar, sizeInPoint);
            //----------------------------------------------------
            var glyphToContour = new GlyphContourBuilder();
            var msdfGenPars = new MsdfGenParams();

            builder.ReadShapes(glyphToContour);
            //glyphToContour.Read(builder.GetOutputPoints(), builder.GetOutputContours());
            MsdfGenParams genParams = new MsdfGenParams();
            GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(glyphToContour, genParams);

            ActualBitmap actualImg = ActualBitmap.CreateFromBuffer(glyphImg.Width, glyphImg.Height, glyphImg.GetImageBuffer());
            painter.DrawImage(actualImg, 0, 0);

            //using (Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            //{
            //    var bmpdata = bmp.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            //    System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
            //    bmp.UnlockBits(bmpdata);
            //    bmp.Save("d:\\WImageTest\\a001_xn2_" + n + ".png");
            //}

            if (_contourAnalysisOpts.ShowGrid)
            {
                //render grid
                RenderGrids(800, 600, _gridSize, painter);
            }

            //6. use this util to copy image from Agg actual image to System.Drawing.Bitmap
            PixelFarm.CpuBlit.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
            //--------------- 
            //7. just render our bitmap
            g.Clear(Color.White);
            g.DrawImage(winBmp, new Point(30, 20));
        }

        void RenderGrids(int width, int height, int sqSize, AggPainter p)
        {
            //render grid 
            p.FillColor = PixelFarm.Drawing.Color.Gray;

            float pointW = (sqSize >= 100) ? 2 : 1;

            for (int y = 0; y < height;)
            {
                for (int x = 0; x < width;)
                {
                    p.FillRect(x, y, pointW, pointW);
                    x += sqSize;
                }
                y += sqSize;
            }
        }




        int _gridSize = 5;//default 

        private void cmdBuildMsdfTexture_Click(object sender, EventArgs e)
        {

            //samples...
            //1. create texture from specific glyph index range
            string sampleFontFile = "../../../TestFonts/tahoma.ttf";
            CreateSampleMsdfTextureFont(
                sampleFontFile,
                18,
                0,
                100,
                "d:\\WImageTest\\sample_msdf.png");
            //---------------------------------------------------------
            //2. for debug, create from some unicode chars
            //
            //CreateSampleMsdfTextureFont(
            //   sampleFontFile,
            //   18,
            //  new char[] { 'I' },
            //  "d:\\WImageTest\\sample_msdf.png");
            //---------------------------------------------------------
            ////3.
            //GlyphTranslatorToContour tx = new GlyphTranslatorToContour();
            //tx.BeginRead(1);
            ////tx.MoveTo(10, 10);
            ////tx.LineTo(25, 25);
            ////tx.LineTo(15, 10);
            //tx.MoveTo(3.84f, 0);
            //tx.LineTo(1.64f, 0);
            //tx.LineTo(1.64f, 18.23f);
            //tx.LineTo(3.84f, 18.23f);
            //tx.CloseContour();
            //tx.EndRead();
            ////
            //CreateSampleMsdfImg(tx, "d:\\WImageTest\\tx_contour2.bmp");

        }
        static void CreateSampleMsdfTextureFont(
          string fontfile, float sizeInPoint,
          char[] chars, string outputFile)
        {
            //sample
            var reader = new OpenFontReader();
            using (var fs = new FileStream(fontfile, FileMode.Open))
            {
                //1. read typeface from font file
                Typeface typeface = reader.Read(fs);
                //sample: create sample msdf texture 
                //-------------------------------------------------------------
                var builder = new GlyphPathBuilder(typeface);
                //builder.UseTrueTypeInterpreter = this.chkTrueTypeHint.Checked;
                //builder.UseVerticalHinting = this.chkVerticalHinting.Checked;
                //-------------------------------------------------------------
                var atlasBuilder = new SimpleFontAtlasBuilder();

                MsdfGenParams msdfGenParams = new MsdfGenParams();

                int j = chars.Length;
                for (int i = 0; i < j; ++i)
                {
                    //build glyph
                    ushort gindex = typeface.LookupIndex(chars[i]);
                    builder.BuildFromGlyphIndex(gindex, -1);

                    var glyphToContour = new GlyphContourBuilder();
                    //glyphToContour.Read(builder.GetOutputPoints(), builder.GetOutputContours());
                    builder.ReadShapes(glyphToContour);
                    msdfGenParams.shapeScale = 1f / 64;
                    GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(glyphToContour, msdfGenParams);
                    atlasBuilder.AddGlyph(gindex, glyphImg);
                    int w = glyphImg.Width;
                    int h = glyphImg.Height;
                    using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                        int[] imgBuffer = glyphImg.GetImageBuffer();
                        System.Runtime.InteropServices.Marshal.Copy(imgBuffer, 0, bmpdata.Scan0, imgBuffer.Length);
                        bmp.UnlockBits(bmpdata);
                        bmp.Save("d:\\WImageTest\\a001_xn2_" + (chars[i]) + ".png");
                    }
                }

                var glyphImg2 = atlasBuilder.BuildSingleImage();
                using (Bitmap bmp = new Bitmap(glyphImg2.Width, glyphImg2.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg2.Width, glyphImg2.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int[] intBuffer = glyphImg2.GetImageBuffer();

                    System.Runtime.InteropServices.Marshal.Copy(intBuffer, 0, bmpdata.Scan0, intBuffer.Length);
                    bmp.UnlockBits(bmpdata);
                    bmp.Save("d:\\WImageTest\\a_total.png");
                }
                atlasBuilder.SaveFontInfo("d:\\WImageTest\\a_info.xml");
            }
        }

        static void CreateSampleMsdfImg(GlyphContourBuilder tx, string outputFile)
        {
            //sample

            MsdfGenParams msdfGenParams = new MsdfGenParams();
            GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(tx, msdfGenParams);
            int w = glyphImg.Width;
            int h = glyphImg.Height;
            using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                int[] imgBuffer = glyphImg.GetImageBuffer();
                System.Runtime.InteropServices.Marshal.Copy(imgBuffer, 0, bmpdata.Scan0, imgBuffer.Length);
                bmp.UnlockBits(bmpdata);
                bmp.Save(outputFile);
            }

        }
        static void CreateSampleMsdfTextureFont(string fontfile, float sizeInPoint, ushort startGlyphIndex, ushort endGlyphIndex, string outputFile)
        {
            //sample
            var reader = new OpenFontReader();

            using (var fs = new FileStream(fontfile, FileMode.Open))
            {
                //1. read typeface from font file
                Typeface typeface = reader.Read(fs);
                //sample: create sample msdf texture 
                //-------------------------------------------------------------
                var builder = new GlyphPathBuilder(typeface);
                //builder.UseTrueTypeInterpreter = this.chkTrueTypeHint.Checked;
                //builder.UseVerticalHinting = this.chkVerticalHinting.Checked;
                //-------------------------------------------------------------
                var atlasBuilder = new SimpleFontAtlasBuilder();


                for (ushort gindex = startGlyphIndex; gindex <= endGlyphIndex; ++gindex)
                {
                    //build glyph
                    builder.BuildFromGlyphIndex(gindex, sizeInPoint);

                    var glyphToContour = new GlyphContourBuilder();
                    //glyphToContour.Read(builder.GetOutputPoints(), builder.GetOutputContours());
                    var genParams = new MsdfGenParams();
                    builder.ReadShapes(glyphToContour);
                    //genParams.shapeScale = 1f / 64; //we scale later (as original C++ code use 1/64)
                    GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(glyphToContour, genParams);
                    atlasBuilder.AddGlyph(gindex, glyphImg);

                    using (Bitmap bmp = new Bitmap(glyphImg.Width, glyphImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        int[] buffer = glyphImg.GetImageBuffer();

                        var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg.Width, glyphImg.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
                        bmp.UnlockBits(bmpdata);
                        bmp.Save("d:\\WImageTest\\a001_xn2_" + gindex + ".png");
                    }
                }

                var glyphImg2 = atlasBuilder.BuildSingleImage();
                using (Bitmap bmp = new Bitmap(glyphImg2.Width, glyphImg2.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg2.Width, glyphImg2.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int[] intBuffer = glyphImg2.GetImageBuffer();

                    System.Runtime.InteropServices.Marshal.Copy(intBuffer, 0, bmpdata.Scan0, intBuffer.Length);
                    bmp.UnlockBits(bmpdata);
                    bmp.Save("d:\\WImageTest\\a_total.png");
                }
                atlasBuilder.SaveFontInfo("d:\\WImageTest\\a_info.bin");
                //
                //-----------
                //test read texture info back
                var atlasBuilder2 = new SimpleFontAtlasBuilder();
                var readbackFontAtlas = atlasBuilder2.LoadFontInfo("d:\\WImageTest\\a_info.bin");
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Render with PixelFarm";
        }

        private void cmdMeasureString_Click(object sender, EventArgs e)
        {

            //How to measure user's string...
            //this demostrate step-by-step

            //similar to ...  selectedTextPrinter.DrawString(printTextBuffer, x_pos, y_pos); 
            string str = txtInputChar.Text;
            //
            Typeface typeface = _basicOptions.Typeface;
            float fontSizeInPoints = _basicOptions.FontSizeInPoints;

            var layout = new Typography.TextLayout.GlyphLayout();
            layout.Typeface = typeface;
            layout.ScriptLang = _basicOptions.ScriptLang;
            layout.PositionTechnique = _basicOptions.PositionTech;
            layout.EnableLigature = false;// true
            layout.EnableComposition = true;

            //3.
            //3.1 : if you want GlyphPlanList too.
            //var resultGlyphPlanList = new Typography.TextLayout.GlyphPlanList();
            //Typography.TextLayout.MeasuredStringBox box = layout.LayoutAndMeasureString(str.ToCharArray(), 0, str.Length, _basicOptions.FontSizeInPoints, resultGlyphPlanList);

            //or
            //3.2 : only MeasuredStringBox
            Typography.TextLayout.MeasuredStringBox box =
                layout.LayoutAndMeasureString(
                    str.ToCharArray(), 0,
                    str.Length,
                    fontSizeInPoints);

            this.lblStringSize.Text = "measure (W,H)= (" + box.width.ToString() + "," + (box.ascending - box.descending) + ") px";
        }
    }
}
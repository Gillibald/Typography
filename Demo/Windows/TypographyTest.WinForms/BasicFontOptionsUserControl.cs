﻿//MIT, 2017-present, WinterDev
using System;
using System.Windows.Forms;

using Typography.TextLayout;
using Typography.FontManagement;
namespace TypographyTest.WinForms
{

    public partial class BasicFontOptionsUserControl : UserControl
    {
        BasicFontOptions _options;

        public BasicFontOptionsUserControl()
        {
            InitializeComponent();
            //
            _options = new BasicFontOptions();

        }
        public BasicFontOptions Options
        {
            get { return _options; }
        }

        private void OpenFontOptions_Load(object sender, EventArgs e)
        {
            if (this.DesignMode) { return; }
            //
            _options.LoadFontList();
            //
            SetupScriptLangComboBox();
            SetupFontList();
            SetupFontSizeList();
            SetupRenderOptions();
            //
            this.lstFontSizes.SelectedIndex = 0;// lstFontSizes.Items.Count - 3;
            var instTypeface = lstFontList.SelectedItem as InstalledTypeface;
            if (instTypeface != null)
            {
                _options.InstalledTypeface = instTypeface;
            }
            SetupPositionTechniqueList();
        }
        void SetupFontList()
        {

            InstalledTypeface selectedInstalledTypeface = null;
            int selected_index = 0;
            int ffcount = 0;
            bool found = false;

            string defaultFont = "Tahoma";
            //string defaultFont = "Alef"; //test hebrew
            //string defaultFont = "Century";
            foreach (InstalledTypeface installedTypeface in _options.GetInstalledTypefaceIter())
            {
                if (!found && installedTypeface.FontName == defaultFont)
                {
                    selectedInstalledTypeface = installedTypeface;
                    selected_index = ffcount;
                    _options.InstalledTypeface = installedTypeface;
                    found = true;
                }
                lstFontList.Items.Add(installedTypeface);
                ffcount++;
            }
            //set default font for current text printer
            //


            if (selected_index < 0) { selected_index = 0; }
            lstFontList.SelectedIndex = selected_index;
            lstFontList.SelectedIndexChanged += (s, e) =>
            {
                InstalledTypeface ff = lstFontList.SelectedItem as InstalledTypeface;
                if (ff != null)
                {
                    _options.InstalledTypeface = ff;
                    _options.InvokeAttachEvents();
                }
            };
        }
        void SetupFontSizeList()
        {
            lstFontSizes.Items.AddRange(
               new object[]{
                        8, 9,
                        10,11,
                        12,
                        14,
                        16,
                        18,20,22,24,26,28,36,48,72,
                        240,280,300,360,400,420,460,
                        620,720,860,920,1024
               });
            lstFontSizes.SelectedIndexChanged += (s, e) =>
            {
                //new font size
                _options.FontSizeInPoints = (int)lstFontSizes.SelectedItem;
                _options.InvokeAttachEvents();
            };
        }


        //
        int _defaultScriptLangComboBoxIndex = 0;
        void SetupScriptLangComboBox()
        {

            //for debug, set default script lang here


            _options.ScriptLang = Typography.OpenFont.ScriptLangs.Latin;
            //
            int index = 0;
            foreach (Typography.OpenFont.ScriptLang scriptLang in Typography.OpenFont.ScriptLangs.GetRegiteredScriptLangIter())
            {
                this.cmbScriptLangs.Items.Add(scriptLang);
                //
                if (scriptLang == _options.ScriptLang)
                {
                    //found default script lang
                    _defaultScriptLangComboBoxIndex = index;
                }
                index++;
            }

            this.cmbScriptLangs.SelectedIndex = _defaultScriptLangComboBoxIndex; //set before** attach event

            this.cmbScriptLangs.SelectedIndexChanged += (s, e) =>
            {
                _options.ScriptLang = (Typography.OpenFont.ScriptLang)this.cmbScriptLangs.SelectedItem;
                _options.InvokeAttachEvents();
            };
        }


        void SetupPositionTechniqueList()
        {
            cmbPositionTech.Items.Add(PositionTechnique.OpenFont);
            cmbPositionTech.Items.Add(PositionTechnique.Kerning);
            cmbPositionTech.Items.Add(PositionTechnique.None);
            cmbPositionTech.SelectedIndex = 0;
            cmbPositionTech.SelectedIndexChanged += (s, e) =>
            {
                _options.PositionTech = (PositionTechnique)cmbPositionTech.SelectedItem;
                _options.InvokeAttachEvents();
            };
        }

        void SetupRenderOptions()
        {
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithTextPrinterAndMiniAgg);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithMiniAgg_SingleGlyph);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithGdiPlusPath);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithMsdfGen);
            cmbRenderChoices.SelectedIndex = 0;
            cmbRenderChoices.SelectedIndexChanged += (s, e) =>
            {
                _options.RenderChoice = (RenderChoice)cmbRenderChoices.SelectedItem;
                _options.InvokeAttachEvents();
            };
        }
    }
}

﻿//MIT, 2016-present, WinterDev 
using System;
using System.Collections.Generic;
using System.IO;
using Typography.OpenFont;


namespace Typography.FontManagement
{

    public class InstalledTypeface
    {
        internal InstalledTypeface(string fontName,
            string fontSubFamily,
            string fontPath)
        {
            FontName = fontName;
            FontSubFamily = fontSubFamily;
            FontPath = fontPath;
        }

        public string FontName { get; internal set; }
        public string FontSubFamily { get; internal set; }
        public string FontPath { get; internal set; }


        public override string ToString()
        {
            return FontName + " " + FontSubFamily;
        }
    }
    [Flags]
    public enum TypefaceStyle
    {
        Others = 0,
        Normal = 1,
        Bold = 1 << 2,
        Italic = 1 << 3,
    }

    public interface FontStreamSource
    {
        Stream ReadFontStream();
        string PathName { get; }
    }

    public class FontFileStreamProvider : FontStreamSource
    {
        public FontFileStreamProvider(string filename)
        {
            this.PathName = filename;
        }
        public string PathName { get; private set; }
        public Stream ReadFontStream()
        {
            //TODO: don't forget to dispose this stream when not use
            return new FileStream(this.PathName, FileMode.Open, FileAccess.Read);

        }
    }

    public delegate void FirstInitFontCollectionDelegate(InstalledFontCollection fontCollection);

    public delegate InstalledTypeface FontNotFoundHandler(InstalledFontCollection fontCollection, string fontName, string fontSubFam, TypefaceStyle wellknownStyle);
    public delegate FontNameDuplicatedDecision FontNameDuplicatedHandler(InstalledTypeface existing, InstalledTypeface newAddedFont);
    public enum FontNameDuplicatedDecision
    {
        /// <summary>
        /// use existing, skip latest font
        /// </summary>
        Skip,
        /// <summary>
        /// replace with existing with the new one
        /// </summary>
        Replace
    }


    public interface IInstalledTypefaceProvider
    {
        InstalledTypeface GetInstalledTypeface(string fontName, TypefaceStyle style);
    }

    public class TypefaceStore : IInstalledTypefaceProvider
    {

        FontNotFoundHandler _defaultFontNotFoundHandler;
        FontNotFoundHandler _fontNotFoundHandler;
        Dictionary<InstalledTypeface, Typeface> _loadedTypefaces = new Dictionary<InstalledTypeface, Typeface>();
        public TypefaceStore()
        {
            _defaultFontNotFoundHandler = (fontCollection, fontName, subfamName, style) =>
            {
                //TODO: implement font not found mapping here
                //_fontsMapping["monospace"] = "Courier New";
                //_fontsMapping["Helvetica"] = "Arial";
                fontName = fontName.ToUpper();
                switch (fontName)
                {
                    case "MONOSPACE":
                        return fontCollection.GetInstalledTypeface("Courier New", style);
                    case "HELVETICA":
                        return fontCollection.GetInstalledTypeface("Arial", style);
                    case "TAHOMA":
                        //use can change this ...
                        //default font must found
                        //if not throw err 
                        //this prevent infinit loop
                        throw new System.NotSupportedException();
                    default:
                        return fontCollection.GetInstalledTypeface("tahoma", style);
                }
            };
        }

        static TypefaceStore s_typefaceStore;
        public static TypefaceStore GetTypefaceStoreOrCreateNewIfNotExist()
        {
            if (s_typefaceStore == null)
            {
                s_typefaceStore = new TypefaceStore();
            }
            return s_typefaceStore;
        }


        /// <summary>
        /// font collection of the store
        /// </summary>
        public InstalledFontCollection FontCollection { get; set; }
        public void SetFontNotFoundHandler(FontNotFoundHandler fontNotFoundHandler)
        {
            _fontNotFoundHandler = fontNotFoundHandler;
        }
        public Typeface GetTypeface(InstalledTypeface installedFont)
        {
            return GetTypefaceOrCreateNew(installedFont);
        }
        public Typeface GetTypeface(string fontname, string fontSubFam)
        {

            InstalledTypeface installedFont = FontCollection.GetInstalledTypeface(fontname, fontSubFam);
            //convert from   
            if (installedFont == null && _fontNotFoundHandler != null)
            {
                installedFont = _fontNotFoundHandler(this.FontCollection, fontname, fontSubFam, FontCollection.GetWellknownFontStyle(fontSubFam));
            }
            if (installedFont == null)
            {
                return null;
            }
            return GetTypefaceOrCreateNew(installedFont);
        }



        /// <summary>
        /// get typeface from wellknown style
        /// </summary>
        /// <param name="fontname"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public Typeface GetTypeface(string fontname, TypefaceStyle style)
        {
            InstalledTypeface installedFont = FontCollection.GetInstalledTypeface(fontname, style);
            if (installedFont == null && _fontNotFoundHandler != null)
            {
                installedFont = _fontNotFoundHandler(this.FontCollection, fontname, null, style);
            }

            if (installedFont == null && style == TypefaceStyle.Normal)
            {
                FontCollection.GetInstalledTypeface(fontname, "Regular");
            }

            if (installedFont == null)
            {
                return null;
            }
            return GetTypefaceOrCreateNew(installedFont);
        }
        Typeface GetTypefaceOrCreateNew(InstalledTypeface installedFont)
        {
            //load 
            //check if we have create this typeface or not 
            Typeface typeface;
            if (!_loadedTypefaces.TryGetValue(installedFont, out typeface))
            {
                //TODO: review how to load font here
                using (var fs = new FileStream(installedFont.FontPath, FileMode.Open, FileAccess.Read))
                {
                    var reader = new OpenFontReader();
                    typeface = reader.Read(fs);
                    typeface.Filename = installedFont.FontPath;
                }
                return _loadedTypefaces[installedFont] = typeface;
            }
            return typeface;
        }


        //----------------------------------------------------------------
        InstalledTypeface IInstalledTypefaceProvider.GetInstalledTypeface(string fontName, TypefaceStyle style)
        {
            //check if we have this font in the collection or not
            InstalledTypeface found = FontCollection.GetInstalledTypeface(fontName, style);
            if (found == null)
            {
                //not found
                if (_fontNotFoundHandler != null)
                {
                    return _fontNotFoundHandler(FontCollection, fontName, null, style);
                }
                else
                {
                    return _defaultFontNotFoundHandler(FontCollection, fontName, null, style);
                }

            }
            return found;
        }

    }

    public class InstalledFontCollection : IInstalledTypefaceProvider
    {



        class InstalledTypefaceGroup
        {

            internal Dictionary<string, InstalledTypeface> _members = new Dictionary<string, InstalledTypeface>();
            public void AddFont(InstalledTypeface installedFont)
            {

                _members.Add(installedFont.FontName.ToUpper(), installedFont);
            }
            public bool TryGetValue(string fontName, out InstalledTypeface found)
            {
                return _members.TryGetValue(fontName, out found);
            }
            public void Replace(InstalledTypeface newone)
            {
                _members[newone.FontName.ToUpper()] = newone;
            }
        }

        /// <summary>
        /// map from font subfam to internal group name
        /// </summary>
        Dictionary<string, InstalledTypefaceGroup> _subFamToFontGroup = new Dictionary<string, InstalledTypefaceGroup>();

        InstalledTypefaceGroup _normal, _bold, _italic, _bold_italic;
        List<InstalledTypefaceGroup> _allFontGroups = new List<InstalledTypefaceGroup>();
        FontNameDuplicatedHandler fontNameDuplicatedHandler;


        public InstalledFontCollection()
        {

            //-----------------------------------------------------
            //init wellknown subfam 
            _normal = CreateNewFontGroup(TypefaceStyle.Normal, "regular", "normal");
            _italic = CreateNewFontGroup(TypefaceStyle.Italic, "Italic", "italique");
            //
            _bold = CreateNewFontGroup(TypefaceStyle.Bold, "bold");
            //
            _bold_italic = CreateNewFontGroup(TypefaceStyle.Bold | TypefaceStyle.Italic, "bold italic");
            //
        }


        static InstalledFontCollection s_sharedFontCollection;
        public static InstalledFontCollection GetSharedFontCollection(FirstInitFontCollectionDelegate initdel)
        {
            if (s_sharedFontCollection == null)
            {
                //first time
                s_sharedFontCollection = new InstalledFontCollection();
                initdel(s_sharedFontCollection);
            }
            return s_sharedFontCollection;
        }
        public TypefaceStyle GetWellknownFontStyle(string subFamName)
        {
            switch (subFamName.ToUpper())
            {
                default: return TypefaceStyle.Others;
                case "NORMAL":
                case "REGULAR":
                    return TypefaceStyle.Normal;
                case "BOLD":
                    return TypefaceStyle.Bold;
                case "ITALIC":
                case "ITALIQUE":
                    return TypefaceStyle.Italic;
                case "BOLD ITALIC":
                    return (TypefaceStyle.Bold | TypefaceStyle.Italic);
            }
        }
        InstalledTypefaceGroup CreateNewFontGroup(TypefaceStyle installedFontStyle, params string[] names)
        {
            //create font group
            var fontGroup = new InstalledTypefaceGroup();
            //single dic may be called by many names            
            foreach (string name in names)
            {
                string upperCaseName = name.ToUpper();
                //register name
                //should not duplicate 
                _subFamToFontGroup.Add(upperCaseName, fontGroup);
            }
            _allFontGroups.Add(fontGroup);
            return fontGroup;
        }

        public void SetFontNameDuplicatedHandler(FontNameDuplicatedHandler handler)
        {
            fontNameDuplicatedHandler = handler;
        }
        public bool AddTypefaceSource(FontStreamSource src)
        {
            //preview data of font
            using (Stream stream = src.ReadFontStream())
            {
                var reader = new OpenFontReader();
                PreviewFontInfo previewFont = reader.ReadPreview(stream);
                if (string.IsNullOrEmpty(previewFont.fontName))
                {
                    //err!
                    return false;
                }
                //if (previewFont.fontName.StartsWith("Bungee"))
                //{

                //}

                return Register(new InstalledTypeface(previewFont.fontName, previewFont.fontSubFamily, src.PathName));
            }
        }
        bool Register(InstalledTypeface newfont)
        {
            InstalledTypefaceGroup selectedFontGroup;
            string fontsubFamUpperCaseName = newfont.FontSubFamily.ToUpper();

            if (!_subFamToFontGroup.TryGetValue(fontsubFamUpperCaseName, out selectedFontGroup))
            {
                //create new group, we don't known this font group before 
                //so we add to 'other group' list
                selectedFontGroup = new InstalledTypefaceGroup();
                _subFamToFontGroup.Add(fontsubFamUpperCaseName, selectedFontGroup);
                _allFontGroups.Add(selectedFontGroup);
            }
            //
            string fontNameUpper = newfont.FontName.ToUpper();

            InstalledTypeface found;
            if (selectedFontGroup.TryGetValue(fontNameUpper, out found))
            {
                //TODO:
                //we already have this font name
                //(but may be different file
                //we let user to handle it        
                switch (fontNameDuplicatedHandler(found, newfont))
                {
                    default: throw new NotSupportedException();
                    case FontNameDuplicatedDecision.Skip:
                        return false;
                    case FontNameDuplicatedDecision.Replace:
                        selectedFontGroup.Replace(newfont);
                        return true;
                }
            }
            else
            {
                selectedFontGroup.AddFont(newfont);
                return true;
            }
        }

        public InstalledTypeface GetInstalledTypeface(string fontName, string subFamName)
        {
            string upperCaseFontName = fontName.ToUpper();
            string upperCaseSubFamName = subFamName.ToUpper();


            //find font group
            InstalledTypefaceGroup foundFontGroup;
            if (_subFamToFontGroup.TryGetValue(upperCaseSubFamName, out foundFontGroup))
            {
                InstalledTypeface foundInstalledFont;
                foundFontGroup.TryGetValue(upperCaseFontName, out foundInstalledFont);
                return foundInstalledFont;
            }
            return null; //not found
        }

        public InstalledTypeface GetInstalledTypeface(string fontName, TypefaceStyle wellknownSubFam)
        {
            //not auto resolve
            InstalledTypefaceGroup selectedFontGroup;
            InstalledTypeface _found;
            switch (wellknownSubFam)
            {
                default: return null;
                case TypefaceStyle.Normal: selectedFontGroup = _normal; break;
                case TypefaceStyle.Bold: selectedFontGroup = _bold; break;
                case TypefaceStyle.Italic: selectedFontGroup = _italic; break;
                case (TypefaceStyle.Bold | TypefaceStyle.Italic): selectedFontGroup = _bold_italic; break;
            }
            if (selectedFontGroup.TryGetValue(fontName.ToUpper(), out _found))
            {
                return _found;
            }
            //-------------------------------------------

            //retry ....
            if (wellknownSubFam == TypefaceStyle.Bold)
            {
                //try get from Gras?
                //eg. tahoma
                if (_subFamToFontGroup.TryGetValue("GRAS", out selectedFontGroup))
                {

                    if (selectedFontGroup.TryGetValue(fontName.ToUpper(), out _found))
                    {
                        return _found;
                    }

                }
            }
            else if (wellknownSubFam == TypefaceStyle.Italic)
            {
                //TODO: simulate oblique (italic) font???
                selectedFontGroup = _normal;

                if (selectedFontGroup.TryGetValue(fontName.ToUpper(), out _found))
                {
                    return _found;
                }

            }



            return _found;
        }

        public IEnumerable<InstalledTypeface> GetInstalledFontIter()
        {
            foreach (InstalledTypefaceGroup fontgroup in _allFontGroups)
            {
                foreach (InstalledTypeface f in fontgroup._members.Values)
                {
                    yield return f;
                }
            }
        }
    }


    public static class InstalledFontCollectionExtension
    {
        public static void LoadFontsFromFolder(this InstalledFontCollection fontCollection, string folder)
        {
            try
            {
                // 1. font dir
                foreach (string file in Directory.GetFiles(folder))
                {
                    //eg. this is our custom font folder
                    string ext = Path.GetExtension(file).ToLower();
                    switch (ext)
                    {
                        default: break;
                        case ".ttf":
                        case ".otf":
                            fontCollection.AddTypefaceSource(new FontFileStreamProvider(file));
                            break;
                    }
                }

                //2. browse recursively; on Linux, fonts are organised in subdirectories
                foreach (string subfolder in Directory.GetDirectories(folder))
                {
                    LoadFontsFromFolder(fontCollection, subfolder);
                }
            }
            catch (DirectoryNotFoundException e)
            {
                return;
            }
        }
        public static void LoadSystemFonts(this InstalledFontCollection fontCollection)
        {
            // Windows system fonts
            LoadFontsFromFolder(fontCollection, "c:\\Windows\\Fonts");

            // These are reasonable places to look for fonts on Linux
            LoadFontsFromFolder(fontCollection, "/usr/share/fonts");
            LoadFontsFromFolder(fontCollection, "/usr/share/wine/fonts");
            LoadFontsFromFolder(fontCollection, "/usr/share/texlive/texmf-dist/fonts");
            LoadFontsFromFolder(fontCollection, "/usr/share/texmf/fonts");

            // OS X system fonts (https://support.apple.com/en-us/HT201722)
            LoadFontsFromFolder(fontCollection, "/System/Library/Fonts");
            LoadFontsFromFolder(fontCollection, "/Library/Fonts");
        }


        //for Windows , how to find Windows' Font Directory from Windows Registry
        //        string[] localMachineFonts = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts", false).GetValueNames();
        //        // get parent of System folder to have Windows folder
        //        DirectoryInfo dirWindowsFolder = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.System));
        //        string strFontsFolder = Path.Combine(dirWindowsFolder.FullName, "Fonts");
        //        RegistryKey regKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts");
        //        //---------------------------------------- 
        //        foreach (string winFontName in localMachineFonts)
        //        {
        //            string f = (string)regKey.GetValue(winFontName);
        //            if (f.EndsWith(".ttf") || f.EndsWith(".otf"))
        //            {
        //                yield return Path.Combine(strFontsFolder, f);
        //            }
        //        }

    }
}
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PKHeX
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            #region Pop open a splash screen while we load up.
            Thread SplashSCR = new Thread(() => new SplashScreen(this).ShowDialog());
            SplashSCR.Start();
            #endregion
            #region Initialize Form
            InitializeComponent();
            defaultControlWhite = CB_Species.BackColor;
            defaultControlText = Label_Species.ForeColor;
            CB_ExtraBytes.SelectedIndex = 0;

            // Resize Main Window to PKX Editing Mode
            largeWidth = Width;
            shortWidth = (Width * (30500 / 620)) / 100 + 1;
            Width = shortWidth;

            // Initialize Boxes
            byte[] ezeros = PKX.encryptArray(new byte[232]);
            for (int i = 0; i < 30 * 31; i++)
                Array.Copy(ezeros, 0, savefile, SaveGame.Box + i * 0xE8, 0xE8);
            #endregion
            #region Language Detection before loading
            // Set up Language Selection
            foreach (var cbItem in main_langlist)
                CB_MainLanguage.Items.Add(cbItem);

            // Try and detect the language
            int[] main_langnum = {2, 1, 3, 4, 5, 7, 8, 9};
            main_langnum = main_langnum.Concat(Enumerable.Range(10, lang_val.Length).Select(i => i).ToArray()).ToArray();
            string filename = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string lastTwoChars = filename.Substring(filename.Length - 2);
            int lang = Array.IndexOf(main_langnum, Array.IndexOf(lang_val, lastTwoChars));

            CB_MainLanguage.SelectedIndex = lang >= 0 ? main_langnum[lang] - 1 : ((lastTwoChars == "jp") ? 1 : 0);

            #region HaX
            HaX = (filename.IndexOf("HaX", StringComparison.Ordinal) >= 0);
            {
                CHK_HackedStats.Enabled = CHK_HackedStats.Visible =
                DEV_Ability.Enabled = DEV_Ability.Visible =
                MT_Level.Enabled = MT_Level.Visible =
                TB_AbilityNumber.Visible =
                MT_Form.Enabled = MT_Form.Visible = HaX;

                TB_Level.Visible =
                CB_Ability.Visible = !HaX;
            }
            #endregion
            Status = "Language set up";
            #endregion
            #region Localize & Populate
            InitializeFields(); Status = "Fields set up";
            CB_Language.SelectedIndex = (lang >= 0 && lang < 7) ? main_langnum[lang] : 1;
            #endregion
            #region Add ContextMenus to the PictureBoxes (PKX slots)

            ContextMenuStrip mnu = new ContextMenuStrip();
            ToolStripMenuItem mnuView = new ToolStripMenuItem("View");
            ToolStripMenuItem mnuSet = new ToolStripMenuItem("Set");
            ToolStripMenuItem mnuDelete = new ToolStripMenuItem("Delete");
            // Assign event handlers
            mnuView.Click += clickView;
            mnuSet.Click += clickSet;
            mnuDelete.Click += clickDelete;
            // Add to main context menu
            mnu.Items.AddRange(new ToolStripItem[] { mnuView, mnuSet, mnuDelete });

            // Assign to datagridview for Box Pokemon and Party Pokemon
            foreach (PictureBox pb in PAN_Box.Controls)
                pb.ContextMenuStrip = mnu;
            foreach (PictureBox pb in PAN_Party.Controls)
                pb.ContextMenuStrip = mnu;

            // Add ContextMenus to the PictureBoxes that are read only
            PictureBox[] pba2 = {
                                    bbpkx1,bbpkx2,bbpkx3,bbpkx4,bbpkx5,bbpkx6,

                                    dcpkx1, dcpkx2, gtspkx, fusedpkx, subepkx1, subepkx2, subepkx3
                               };
            ContextMenuStrip mnu2 = new ContextMenuStrip();
            ToolStripMenuItem mnu2View = new ToolStripMenuItem("View");

            // Assign event handlers
            mnu2View.Click += clickView;

            // Add to main context menu
            mnu2.Items.AddRange(new ToolStripItem[] { mnu2View });

            // Assign to datagridview
            foreach (PictureBox p in pba2)
                p.ContextMenuStrip = mnu2;

            #endregion
            #region Enable Drag and Drop on the form & tab control.
            AllowDrop = true;
            DragEnter += tabMain_DragEnter;
            DragDrop += tabMain_DragDrop;

            // Enable Drag and Drop on each tab.
            tabMain.AllowDrop = true;
            tabMain.DragEnter += tabMain_DragEnter;
            tabMain.DragDrop += tabMain_DragDrop;

            foreach (TabPage tab in tabMain.Controls)
            {
                tab.AllowDrop = true;
                tab.DragEnter += tabMain_DragEnter;
                tab.DragDrop += tabMain_DragDrop;
            }

            // ToolTips for Drag&Drop
            ToolTip dragoutTip1 = new ToolTip();
            dragoutTip1.SetToolTip(dragout, "PK6 QuickSave");

            // Box Drag & Drop
            foreach (PictureBox pb in PAN_Box.Controls)
                pb.AllowDrop = true;

            // Box to Tabs D&D
            dragout.AllowDrop = true;
            #endregion
            #region Finish Up
            // Load the arguments
            Status = "Checking load args.";
            string[] args = Environment.GetCommandLineArgs();
            pathSDF = Util.GetSDFLocation();
            path3DS = Util.get3DSLocation();
            if (args.Length > 1)
                openQuick(args[1]);
            else if (pathSDF != null)
                openQuick(Path.Combine(pathSDF, "main"));
            else if (File.Exists(Util.NormalizePath(Path.Combine(Util.GetTempFolder(), "root" + Path.DirectorySeparatorChar + "main"))))
                openQuick(Util.NormalizePath(Path.Combine(Util.GetTempFolder(), "root" + Path.DirectorySeparatorChar + "main")));

            GB_OT.Click += clickGT;
            GB_nOT.Click += clickGT;
            GB_Daycare.Click += switchDaycare;

            Status = "Setting game font.";
            TB_Nickname.Font = PKX.getPKXFont(11);
            TB_OT.Font = (Font)TB_Nickname.Font.Clone();
            TB_OTt2.Font = (Font)TB_Nickname.Font.Clone();
            Status = "Initialized!";
            CB_Species.SelectedIndex = 1;

            init = true;

            // Splash Screen closes on its own.
            BringToFront();
            WindowState = FormWindowState.Minimized;
            Show();
            WindowState = FormWindowState.Normal;
            if (HaX) Util.Alert("Illegal mode activated.", "Please behave.");
            #endregion
        }

        #region Global Variables: Always Visible!
        public byte[] buff = new byte[260]; // Tab Pokemon Data Storage
        public byte[] savefile = new byte[0x100000];
        public byte[] cyberSAV = new byte[0x65600];
        public static byte[] ramsav;
        public static bool ramsavloaded;
        public bool savegame_oras = true;
        public bool cybergadget;
        public bool savLoaded;
        public int savindex;
        public bool savedited;
        public string pathSDF;
        public string path3DS;

        public static string Status = "Starting up PKHeX...";
        public static bool HaX;
        public static bool specialChars; // Open Form Tracking
        public static Color defaultControlWhite;
        public static Color defaultControlText;
        public static int colorizedbox = 32;
        public static Image mixedHighlight = Util.LayerImage(Properties.Resources.slotSet, Properties.Resources.slotView, 0, 0, 0.5);
        public static Image colorizedcolor;
        public static int colorizedslot;
        public static int largeWidth, shortWidth;
        public static string eggname = "";
        public static string[] lang_val = { "en", "ja", "fr", "it", "de", "es", "ko", "zh", "pt" };
        public static string[] main_langlist = 
            {
                "English", // ENG
                "日本語", // JPN
                "Français", // FRE
                "Italiano", // ITA
                "Deutsch", // GER
                "Español", // SPA
                "한국어", // KOR
                "中文", // CHN
                "Português", // Portuguese
            };
        public static string[] gendersymbols = { "♂", "♀", "-" };
        public static string[] specieslist = { };
        public static string[] movelist = { };
        public static string[] itemlist = { };
        public static string[] abilitylist = { };
        public static string[] types = { };
        public static string[] natures = { };
        public static string[] characteristics = { };
        public static string[] memories = { };
        public static string[] genloc = { };
        public static string[] forms = { };
        public static string[] metHGSS_00000 = { };
        public static string[] metHGSS_02000 = { };
        public static string[] metHGSS_03000 = { };
        public static string[] metBW2_00000 = { };
        public static string[] metBW2_30000 = { };
        public static string[] metBW2_40000 = { };
        public static string[] metBW2_60000 = { };
        public static string[] metXY_00000 = { };
        public static string[] metXY_30000 = { };
        public static string[] metXY_40000 = { };
        public static string[] metXY_60000 = { };
        public static string[] trainingbags = { };
        public static string[] trainingstage = { };
        public static string[] wallpapernames = { };
        public static string[] encountertypelist = { };
        public static string[] gamelanguages = { };
        public static string[] consoleregions = { };
        public static string[] balllist = { };
        public static string[] gamelist = { };
        public static string[] puffs = { };
        public static string[] itempouch = { };
        public static string origintrack;
        public static string curlanguage = "en";
        public volatile bool init;
        public static bool unicode;
        public ToolTip Tip1 = new ToolTip();
        public ToolTip Tip2 = new ToolTip();
        public ToolTip Tip3 = new ToolTip();
        public ToolTip NatureTip = new ToolTip();
        public PKX.Structures.SaveGame SaveGame = new PKX.Structures.SaveGame("ORAS");
        #endregion

        #region //// MAIN MENU FUNCTIONS ////
        // Main Menu Strip UI Functions
        private void mainMenuOpen(object sender, EventArgs e)
        {
            string cyberpath = Util.GetTempFolder();
            pathSDF = Util.GetSDFLocation();
            path3DS = Util.get3DSLocation();
            if (pathSDF != null)
            {
                OpenPKX.InitialDirectory = pathSDF;
                OpenPKX.RestoreDirectory = true;
                OpenPKX.FilterIndex = 4;
            }
            else if (path3DS != null)
            {
                OpenPKX.InitialDirectory = path3DS;
                OpenPKX.RestoreDirectory = true;
                OpenPKX.FilterIndex = 4;
            }
            else if (Directory.Exists(Path.Combine(cyberpath, "root")))
            {
                OpenPKX.InitialDirectory = Path.Combine(cyberpath, "root");
                OpenPKX.RestoreDirectory = true;
                OpenPKX.FilterIndex = 4;
            }
            else if (Directory.Exists(cyberpath))
            {
                OpenPKX.InitialDirectory = cyberpath;
                OpenPKX.RestoreDirectory = true;
                OpenPKX.FilterIndex = 4;
            }

            DialogResult result = OpenPKX.ShowDialog();
            if (result != DialogResult.OK) return;

            string path = OpenPKX.FileName;
            openQuick(path);
        }
        private void mainMenuSave(object sender, EventArgs e)
        {
            if (!verifiedPKX()) { return; }
            SavePKX.FileName = TB_Nickname.Text + " - " + TB_PID.Text;
            DialogResult result = SavePKX.ShowDialog();
            if (result != DialogResult.OK) return;
            string path = SavePKX.FileName;
            // Injection Dummy Override
            if (path.Contains("pokemon.ekx")) path = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + "pokemon.ekx";
            string ext = Path.GetExtension(path);

            if (File.Exists(path) && !path.Contains("pokemon.ekx"))
            {
                // File already exists, save a .bak
                byte[] backupfile = File.ReadAllBytes(path);
                File.WriteAllBytes(path + ".bak", backupfile);
            }
            byte[] pkx = preparepkx();

            if ((ext == ".ekx") || (ext == ".bin") || (ext == ".pkx") || (ext == ".ek6") || (ext == ".pk6"))
            {
                if ((ext == ".ekx") || (ext == ".bin") || (ext == ".ek6")) // User Requested Encrypted File
                    pkx = PKX.encryptArray(pkx);
                File.WriteAllBytes(path, pkx.ToArray());
            }
            else
            {
                Util.Error(String.Format("Foreign File Extension: {0}", ext), "Exporting as encrypted.");
                pkx = PKX.encryptArray(pkx);
                File.WriteAllBytes(path, pkx);
            }
        }
        private void mainMenuExit(object sender, EventArgs e)
        {
            if (ModifierKeys == (Keys.Control | Keys.E)) // Hotkey Triggered
                if (DialogResult.Yes != Util.Prompt(MessageBoxButtons.YesNo, "Quit PKHeX?")) return;
            Close();
        }
        private void mainMenuAbout(object sender, EventArgs e)
        {
            // Open a new form with the About details.
            new About().ShowDialog();
        }
        private void mainMenuWiden(object sender, EventArgs e)
        {
            int newwidth;
            if (Width < Height)
            {
                newwidth = largeWidth;
                tabBoxMulti.SelectedIndex = 0;
            }
            else
                newwidth = shortWidth;

            Width = newwidth;
        }
        private void mainMenuCodeGen(object sender, EventArgs e)
        {
            // Open Code Generator
            byte[] formdata = null;
            if (verifiedPKX()) formdata = preparepkx();
            CodeGenerator CodeGen = new CodeGenerator(this, formdata);
            CodeGen.ShowDialog();
            byte[] data = CodeGen.returnArray;
            if (data == null) return;
            byte[] decdata = PKX.decryptArray(data);
            Array.Copy(decdata, buff, 232);
            try { populateFields(buff); }
            catch
            {
                Array.Copy(new byte[232], buff, 232);
                populateFields(buff);
                Util.Error("Imported code did not decrypt properly", "Please verify that what you imported was correct.");
            }
        }
        private void mainMenuBoxReport(object sender, EventArgs e)
        {
            frmReport ReportForm = new frmReport();
            int offset = 0x27A00; if (savegame_oras) offset = 0x33000 + 0x5400;
            ReportForm.Show();
            ReportForm.PopulateData(savefile, savindex, offset);
        }
        private void mainMenuUnicode(object sender, EventArgs e)
        {
            unicode = (gendersymbols[0] == "♂");
            if (unicode)
            {
                gendersymbols = new[] { "M", "F", "-" };
                BTN_Shinytize.Text = "*";
                TB_Nickname.Font = TB_OT.Font = TB_OTt2.Font = Label_TID.Font;
            }
            else
            {
                gendersymbols = new[] { "♂", "♀", "-" };
                BTN_Shinytize.Text = "☆";
                TB_Nickname.Font = TB_OT.Font = TB_OTt2.Font = PKX.getPKXFont(11);
            }
            // Switch active gender labels to new if they are active.
            if (PKX.getGender(Label_Gender.Text) < 2)
                Label_Gender.Text = gendersymbols[PKX.getGender(Label_Gender.Text)];
            if (PKX.getGender(Label_OTGender.Text) < 2)
                Label_OTGender.Text = gendersymbols[PKX.getGender(Label_OTGender.Text)];
            if (PKX.getGender(Label_CTGender.Text) < 2)
                Label_CTGender.Text = gendersymbols[PKX.getGender(Label_CTGender.Text)];
        }

        // Main Menu Subfunctions
        private void openQuick(string path)
        {
            // detect if it is a folder (load into boxes or not)
            if (Directory.Exists(path))
            { loadBoxesFromDB(path); return; }

            string ext = Path.GetExtension(path);
            FileInfo fi = new FileInfo(path);
            if (fi.Length > 0x10009C)
                Util.Error("Input file is too large.", path);
            else
            {

                byte[] input; try { input = File.ReadAllBytes(path); }
                catch { Util.Error("File is in use by another program!", path); return; }

                try { openFile(input, path, ext); }
                catch
                {
                    try
                    {
                        byte[] blank = PKX.encryptArray(new byte[260]);

                        for (int i = 0; i < 232; i++)
                            blank[i] ^= input[i];

                        openFile(blank, path, ext);
                    }
                    catch { openFile(input, path, ext); }
                }
            }
        }
        private void openFile(byte[] input, string path, string ext)
        {
            #region Powersaves Read-Only Conversion
            if (input.Length == 0x10009C) // Resize to 1MB
            {
                Array.Copy(input, 0x9C, input, 0, 0x100000);
                Array.Resize(ref input, 0x100000);
            }
            #endregion

            #region Saves
            if ((input.Length == 0x76000) && BitConverter.ToUInt32(input, 0x75E10) == 0x42454546) // ORAS
                openMAIN(input, path, "ORAS", true);
            else if ((input.Length == 0x65600) && BitConverter.ToUInt32(input, 0x65410) == 0x42454546) // XY
                openMAIN(input, path, "XY", false);
            // Verify the Data Input Size is Proper
            else if (input.Length == 0x100000)
            {
                if (openXOR(input, path)) // Check if we can load the save via xorpad
                    return; // only if a save is loaded we abort
                if (BitConverter.ToUInt64(input, 0x10) != 0) // encrypted save
                { Util.Error("PKHeX only edits decrypted save files.", "This save file is not decrypted."); return; }

                string GameType = "XY"; // Default Game Type to load.
                if (BitConverter.ToUInt32(input, 0x7B210) == 0x42454546) GameType = "ORAS"; // BEEF magic in checksum block
                if ((BitConverter.ToUInt32(input, 0x100) != 0x41534944) && (BitConverter.ToUInt32(input, 0x5234) != 0x6E69616D))
                {
                    DialogResult dialogResult = Util.Prompt(MessageBoxButtons.YesNo, "Save file is not decrypted.", "Press Yes to ignore this warning and continue loading the save file.");
                    if (dialogResult != DialogResult.Yes) return;

                    DialogResult sdr = Util.Prompt(MessageBoxButtons.YesNoCancel, "Press Yes to load the sav at 0x3000", "Press No for the one at 0x82000");
                    if (sdr == DialogResult.Cancel) return;

                    savindex = (sdr == DialogResult.Yes) ? 0 : 1;
                    B_SwitchSAV.Enabled = true;
                    open1MB(input, path, GameType, false);
                }
                else if (PKX.detectSAVIndex(input, ref savindex) == 2)
                {
                    DialogResult dialogResult = Util.Prompt(MessageBoxButtons.YesNo, "Hash verification failed.", "Press Yes to ignore this warning and continue loading the save file.");
                    if (dialogResult != DialogResult.Yes) return;

                    DialogResult sdr = Util.Prompt(MessageBoxButtons.YesNoCancel, "Press Yes to load the sav at 0x3000", "Press No for the one at 0x82000");
                    if (sdr == DialogResult.Cancel)
                    {
                        savindex = 0;
                        return; // abort load
                    }
                    savindex = (sdr == DialogResult.Yes) ? 0 : 1;
                    B_SwitchSAV.Enabled = true;
                    open1MB(input, path, GameType, false);
                }
                else
                {
                    B_ExportSAV.Enabled = true;
                    B_SwitchSAV.Enabled = true;
                    PKX.detectSAVIndex(input, ref savindex);
                    open1MB(input, path, GameType, false);
                }
            }
            #endregion
            #region PK6/EK6
            else if ((input.Length == 260) || (input.Length == 232))
            {
                // Check if Input is PKX
                if ((ext == ".pk6") || (ext == ".ek6") || (ext == ".pkx") || (ext == ".ekx") || (ext == ".bin") || (ext == ""))
                {
                    // Check if Encrypted before Loading
                    buff = (BitConverter.ToUInt16(input, 0xC8) == 0 && BitConverter.ToUInt16(input, 0x58) == 0) ? input : PKX.decryptArray(input);
                    populateFields(buff);
                }
                else
                    Util.Error("Unable to recognize file." + Environment.NewLine + "Only valid .pk* .ek* .bin supported.", String.Format("File Loaded:{0}{1}", Environment.NewLine, path));
            }
            #endregion
            #region PK3/PK4/PK5
            else if ((input.Length == 136) || (input.Length == 220) || (input.Length == 236) || (input.Length == 100) || (input.Length == 80)) // to convert g5pkm
            {
                var Converter = new pk2pk();
                if (!PKX.verifychk(input)) Util.Error("Invalid File (Checksum Error)");
                try // to convert g5pkm
                {
                    byte[] data = Converter.ConvertPKM(input, savefile, savindex);
                    Array.Copy(data, buff, 232);
                    populateFields(buff);
                }
                catch
                {
                    Array.Copy(new byte[232], buff, 232);
                    populateFields(buff);
                    Util.Error("Attempted to load previous generation PKM.", "Conversion failed.");
                }
            }
            #endregion
            #region Trade Packets
            else if (input.Length == 363 && BitConverter.ToUInt16(input, 0x6B) == 0)
            {
                // EAD Packet of 363 length
                byte[] c = new byte[260];
                Array.Copy(input, 0x67, c, 0, 260);
            }
            else if (input.Length == 407 && BitConverter.ToUInt16(input, 0x98) == 0)
            {
                // EAD Packet of 407 length
                byte[] c = new byte[260];
                Array.Copy(input, 0x93, c, 0, 260);
            }
            #endregion
            #region Box Data
            else if ((input.Length == 0xE8 * 30 || input.Length == 0xE8 * 30 * 31) && BitConverter.ToUInt16(input, 4) == 0 && BitConverter.ToUInt32(input , 8) > 0)
            { 
                Array.Copy(input, 0, savefile, SaveGame.Box + 0xE8 * 30 * ((input.Length == 0xE8*30) ? C_BoxSelect.SelectedIndex : 0), input.Length); 
                setPKXBoxes();
                Width = largeWidth;
                Util.Alert("Box Binary loaded."); }
            #endregion
            #region injectiondebug
            else if (input.Length == 0x10000)
            { 
                int offset = -1; // Seek to find data start
                for (int i = 0; i < 0x800; i++)
                {
                    byte[] data = PKX.decryptArray(input.Skip(i).Take(0xE8).ToArray());
                    if (PKX.getCHK(data) != BitConverter.ToUInt16(data, 6)) continue;
                    offset = i; break;
                }
                if (offset < 0) { Util.Alert(path, "Unable to read the input file; not an expected injectiondebug.bin."); return; }
                C_BoxSelect.SelectedIndex = 0;
                Array.Copy(input, offset, savefile, SaveGame.Box + 0xE8 * 30 * C_BoxSelect.SelectedIndex, 9 * 30 * 0xE8);
                setPKXBoxes();
                Width = largeWidth;
                Util.Alert("Injection Binary loaded."); }
            #endregion
            #region RAMSAV
            else if (( /*XY*/ input.Length == 0x70000 || /*ORAS*/ input.Length == 0x80000) && Path.GetFileName(path).Contains("ram"))
            {
                if (input.Length == 0x80000)
                    // Scan for FEEB in XY location, 3DS only overwrites data if file already exists.
                    for (int i = 0x60000; i < 0x64000; i+=4)
                        if (BitConverter.ToUInt32(input, i) == 0x42454546) { Array.Resize(ref input, 0x70000); break; }

                bool o = (input.Length == 0x80000);
                try { openMAIN(ram2sav.getMAIN(input), path, (o) ? "ORAS" : "XY", o, true); } catch { return; }
                ramsav = (byte[])input.Clone();
            }
            #endregion
            #region Battle Video
            else if (input.Length == 0x2E60 && BitConverter.ToUInt64(input, 0xE18) != 0 && BitConverter.ToUInt16(input, 0xE12) == 0)
            {
                if (Util.Prompt(MessageBoxButtons.YesNo, "Load Batte Video Pokémon data to " + C_BoxSelect.Text + "?", "The first 24 slots will be overwritten.") != DialogResult.Yes) return;
                for (int i = 0; i < 24; i++)
                    Array.Copy(input, 0xE18 + 260 * i + (i / 6) * 8, savefile, SaveGame.Box + i * 0xE8 + C_BoxSelect.SelectedIndex * 30 * 0xE8, 0xE8);
                setPKXBoxes();
            }
            #endregion
            else
                Util.Error("Attempted to load an unsupported file type/size.", "File Loaded:" + Environment.NewLine + path, "File Size:" + Environment.NewLine + new FileInfo(path).Length.ToString("X8"));
        }
        private void openMAIN(byte[] input, string path, string GameType, bool oras, bool ram = false)
        {
            ramsavloaded = ram;
            L_Save.Text = "SAV: " + Path.GetFileName(path);
            SaveGame = new PKX.Structures.SaveGame(GameType);

            // Load CyberGadget
            savindex = 0;
            savefile = new byte[0x100000];
            cyberSAV = input;
            cybergadget = true;
            B_ExportSAV.Enabled = true;
            B_SwitchSAV.Enabled = false;
            Array.Copy(input, 0, savefile, 0x5400, input.Length);

            openSave(oras);
        }
        private void open1MB(byte[] input, string path, string GameType, bool oras)
        {
            ramsavloaded = false;
            L_Save.Text = "SAV: " + Path.GetFileName(path);
            SaveGame = new PKX.Structures.SaveGame(GameType);
            savegame_oras = oras;

            savefile = input;
            cybergadget = false;

            // Logic to allow unlocking of Switch SAV
            // Setup SHA
            SHA256 mySHA256 = SHA256.Create();

            // Check both IVFC Hashes
            byte[] zeroarray = new byte[0x200];
            Array.Copy(savefile, 0x2000 + 0 * 0x7F000, zeroarray, 0, 0x20);
            byte[] hashValue1 = mySHA256.ComputeHash(zeroarray);
            Array.Copy(savefile, 0x2000 + 1 * 0x7F000, zeroarray, 0, 0x20);
            byte[] hashValue2 = mySHA256.ComputeHash(zeroarray);

            byte[] realHash1 = new byte[0x20];
            byte[] realHash2 = new byte[0x20];

            Array.Copy(savefile, 0x43C - 0 * 0x130, realHash1, 0, 0x20);
            Array.Copy(savefile, 0x43C - 1 * 0x130, realHash2, 0, 0x20);

            B_SwitchSAV.Enabled = (hashValue1.SequenceEqual(realHash1) && hashValue2.SequenceEqual(realHash2));
            getSAVOffsets(ref oras); // to detect if we are ORAS or not
            Array.Copy(savefile, 0x5400 + 0x7F000 * savindex, cyberSAV, 0, cyberSAV.Length);

            openSave(oras);
        }
        private bool openXOR(byte[] input, string path)
        {
            // Detection of stored Decryption XORpads:
            if (ModifierKeys == Keys.Control) return false; // no xorpad compatible
            byte[] savID = new byte[0x10]; Array.Copy(input, 0x10, savID, 0, 0x10);
            string exepath = Application.StartupPath;
            string xorpath = exepath.Clone().ToString();
            string[] XORpads = Directory.GetFiles(xorpath);

            check:
            foreach (byte[] data in from file in XORpads let fi = new FileInfo(file) where (fi.Name.ToLower().Contains("xorpad") || fi.Name.ToLower().Contains("key")) && (fi.Length == 0x10009C || fi.Length == 0x100000) select File.ReadAllBytes(file))
            {
                // Fix xorpad alignment
                byte[] xorpad = data;
                if (xorpad.Length == 0x10009C)
                {
                    Array.Copy(xorpad, 0x9C, xorpad, 0, 0x100000);
                    Array.Resize(ref xorpad, 0x100000);
                }
                byte[] xorID = new byte[0x10]; Array.Copy(xorpad, 0x10, xorID, 0, 0x10);
                if (!xorID.SequenceEqual(savID)) continue;

                // Set up Decrypted File
                byte[] decryptedPS = new byte[0x76000];
                Array.Copy(input, 0x5400, decryptedPS, 0, 0x76000);

                // xor through and decrypt
                for (int z = 0; z < 0x76000; z++)
                    decryptedPS[z] ^= xorpad[0x5400 + z];

                // Weakly check the validity of the decrypted content
                if (BitConverter.ToUInt32(decryptedPS, 0x76000 - 0x1F0) != 0x42454546) // Not OR/AS
                    if (BitConverter.ToUInt32(decryptedPS, 0x65600 - 0x1F0) != 0x42454546)
                        continue; // Not X/Y, so continue.
                    else
                        Array.Resize(ref decryptedPS, 0x65600); // set to X/Y size
                else Array.Resize(ref decryptedPS, 0x76000); // set to ORAS size just in case

                // Save file is now decrypted! Reset the loading variables.
                bool oras = (decryptedPS.Length == 0x76000);
                string GameType = oras ? "ORAS" : "XY";

                // Trigger Loading of the decrypted save file.
                openMAIN(decryptedPS, path, GameType, oras);

                // Abort the opening of a non-cyber file.
                return true;
            }
            // End file check loop, check the input path for xorpads too if it isn't the same as the EXE (quite common).
            if (xorpath != exepath) return false; // no xorpad compatible
            xorpath = Path.GetDirectoryName(path); goto check;
        }
        private void openSave(bool oras)
        {
            savegame_oras = oras;
            // Enable Secondary Tools
            GB_SAVtools.Enabled =
                B_JPEG.Enabled = true;

            savedited = false;
            Menu_ToggleBoxUI.Visible = false;

            B_VerifySHA.Enabled = !cybergadget;
            B_VerifyCHK.Enabled = !ramsavloaded;

            // Set up Boxes
            C_BoxSelect.SelectedIndex = 0;
            tabBoxMulti.SelectedIndex = 0;

            setBoxNames();   // Display the Box Names
            setPKXBoxes();   // Reload all of the PKX Windows
            setSAVLabel();   // Reload the label indicating current save

            // Version Exclusive Editors
            GB_SUBE.Visible = !oras;
            B_OpenSecretBase.Visible = oras;

            Width = largeWidth;
            savLoaded = true;
            // Indicate audibly the save is loaded
            System.Media.SystemSounds.Beep.Play();
        }

        // Language Translation
        private void changeMainLanguage(object sender, EventArgs e)
        {
            if (init) buff = preparepkx(); // get data currently in form

            Menu_Options.DropDown.Close();
            InitializeStrings();
            InitializeLanguage();
            Util.TranslateInterface(this, lang_val[CB_MainLanguage.SelectedIndex], menuStrip1); // Translate the UI to language.
            populateFields(buff); // put data back in form
        }
        private void InitializeStrings()
        {
            if (CB_MainLanguage.SelectedIndex < 8)
                curlanguage = lang_val[CB_MainLanguage.SelectedIndex];

            string l = curlanguage;
            natures = Util.getStringList("Natures", l);
            types = Util.getStringList("Types", l);
            abilitylist = Util.getStringList("Abilities", l);
            movelist = Util.getStringList("Moves", l);
            itemlist = Util.getStringList("Items", l);
            characteristics = Util.getStringList("Character", l);
            specieslist = Util.getStringList("Species", l);
            wallpapernames = Util.getStringList("Wallpaper", l);
            itempouch = Util.getStringList("ItemPouch", l);
            encountertypelist = Util.getStringList("EncounterType", l);
            gamelist = Util.getStringList("Games", l);
            gamelanguages = Util.getNulledStringArray(Util.getSimpleStringList("languages"));
            consoleregions = Util.getNulledStringArray(Util.getSimpleStringList("regions3ds"));

            balllist = new string[Legal.Items_Ball.Length];
            for (int i = 0; i < balllist.Length; i++)
                balllist[i] = itemlist[Legal.Items_Ball[i]];

            if ((l != "zh") || (l == "zh" && !init)) // load initial binaries
            {
                forms = Util.getStringList("Forms", l);
                memories = Util.getStringList("Memories", l);
                genloc = Util.getStringList("GenLoc", l);
                trainingbags = Util.getStringList("TrainingBag", l);
                trainingstage = Util.getStringList("SuperTraining", l);
                puffs = Util.getStringList("Puff", l);
            }

            // Fix Item Names (Duplicate entries)
            itemlist[456] += " (OLD)"; // S.S. Ticket
            itemlist[463] += " (OLD)"; // Storage Key
            itemlist[478] += " (OLD)"; // Basement Key
            itemlist[626] += " (2)"; // Xtransceiver
            itemlist[629] += " (2)"; // DNA Splicers
            itemlist[637] += " (2)"; // Dropped Item
            itemlist[707] += " (2)"; // Travel Trunk
            itemlist[713] += " (2)"; // Alt Bike
            itemlist[714] += " (2)"; // Holo Caster
            itemlist[729] += " (1)"; // Meteorite
            itemlist[740] += " (2)"; // Contest Costume
            itemlist[751] += " (2)"; // Meteorite
            itemlist[771] += " (3)"; // Meteorite
            itemlist[772] += " (4)"; // Meteorite

            // Get the Egg Name and then replace it with --- for the comboboxes.
            eggname = specieslist[0];
            specieslist[0] = "---";

            // Get the met locations... for all of the games...
            metHGSS_00000 = Util.getStringList("hgss_00000", l);
            metHGSS_02000 = Util.getStringList("hgss_02000", l);
            metHGSS_03000 = Util.getStringList("hgss_03000", l);

            metBW2_00000 = Util.getStringList("bw2_00000", l);
            metBW2_30000 = Util.getStringList("bw2_30000", l);
            metBW2_40000 = Util.getStringList("bw2_40000", l);
            metBW2_60000 = Util.getStringList("bw2_60000", l);

            metXY_00000 = Util.getStringList("xy_00000", l);
            metXY_30000 = Util.getStringList("xy_30000", l);
            metXY_40000 = Util.getStringList("xy_40000", l);
            metXY_60000 = Util.getStringList("xy_60000", l);

            // Fix up some of the Location strings to make them more descriptive:
            metHGSS_02000[1] += " (NPC)";         // Anything from an NPC
            metHGSS_02000[2] += " (" + eggname + ")"; // Egg From Link Trade
            metBW2_00000[36] = metBW2_00000[84] + "/" + metBW2_00000[36]; // Cold Storage in BW = PWT in BW2

            // BW2 Entries from 76 to 105 are for Entralink in BW
            for (int i = 76; i < 106; i++)
                metBW2_00000[i] = metBW2_00000[i] + "●";

            // Localize the Poketransfer to the language (30001)
            string[] ptransp = { "Poké Transfer", "ポケシフター", "Poké Fret", "Pokétrasporto", "Poképorter", "Pokétransfer", "포케시프터", "ポケシフター" };
            metBW2_30000[1 - 1] = ptransp[Array.IndexOf(lang_val, curlanguage)];
            metBW2_30000[2 - 1] += " (NPC)";              // Anything from an NPC
            metBW2_30000[3 - 1] += " (" + eggname + ")";  // Egg From Link Trade

            // Zorua/Zoroark events
            metBW2_30000[10 - 1] = specieslist[251] + " (" + specieslist[570] + " 1)"; // Celebi's Zorua Event
            metBW2_30000[11 - 1] = specieslist[251] + " (" + specieslist[570] + " 2)"; // Celebi's Zorua Event
            metBW2_30000[12 - 1] = specieslist[571] + " (" + "1)"; // Zoroark
            metBW2_30000[13 - 1] = specieslist[571] + " (" + "2)"; // Zoroark

            metBW2_60000[3 - 1] += " (" + eggname + ")";  // Egg Treasure Hunter/Breeder, whatever...

            metXY_00000[104] += " (X/Y)";              // Victory Road
            metXY_00000[298] += " (OR/AS)";            // Victory Road
            metXY_30000[0] += " (NPC)";                // Anything from an NPC
            metXY_30000[1] += " (" + eggname + ")";    // Egg From Link Trade

            // Set the first entry of a met location to "" (nothing)
            // Fix (None) tags
            abilitylist[0] = itemlist[0] = movelist[0] = metXY_00000[0] = metBW2_00000[0] = metHGSS_00000[0] = "(" + itemlist[0] + ")";

            // Force an update to the met locations
            origintrack = "";

            if (init)
                updateIVs(null, null); // Prompt an update for the characteristics
        }
        #endregion

        #region //// PKX WINDOW FUNCTIONS ////
        private void InitializeFields()
        {
            // Now that the ComboBoxes are ready, load the data.
            populateFields(buff);
            {
                TB_OT.Text = "PKHeX";
                TB_TID.Text = 12345.ToString();
                TB_SID.Text = 54321.ToString();
                CB_Species.SelectedIndex = 1;
                CB_GameOrigin.SelectedIndex = 0;
                CB_Language.SelectedIndex = 0;
                C_BoxSelect.SelectedIndex = 0;
                CB_GameOrigin.SelectedIndex = 0;
                CB_PPu1.SelectedIndex = CB_PPu2.SelectedIndex = CB_PPu3.SelectedIndex = CB_PPu4.SelectedIndex = 0;
                CB_Ball.SelectedIndex = 0;
                CB_Country.SelectedIndex = 0;
                setAbilityList(TB_AbilityNumber, Util.getIndex(CB_Species), CB_Ability, CB_Form);
            }
        }
        private void InitializeLanguage()
        {
            // Set the Display
            CB_Country.DisplayMember = 
                CB_SubRegion.DisplayMember = 
                CB_3DSReg.DisplayMember =
                CB_Language.DisplayMember =
                CB_Ball.DisplayMember =
                CB_HeldItem.DisplayMember =
                CB_Species.DisplayMember =
                DEV_Ability.DisplayMember =
                CB_Nature.DisplayMember =
                CB_EncounterType.DisplayMember =
                CB_GameOrigin.DisplayMember =
                CB_HPType.DisplayMember = "Text";

            // Set the Value
            CB_Country.ValueMember = 
                CB_SubRegion.ValueMember = 
                CB_3DSReg.ValueMember =
                CB_Language.ValueMember =
                CB_Ball.ValueMember =
                CB_HeldItem.ValueMember =
                CB_Species.ValueMember =
                DEV_Ability.ValueMember =
                CB_Nature.ValueMember =
                CB_EncounterType.ValueMember =
                CB_GameOrigin.ValueMember = 
                CB_HPType.ValueMember = "Value";

            // Set the various ComboBox DataSources up with their allowed entries
            setCountrySubRegion(CB_Country, "countries");
            CB_3DSReg.DataSource = Util.getUnsortedCBList("regions3ds");
            CB_Language.DataSource = Util.getUnsortedCBList("languages");
            int[] ball_nums = { 7, 576, 13, 492, 497, 14, 495, 493, 496, 494, 11, 498, 8, 6, 12, 15, 9, 5, 499, 10, 1, 16 };
            int[] ball_vals = { 7, 25, 13, 17, 22, 14, 20, 18, 21, 19, 11, 23, 8, 6, 12, 15, 9, 5, 24, 10, 1, 16 };
            CB_Ball.DataSource = Util.getVariedCBList(Util.getCBList(itemlist, new[] { 4 }, new[] { 3 }, new[] { 2 }, new[] { 1 }), itemlist, ball_nums, ball_vals);
            CB_HeldItem.DataSource = Util.getCBList(itemlist, (DEV_Ability.Enabled) ? null : Legal.Items_Held);
            CB_Species.DataSource = Util.getCBList(specieslist, null);
            DEV_Ability.DataSource = Util.getCBList(abilitylist, null);
            CB_Nature.DataSource = Util.getCBList(natures, null);
            CB_EncounterType.DataSource = Util.getCBList(encountertypelist, new[] { 0 }, Legal.Gen4EncounterTypes);
            CB_GameOrigin.DataSource = Util.getCBList(gamelist, Legal.Games_6oras, Legal.Games_6xy, Legal.Games_5, Legal.Games_4, Legal.Games_4e, Legal.Games_4r, Legal.Games_3, Legal.Games_3e, Legal.Games_3r, Legal.Games_3s);

            string[] hptypes = new string[types.Length - 2]; Array.Copy(types, 1, hptypes, 0, hptypes.Length);
            CB_HPType.DataSource = Util.getCBList(hptypes, null);

            // Set the Move ComboBoxes too..
            {
                var moves = Util.getCBList(movelist, null);
                foreach (ComboBox cb in new[] { CB_Move1, CB_Move2, CB_Move3, CB_Move4, CB_RelearnMove1, CB_RelearnMove2, CB_RelearnMove3, CB_RelearnMove4 })
                {
                    cb.DisplayMember = "Text"; cb.ValueMember = "Value";
                    cb.DataSource = new BindingSource(moves, null);
                }
            }
        }
        private void populateFields(byte[] data)
        {
            // Store all loaded data in a persistent buffer for easy access.
            Array.Resize(ref buff, data.Length);
            Array.Copy(data, buff, data.Length);

            init = false;
            CAL_EggDate.Value = new DateTime(2000, 01, 01);
            Tab_Main.Focus();
            // Encryption Constant
            TB_EC.Text = BitConverter.ToUInt32(buff, 0).ToString("X8");

            // Block A
            int species = BitConverter.ToInt16(buff, 0x08);
            int helditem = BitConverter.ToUInt16(buff, 0x0A);
            uint TID = BitConverter.ToUInt16(buff, 0x0C);
            uint SID = BitConverter.ToUInt16(buff, 0x0E);
            uint exp = BitConverter.ToUInt32(buff, 0x10);
            int ability = buff[0x14];
            int abilitynum = buff[0x15];
            // 0x16, 0x17 - Training Bags handled by the Ribbon Editor
            uint PID = BitConverter.ToUInt32(buff, 0x18);
            int nature = buff[0x1C];
            int feflag = buff[0x1D] % 2;
            int genderflag = (buff[0x1D] >> 1) & 0x3;
            int altforms = (buff[0x1D] >> 3);
            int HP_EV = buff[0x1E];
            int ATK_EV = buff[0x1F];
            int DEF_EV = buff[0x20];
            int SPA_EV = buff[0x22];
            int SPD_EV = buff[0x23];
            int SPE_EV = buff[0x21];
            int cnt_cool = buff[0x24];
            int cnt_beauty = buff[0x25];
            int cnt_cute = buff[0x26];
            int cnt_smart = buff[0x27];
            int cnt_tough = buff[0x28];
            int cnt_sheen = buff[0x29];
            int markings = buff[0x2A];
            int PKRS_Strain = buff[0x2B] >> 4;
            int PKRS_Duration = buff[0x2B] % 0x10;

            // Medals and Ribbons, passed with buff to new form
            // 0x2C, 0x2D, 0x2E, 0x2F
            // 0x33, 0x34, 0x35, 0x36
            // 0x34, 0x35, 0x36, 0x37
            // 0x38, 0x39, 0x3A

            // 0x3B, 0x3C, 0x3D, 0x3E, 0x3F - Unused/Unknown

            // Block B
            string nicknamestr = Util.TrimFromZero(Encoding.Unicode.GetString(buff, 0x40, 24));
            // 0x58, 0x59 - unused
            int move1 = BitConverter.ToInt16(buff, 0x5A);
            int move2 = BitConverter.ToInt16(buff, 0x5C);
            int move3 = BitConverter.ToInt16(buff, 0x5E);
            int move4 = BitConverter.ToInt16(buff, 0x60);
            int move1_pp = buff[0x62];
            int move2_pp = buff[0x63];
            int move3_pp = buff[0x64];
            int move4_pp = buff[0x65];
            int move1_ppu = buff[0x66];
            int move2_ppu = buff[0x67];
            int move3_ppu = buff[0x68];
            int move4_ppu = buff[0x69];
            int eggmove1 = BitConverter.ToInt16(buff, 0x6A);
            int eggmove2 = BitConverter.ToInt16(buff, 0x6C);
            int eggmove3 = BitConverter.ToInt16(buff, 0x6E);
            int eggmove4 = BitConverter.ToInt16(buff, 0x70);

            // 0x72 - Super Training Flag - Passed with buff to new form

            // 0x73 - Unused/Unknown
            uint IV32 = BitConverter.ToUInt32(buff, 0x74);
            uint HP_IV = IV32 & 0x1F;
            uint ATK_IV = (IV32 >> 5) & 0x1F;
            uint DEF_IV = (IV32 >> 10) & 0x1F;
            uint SPE_IV = (IV32 >> 15) & 0x1F;
            uint SPA_IV = (IV32 >> 20) & 0x1F;
            uint SPD_IV = (IV32 >> 25) & 0x1F;
            uint isegg = (IV32 >> 30) & 1;
            uint isnick = (IV32 >> 31);

            // Block C
            string notOT = Util.TrimFromZero(Encoding.Unicode.GetString(buff, 0x78, 24));
            bool notOTG = Convert.ToBoolean(buff[0x92]);
            // Memory Editor edits everything else with buff in a new form

            // Block D
            string ot = Util.TrimFromZero(Encoding.Unicode.GetString(buff, 0xB0, 24));
            // 0xC8, 0xC9 - unused
            int OTfriendship = buff[0xCA];
            // int OTaffection = buff[0xCB]; // Handled by Memory Editor
            // 0xCC, 0xCD, 0xCE, 0xCF, 0xD0
            int egg_year = buff[0xD1];
            int egg_month = buff[0xD2];
            int egg_day = buff[0xD3];
            int met_year = buff[0xD4];
            int met_month = buff[0xD5];
            int met_day = buff[0xD6];
            // 0xD7 - unused
            int eggloc = BitConverter.ToUInt16(buff, 0xD8);
            int metloc = BitConverter.ToUInt16(buff, 0xDA);
            int ball = buff[0xDC];
            int metlevel = buff[0xDD] & 0x7F;
            int otgender = (buff[0xDD]) >> 7;
            int encountertype = buff[0xDE];
            int gamevers = buff[0xDF];
            int countryID = buff[0xE0];
            int regionID = buff[0xE1];
            int dsregID = buff[0xE2];
            int otlang = buff[0xE3];
            // 0xE4, 0xE5, 0xE6, 0xE7 - unused

            //
            // Populate Fields
            //

            CHK_Fateful.Checked = Convert.ToBoolean(feflag);
            CHK_IsEgg.Checked = Convert.ToBoolean(isegg);
            CHK_Nicknamed.Checked = Convert.ToBoolean(isnick);
            Label_OTGender.Text = gendersymbols[otgender];

            // Private Use Character Fixing Text
            {
                nicknamestr = nicknamestr.Replace("\uE08F", "\u2640");
                nicknamestr = nicknamestr.Replace("\uE08E", "\u2642");
            }

            // Set Markings
            CHK_Circle.Checked = ((markings >> 0) & 1) == 1;
            CHK_Triangle.Checked = ((markings >> 1) & 1) == 1;
            CHK_Square.Checked = ((markings >> 2) & 1) == 1;
            CHK_Heart.Checked = ((markings >> 3) & 1) == 1;
            CHK_Star.Checked = ((markings >> 4) & 1) == 1;
            CHK_Diamond.Checked = ((markings >> 5) & 1) == 1;

            // Set Generic Identification Data
            TB_PID.Text = PID.ToString("X8");
            CB_Species.SelectedValue = species;
            CB_HeldItem.SelectedValue = helditem;
            setAbilityList(TB_AbilityNumber, species, CB_Ability, CB_Form);
            TB_AbilityNumber.Text = abilitynum.ToString();
            CB_Ability.SelectedIndex = (abilitynum < 6) ? abilitynum >> 1 : 0; // with some simple error handling
            CB_Nature.SelectedValue = nature;

            TB_EXP.Text = exp.ToString();
            TB_TID.Text = TID.ToString("00000");
            TB_SID.Text = SID.ToString("00000");

            TB_OT.Text = ot;
            TB_Nickname.Text = nicknamestr;
            TB_OTt2.Text = notOT;

            if (buff[0x93] == 1)  // = 1
            {
                TB_Friendship.Text = buff[0xA2].ToString();
                GB_nOT.BackgroundImage = mixedHighlight;
                GB_OT.BackgroundImage = null;
            }
            else                  // = 0
            {
                TB_Friendship.Text = OTfriendship.ToString();
                GB_OT.BackgroundImage = mixedHighlight;
                GB_nOT.BackgroundImage = null;
            }

            CB_Language.SelectedValue = otlang;
            CB_Country.SelectedValue = countryID;
            CB_SubRegion.SelectedValue = regionID;
            CB_3DSReg.SelectedValue = dsregID;
            CB_GameOrigin.SelectedValue = gamevers;
            CB_EncounterType.SelectedValue = encountertype;
            CB_Ball.SelectedValue = ball;

            if (met_month == 0) { met_month = 1; }
            if (met_day == 0) { met_day = 1; }
            try { CAL_MetDate.Value = new DateTime(met_year + 2000, met_month, met_day); }
            catch { CAL_MetDate.Value = new DateTime(2000, 1, 1); }

            if (eggloc != 0)
            {
                // Was obtained initially as an egg.
                CHK_AsEgg.Checked = true;
                GB_EggConditions.Enabled = true;

                CB_EggLocation.SelectedValue = eggloc;
                try { CAL_EggDate.Value = new DateTime(egg_year + 2000, egg_month, egg_day); }
                catch { CAL_MetDate.Value = new DateTime(2000, 1, 1); }
            }
            else { CHK_AsEgg.Checked = GB_EggConditions.Enabled = false; CB_EggLocation.SelectedValue = 0; }

            CB_MetLocation.SelectedValue = metloc;

            // Set CT Gender to None if no CT, else set to gender symbol.
            Label_CTGender.Text = ((TB_OTt2.Text == "") || (notOT == "")) ? "" : (notOTG) ? gendersymbols[1] : gendersymbols[0];

            TB_MetLevel.Text = metlevel.ToString();

            // Reset Label and ComboBox visibility, as well as non-data checked status.
            Label_PKRS.Visible = CB_PKRSStrain.Visible = CHK_Infected.Checked = PKRS_Strain != 0;
            Label_PKRSdays.Visible = CB_PKRSDays.Visible = PKRS_Duration != 0;
            CHK_Cured.Checked = (PKRS_Strain > 0 && PKRS_Duration == 0);

            // Set SelectedIndexes for PKRS
            CB_PKRSStrain.SelectedIndex = PKRS_Strain;
            CB_PKRSDays.SelectedIndex = Math.Min(CB_PKRSStrain.Items.Count - 1, PKRS_Duration); // to strip out bad hacked 'rus

            TB_Cool.Text = cnt_cool.ToString();
            TB_Beauty.Text = cnt_beauty.ToString();
            TB_Cute.Text = cnt_cute.ToString();
            TB_Smart.Text = cnt_smart.ToString();
            TB_Tough.Text = cnt_tough.ToString();
            TB_Sheen.Text = cnt_sheen.ToString();

            TB_HPIV.Text = HP_IV.ToString();
            TB_ATKIV.Text = ATK_IV.ToString();
            TB_DEFIV.Text = DEF_IV.ToString();
            TB_SPAIV.Text = SPA_IV.ToString();
            TB_SPDIV.Text = SPD_IV.ToString();
            TB_SPEIV.Text = SPE_IV.ToString();

            TB_HPEV.Text = HP_EV.ToString();
            TB_ATKEV.Text = ATK_EV.ToString();
            TB_DEFEV.Text = DEF_EV.ToString();
            TB_SPAEV.Text = SPA_EV.ToString();
            TB_SPDEV.Text = SPD_EV.ToString();
            TB_SPEEV.Text = SPE_EV.ToString();

            CB_Move1.SelectedValue = move1;
            CB_Move2.SelectedValue = move2;
            CB_Move3.SelectedValue = move3;
            CB_Move4.SelectedValue = move4;
            CB_RelearnMove1.SelectedValue = eggmove1;
            CB_RelearnMove2.SelectedValue = eggmove2;
            CB_RelearnMove3.SelectedValue = eggmove3;
            CB_RelearnMove4.SelectedValue = eggmove4;
            CB_PPu1.SelectedIndex = move1_ppu;
            CB_PPu2.SelectedIndex = move2_ppu;
            CB_PPu3.SelectedIndex = move3_ppu;
            CB_PPu4.SelectedIndex = move4_ppu;
            TB_PP1.Text = move1_pp.ToString();
            TB_PP2.Text = move2_pp.ToString();
            TB_PP3.Text = move3_pp.ToString();
            TB_PP4.Text = move4_pp.ToString();

            int level = PKX.getLevel(species, ref exp);
            TB_Level.Text = level.ToString();

            // Set Form if count is enough, else if count is more than 1 set equal to max else zero.
            CB_Form.SelectedIndex = (CB_Form.Items.Count > altforms) ? altforms : (CB_Form.Items.Count > 1) ? CB_Form.Items.Count - 1 : 0;

            // Load Extrabyte Value
            TB_ExtraByte.Text = buff[Convert.ToInt32(CB_ExtraBytes.Text, 16)].ToString();

            // Load Gender Flag
            Label_Gender.Text = gendersymbols[genderflag];
            updateStats();
            setIsShiny();

            CB_EncounterType.Visible = Label_EncounterType.Visible = !(gamevers > 12 || gamevers < 7);
            if (gamevers > 12 || gamevers < 7)
                CB_EncounterType.SelectedValue = 0;

            init = true;
            updatePKRSInfected(null, null);
            updatePKRSCured(null, null);

            if (HaX) // DEV Illegality
            {
                DEV_Ability.SelectedValue = ability;
                MT_Level.Text = level.ToString();
                MT_Form.Text = altforms.ToString();
            }

            // Set the Preview Box
            getQuickFiller(dragout);

            // Highlight the Current Handler
            clickGT((buff[0x93] == 1) ? GB_nOT : GB_OT, null);

            if (!PKX.verifychk(buff))
                Util.Alert("PKX File has an invalid checksum.");
        }
        // General Use Functions shared by other Forms // 
        public void setCountrySubRegion(ComboBox CB, string type)
        {
            int index = CB.SelectedIndex;
            // fix for Korean / Chinese being swapped
            string cl = curlanguage + "";
            cl = (cl == "zh") ? "ko" : (cl == "ko") ? "zh" : cl;

            CB.DataSource = Util.getCBList(type, cl);

            if (index > 0 && index < CB.Items.Count && init)
                CB.SelectedIndex = index;
        }
        public void setForms(int species, ComboBox cb, Label l = null)
        {
            // Form Tables
            cb.DisplayMember = "Text";
            cb.ValueMember = "Value";
            PKX.PersonalParser.Personal MonData = PKX.PersonalGetter.GetPersonal(species);
            bool hasForms = !(MonData.AltFormCount == 0 && species != 664 && species != 665); // If no forms & not Scatterbug / Spewpa...
            cb.Enabled = cb.Visible = hasForms;
            if (l != null) l.Visible = hasForms;
            
            cb.DataSource = PKX.getFormList(species, types, forms, gendersymbols).ToList();
        }
        public void setAbilityList(MaskedTextBox tb_abil, int species, ComboBox cb_abil, ComboBox cb_forme)
        {
            if (!init && tb_abil.Text == "")
                return;
            int newabil = Convert.ToInt16(tb_abil.Text) >> 1;

            int form = cb_forme.SelectedIndex;
            byte[] abils = PKX.getAbilities(species, form);

            // Build Ability List
            List<string> ability_list = new List<string>
            {
                abilitylist[abils[0]] + " (1)",
                abilitylist[abils[1]] + " (2)",
                abilitylist[abils[2]] + " (H)"
            };
            cb_abil.DataSource = ability_list;

            cb_abil.SelectedIndex = newabil < 3 ? newabil : 0;
        }
        // PKX Data Calculation Functions //
        private void setIsShiny()
        {
            bool isShiny = PKX.getIsShiny(Util.getHEXval(TB_PID), Util.ToUInt32(TB_TID.Text), Util.ToUInt32(TB_SID.Text));

            // Set the Controls
            BTN_Shinytize.Visible = BTN_Shinytize.Enabled = !isShiny;
            Label_IsShiny.Visible = isShiny;

            // Refresh Markings (for Shiny Star if applicable)
            setMarkings();
        }
        private void setMarkings()
        {
            PictureBox[] pba = { PB_Mark1, PB_Mark2, PB_Mark3, PB_Mark4, PB_Mark5, PB_Mark6 };
            CheckBox[] cba = { CHK_Circle, CHK_Triangle, CHK_Square, CHK_Heart, CHK_Star, CHK_Diamond };
            for (int i = 0; i < 6; i++)
                pba[i].Image = Util.ChangeOpacity(pba[i].InitialImage, (Convert.ToUInt16(cba[i].Checked)) * 0.9 + 0.1);

            PB_MarkShiny.Image = Util.ChangeOpacity(PB_MarkShiny.InitialImage, (Convert.ToUInt16(!BTN_Shinytize.Enabled)) * 0.9 + 0.1);
            PB_MarkCured.Image = Util.ChangeOpacity(PB_MarkCured.InitialImage, (Convert.ToUInt16(CHK_Cured.Checked)) * 0.9 + 0.1);
            int gameindex = Util.getIndex(CB_GameOrigin);
            PB_MarkPentagon.Image = Util.ChangeOpacity(PB_MarkPentagon.InitialImage, (Convert.ToUInt16(gameindex == 24 || gameindex == 25 || gameindex == 26 || gameindex == 27)) * 0.9 + 0.1);
        }
        // Clicked Label Shortcuts //
        private void clickQR(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Alt)
            {
                // Fetch data from QR code...
                byte[] ekx = Util.getQRData();

                if (ekx == null) return;

                if (ekx.Length != 232) { Util.Alert("Decoded data not 232 bytes.", String.Format("QR Data Size: {0}", ekx.Length)); }
                else try
                    {
                        byte[] pkx = PKX.decryptArray(ekx);
                        if (PKX.verifychk(pkx)) { Array.Copy(pkx, buff, 0xE8); populateFields(buff); }
                        else Util.Alert("Invalid checksum in QR data.");
                    }
                    catch { Util.Alert("Error loading decrypted data."); }
            }
            else
            {
                if (!verifiedPKX()) return;
                byte[] pkx = preparepkx();
                byte[] ekx = PKX.encryptArray(pkx);

                Array.Resize(ref ekx, 232);
                const string server = "http://loadcode.projectpokemon.org/b1s1.html#"; // Rehosted with permission from LC/MS -- massive thanks!
                Image qr = Util.getQRImage(ekx, server);

                if (qr == null) return;

                PKX data = new PKX(pkx, "Tabs");
                string[] r = PKX.getPKXSummary(data);
                new QR(qr, dragout.Image, r[0], r[1], r[2], "PKHeX @ ProjectPokemon.org").ShowDialog();
            }
        }
        private void clickFriendship(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Control) // prompt to reset
                TB_Friendship.Text = buff[0x93] == 0 ? buff[0xCA].ToString() : buff[0xA2].ToString();
            else
                TB_Friendship.Text = TB_Friendship.Text == "255" ? PKX.getBaseFriendship(Util.getIndex(CB_Species)).ToString() : "255";
        }
        private void clickGender(object sender, EventArgs e)
        {
            // Get Gender Threshold
            PKX.PersonalParser.Personal MonData = PKX.PersonalGetter.GetPersonal(Util.getIndex(CB_Species));
            int gt = MonData.GenderRatio;

            if (gt == 255 || gt == 0 || gt == 254) // Single gender/genderless
                return;

            if (gt >= 255) return; 
            // If not a single gender(less) species: (should be <254 but whatever, 255 never happens^)
            Label_Gender.Text = gendersymbols[PKX.getGender(Label_Gender.Text) ^ 1];

            if (PKX.getGender(CB_Form.Text) < 2) // Gendered Forms
                CB_Form.SelectedIndex = PKX.getGender(Label_Gender.Text);

            getQuickFiller(dragout);
        }
        private void clickPPUps(object sender, EventArgs e)
        {
            CB_PPu1.SelectedIndex = (ModifierKeys != Keys.Control && Util.getIndex(CB_Move1) > 0) ? 3 : 0;
            CB_PPu2.SelectedIndex = (ModifierKeys != Keys.Control && Util.getIndex(CB_Move2) > 0) ? 3 : 0;
            CB_PPu3.SelectedIndex = (ModifierKeys != Keys.Control && Util.getIndex(CB_Move3) > 0) ? 3 : 0;
            CB_PPu4.SelectedIndex = (ModifierKeys != Keys.Control && Util.getIndex(CB_Move4) > 0) ? 3 : 0;
        }
        private void clickMarking(object sender, EventArgs e)
        {
            PictureBox[] pba = { PB_Mark1, PB_Mark2, PB_Mark3, PB_Mark4, PB_Mark5, PB_Mark6 };
            CheckBox[] cba = { CHK_Circle, CHK_Triangle, CHK_Square, CHK_Heart, CHK_Star, CHK_Diamond };

            CheckBox cb = cba[Array.IndexOf(pba, sender as PictureBox)];
            cb.Checked = !cb.Checked;
            setMarkings();
        }
        private void clickOT(object sender, EventArgs e)
        {
            string OT = Util.TrimFromZero(Encoding.Unicode.GetString(savefile, SaveGame.TrainerCard + 0x48 + savindex * 0x7F000, 0x1A));
            if (OT.Length <= 0) return;

            TB_OT.Text = OT;
            int savshift = 0x7F000 * savindex;
            // Set Gender Label
            int g6trgend = savefile[SaveGame.TrainerCard + 0x5 + savshift];
            Label_OTGender.Text = g6trgend == 1 ? gendersymbols[1] : gendersymbols[0];

            // Get TID/SID
            TB_TID.Text = BitConverter.ToUInt16(savefile, SaveGame.TrainerCard + 0 + savshift).ToString();
            TB_SID.Text = BitConverter.ToUInt16(savefile, SaveGame.TrainerCard + 2 + savshift).ToString();
            int game = savefile[SaveGame.TrainerCard + 0x4 + savshift];
            int subreg = savefile[SaveGame.TrainerCard + 0x26 + savshift];
            int country = savefile[SaveGame.TrainerCard + 0x27 + savshift];
            int _3DSreg = savefile[SaveGame.TrainerCard + 0x2C + savshift];
            int lang = savefile[SaveGame.TrainerCard + 0x2D + savshift];

            // CB_GameOrigin.SelectedValue = game;

            CB_GameOrigin.SelectedValue = game;
            CB_SubRegion.SelectedValue = subreg;
            CB_Country.SelectedValue = country;
            CB_3DSReg.SelectedValue = _3DSreg;
            CB_Language.SelectedValue = lang;
            updateNickname(null, null);
        }
        private void clickCT(object sender, EventArgs e)
        {
            if (TB_OTt2.Text.Length > 0)
                Label_CTGender.Text = gendersymbols[savefile[0x19405 + savindex * 0x7F000]];
        }
        private void clickGT(object sender, EventArgs e)
        {
            if (sender as GroupBox == GB_OT)
            {
                buff[0x93] = 0;
                TB_Friendship.Text = buff[0xCA].ToString();
                GB_OT.BackgroundImage = mixedHighlight;
                GB_nOT.BackgroundImage = null;
            }
            else if (TB_OTt2.Text.Length > 0)
            {
                buff[0x93] = 1;
                TB_Friendship.Text = buff[0xA2].ToString();
                GB_OT.BackgroundImage = null;
                GB_nOT.BackgroundImage = mixedHighlight;
            }
        }
        private void clickTRGender(object sender, EventArgs e)
        {
            Label lbl = sender as Label;
            if (lbl.Text != "") // set gender label (toggle M/F)
                lbl.Text = (PKX.getGender(lbl.Text) == 0) ? gendersymbols[1] : gendersymbols[0];
        }
        // Prompted Updates of PKX Functions // 
        private bool changingFields;
        private void updateEXPLevel(object sender, EventArgs e)
        {
            if (changingFields) return;

            if (sender as MaskedTextBox == TB_EXP)
            {
                changingFields = true;
                // Change the Level
                uint exp = Util.ToUInt32(TB_EXP);
                int level = (Util.ToInt32(TB_EXP.Text) == 0) ? 1 : PKX.getLevel(Util.getIndex(CB_Species), ref exp);

                TB_Level.Text = level.ToString();
                if (!MT_Level.Visible)
                    TB_EXP.Text = exp.ToString();
                else
                    MT_Level.Text = level.ToString();
            }
            else
            {
                changingFields = true;
                // Change the XP
                int level = Util.ToInt32((MT_Level.Focused ? MT_Level : TB_Level).Text);
                if (level > 100) TB_Level.Text = "100";
                if (level > 255) MT_Level.Text = "255";

                TB_EXP.Text = PKX.getEXP(level, Util.getIndex(CB_Species)).ToString();
            }
            changingFields = false;
            updateStats();
        }
        private void updateHPType(object sender, EventArgs e)
        {
            if (changingFields) return;
            changingFields = true;
            int[] ivs =
            { 
                Util.ToInt32(TB_HPIV.Text), Util.ToInt32(TB_ATKIV.Text), Util.ToInt32(TB_DEFIV.Text), 
                Util.ToInt32(TB_SPAIV.Text), Util.ToInt32(TB_SPDIV.Text), Util.ToInt32(TB_SPEIV.Text) 
            };

            // Change IVs to match the new Hidden Power
            int[] newIVs = PKX.setHPIVs(Util.getIndex(CB_HPType), ivs);
            TB_HPIV.Text = newIVs[0].ToString();
            TB_ATKIV.Text = newIVs[1].ToString();
            TB_DEFIV.Text = newIVs[2].ToString();

            TB_SPAIV.Text = newIVs[3].ToString();
            TB_SPDIV.Text = newIVs[4].ToString();
            TB_SPEIV.Text = newIVs[5].ToString();

            // Refresh View
            changingFields = false;
            updateIVs(null, null);
        }
        private void updateIVs(object sender, EventArgs e)
        {
            if (changingFields) return;
            if (sender != null)
                if (Util.ToInt32((sender as MaskedTextBox).Text) > 31)
                    (sender as MaskedTextBox).Text = "31";

            int[] ivs =
            { 
                Util.ToInt32(TB_HPIV.Text), Util.ToInt32(TB_ATKIV.Text), Util.ToInt32(TB_DEFIV.Text), 
                Util.ToInt32(TB_SPAIV.Text), Util.ToInt32(TB_SPDIV.Text), Util.ToInt32(TB_SPEIV.Text) 
            };

            changingFields = true;
            CB_HPType.SelectedValue = PKX.getHPType(ivs);
            changingFields = false;

            int ivtotal = ivs.Sum();
            TB_IVTotal.Text = ivtotal.ToString();

            // Potential Reading
            if (!unicode)
            {
                if (ivtotal <= 90)
                    L_Potential.Text = "★☆☆☆";
                else if (ivtotal <= 120)
                    L_Potential.Text = "★★☆☆";
                else if (ivtotal <= 150)
                    L_Potential.Text = "★★★☆";
                else
                    L_Potential.Text = "★★★★";
            }
            else
            {
                if (ivtotal <= 90)
                    L_Potential.Text = "+";
                else if (ivtotal <= 120)
                    L_Potential.Text = "++";
                else if (ivtotal <= 150)
                    L_Potential.Text = "+++";
                else
                    L_Potential.Text = "++++";
            }

            // Characteristic with EC%6
            int pm6 = (int)(Util.getHEXval(TB_EC) % 6); // EC MOD 6
            int maxIV = ivs.Max();
            int pm6stat = 0;

            for (int i = 0; i < 6; i++)
            {
                pm6stat = (pm6 + i) % 6;
                if (ivs[pm6stat] == maxIV)
                    break; // P%6 is this stat
            }

            L_Characteristic.Text = characteristics[pm6stat * 5 + maxIV % 5];
            updateStats();
        }
        private void updateEVs(object sender, EventArgs e)
        {
            if (sender != null)
                if (Util.ToInt32((sender as MaskedTextBox).Text) > 252)
                    (sender as MaskedTextBox).Text = "252";

            int HP_EV = Util.ToInt32(TB_HPEV.Text);
            int ATK_EV = Util.ToInt32(TB_ATKEV.Text);
            int DEF_EV = Util.ToInt32(TB_DEFEV.Text);
            int SPA_EV = Util.ToInt32(TB_SPAEV.Text);
            int SPD_EV = Util.ToInt32(TB_SPDEV.Text);
            int SPE_EV = Util.ToInt32(TB_SPEEV.Text);

            int evtotal = HP_EV + ATK_EV + DEF_EV + SPA_EV + SPD_EV + SPE_EV;

            if (evtotal > 510) // Background turns Red
                TB_EVTotal.BackColor = Color.Red;
            else if (evtotal == 510) // Maximum EVs
                TB_EVTotal.BackColor = Color.Honeydew;
            else TB_EVTotal.BackColor = Color.WhiteSmoke;

            TB_EVTotal.Text = evtotal.ToString();
            updateStats();
        }
        private void updateRandomIVs(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift)
            {
                // Max IVs
                TB_HPIV.Text = 31.ToString();
                TB_ATKIV.Text = 31.ToString();
                TB_DEFIV.Text = 31.ToString();
                TB_SPAIV.Text = 31.ToString();
                TB_SPDIV.Text = 31.ToString();
                TB_SPEIV.Text = 31.ToString();
            }
            else
            {
                TB_HPIV.Text = (Util.rnd32() & 0x1F).ToString();
                TB_ATKIV.Text = (Util.rnd32() & 0x1F).ToString();
                TB_DEFIV.Text = (Util.rnd32() & 0x1F).ToString();
                TB_SPAIV.Text = (Util.rnd32() & 0x1F).ToString();
                TB_SPDIV.Text = (Util.rnd32() & 0x1F).ToString();
                TB_SPEIV.Text = (Util.rnd32() & 0x1F).ToString();
            }
        }
        private void updateRandomEVs(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift)
            {
                // Max IVs
                TB_HPEV.Text = 0.ToString();
                TB_ATKEV.Text = 0.ToString();
                TB_DEFEV.Text = 0.ToString();
                TB_SPAEV.Text = 0.ToString();
                TB_SPDEV.Text = 0.ToString();
                TB_SPEEV.Text = 0.ToString();
            }
            else
            {
                byte[] evs = PKX.getRandomEVs();
                TB_HPEV.Text = evs[0].ToString();
                TB_ATKEV.Text = evs[1].ToString();
                TB_DEFEV.Text = evs[2].ToString();
                TB_SPAEV.Text = evs[3].ToString();
                TB_SPDEV.Text = evs[4].ToString();
                TB_SPEEV.Text = evs[5].ToString();
            }
        }
        private void updateRandomPID(object sender, EventArgs e)
        {
            TB_PID.Text = PKX.getRandomPID(Util.getIndex(CB_Species), PKX.getGender(Label_Gender.Text)).ToString("X8");
            getQuickFiller(dragout);
        }
        private void updateRandomEC(object sender, EventArgs e)
        {
            TB_EC.Text = Util.rnd32().ToString("X8");
        }
        private void updateHackedStats(object sender, EventArgs e)
        {
            Stat_HP.Enabled =
                Stat_ATK.Enabled =
                Stat_DEF.Enabled =
                Stat_SPA.Enabled =
                Stat_SPD.Enabled =
                Stat_SPE.Enabled = CHK_HackedStats.Checked;
        }
        private void update255_MTB(object sender, EventArgs e)
        {
            MaskedTextBox mtb = sender as MaskedTextBox;
            try
            {
                if (Util.ToInt32((sender as MaskedTextBox).Text) > 255)
                    (sender as MaskedTextBox).Text = "255";
            }
            catch { mtb.Text = "0"; }
        }
        private void update255_TB(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            try
            {
                if (Util.ToInt32((sender as TextBox).Text) > 255)
                    (sender as TextBox).Text = "255";
            }
            catch { tb.Text = "0"; }
        }
        private void updateForm(object sender, EventArgs e)
        {
            updateStats();
            // Repopulate Abilities if Species Form has different abilities
            setAbilityList(TB_AbilityNumber, Util.getIndex(CB_Species), CB_Ability, CB_Form);

            // Gender Forms
            if (PKX.getGender(CB_Form.Text) < 2 && Util.getIndex(CB_Species) != 201) // don't do this for Unown
                Label_Gender.Text = CB_Form.Text;
        }
        private void updatePP(object sender, EventArgs e)
        {
            TB_PP1.Text = (PKX.getMovePP(Util.getIndex(CB_Move1), CB_PPu1.SelectedIndex)).ToString();
            TB_PP2.Text = (PKX.getMovePP(Util.getIndex(CB_Move2), CB_PPu2.SelectedIndex)).ToString();
            TB_PP3.Text = (PKX.getMovePP(Util.getIndex(CB_Move3), CB_PPu3.SelectedIndex)).ToString();
            TB_PP4.Text = (PKX.getMovePP(Util.getIndex(CB_Move4), CB_PPu4.SelectedIndex)).ToString();
        }
        private void updatePKRSstrain(object sender, EventArgs e)
        {
            // Change the PKRS Days to the legal bounds.
            int currentDuration = CB_PKRSDays.SelectedIndex;
            CB_PKRSDays.Items.Clear();
            int[] days = Enumerable.Range(0, CB_PKRSStrain.SelectedIndex % 4 + 1).Select(i => i).ToArray();
            foreach (int day in days) CB_PKRSDays.Items.Add(day);

            // Set the days back if they're legal, else set it to 1. (0 always passes).
            CB_PKRSDays.SelectedIndex = (currentDuration < CB_PKRSDays.Items.Count) ? currentDuration : 1;

            if (CB_PKRSStrain.SelectedIndex != 0) return;
            
            // Never Infected
            CB_PKRSDays.SelectedValue = 0;
            CHK_Cured.Checked = false;
            CHK_Infected.Checked = false;
        }
        private void updatePKRSdays(object sender, EventArgs e)
        {
            if (CB_PKRSDays.SelectedIndex != 0) return;

            // If no days are selected
            if (CB_PKRSStrain.SelectedIndex == 0)
            {
                // Never Infected
                CHK_Cured.Checked = false;
                CHK_Infected.Checked = false;
            }
            else CHK_Cured.Checked = true;
        }
        private void updatePKRSCured(object sender, EventArgs e)
        {
            if (!init) return;
            // Cured PokeRus is toggled
            if (CHK_Cured.Checked)
            {
                // Has Had PokeRus
                Label_PKRSdays.Visible = CB_PKRSDays.Visible = false;
                CB_PKRSDays.SelectedIndex = 0;

                Label_PKRS.Visible = CB_PKRSStrain.Visible = true;
                CHK_Infected.Checked = true;

                // If we're cured we have to have a strain infection.
                if (CB_PKRSStrain.SelectedIndex == 0)
                    CB_PKRSStrain.SelectedIndex = 1;
            }
            else if (!CHK_Infected.Checked)
            {
                // Not Infected, Disable the other
                Label_PKRS.Visible = CB_PKRSStrain.Visible = false;
                CB_PKRSStrain.SelectedIndex = 0;
            }
            else
            {
                // Still Infected for a duration
                Label_PKRSdays.Visible = CB_PKRSDays.Visible = true;
                CB_PKRSDays.SelectedValue = 1;
            }
            // if not cured yet, days > 0
            if (!CHK_Cured.Checked && CHK_Infected.Checked && CB_PKRSDays.SelectedIndex == 0)
                CB_PKRSDays.SelectedIndex++;

            setMarkings();
        }
        private void updatePKRSInfected(object sender, EventArgs e)
        {
            if (!init) return;
            if (CHK_Cured.Checked && !CHK_Infected.Checked) { CHK_Cured.Checked = false; return; }
            if (CHK_Cured.Checked) return;
            Label_PKRS.Visible = CB_PKRSStrain.Visible = CHK_Infected.Checked;
            if (!CHK_Infected.Checked) { CB_PKRSStrain.SelectedIndex = 0; CB_PKRSDays.SelectedIndex = 0; Label_PKRSdays.Visible = CB_PKRSDays.Visible = false; }
            else if (CB_PKRSStrain.SelectedIndex == 0) { CB_PKRSStrain.SelectedIndex = 1; Label_PKRSdays.Visible = CB_PKRSDays.Visible = true; CB_PKRSDays.SelectedIndex = 1; }

            // if not cured yet, days > 0
            if (CHK_Infected.Checked && CB_PKRSDays.SelectedIndex == 0) CB_PKRSDays.SelectedIndex++;
        }
        private void updateCountry(object sender, EventArgs e)
        {
            if (Util.getIndex(sender as ComboBox) > 0)
                setCountrySubRegion(CB_SubRegion, "sr_" + Util.getIndex(sender as ComboBox).ToString("000"));
        }
        private void updateSpecies(object sender, EventArgs e)
        {
            // Change Species Prompted
            int species = Util.getIndex(CB_Species);
            int level = Util.ToInt32(TB_Level.Text);
            if (MT_Level.Visible) level = Util.ToInt32(MT_Level.Text);

            // Get Forms for Given Species
            setForms(species, CB_Form, Label_Form);

            // Recalculate EXP for Given Level
            uint exp = PKX.getEXP(level, species);
            TB_EXP.Text = exp.ToString();

            // Check for Gender Changes
            // Get Gender Threshold
            species = Util.getIndex(CB_Species);
            PKX.PersonalParser.Personal MonData = PKX.PersonalGetter.GetPersonal(species);
            int gt = MonData.GenderRatio;
            int genderflag;

            if (gt == 255)      // Genderless
                genderflag = 2;
            else if (gt == 254) // Female Only
                genderflag = 1;
            else if (gt == 0) // Male Only
                genderflag = 0;
            else // get gender from old PID correlation
                genderflag = ((Util.getHEXval(TB_PID) & 0xFF) <= gt) ? 1 : 0;

            Label_Gender.Text = gendersymbols[genderflag];
            setAbilityList(TB_AbilityNumber, Util.getIndex(CB_Species), CB_Ability, CB_Form);
            updateForm(null, null);

            // If species changes and no nickname, set the new name == speciesName.
            if (!CHK_Nicknamed.Checked)
                updateNickname(sender, e);
        }
        private void updateOriginGame(object sender, EventArgs e)
        {
            int gameorigin = Util.getIndex(CB_GameOrigin);

            if (gameorigin < 24 && origintrack != "Past")
            {
                // Load Past Gen Locations
                #region B2W2 Met Locations
                {
                    // Build up our met list
                    var met_list = Util.getCBList(metBW2_00000, new[] { 0 });
                    met_list = Util.getOffsetCBList(met_list, metBW2_60000, 60001, new[] { 60002 });
                    met_list = Util.getOffsetCBList(met_list, metBW2_30000, 30001, new[] { 30003 });
                    met_list = Util.getOffsetCBList(met_list, metBW2_00000, 00000, Legal.Met_BW2_0);
                    met_list = Util.getOffsetCBList(met_list, metBW2_30000, 30001, Legal.Met_BW2_3);
                    met_list = Util.getOffsetCBList(met_list, metBW2_40000, 40001, Legal.Met_BW2_4);
                    met_list = Util.getOffsetCBList(met_list, metBW2_60000, 60001, Legal.Met_BW2_6);
                    CB_MetLocation.DisplayMember = "Text";
                    CB_MetLocation.ValueMember = "Value";
                    CB_MetLocation.DataSource = met_list;
                    CB_EggLocation.DisplayMember = "Text";
                    CB_EggLocation.ValueMember = "Value";
                    CB_EggLocation.DataSource = new BindingSource(met_list, null);
                    CB_EggLocation.SelectedValue = 0;
                    CB_MetLocation.SelectedValue = gameorigin < 20 ? 30001 : 60001;
                    origintrack = "Past";
                }
                #endregion
            }
            else if (gameorigin > 23 && (origintrack != "XY"))
            {
                // Load X/Y/OR/AS locations
                #region ORAS Met Locations
                {
                    // Build up our met list
                    var met_list = Util.getCBList(metXY_00000, new[] { 0 });
                    met_list = Util.getOffsetCBList(met_list, metXY_60000, 60001, new[] { 60002 });
                    met_list = Util.getOffsetCBList(met_list, metXY_30000, 30001, new[] { 30002 });
                    met_list = Util.getOffsetCBList(met_list, metXY_00000, 00000, Legal.Met_XY_0);
                    met_list = Util.getOffsetCBList(met_list, metXY_30000, 30001, Legal.Met_XY_3);
                    met_list = Util.getOffsetCBList(met_list, metXY_40000, 40001, Legal.Met_XY_4);
                    met_list = Util.getOffsetCBList(met_list, metXY_60000, 60001, Legal.Met_XY_6);
                    CB_MetLocation.DisplayMember = "Text";
                    CB_MetLocation.ValueMember = "Value";
                    CB_MetLocation.DataSource = met_list;
                    CB_EggLocation.DisplayMember = "Text";
                    CB_EggLocation.ValueMember = "Value";
                    CB_EggLocation.DataSource = new BindingSource(met_list, null);
                    CB_EggLocation.SelectedValue = 0;
                    CB_MetLocation.SelectedValue = 0;
                    origintrack = "XY";
                }
                #endregion
            }
            if (gameorigin < 0x10 && origintrack != "Gen4")
            {
                // Load Gen 4 egg locations if Gen 4 Origin.
                #region HGSS Met Locations
                var met_list = Util.getCBList(metHGSS_00000, new[] { 0 });
                met_list = Util.getOffsetCBList(met_list, metHGSS_02000, 2000, new[] { 2000 });
                met_list = Util.getOffsetCBList(met_list, metHGSS_02000, 2000, new[] { 2002 });
                met_list = Util.getOffsetCBList(met_list, metHGSS_03000, 3000, new[] { 3001 });
                met_list = Util.getOffsetCBList(met_list, metHGSS_00000, 0000, Legal.Met_HGSS_0);
                met_list = Util.getOffsetCBList(met_list, metHGSS_02000, 2000, Legal.Met_HGSS_2);
                met_list = Util.getOffsetCBList(met_list, metHGSS_03000, 3000, Legal.Met_HGSS_3);

                CB_EggLocation.DisplayMember = "Text";
                CB_EggLocation.ValueMember = "Value";
                CB_EggLocation.DataSource = met_list;
                CB_EggLocation.SelectedValue = 0;
                origintrack = "Gen4";
                #endregion
            }

            // Visibility logic for Gen 4 encounter type; only show for Gen 4 Pokemon.
            CB_EncounterType.Visible = Label_EncounterType.Visible = !(gameorigin > 12 || gameorigin < 7);
            // If not Gen 4, set Encounter Type to 0 after it set !visible.
            if (gameorigin > 12 || gameorigin < 7)
                CB_EncounterType.SelectedValue = 0;

            setMarkings(); // Set/Remove KB marking
        }
        private void updateExtraByteValue(object sender, EventArgs e)
        {
            // Changed Extra Byte's Value
            if (Util.ToInt32((sender as MaskedTextBox).Text) > 255)
                (sender as MaskedTextBox).Text = "255";

            int value = Util.ToInt32(TB_ExtraByte.Text);
            int offset = Convert.ToInt32(CB_ExtraBytes.Text, 16);
            buff[offset] = (byte)value;
        }
        private void updateExtraByteIndex(object sender, EventArgs e)
        {
            // Byte changed, need to refresh the Text box for the byte's value.
            TB_ExtraByte.Text = buff[Convert.ToInt32(CB_ExtraBytes.Text, 16)].ToString();
        }
        private void updateNickname(object sender, EventArgs e)
        {
            if (init && ModifierKeys == Keys.Control) { getShowdownSet(); return; }
            if (!init || (CHK_Nicknamed.Checked)) return;

            // Fetch Current Species and set it as Nickname Text
            int species = Util.getIndex(CB_Species);
            if (species == 0 || species > 721)
                TB_Nickname.Text = "";
            else
            {
                // get language
                int lang = Util.getIndex(CB_Language);

                string l;
                switch (lang)
                {
                    case 1: l = "ja"; break;
                    case 2: l = "en"; break;
                    case 3: l = "fr"; break;
                    case 4: l = "it"; break;
                    case 5: l = "de"; break;
                    case 7: l = "es"; break;
                    case 8: l = "ko"; break;
                    default: l = curlanguage; break;
                }
                if (CHK_IsEgg.Checked) species = 0; // Set species to 0 to get the egg name.
                TB_Nickname.Text = Util.getStringList("Species", l)[(CHK_IsEgg.Checked) ? 0 : species];
            }
        }
        private void updateNicknameClick(object sender, MouseEventArgs e)
        {
            TextBox tb = (!(sender is TextBox)) ? TB_Nickname : (sender as TextBox);
            // Special Character Form
            if (ModifierKeys == Keys.Control && !specialChars)
                (new f2_Text(tb)).Show();
        }
        private void updateNotOT(object sender, EventArgs e)
        {
            if (TB_OTt2.Text == "")
            {
                clickGT(GB_OT, null); // Switch CT over to OT.
                Label_CTGender.Text = "";
                TB_Friendship.Text = buff[0xCA].ToString();
            }
            else if (Label_CTGender.Text == "")
                Label_CTGender.Text = gendersymbols[0];
        }
        private void updateIsEgg(object sender, EventArgs e)
        {
            if (CHK_IsEgg.Checked)
            {
                CHK_Nicknamed.Checked = false;
                TB_Friendship.Text = "1";

                // If we are an egg, it won't have a met location.
                CHK_AsEgg.Checked = true;
                GB_EggConditions.Enabled = true;

                CAL_MetDate.Value = new DateTime(2000, 01, 01);
                CB_MetLocation.SelectedIndex = 2;
            }
            else // Not Egg
            {
                if (!CHK_Nicknamed.Checked)
                    updateNickname(null, null);

                TB_Friendship.Text = PKX.getBaseFriendship(Util.getIndex(CB_Species)).ToString();

                if (CB_EggLocation.SelectedIndex == 0)
                {
                    CAL_EggDate.Value = new DateTime(2000, 01, 01);
                    CHK_AsEgg.Checked = false;
                    GB_EggConditions.Enabled = false;
                }
            }
            // Display hatch counter if it is an egg, Display Friendship if it is not.
            Label_HatchCounter.Visible = CHK_IsEgg.Checked;
            Label_Friendship.Visible = !CHK_IsEgg.Checked;

            // Update image to (not) show egg.
            if (!init) return;
            updateNickname(null, null);
            getQuickFiller(dragout);
        }
        private void updateMetAsEgg(object sender, EventArgs e)
        {
            GB_EggConditions.Enabled = CHK_AsEgg.Checked;
            if (CHK_AsEgg.Checked) return;
            // Remove egg met data
            CHK_IsEgg.Checked = false;
            CAL_EggDate.Value = new DateTime(2000, 01, 01);
            CB_EggLocation.SelectedValue = 0;
        }
        private void updateShinyPID(object sender, EventArgs e)
        {
            uint PID = Util.getHEXval(TB_PID);
            uint UID = (PID >> 16);
            uint LID = (PID & 0xFFFF);
            uint PSV = UID ^ LID;
            uint TID = Util.ToUInt32(TB_TID);
            uint SID = Util.ToUInt32(TB_SID);
            uint TSV = TID ^ SID;
            uint XOR = TSV ^ PSV;

            // Preserve Gen5 Origin Ability bit just in case
            XOR &= 0xFFFE; XOR |= UID & 1;

            // New XOR should be 0 or 1.
            if (XOR > 16)
                TB_PID.Text = (((UID ^ XOR) << 16) + LID).ToString("X8");

            setIsShiny();
            getQuickFiller(dragout);
        }
        private void update_ID(object sender, EventArgs e)
        {
            // Trim out nonhex characters
            TB_PID.Text = Util.getHEXval(TB_PID).ToString("X8");
            TB_EC.Text = Util.getHEXval(TB_EC).ToString("X8");

            // Max TID/SID is 65535
            if (Util.ToUInt32(TB_TID.Text) > 65535) TB_TID.Text = "65535";
            if (Util.ToUInt32(TB_SID.Text) > 65535) TB_SID.Text = "65535";

            setIsShiny();
            getQuickFiller(dragout);
            updateIVs(null, null);   // If the PID is changed, PID%6 (Characteristic) might be changed. 
            TB_PID.Select(60, 0);   // position cursor at end of field
        }
        private void validateComboBox(object sender, CancelEventArgs e)
        {
            if (!(sender is ComboBox)) { return; }

            ComboBox cb = sender as ComboBox;
            cb.SelectionLength = 0;

            cb.BackColor = cb.SelectedValue == null ? Color.DarkSalmon : defaultControlWhite;

            if (init)
            { getQuickFiller(dragout); }
        }
        private void validateComboBox2(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            validateComboBox(sender, e as CancelEventArgs);
            if (cb == CB_Ability)
                TB_AbilityNumber.Text = (1 << CB_Ability.SelectedIndex).ToString();
            else if ((cb == CB_Move1) || (cb == CB_Move2) || (cb == CB_Move3) || (cb == CB_Move4))
                updatePP(sender, e);
            getNatureModification(sender, null);
            updateIVs(null, null); // updating Nature will trigger stats to update as well
        }
        private void removedropCB(object sender, KeyEventArgs e)
        {
            ((ComboBox)sender).DroppedDown = false;
        }
        private void updateStats()
        {
            // Gather the needed information.
            int species = Util.getIndex(CB_Species);
            int level = Util.ToInt32((MT_Level.Enabled) ? MT_Level.Text : TB_Level.Text);
            if (level == 0) { level = 1; }
            int form = CB_Form.SelectedIndex;
            int HP_IV = Util.ToInt32(TB_HPIV.Text);
            int ATK_IV = Util.ToInt32(TB_ATKIV.Text);
            int DEF_IV = Util.ToInt32(TB_DEFIV.Text);
            int SPA_IV = Util.ToInt32(TB_SPAIV.Text);
            int SPD_IV = Util.ToInt32(TB_SPDIV.Text);
            int SPE_IV = Util.ToInt32(TB_SPEIV.Text);

            int HP_EV = Util.ToInt32(TB_HPEV.Text);
            int ATK_EV = Util.ToInt32(TB_ATKEV.Text);
            int DEF_EV = Util.ToInt32(TB_DEFEV.Text);
            int SPA_EV = Util.ToInt32(TB_SPAEV.Text);
            int SPD_EV = Util.ToInt32(TB_SPDEV.Text);
            int SPE_EV = Util.ToInt32(TB_SPEEV.Text);

            int nature = Util.getIndex(CB_Nature);

            // Generate the stats.
            ushort[] stats = PKX.getStats(species, level, nature, form,
                                        HP_EV, ATK_EV, DEF_EV, SPA_EV, SPD_EV, SPE_EV,
                                        HP_IV, ATK_IV, DEF_IV, SPA_IV, SPD_IV, SPE_IV);

            Stat_HP.Text = stats[0].ToString();
            Stat_ATK.Text = stats[1].ToString();
            Stat_DEF.Text = stats[2].ToString();
            Stat_SPA.Text = stats[4].ToString();
            Stat_SPD.Text = stats[5].ToString();
            Stat_SPE.Text = stats[3].ToString();

            // Recolor the Stat Labels based on boosted stats.
            {
                int incr = nature / 5;
                int decr = nature % 5;

                Label[] labarray = { Label_ATK, Label_DEF, Label_SPE, Label_SPA, Label_SPD };
                // Reset Label Colors
                foreach (Label label in labarray)
                    label.ForeColor = defaultControlText;

                // Set Colored StatLabels only if Nature isn't Neutral
                if (incr == decr) return;
                labarray[incr].ForeColor = Color.Red;
                labarray[decr].ForeColor = Color.Blue;
            }
        }
        private void getShowdownSet()
        {
            if (!Clipboard.ContainsText()) return;

            // Get Simulator Data
            PKX.Simulator.Set Set = new PKX.Simulator.Set(
                Clipboard.GetText(), // Input Set
                Util.getStringList("Species", "en"),
                Util.getStringList("Items", "en"),
                Util.getStringList("Natures", "en"),
                Util.getStringList("Moves", "en"),
                Util.getStringList("Abilities", "en"));

            if (Set.Species < 0) return;
            if (DialogResult.Yes != Util.Prompt(MessageBoxButtons.YesNo, "Import this set?", Clipboard.GetText())) return;

            // Set Species & Nickname
            CB_Species.SelectedValue = Set.Species;
            CHK_Nicknamed.Checked = (Set.Nickname != null);
            if (Set.Nickname != null) 
                TB_Nickname.Text = Set.Nickname;
            if (Set.Gender != null && PKX.getGender(Label_Gender.Text) != 2 && PKX.getGender(Set.Gender) != 2) 
                Label_Gender.Text = Set.Gender;

            // Set Form
            string[] formStrings = PKX.getFormList(Set.Species,
                Util.getStringList("Types", "en"),
                Util.getStringList("Forms", "en"), gendersymbols);
            int form = 0;
            for (int i = 0; i < formStrings.Length; i++)
                if (formStrings[i].Contains(Set.Form ?? ""))
                { form = i; break; }
            CB_Form.SelectedIndex = form;

            // Set Ability
            byte[] abilities = PKX.getAbilities(Set.Species, form);
            int ability = Array.IndexOf(abilities, (byte)Set.Ability);
            if (ability < 0) ability = 0;
            CB_Ability.SelectedIndex = ability;
            ComboBox[] m = { CB_Move1, CB_Move2, CB_Move3, CB_Move4, };
            for (int i = 0; i < 4; i++) m[i].SelectedValue = Set.Moves[i];

            // Set Item and Nature
            CB_HeldItem.SelectedValue = (Set.Item < 0) ? 0 : Set.Item;
            CB_Nature.SelectedValue = (Set.Nature < 0) ? 0 : Set.Nature;

            // Set IVs
            TB_HPIV.Text = Set.IVs[0].ToString();
            TB_ATKIV.Text = Set.IVs[1].ToString();
            TB_DEFIV.Text = Set.IVs[2].ToString();
            TB_SPAIV.Text = Set.IVs[3].ToString();
            TB_SPDIV.Text = Set.IVs[4].ToString();
            TB_SPEIV.Text = Set.IVs[5].ToString();

            // Set EVs
            TB_HPEV.Text = Set.EVs[0].ToString();
            TB_ATKEV.Text = Set.EVs[1].ToString();
            TB_DEFEV.Text = Set.EVs[2].ToString();
            TB_SPAEV.Text = Set.EVs[3].ToString();
            TB_SPDEV.Text = Set.EVs[4].ToString();
            TB_SPEEV.Text = Set.EVs[5].ToString();

            // Set Level and Friendship
            TB_Level.Text = Set.Level.ToString();
            TB_Friendship.Text = Set.Friendship.ToString();

            // Reset IV/EVs
            BTN_RerollPID.PerformClick();
            BTN_RerollEC.PerformClick();
            if (Set.Shiny) BTN_Shinytize.PerformClick();
        }
        // Secondary Windows for Ribbons/Amie/Memories
        private void openRibbons(object sender, EventArgs e)
        {
            new RibbMedal(this).ShowDialog();
        }
        private void openHistory(object sender, EventArgs e)
        {
            new MemoryAmie(this).ShowDialog();
        }
        // Open/Save Array Manipulation //
        private bool verifiedPKX()
        {
            if (ModifierKeys == (Keys.Control | Keys.Shift | Keys.Alt))
                return true; // Override
            // Make sure the PKX Fields are filled out properly (color check)
            #region ComboBoxes to verify they are set.
            ComboBox[] cba = {
                                 CB_Species, CB_Nature, CB_HeldItem, CB_Ability, // Main Tab
                                 CB_MetLocation, CB_EggLocation, CB_Ball,   // Met Tab
                                 CB_Move1, CB_Move2, CB_Move3, CB_Move4,    // Moves
                                 CB_RelearnMove1, CB_RelearnMove2, CB_RelearnMove3, CB_RelearnMove4 // Moves
                             };
            for (int i = 0; i < cba.Length; i++)
            {
                int back = cba[i].BackColor.ToArgb();
                if (back == SystemColors.Control.ToArgb() || back == 0 ||
                    !(back != -1 & back != defaultControlWhite.ToArgb())) continue;
                if (i < 6)      // Main Tab
                    tabMain.SelectedIndex = 0;
                else if (i < 9) // Met Tab
                    tabMain.SelectedIndex = 1;
                else            // Moves
                    tabMain.SelectedIndex = 3;
                goto invalid;
            }
            #endregion
            // Further logic checking
            if (Convert.ToUInt32(TB_EVTotal.Text) > 510 && !CHK_HackedStats.Checked)
            { tabMain.SelectedIndex = 2; goto invalid; }
            // If no errors detected...
            if (Util.getIndex(CB_Species) != 0) return true;
            // Else
            tabMain.SelectedIndex = 0;

            // else...
        invalid:
            { System.Media.SystemSounds.Exclamation.Play(); return false; }
        }
        private byte[] preparepkx(bool click = true)
        {
            if (click)
                tabMain.Select(); // hack to make sure comboboxes are set (users scrolling through and immediately setting causes this)
            // Stuff the global byte array with our PKX form data
            // Create a new storage so we don't muck up things with the original
            if (buff.Length == 232) Array.Resize(ref buff, 260);
            byte[] pkx = new byte[0x104];
            Array.Copy(buff, pkx, 0x104);
            // Repopulate PKX with Edited Stuff
            if (Util.getIndex(CB_GameOrigin) < 24)
            {
                uint EC = Util.getHEXval(TB_EC);
                uint PID = Util.getHEXval(TB_PID);
                uint SID = Util.ToUInt32(TB_TID.Text);
                uint TID = Util.ToUInt32(TB_TID.Text);
                uint LID = PID & 0xFFFF;
                uint HID = PID >> 16;
                uint XOR = (TID ^ LID ^ SID ^ HID);

                // Ensure we don't have a shiny.
                if (XOR >> 3 == 1) // Illegal, fix. (not 16<XOR>=8)
                {
                    // Keep as shiny, so we have to mod the PID
                    PID ^= XOR;
                    TB_PID.Text = PID.ToString("X8");
                    TB_EC.Text = PID.ToString("X8");
                }
                else if ((XOR ^ 0x8000) >> 3 == 1 && PID != EC)
                    TB_EC.Text = (PID ^ 0x80000000).ToString("X8");
                else // Not Illegal, no fix.
                    TB_EC.Text = PID.ToString("X8");
            }

            Array.Copy(BitConverter.GetBytes(Util.getHEXval(TB_EC)), 0, pkx, 0, 4);  // EC
            Array.Copy(BitConverter.GetBytes(0), 0, pkx, 0x4, 4);  // 0 CHK for now

            // Block A
            int species = Util.getIndex(CB_Species);
            Array.Copy(BitConverter.GetBytes(species), 0, pkx, 0x08, 2);      // Species
            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_HeldItem)), 0, pkx, 0x0A, 2);     // Held Item
            Array.Copy(BitConverter.GetBytes(Util.ToUInt32(TB_TID.Text)), 0, pkx, 0x0C, 2);     // TID
            Array.Copy(BitConverter.GetBytes(Util.ToUInt32(TB_SID.Text)), 0, pkx, 0x0E, 2);     // SID
            Array.Copy(BitConverter.GetBytes(Convert.ToUInt32(TB_EXP.Text)), 0, pkx, 0x10, 4);  // EXP
            pkx[0x14] = (byte)Array.IndexOf(abilitylist, (CB_Ability.Text).Remove((CB_Ability.Text).Length - 4)); // Ability
            pkx[0x15] = (byte)(Util.ToInt32((TB_AbilityNumber.Text)));   // Number
            // pkx[0x16], pkx[0x17] are handled by the Medals UI (Hits & Training Bag)
            Array.Copy(BitConverter.GetBytes(Util.getHEXval(TB_PID)), 0, pkx, 0x18, 4);         // PID
            pkx[0x1C] = (byte)((Util.getIndex(CB_Nature)));                                     // Nature
            int fegform = Convert.ToInt32(CHK_Fateful.Checked);                                 // Fateful
            fegform |= (PKX.getGender(Label_Gender.Text) << 1);                                 // Gender
            fegform |= (Math.Min((MT_Form.Enabled) ? Convert.ToInt32(MT_Form.Text) : CB_Form.SelectedIndex, 32) << 3); // Form
            pkx[0x1D] = (byte)fegform;
            pkx[0x1E] = (byte)Util.ToInt32(TB_HPEV.Text);       // EVs
            pkx[0x1F] = (byte)Util.ToInt32(TB_ATKEV.Text);
            pkx[0x20] = (byte)Util.ToInt32(TB_DEFEV.Text);
            pkx[0x21] = (byte)Util.ToInt32(TB_SPEEV.Text);
            pkx[0x22] = (byte)Util.ToInt32(TB_SPAEV.Text);
            pkx[0x23] = (byte)Util.ToInt32(TB_SPDEV.Text);

            pkx[0x24] = (byte)Util.ToInt32(TB_Cool.Text);       // CNT
            pkx[0x25] = (byte)Util.ToInt32(TB_Beauty.Text);
            pkx[0x26] = (byte)Util.ToInt32(TB_Cute.Text);
            pkx[0x27] = (byte)Util.ToInt32(TB_Smart.Text);
            pkx[0x28] = (byte)Util.ToInt32(TB_Tough.Text);
            pkx[0x29] = (byte)Util.ToInt32(TB_Sheen.Text);

            int markings = CHK_Circle.Checked ? (1 << 0) : 0;
            markings |= CHK_Triangle.Checked ? (1 << 1) : 0;
            markings |= CHK_Square.Checked ? (1 << 2) : 0;
            markings |= CHK_Heart.Checked ? (1 << 3) : 0;
            markings |= CHK_Star.Checked ? (1 << 4) : 0;
            markings |= CHK_Diamond.Checked ? (1 << 5) : 0;
            pkx[0x2A] = (byte)markings;
            pkx[0x2B] = (byte)(CB_PKRSStrain.SelectedIndex << 4 | CB_PKRSDays.SelectedIndex);

            // Already in buff (then transferred to new pkx)
            // 0x2C, 0x2D, 0x2E, 0x2F
            // 0x30, 0x31, 0x32, 0x33
            // 0x34, 0x35, 0x36, 0x37
            // 0x38, 0x39

            // Unused
            // 0x3A, 0x3B
            // 0x3C, 0x3D, 0x3E, 0x3F

            // Block B
            // Convert Nickname field back to bytes
            string nicknamestr = TB_Nickname.Text;
            {
                nicknamestr = nicknamestr.Replace("\u2640", "\uE08F");
                nicknamestr = nicknamestr.Replace("\u2642", "\uE08E");
                nicknamestr = nicknamestr.Replace("\u0027", "\u2019"); // ' to ’
            }
            byte[] nicknamebytes = Encoding.Unicode.GetBytes(nicknamestr);
            Array.Resize(ref nicknamebytes, nicknamebytes.Length + 2); // pad with zero byte terminator
            Array.Copy(nicknamebytes, 0, pkx, 0x40, nicknamebytes.Length);

            // 0x58, 0x59 unused
            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_Move1)), 0, pkx, 0x5A, 2);  // Move 1
            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_Move2)), 0, pkx, 0x5C, 2);  // Move 2
            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_Move3)), 0, pkx, 0x5E, 2);  // Move 3
            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_Move4)), 0, pkx, 0x60, 2);  // Move 4

            pkx[0x62] = (byte)(Util.getIndex(CB_Move1) > 0 ? Util.ToInt32(TB_PP1.Text) : 0);    // Max PP
            pkx[0x63] = (byte)(Util.getIndex(CB_Move2) > 0 ? Util.ToInt32(TB_PP2.Text) : 0);
            pkx[0x64] = (byte)(Util.getIndex(CB_Move3) > 0 ? Util.ToInt32(TB_PP3.Text) : 0);
            pkx[0x65] = (byte)(Util.getIndex(CB_Move4) > 0 ? Util.ToInt32(TB_PP4.Text) : 0);

            pkx[0x66] = (byte)(Util.getIndex(CB_Move1) > 0 ? CB_PPu1.SelectedIndex : 0);        // PP Ups
            pkx[0x67] = (byte)(Util.getIndex(CB_Move2) > 0 ? CB_PPu2.SelectedIndex : 0);
            pkx[0x68] = (byte)(Util.getIndex(CB_Move3) > 0 ? CB_PPu3.SelectedIndex : 0);
            pkx[0x69] = (byte)(Util.getIndex(CB_Move4) > 0 ? CB_PPu4.SelectedIndex : 0);

            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_RelearnMove1)), 0, pkx, 0x6A, 2);  // EggMove 1
            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_RelearnMove2)), 0, pkx, 0x6C, 2);  // EggMove 2
            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_RelearnMove3)), 0, pkx, 0x6E, 2);  // EggMove 3
            Array.Copy(BitConverter.GetBytes(Util.getIndex(CB_RelearnMove4)), 0, pkx, 0x70, 2);  // EggMove 4

            // 0x72 - Ribbon editor sets this flag (Secret Super Training)
            // 0x73

            uint IV32 = Util.ToUInt32(TB_HPIV.Text) & 0x1F;
            IV32 |= ((Util.ToUInt32(TB_ATKIV.Text) & 0x1F) << 5);
            IV32 |= ((Util.ToUInt32(TB_DEFIV.Text) & 0x1F) << 10);
            IV32 |= ((Util.ToUInt32(TB_SPEIV.Text) & 0x1F) << 15);
            IV32 |= ((Util.ToUInt32(TB_SPAIV.Text) & 0x1F) << 20);
            IV32 |= ((Util.ToUInt32(TB_SPDIV.Text) & 0x1F) << 25);
            IV32 |= (Convert.ToUInt32(CHK_IsEgg.Checked) << 30);
            IV32 |= (Convert.ToUInt32(CHK_Nicknamed.Checked) << 31);

            Array.Copy(BitConverter.GetBytes(IV32), 0, pkx, 0x74, 4);  // Copy in IVs

            // Block C
            // Convert Latest OT field back to bytes
            byte[] OT2 = Encoding.Unicode.GetBytes(Util.TrimFromZero(TB_OTt2.Text).Replace("\u0027", "\u2019"));
            Array.Resize(ref OT2, OT2.Length + 2); // Allow Trash
            Array.Copy(OT2, 0, pkx, 0x78, OT2.Length);

            // 0x90-0xAF
            pkx[0x92] = Convert.ToByte(PKX.getGender(Label_CTGender.Text) == 1);
            // Plus more, set by MemoryAmie (already in buff)

            // Block D
            // Convert OT field back to bytes
            byte[] OT = Encoding.Unicode.GetBytes(TB_OT.Text.Replace("\u0027", "\u2019"));
            Array.Resize(ref OT, OT.Length + 2); // Pad with \0000 to terminate
            Array.Copy(OT, 0, pkx, 0xB0, OT.Length);

            if (pkx[0x93] == 0)
                pkx[0xCA] = (byte)(Util.ToInt32(TB_Friendship.Text) & 0xFF);
            else          // 1
                pkx[0xA2] = (byte)(Util.ToInt32(TB_Friendship.Text) & 0xFF);

            int egg_year = 2000;                                   // Dates
            int egg_month = 0;
            int egg_day = 0;
            int egg_location = 0;
            if (CHK_AsEgg.Checked)      // If encountered as an egg, load the Egg Met data from fields.
            {
                egg_year = CAL_EggDate.Value.Year;
                egg_month = CAL_EggDate.Value.Month;
                egg_day = CAL_EggDate.Value.Day;
                egg_location = Util.getIndex(CB_EggLocation);
            }
            // Egg Met Data
            pkx[0xD1] = (byte)(egg_year - 2000);
            pkx[0xD2] = (byte)egg_month;
            pkx[0xD3] = (byte)egg_day;
            // Met Data
            pkx[0xD4] = (byte)(CAL_MetDate.Value.Year - 2000);
            pkx[0xD5] = (byte)CAL_MetDate.Value.Month;
            pkx[0xD6] = (byte)CAL_MetDate.Value.Day;

            if (CHK_IsEgg.Checked && CB_MetLocation.SelectedIndex == 0)    // If still an egg, it has no hatch location/date. Zero it!
            {
                pkx[0xD4] = 0;
                pkx[0xD5] = 0;
                pkx[0xD6] = 0;
            }

            // 0xD7 Unknown
            int met_location = Util.getIndex(CB_MetLocation);    // Locations
            Array.Copy(BitConverter.GetBytes(egg_location), 0, pkx, 0xD8, 2);   // Egg Location
            Array.Copy(BitConverter.GetBytes(met_location), 0, pkx, 0xDA, 2);   // Met Location

            pkx[0xDC] = (byte)Util.getIndex(CB_Ball);
            pkx[0xDD] = (byte)(((Util.ToInt32(TB_MetLevel.Text) & 0x7F) | (Convert.ToInt32(PKX.getGender(Label_OTGender.Text) == 1) << 7)));
            pkx[0xDE] = (byte)(Util.ToInt32(CB_EncounterType.SelectedValue.ToString()));
            pkx[0xDF] = (byte)Util.getIndex(CB_GameOrigin);
            pkx[0xE0] = (byte)Util.getIndex(CB_Country);
            pkx[0xE1] = (byte)Util.getIndex(CB_SubRegion);
            pkx[0xE2] = (byte)Util.getIndex(CB_3DSReg);
            pkx[0xE3] = (byte)Util.getIndex(CB_Language);
            // 0xE4-0xE7

            Array.Resize(ref pkx, 260);
            // Party Stats
            pkx[0xE8] = 0; pkx[0xE9] = 0;
            pkx[0xEA] = 0; pkx[0xEB] = 0;
            pkx[0xEC] = (byte)Util.ToInt32(TB_Level.Text);                                                     // Level
            pkx[0xED] = 0; pkx[0xEE] = 0; pkx[0xEF] = 0;
            Array.Copy(BitConverter.GetBytes(Math.Min(Util.ToInt32(Stat_HP.Text), 65535)), 0, pkx, 0xF0, 2);   // Current HP
            Array.Copy(BitConverter.GetBytes(Math.Min(Util.ToInt32(Stat_HP.Text), 65535)), 0, pkx, 0xF2, 2);   // Max HP
            Array.Copy(BitConverter.GetBytes(Math.Min(Util.ToInt32(Stat_ATK.Text), 65535)), 0, pkx, 0xF4, 2);  // ATK
            Array.Copy(BitConverter.GetBytes(Math.Min(Util.ToInt32(Stat_DEF.Text), 65535)), 0, pkx, 0xF6, 2);  // DEF
            Array.Copy(BitConverter.GetBytes(Math.Min(Util.ToInt32(Stat_SPE.Text), 65535)), 0, pkx, 0xF8, 2);  // SPE
            Array.Copy(BitConverter.GetBytes(Math.Min(Util.ToInt32(Stat_SPA.Text), 65535)), 0, pkx, 0xFA, 2);  // SPA
            Array.Copy(BitConverter.GetBytes(Math.Min(Util.ToInt32(Stat_SPD.Text), 65535)), 0, pkx, 0xFC, 2);  // SPD
            pkx[0xFE] = 0; pkx[0xFF] = 0;
            pkx[0x100] = 0; pkx[0x101] = 0; pkx[0x102] = 0; pkx[0x103] = 0;

            // Hax Illegality
            if (HaX)
            {
                pkx[0x14] = (byte)Util.getIndex(DEV_Ability);                                                   // Ability
                pkx[0xEC] = (byte)Math.Min(Convert.ToInt32(MT_Level.Text), 255);                                // Level
            }

            // Fix Moves if a slot is empty
            for (int i = 0; i < 3; i++)
            {
                if (BitConverter.ToUInt16(pkx, 0x5A + 2*i) != 0) continue;
                Array.Copy(pkx, 0x5C + 2 * i, pkx, 0x5A + 2 * i, 2); // Shift moves down
                Array.Copy(new byte[2], 0, pkx, 0x5C + 2 * i, 2);   // Clear next move (error shifted down)

                // Move PP and PP Ups down one byte.
                pkx[0x62 + i] = pkx[0x63 + i]; pkx[0x63 + i] = 0; // PP
                pkx[0x66 + i] = pkx[0x67 + i]; pkx[0x67 + i] = 0; // PP Ups
            }

            // Fix Relearn moves if a slot is empty
            for (int i = 0; i < 3; i++)
            {
                if (BitConverter.ToUInt16(pkx, 0x6A + 2*i) != 0) continue;
                Array.Copy(pkx, 0x6C + 2 * i, pkx, 0x6A + 2 * i, 2); // Shift moves down
                Array.Copy(new byte[2], 0, pkx, 0x6C + 2 * i, 2);   // Clear next move (error shifted down)
            }

            // No foreign memories for Pokemon without a foreign trainer
            if (BitConverter.ToUInt16(pkx, 0x78) == 0)
            {
                pkx[0xA2] = pkx[0xA3] =                 // No Friendship/Affection
                pkx[0xA8] = pkx[0xA9] =                 // No Memory Var
                pkx[0xA4] = pkx[0xA5] = pkx[0xA6] = 0;  // No Memory Types
            }

            // Now we fix the checksum!
            ushort chk = 0;
            for (int i = 8; i < 232; i += 2) // Loop through the entire PKX
                chk += BitConverter.ToUInt16(pkx, i);

            // Apply New Checksum
            Array.Copy(BitConverter.GetBytes(chk), 0, pkx, 6, 2);

            // PKX is now filled
            return pkx;
        }
        // Drag & Drop Events
        private void tabMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        private void tabMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string path = files[0]; // open first D&D
            openQuick(path);
        }
        // Decrypted Export
        private void dragout_MouseDown(object sender, MouseEventArgs e)
        {
            if (!verifiedPKX()) { return; }
            {
                // Create Temp File to Drag
                string basepath = Application.StartupPath;
                Cursor.Current = Cursors.Hand;

                // Make a new file name
                byte[] dragdata = preparepkx();
                PKX pkx = new PKX(dragdata, "Tabs");
                string filename = pkx.Nickname;
                if (filename != pkx.Species)
                    filename += " (" + pkx.Species + ")";
                filename += " - " + pkx.PID;

                filename += (e.Button == MouseButtons.Right) ? ".ek6" : ".pk6";
                dragdata = (e.Button == MouseButtons.Right) ? PKX.encryptArray(preparepkx()) : preparepkx();
                // Strip out party stats (if they are there)
                Array.Resize(ref dragdata, 232);
                // Make file
                string newfile = Path.Combine(basepath, Util.CleanFileName(filename));
                try
                {
                    File.WriteAllBytes(newfile, dragdata);

                    string[] filesToDrag = { newfile };
                    dragout.DoDragDrop(new DataObject(DataFormats.FileDrop, filesToDrag), DragDropEffects.Move);
                    File.Delete(newfile);
                }
                catch (Exception x)
                { Util.Error("Drag & Drop Error", x.ToString()); }
                File.Delete(newfile);
            }
        }
        private void dragout_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
        // Dragout Display
        private void dragoutHover(object sender, EventArgs e)
        {
            dragout.BackgroundImage = (Util.getIndex(CB_Species) > 0) ? Properties.Resources.slotSet : Properties.Resources.slotDel;
        }
        private void dragoutLeave(object sender, EventArgs e)
        {
            dragout.BackgroundImage = Properties.Resources.slotTrans;
        }
        private void dragoutDrop(object sender, DragEventArgs e)
        {
            openQuick(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
        }
        #endregion

        #region //// SAVE FILE FUNCTIONS ////
        // Integrity Checks // 
        private void clickVerifyCHK(object sender, EventArgs e)
        {
            if (savedited)
            {
                Util.Alert("Save has been edited. Cannot integrity check.");
                return;
            }
            // Verify Checksums
            RTB_S.Text = "";
            int invalid1 = 0;
            uint[] start = { 0x05400, 0x05800, 0x06400, 0x06600, 0x06800, 0x06A00, 0x06C00, 0x06E00, 0x07000, 0x07200, 0x07400, 0x09600, 0x09800, 0x09E00, 0x0A400, 0x0F400, 0x14400, 0x19400, 0x19600, 0x19E00, 0x1A400, 0x1AC00, 0x1B400, 0x1B600, 0x1B800, 0x1BE00, 0x1C000, 0x1C400, 0x1CC00, 0x1CE00, 0x1D000, 0x1D200, 0x1D400, 0x1D600, 0x1DE00, 0x1E400, 0x1E800, 0x20400, 0x20600, 0x20800, 0x20C00, 0x21000, 0x22C00, 0x23000, 0x23800, 0x23C00, 0x24600, 0x24A00, 0x25200, 0x26000, 0x26200, 0x26400, 0x27200, 0x27A00, 0x5C600, };
            uint[] length = { 0x000002C8, 0x00000B88, 0x0000002C, 0x00000038, 0x00000150, 0x00000004, 0x00000008, 0x000001C0, 0x000000BE, 0x00000024, 0x00002100, 0x00000140, 0x00000440, 0x00000574, 0x00004E28, 0x00004E28, 0x00004E28, 0x00000170, 0x0000061C, 0x00000504, 0x000006A0, 0x00000644, 0x00000104, 0x00000004, 0x00000420, 0x00000064, 0x000003F0, 0x0000070C, 0x00000180, 0x00000004, 0x0000000C, 0x00000048, 0x00000054, 0x00000644, 0x000005C8, 0x000002F8, 0x00001B40, 0x000001F4, 0x000001F0, 0x00000216, 0x00000390, 0x00001A90, 0x00000308, 0x00000618, 0x0000025C, 0x00000834, 0x00000318, 0x000007D0, 0x00000C48, 0x00000078, 0x00000200, 0x00000C84, 0x00000628, 0x00034AD0, 0x0000E058, };
            int csoff = 0x6A81A;

            if (savegame_oras)
            {
                start = new uint[] { 0x05400, 0x05800, 0x06400, 0x06600, 0x06800, 0x06A00, 0x06C00, 0x06E00, 0x07000, 0x07200, 0x07400, 0x09600, 0x09800, 0x09E00, 0x0A400, 0x0F400, 0x14400, 0x19400, 0x19600, 0x19E00, 0x1A400, 0x1B600, 0x1BE00, 0x1C000, 0x1C200, 0x1C800, 0x1CA00, 0x1CE00, 0x1D600, 0x1D800, 0x1DA00, 0x1DC00, 0x1DE00, 0x1E000, 0x1E800, 0x1EE00, 0x1F200, 0x20E00, 0x21000, 0x21400, 0x21800, 0x22000, 0x23C00, 0x24000, 0x24800, 0x24C00, 0x25600, 0x25A00, 0x26200, 0x27000, 0x27200, 0x27400, 0x28200, 0x28A00, 0x28E00, 0x30A00, 0x38400, 0x6D000, };
                length = new uint[] { 0x000002C8, 0x00000B90, 0x0000002C, 0x00000038, 0x00000150, 0x00000004, 0x00000008, 0x000001C0, 0x000000BE, 0x00000024, 0x00002100, 0x00000130, 0x00000440, 0x00000574, 0x00004E28, 0x00004E28, 0x00004E28, 0x00000170, 0x0000061C, 0x00000504, 0x000011CC, 0x00000644, 0x00000104, 0x00000004, 0x00000420, 0x00000064, 0x000003F0, 0x0000070C, 0x00000180, 0x00000004, 0x0000000C, 0x00000048, 0x00000054, 0x00000644, 0x000005C8, 0x000002F8, 0x00001B40, 0x000001F4, 0x000003E0, 0x00000216, 0x00000640, 0x00001A90, 0x00000400, 0x00000618, 0x0000025C, 0x00000834, 0x00000318, 0x000007D0, 0x00000C48, 0x00000078, 0x00000200, 0x00000C84, 0x00000628, 0x00000400, 0x00007AD0, 0x000078B0, 0x00034AD0, 0x0000E058, };
                csoff = 0x7B21A;
            }

            for (int i = 0; i < length.Length; i++)
            {
                byte[] data = new byte[length[i]];
                Array.Copy(savefile, start[i], data, 0, length[i]);
                ushort checksum = PKX.ccitt16(data);
                ushort actualsum = BitConverter.ToUInt16(savefile, csoff + i * 0x8);
                if (checksum == actualsum) continue;

                invalid1++;
                RTB_S.Text += "Invalid: " + i.ToString("X2") + " @ region " + start[i].ToString("X5") + Environment.NewLine;
            }
            RTB_S.Text += "1st SAV: " + (start.Length - invalid1) + "/" + start.Length + Environment.NewLine;

            if (cybergadget) return;

            // Do it again, but for the second SAV.
            csoff += 0x7F000;

            for (int i = 0; i < start.Length; i++) // Shift all the save offsets...
                start[i] += 0x7F000;

            int invalid2 = 0;
            for (int i = 0; i < length.Length; i++)
            {
                byte[] data = new byte[length[i]];
                Array.Copy(savefile, start[i], data, 0, length[i]);
                ushort checksum = PKX.ccitt16(data);
                ushort actualsum = BitConverter.ToUInt16(savefile, csoff + i * 0x8);
                if (checksum == actualsum) continue;
                invalid2++;
                RTB_S.Text += "Invalid: " + i.ToString("X2") + " @ region " + start[i].ToString("X5") + Environment.NewLine;
            }
            RTB_S.Text += "2nd SAV: " + (start.Length - invalid2) + "/" + start.Length + Environment.NewLine;
            if (invalid1 + invalid2 == (start.Length * 2))
                RTB_S.Text = "No checksums are valid.";
        }
        private void clickVerifySHA(object sender, EventArgs e)
        {
            if (savedited)
            {
                RTB_S.Text = "Save has been edited. Cannot integrity check.";
                return;
            }
            // Verify Hashes
            RTB_S.Text = "";
            int invalid1 = 0;
            #region hash table data
            uint[] hashtabledata = {
                                    0x2020,	    0x203F,	    0x2000,	0x200,
                                    0x2040,	    0x2FFF,	    0x2020,	0x1000,
                                    0x3000,	    0x3FFF,	    0x2040,	0x1000,
                                    0x4000,	    0x4FFF,	    0x2060,	0x1000,
                                    0x5000,	    0x5FFF,	    0x2080,	0x1000,
                                    0x6000,	    0x6FFF,	    0x20A0,	0x1000,
                                    0x7000,	    0x7FFF,	    0x20C0,	0x1000,
                                    0x8000,	    0x8FFF,	    0x20E0,	0x1000,
                                    0x9000,	    0x9FFF,	    0x2100,	0x1000,
                                    0xA000,	    0xAFFF,	    0x2120,	0x1000,
                                    0xB000,	    0xBFFF,	    0x2140,	0x1000,
                                    0xC000,	    0xCFFF,	    0x2160,	0x1000,
                                    0xD000,	    0xDFFF,	    0x2180,	0x1000,
                                    0xE000,	    0xEFFF,	    0x21A0,	0x1000,
                                    0xF000,	    0xFFFF,	    0x21C0,	0x1000,
                                    0x10000,	0x10FFF,	0x21E0,	0x1000,
                                    0x11000,	0x11FFF,	0x2200,	0x1000,
                                    0x12000,	0x12FFF,	0x2220,	0x1000,
                                    0x13000,	0x13FFF,	0x2240,	0x1000,
                                    0x14000,	0x14FFF,	0x2260,	0x1000,
                                    0x15000,	0x15FFF,	0x2280,	0x1000,
                                    0x16000,	0x16FFF,	0x22A0,	0x1000,
                                    0x17000,	0x17FFF,	0x22C0,	0x1000,
                                    0x18000,	0x18FFF,	0x22E0,	0x1000,
                                    0x19000,	0x19FFF,	0x2300,	0x1000,
                                    0x1A000,	0x1AFFF,	0x2320,	0x1000,
                                    0x1B000,	0x1BFFF,	0x2340,	0x1000,
                                    0x1C000,	0x1CFFF,	0x2360,	0x1000,
                                    0x1D000,	0x1DFFF,	0x2380,	0x1000,
                                    0x1E000,	0x1EFFF,	0x23A0,	0x1000,
                                    0x1F000,	0x1FFFF,	0x23C0,	0x1000,
                                    0x20000,	0x20FFF,	0x23E0,	0x1000,
                                    0x21000,	0x21FFF,	0x2400,	0x1000,
                                    0x22000,	0x22FFF,	0x2420,	0x1000,
                                    0x23000,	0x23FFF,	0x2440,	0x1000,
                                    0x24000,	0x24FFF,	0x2460,	0x1000,
                                    0x25000,	0x25FFF,	0x2480,	0x1000,
                                    0x26000,	0x26FFF,	0x24A0,	0x1000,
                                    0x27000,	0x27FFF,	0x24C0,	0x1000,
                                    0x28000,	0x28FFF,	0x24E0,	0x1000,
                                    0x29000,	0x29FFF,	0x2500,	0x1000,
                                    0x2A000,	0x2AFFF,	0x2520,	0x1000,
                                    0x2B000,	0x2BFFF,	0x2540,	0x1000,
                                    0x2C000,	0x2CFFF,	0x2560,	0x1000,
                                    0x2D000,	0x2DFFF,	0x2580,	0x1000,
                                    0x2E000,	0x2EFFF,	0x25A0,	0x1000,
                                    0x2F000,	0x2FFFF,	0x25C0,	0x1000,
                                    0x30000,	0x30FFF,	0x25E0,	0x1000,
                                    0x31000,	0x31FFF,	0x2600,	0x1000,
                                    0x32000,	0x32FFF,	0x2620,	0x1000,
                                    0x33000,	0x33FFF,	0x2640,	0x1000,
                                    0x34000,	0x34FFF,	0x2660,	0x1000,
                                    0x35000,	0x35FFF,	0x2680,	0x1000,
                                    0x36000,	0x36FFF,	0x26A0,	0x1000,
                                    0x37000,	0x37FFF,	0x26C0,	0x1000,
                                    0x38000,	0x38FFF,	0x26E0,	0x1000,
                                    0x39000,	0x39FFF,	0x2700,	0x1000,
                                    0x3A000,	0x3AFFF,	0x2720,	0x1000,
                                    0x3B000,	0x3BFFF,	0x2740,	0x1000,
                                    0x3C000,	0x3CFFF,	0x2760,	0x1000,
                                    0x3D000,	0x3DFFF,	0x2780,	0x1000,
                                    0x3E000,	0x3EFFF,	0x27A0,	0x1000,
                                    0x3F000,	0x3FFFF,	0x27C0,	0x1000,
                                    0x40000,	0x40FFF,	0x27E0,	0x1000,
                                    0x41000,	0x41FFF,	0x2800,	0x1000,
                                    0x42000,	0x42FFF,	0x2820,	0x1000,
                                    0x43000,	0x43FFF,	0x2840,	0x1000,
                                    0x44000,	0x44FFF,	0x2860,	0x1000,
                                    0x45000,	0x45FFF,	0x2880,	0x1000,
                                    0x46000,	0x46FFF,	0x28A0,	0x1000,
                                    0x47000,	0x47FFF,	0x28C0,	0x1000,
                                    0x48000,	0x48FFF,	0x28E0,	0x1000,
                                    0x49000,	0x49FFF,	0x2900,	0x1000,
                                    0x4A000,	0x4AFFF,	0x2920,	0x1000,
                                    0x4B000,	0x4BFFF,	0x2940,	0x1000,
                                    0x4C000,	0x4CFFF,	0x2960,	0x1000,
                                    0x4D000,	0x4DFFF,	0x2980,	0x1000,
                                    0x4E000,	0x4EFFF,	0x29A0,	0x1000,
                                    0x4F000,	0x4FFFF,	0x29C0,	0x1000,
                                    0x50000,	0x50FFF,	0x29E0,	0x1000,
                                    0x51000,	0x51FFF,	0x2A00,	0x1000,
                                    0x52000,	0x52FFF,	0x2A20,	0x1000,
                                    0x53000,	0x53FFF,	0x2A40,	0x1000,
                                    0x54000,	0x54FFF,	0x2A60,	0x1000,
                                    0x55000,	0x55FFF,	0x2A80,	0x1000,
                                    0x56000,	0x56FFF,	0x2AA0,	0x1000,
                                    0x57000,	0x57FFF,	0x2AC0,	0x1000,
                                    0x58000,	0x58FFF,	0x2AE0,	0x1000,
                                    0x59000,	0x59FFF,	0x2B00,	0x1000,
                                    0x5A000,	0x5AFFF,	0x2B20,	0x1000,
                                    0x5B000,	0x5BFFF,	0x2B40,	0x1000,
                                    0x5C000,	0x5CFFF,	0x2B60,	0x1000,
                                    0x5D000,	0x5DFFF,	0x2B80,	0x1000,
                                    0x5E000,	0x5EFFF,	0x2BA0,	0x1000,
                                    0x5F000,	0x5FFFF,	0x2BC0,	0x1000,
                                    0x60000,	0x60FFF,	0x2BE0,	0x1000,
                                    0x61000,	0x61FFF,	0x2C00,	0x1000,
                                    0x62000,	0x62FFF,	0x2C20,	0x1000,
                                    0x63000,	0x63FFF,	0x2C40,	0x1000,
                                    0x64000,	0x64FFF,	0x2C60,	0x1000,
                                    0x65000,	0x65FFF,	0x2C80,	0x1000,
                                    0x66000,	0x66FFF,	0x2CA0,	0x1000,
                                    0x67000,	0x67FFF,	0x2CC0,	0x1000,
                                    0x68000,	0x68FFF,	0x2CE0,	0x1000,
                                    0x69000,	0x69FFF,	0x2D00,	0x1000,
                                    0x6A000,	0x6AFFF,	0x2D20,	0x1000,
                                 };
            #endregion
            SHA256 mySHA256 = SHA256.Create();

            for (int i = 0; i < hashtabledata.Length / 4; i++)
            {
                uint start = hashtabledata[0 + 4 * i];
                uint length = hashtabledata[1 + 4 * i] - hashtabledata[0 + 4 * i];
                uint offset = hashtabledata[2 + 4 * i];
                uint blocksize = hashtabledata[3 + 4 * i];

                byte[] zeroarray = new byte[blocksize];
                Array.Copy(savefile, start, zeroarray, 0, length + 1);
                byte[] hashValue = mySHA256.ComputeHash(zeroarray);
                byte[] actualhash = new byte[0x20];
                Array.Copy(savefile, offset, actualhash, 0, 0x20);

                if (hashValue.SequenceEqual(actualhash)) continue;
                invalid1++;
                RTB_S.Text += "Invalid: " + hashtabledata[2 + 4 * i].ToString("X5") + " @ " + hashtabledata[0 + 4 * i].ToString("X5") + "-" + hashtabledata[1 + 4 * i].ToString("X5") + Environment.NewLine;
            }
            RTB_S.Text += "1st SAV: " + (106 - invalid1) + "/" + 106 + Environment.NewLine;

            // Check The Second Half of Hashes
            for (int i = 0; i < hashtabledata.Length; i += 4)
            {
                hashtabledata[i + 0] += 0x7F000;
                hashtabledata[i + 1] += 0x7F000;
                hashtabledata[i + 2] += 0x7F000;
            }
            // Problem with save2 saves is that 0x3000-0x4FFF doesn't use save2 data. Probably different when hashed, but different when stored.
            for (int i = 2; i < 4; i++)
            {
                hashtabledata[i * 4 + 0] -= 0x7F000;
                hashtabledata[i * 4 + 1] -= 0x7F000;
            }
            int invalid2 = 0;

            for (int i = 0; i < hashtabledata.Length / 4; i++)
            {
                uint start = hashtabledata[0 + 4 * i];
                uint length = hashtabledata[1 + 4 * i] - hashtabledata[0 + 4 * i];
                uint offset = hashtabledata[2 + 4 * i];
                uint blocksize = hashtabledata[3 + 4 * i];

                byte[] zeroarray = new byte[blocksize];
                Array.Copy(savefile, start, zeroarray, 0, length + 1);
                byte[] hashValue = mySHA256.ComputeHash(zeroarray);
                byte[] actualhash = new byte[0x20];
                Array.Copy(savefile, offset, actualhash, 0, 0x20);

                if (hashValue.SequenceEqual(actualhash)) continue;
                invalid2++;
                RTB_S.Text += "Invalid: " + hashtabledata[2 + 4 * i].ToString("X5") + " @ " + hashtabledata[0 + 4 * i].ToString("X5") + "-" + hashtabledata[1 + 4 * i].ToString("X5") + Environment.NewLine;
            }
            RTB_S.Text += "2nd SAV: " + (106 - invalid2) + "/" + 106 + Environment.NewLine;

            if (invalid1 + invalid2 == (2 * 106))
                RTB_S.Text = "None of the IVFC hashes are valid." + Environment.NewLine;

            // Check the Upper Level IVFC Hashes
            {
                byte[] zeroarray = new byte[0x200];
                Array.Copy(savefile, 0x2000, zeroarray, 0, 0x20);
                byte[] hashValue = mySHA256.ComputeHash(zeroarray);
                byte[] actualhash = new byte[0x20];
                Array.Copy(savefile, 0x43C, actualhash, 0, 0x20);
                if (!hashValue.SequenceEqual(actualhash))
                    RTB_S.Text += "Invalid: " + 0x2000.ToString("X5") + " @ " + 0x43C.ToString("X3") + Environment.NewLine;
            }
            {
                byte[] zeroarray = new byte[0x200];
                Array.Copy(savefile, 0x81000, zeroarray, 0, 0x20);
                byte[] hashValue = mySHA256.ComputeHash(zeroarray);
                byte[] actualhash = new byte[0x20];
                Array.Copy(savefile, 0x30C, actualhash, 0, 0x20);
                if (!hashValue.SequenceEqual(actualhash))
                    RTB_S.Text += "Invalid: " + 0x81000.ToString("X5") + " @ " + 0x30C.ToString("X3") + Environment.NewLine;
            }
            {
                byte[] difihash1 = new byte[0x12C];
                byte[] difihash2 = new byte[0x12C];
                Array.Copy(savefile, 0x330, difihash1, 0, 0x12C);
                Array.Copy(savefile, 0x200, difihash2, 0, 0x12C);
                byte[] hashValue1 = mySHA256.ComputeHash(difihash1);
                byte[] hashValue2 = mySHA256.ComputeHash(difihash2);
                byte[] actualhash = new byte[0x20];
                Array.Copy(savefile, 0x16C, actualhash, 0, 0x20);
                if (hashValue1.SequenceEqual(actualhash))
                    RTB_S.Text += "Active DIFI partition is Save 1.";
                else if (hashValue2.SequenceEqual(actualhash))
                    RTB_S.Text += "Active DIFI partition is Save 2.";
                else
                    RTB_S.Text += "ERROR: NO ACTIVE DIFI HASH MATCH";
            }
        }
        private void clickExportSAV(object sender, EventArgs e)
        {            
            // Create another version of the save file.
            byte[] editedsav = new byte[0x100000];
            Array.Copy(savefile, editedsav, savefile.Length);
            // Since we only edited one of the save files, we only have to fix half of the chk/hashes!

            // Fix Checksums
            {
                uint[] start = { 0x05400, 0x05800, 0x06400, 0x06600, 0x06800, 0x06A00, 0x06C00, 0x06E00, 0x07000, 0x07200, 0x07400, 0x09600, 0x09800, 0x09E00, 0x0A400, 0x0F400, 0x14400, 0x19400, 0x19600, 0x19E00, 0x1A400, 0x1AC00, 0x1B400, 0x1B600, 0x1B800, 0x1BE00, 0x1C000, 0x1C400, 0x1CC00, 0x1CE00, 0x1D000, 0x1D200, 0x1D400, 0x1D600, 0x1DE00, 0x1E400, 0x1E800, 0x20400, 0x20600, 0x20800, 0x20C00, 0x21000, 0x22C00, 0x23000, 0x23800, 0x23C00, 0x24600, 0x24A00, 0x25200, 0x26000, 0x26200, 0x26400, 0x27200, 0x27A00, 0x5C600, };
                uint[] length = { 0x000002C8, 0x00000B88, 0x0000002C, 0x00000038, 0x00000150, 0x00000004, 0x00000008, 0x000001C0, 0x000000BE, 0x00000024, 0x00002100, 0x00000140, 0x00000440, 0x00000574, 0x00004E28, 0x00004E28, 0x00004E28, 0x00000170, 0x0000061C, 0x00000504, 0x000006A0, 0x00000644, 0x00000104, 0x00000004, 0x00000420, 0x00000064, 0x000003F0, 0x0000070C, 0x00000180, 0x00000004, 0x0000000C, 0x00000048, 0x00000054, 0x00000644, 0x000005C8, 0x000002F8, 0x00001B40, 0x000001F4, 0x000001F0, 0x00000216, 0x00000390, 0x00001A90, 0x00000308, 0x00000618, 0x0000025C, 0x00000834, 0x00000318, 0x000007D0, 0x00000C48, 0x00000078, 0x00000200, 0x00000C84, 0x00000628, 0x00034AD0, 0x0000E058, };
                int csoff = 0x6A81A;

                if (savegame_oras)
                {
                    start = new uint[] { 0x05400, 0x05800, 0x06400, 0x06600, 0x06800, 0x06A00, 0x06C00, 0x06E00, 0x07000, 0x07200, 0x07400, 0x09600, 0x09800, 0x09E00, 0x0A400, 0x0F400, 0x14400, 0x19400, 0x19600, 0x19E00, 0x1A400, 0x1B600, 0x1BE00, 0x1C000, 0x1C200, 0x1C800, 0x1CA00, 0x1CE00, 0x1D600, 0x1D800, 0x1DA00, 0x1DC00, 0x1DE00, 0x1E000, 0x1E800, 0x1EE00, 0x1F200, 0x20E00, 0x21000, 0x21400, 0x21800, 0x22000, 0x23C00, 0x24000, 0x24800, 0x24C00, 0x25600, 0x25A00, 0x26200, 0x27000, 0x27200, 0x27400, 0x28200, 0x28A00, 0x28E00, 0x30A00, 0x38400, 0x6D000, };
                    length = new uint[] { 0x000002C8, 0x00000B90, 0x0000002C, 0x00000038, 0x00000150, 0x00000004, 0x00000008, 0x000001C0, 0x000000BE, 0x00000024, 0x00002100, 0x00000130, 0x00000440, 0x00000574, 0x00004E28, 0x00004E28, 0x00004E28, 0x00000170, 0x0000061C, 0x00000504, 0x000011CC, 0x00000644, 0x00000104, 0x00000004, 0x00000420, 0x00000064, 0x000003F0, 0x0000070C, 0x00000180, 0x00000004, 0x0000000C, 0x00000048, 0x00000054, 0x00000644, 0x000005C8, 0x000002F8, 0x00001B40, 0x000001F4, 0x000003E0, 0x00000216, 0x00000640, 0x00001A90, 0x00000400, 0x00000618, 0x0000025C, 0x00000834, 0x00000318, 0x000007D0, 0x00000C48, 0x00000078, 0x00000200, 0x00000C84, 0x00000628, 0x00000400, 0x00007AD0, 0x000078B0, 0x00034AD0, 0x0000E058, };
                    csoff = 0x7B21A;
                }

                if (savindex == 1)
                {
                    csoff += 0x7F000;
                    for (int i = 0; i < start.Length; i++)
                        start[i] += 0x7F000;
                }

                for (int i = 0; i < length.Length; i++)
                {
                    byte[] data = new byte[length[i]];
                    Array.Copy(editedsav, start[i], data, 0, length[i]);
                    ushort checksum = PKX.ccitt16(data);
                    Array.Copy(BitConverter.GetBytes(checksum), 0, editedsav, csoff + i * 8, 2);
                }

                if (cybergadget) goto export;
            }

            // Fix Hashes
            {
                #region hash table data
                uint[] hashtabledata = {
                                    0x2020,	    0x203F,	    0x2000,	0x200,
                                    0x2040,	    0x2FFF,	    0x2020,	0x1000,
                                    0x3000,	    0x3FFF,	    0x2040,	0x1000,
                                    0x4000,	    0x4FFF,	    0x2060,	0x1000,
                                    0x5000,	    0x5FFF,	    0x2080,	0x1000,
                                    0x6000,	    0x6FFF,	    0x20A0,	0x1000,
                                    0x7000,	    0x7FFF,	    0x20C0,	0x1000,
                                    0x8000,	    0x8FFF,	    0x20E0,	0x1000,
                                    0x9000,	    0x9FFF,	    0x2100,	0x1000,
                                    0xA000,	    0xAFFF,	    0x2120,	0x1000,
                                    0xB000,	    0xBFFF,	    0x2140,	0x1000,
                                    0xC000,	    0xCFFF,	    0x2160,	0x1000,
                                    0xD000,	    0xDFFF,	    0x2180,	0x1000,
                                    0xE000,	    0xEFFF,	    0x21A0,	0x1000,
                                    0xF000,	    0xFFFF,	    0x21C0,	0x1000,
                                    0x10000,	0x10FFF,	0x21E0,	0x1000,
                                    0x11000,	0x11FFF,	0x2200,	0x1000,
                                    0x12000,	0x12FFF,	0x2220,	0x1000,
                                    0x13000,	0x13FFF,	0x2240,	0x1000,
                                    0x14000,	0x14FFF,	0x2260,	0x1000,
                                    0x15000,	0x15FFF,	0x2280,	0x1000,
                                    0x16000,	0x16FFF,	0x22A0,	0x1000,
                                    0x17000,	0x17FFF,	0x22C0,	0x1000,
                                    0x18000,	0x18FFF,	0x22E0,	0x1000,
                                    0x19000,	0x19FFF,	0x2300,	0x1000,
                                    0x1A000,	0x1AFFF,	0x2320,	0x1000,
                                    0x1B000,	0x1BFFF,	0x2340,	0x1000,
                                    0x1C000,	0x1CFFF,	0x2360,	0x1000,
                                    0x1D000,	0x1DFFF,	0x2380,	0x1000,
                                    0x1E000,	0x1EFFF,	0x23A0,	0x1000,
                                    0x1F000,	0x1FFFF,	0x23C0,	0x1000,
                                    0x20000,	0x20FFF,	0x23E0,	0x1000,
                                    0x21000,	0x21FFF,	0x2400,	0x1000,
                                    0x22000,	0x22FFF,	0x2420,	0x1000,
                                    0x23000,	0x23FFF,	0x2440,	0x1000,
                                    0x24000,	0x24FFF,	0x2460,	0x1000,
                                    0x25000,	0x25FFF,	0x2480,	0x1000,
                                    0x26000,	0x26FFF,	0x24A0,	0x1000,
                                    0x27000,	0x27FFF,	0x24C0,	0x1000,
                                    0x28000,	0x28FFF,	0x24E0,	0x1000,
                                    0x29000,	0x29FFF,	0x2500,	0x1000,
                                    0x2A000,	0x2AFFF,	0x2520,	0x1000,
                                    0x2B000,	0x2BFFF,	0x2540,	0x1000,
                                    0x2C000,	0x2CFFF,	0x2560,	0x1000,
                                    0x2D000,	0x2DFFF,	0x2580,	0x1000,
                                    0x2E000,	0x2EFFF,	0x25A0,	0x1000,
                                    0x2F000,	0x2FFFF,	0x25C0,	0x1000,
                                    0x30000,	0x30FFF,	0x25E0,	0x1000,
                                    0x31000,	0x31FFF,	0x2600,	0x1000,
                                    0x32000,	0x32FFF,	0x2620,	0x1000,
                                    0x33000,	0x33FFF,	0x2640,	0x1000,
                                    0x34000,	0x34FFF,	0x2660,	0x1000,
                                    0x35000,	0x35FFF,	0x2680,	0x1000,
                                    0x36000,	0x36FFF,	0x26A0,	0x1000,
                                    0x37000,	0x37FFF,	0x26C0,	0x1000,
                                    0x38000,	0x38FFF,	0x26E0,	0x1000,
                                    0x39000,	0x39FFF,	0x2700,	0x1000,
                                    0x3A000,	0x3AFFF,	0x2720,	0x1000,
                                    0x3B000,	0x3BFFF,	0x2740,	0x1000,
                                    0x3C000,	0x3CFFF,	0x2760,	0x1000,
                                    0x3D000,	0x3DFFF,	0x2780,	0x1000,
                                    0x3E000,	0x3EFFF,	0x27A0,	0x1000,
                                    0x3F000,	0x3FFFF,	0x27C0,	0x1000,
                                    0x40000,	0x40FFF,	0x27E0,	0x1000,
                                    0x41000,	0x41FFF,	0x2800,	0x1000,
                                    0x42000,	0x42FFF,	0x2820,	0x1000,
                                    0x43000,	0x43FFF,	0x2840,	0x1000,
                                    0x44000,	0x44FFF,	0x2860,	0x1000,
                                    0x45000,	0x45FFF,	0x2880,	0x1000,
                                    0x46000,	0x46FFF,	0x28A0,	0x1000,
                                    0x47000,	0x47FFF,	0x28C0,	0x1000,
                                    0x48000,	0x48FFF,	0x28E0,	0x1000,
                                    0x49000,	0x49FFF,	0x2900,	0x1000,
                                    0x4A000,	0x4AFFF,	0x2920,	0x1000,
                                    0x4B000,	0x4BFFF,	0x2940,	0x1000,
                                    0x4C000,	0x4CFFF,	0x2960,	0x1000,
                                    0x4D000,	0x4DFFF,	0x2980,	0x1000,
                                    0x4E000,	0x4EFFF,	0x29A0,	0x1000,
                                    0x4F000,	0x4FFFF,	0x29C0,	0x1000,
                                    0x50000,	0x50FFF,	0x29E0,	0x1000,
                                    0x51000,	0x51FFF,	0x2A00,	0x1000,
                                    0x52000,	0x52FFF,	0x2A20,	0x1000,
                                    0x53000,	0x53FFF,	0x2A40,	0x1000,
                                    0x54000,	0x54FFF,	0x2A60,	0x1000,
                                    0x55000,	0x55FFF,	0x2A80,	0x1000,
                                    0x56000,	0x56FFF,	0x2AA0,	0x1000,
                                    0x57000,	0x57FFF,	0x2AC0,	0x1000,
                                    0x58000,	0x58FFF,	0x2AE0,	0x1000,
                                    0x59000,	0x59FFF,	0x2B00,	0x1000,
                                    0x5A000,	0x5AFFF,	0x2B20,	0x1000,
                                    0x5B000,	0x5BFFF,	0x2B40,	0x1000,
                                    0x5C000,	0x5CFFF,	0x2B60,	0x1000,
                                    0x5D000,	0x5DFFF,	0x2B80,	0x1000,
                                    0x5E000,	0x5EFFF,	0x2BA0,	0x1000,
                                    0x5F000,	0x5FFFF,	0x2BC0,	0x1000,
                                    0x60000,	0x60FFF,	0x2BE0,	0x1000,
                                    0x61000,	0x61FFF,	0x2C00,	0x1000,
                                    0x62000,	0x62FFF,	0x2C20,	0x1000,
                                    0x63000,	0x63FFF,	0x2C40,	0x1000,
                                    0x64000,	0x64FFF,	0x2C60,	0x1000,
                                    0x65000,	0x65FFF,	0x2C80,	0x1000,
                                    0x66000,	0x66FFF,	0x2CA0,	0x1000,
                                    0x67000,	0x67FFF,	0x2CC0,	0x1000,
                                    0x68000,	0x68FFF,	0x2CE0,	0x1000,
                                    0x69000,	0x69FFF,	0x2D00,	0x1000,
                                    0x6A000,	0x6AFFF,	0x2D20,	0x1000,
                                 };
                #endregion
                if (savindex == 1)
                {
                    for (int i = 0; i < hashtabledata.Length / 4; i++)
                    {
                        hashtabledata[i * 4 + 0] += 0x7F000;
                        hashtabledata[i * 4 + 1] += 0x7F000;
                        hashtabledata[i * 4 + 2] += 0x7F000;
                    }

                    // Problem with save2 saves is that 0x3000-0x4FFF doesn't use save2 data. Probably different when hashed, but different when stored.
                    for (int i = 2; i < 4; i++)
                    {
                        hashtabledata[i * 4 + 0] -= 0x7F000;
                        hashtabledata[i * 4 + 1] -= 0x7F000;
                    }
                }

                SHA256 mySHA256 = SHA256.Create();

                // Hash for 0x3000 onwards
                for (int i = 2; i < hashtabledata.Length / 4; i++)
                {
                    uint start = hashtabledata[0 + 4 * i];
                    uint length = hashtabledata[1 + 4 * i] - hashtabledata[0 + 4 * i];
                    uint offset = hashtabledata[2 + 4 * i];
                    uint blocksize = hashtabledata[3 + 4 * i];

                    byte[] zeroarray = new byte[blocksize];
                    Array.Copy(editedsav, start, zeroarray, 0, length + 1);
                    byte[] hashValue = mySHA256.ComputeHash(zeroarray);
                    Array.Copy(hashValue, 0, editedsav, offset, 0x20);
                }
                // Fix 2nd Hash
                {
                    uint start = hashtabledata[0 + 4 * 1];
                    uint length = hashtabledata[1 + 4 * 1] - hashtabledata[0 + 4 * 1];
                    uint offset = hashtabledata[2 + 4 * 1];
                    uint blocksize = hashtabledata[3 + 4 * 1];

                    byte[] zeroarray = new byte[blocksize];
                    Array.Copy(editedsav, start, zeroarray, 0, length + 1);
                    byte[] hashValue = mySHA256.ComputeHash(zeroarray);

                    Array.Copy(hashValue, 0, editedsav, offset, 0x20);
                }
                // Fix 1st Hash
                {
                    uint start = hashtabledata[0 + 4 * 0];
                    uint length = hashtabledata[1 + 4 * 0] - hashtabledata[0 + 4 * 0];
                    uint offset = hashtabledata[2 + 4 * 0];
                    uint blocksize = hashtabledata[3 + 4 * 0];

                    byte[] zeroarray = new byte[blocksize];
                    Array.Copy(editedsav, start, zeroarray, 0, length + 1);
                    byte[] hashValue = mySHA256.ComputeHash(zeroarray);

                    Array.Copy(hashValue, 0, editedsav, offset, 0x20);
                }
                // Fix IVFC Hash
                {
                    byte[] zeroarray = new byte[0x200];
                    Array.Copy(editedsav, 0x2000 + savindex * 0x7F000, zeroarray, 0, 0x20);
                    byte[] hashValue = mySHA256.ComputeHash(zeroarray);

                    Array.Copy(hashValue, 0, editedsav, 0x43C - savindex * 0x130, 0x20);
                }
                // Fix DISA Hash
                {
                    byte[] difihash = new byte[0x12C];
                    Array.Copy(editedsav, 0x330 - savindex * 0x130, difihash, 0, 0x12C);
                    byte[] hashValue = mySHA256.ComputeHash(difihash);

                    Array.Copy(hashValue, 0, editedsav, 0x16C, 0x20);
                }
            }
            // Write the active save index
            editedsav[0x168] = (byte)(savindex ^ 1);
        export:
            // File Integrity has been restored as well as it can. Export!

            // If CyberGadget
            if (cybergadget)
            {
                byte[] cybersav = new byte[0x65600];
                if (savegame_oras) cybersav = new byte[0x76000];
                Array.Copy(editedsav, 0x5400, cybersav, 0, cybersav.Length);
                if (ramsav == null)
                {
                    // Chunk Error Checking
                    byte[] FFFF = new byte[0x200];
                    byte[] section = new byte[0x200];
                    for (int i = 0; i < 0x200; i++)
                        FFFF[i] = 0xFF;

                    for (int i = 0; i < cybersav.Length / 0x200; i++)
                    {
                        Array.Copy(cybersav, i * 0x200, section, 0, 0x200);
                        if (!section.SequenceEqual(FFFF)) continue;
                        string problem = String.Format("0x200 chunk @ 0x{0} is FF'd.", (i * 0x200).ToString("X5"))
                                         + Environment.NewLine + "Cyber will screw up (as of August 31st)." + Environment.NewLine + Environment.NewLine;

                        // Check to see if it is in the Pokedex
                        if (i * 0x200 > 0x14E00 && i * 0x200 < 0x15700)
                        {
                            problem += "Problem lies in the Pokedex. ";
                            if (i * 0x200 == 0x15400)
                                problem += "Remove a language flag for a species ~ ex " + specieslist[548];
                        }

                        if (Util.Prompt(MessageBoxButtons.YesNo, problem, "Continue saving?") != DialogResult.Yes)
                            return;
                    }
                }
                SaveFileDialog cySAV = new SaveFileDialog();

                // Try for file path
                string cyberpath = Util.GetTempFolder();
                if (ramsav != null && Directory.Exists(path3DS))
                {
                    cySAV.InitialDirectory = path3DS;
                    cySAV.RestoreDirectory = true;
                }
                else if (pathSDF != null && Directory.Exists(pathSDF))
                {
                    cySAV.InitialDirectory = pathSDF;
                    cySAV.RestoreDirectory = true;
                }
                else if (Directory.Exists(Path.Combine(cyberpath, "root")))
                {
                    cySAV.InitialDirectory = Path.Combine(cyberpath, "root");
                    cySAV.RestoreDirectory = true;
                }
                else if (Directory.Exists(cyberpath))
                {
                    cySAV.InitialDirectory = cyberpath;
                    cySAV.RestoreDirectory = true;
                }
                if (ramsavloaded && ModifierKeys == Keys.Shift) // Export RAM SAV to another.
                {
                    Util.Alert("Please specify the target cart/console-RAM save.");
                    OpenFileDialog ofd = new OpenFileDialog();
                    if (ofd.ShowDialog() != DialogResult.OK) return;
                    string target = ofd.FileName;
                    byte[] targetRAM = File.ReadAllBytes(target);
                    byte[] newRAM = ram2sav.getRAM(targetRAM, cybersav);

                    cySAV.Filter = "ramsav|*.bin";
                    cySAV.FileName = "ramsav.bin";
                    DialogResult sdr = cySAV.ShowDialog();
                    if (sdr != DialogResult.OK) return;
                    string path = cySAV.FileName;
                    File.WriteAllBytes(path, newRAM);
                    Util.Alert("Saved RAM SAV to:" + Environment.NewLine + path, "Target RAM:" + Environment.NewLine + target);
                }
                else if (ramsavloaded && ModifierKeys != Keys.Control) // Export RAM SAV if it is the currently loaded one.
                {
                    cySAV.Filter = "ramsav|*.bin";
                    cySAV.FileName = "ramsav.bin";
                    DialogResult sdr = cySAV.ShowDialog();
                    if (sdr != DialogResult.OK) return;
                    string path = cySAV.FileName;
                    File.WriteAllBytes(path, ram2sav.getRAM(ramsav, cybersav));
                    Util.Alert("Saved RAM SAV to:", path);
                }
                else
                {
                    cySAV.Filter = "Cyber SAV|*.*";
                    cySAV.FileName = L_Save.Text.Split(new[] { ": " }, StringSplitOptions.None)[1];
                    DialogResult sdr = cySAV.ShowDialog();
                    if (sdr != DialogResult.OK) return;
                    string path = cySAV.FileName;
                    File.WriteAllBytes(path, cybersav);
                    Util.Alert("Saved Cyber SAV to:", path);
                }
            }
            else
            {
                // Save Full Save File
                SaveFileDialog savesav = new SaveFileDialog
                {
                    Filter = "SAV|*.bin;*.sav",
                    FileName = L_Save.Text.Split(new[] {": "}, StringSplitOptions.None)[1]
                };
                DialogResult result = savesav.ShowDialog();
                if (result != DialogResult.OK) return;
                string path = savesav.FileName;

                if (File.Exists(path))
                {
                    // File already exists, save a .bak
                    byte[] backupfile = File.ReadAllBytes(path);
                    File.WriteAllBytes(path + ".bak", backupfile);
                }
                File.WriteAllBytes(path, editedsav);
                Util.Alert("Saved 1MB SAV to:", path);
            }
        }
        // Box/SAV Functions //
        private void clickBoxRight(object sender, EventArgs e)
        {
            C_BoxSelect.SelectedIndex = (C_BoxSelect.SelectedIndex + 1) % 31;
        }
        private void clickBoxLeft(object sender, EventArgs e)
        {
            C_BoxSelect.SelectedIndex = (C_BoxSelect.SelectedIndex + 30) % 31;
        }
        private void clickSlot(object sender, EventArgs e)
        {
            switch (ModifierKeys)
            {
                case (Keys.Control | Keys.Alt): clickClone(sender, e); break;
                case Keys.Control: clickView(sender, e); break;
                case Keys.Shift: clickSet(sender, e); break;
                case Keys.Alt: clickDelete(sender, e); break;
            }
        }
        private void clickView(object sender, EventArgs e)
        {
            int slot = getSlot(sender);
            int offset = getPKXOffset(slot);

            PictureBox[] pba = {
                                    bpkx1, bpkx2, bpkx3, bpkx4, bpkx5, bpkx6,
                                    bpkx7, bpkx8, bpkx9, bpkx10,bpkx11,bpkx12,
                                    bpkx13,bpkx14,bpkx15,bpkx16,bpkx17,bpkx18,
                                    bpkx19,bpkx20,bpkx21,bpkx22,bpkx23,bpkx24,
                                    bpkx25,bpkx26,bpkx27,bpkx28,bpkx29,bpkx30,

                                    ppkx1, ppkx2, ppkx3, ppkx4, ppkx5, ppkx6,
                                    bbpkx1,bbpkx2,bbpkx3,bbpkx4,bbpkx5,bbpkx6,

                                    dcpkx1, dcpkx2, gtspkx, fusedpkx,subepkx1,subepkx2,subepkx3,
                                };

            PictureBox picturebox = pba[slot];
            if (picturebox.Image == null)
            { System.Media.SystemSounds.Exclamation.Play(); return; }

            // Load the PKX file
            if (BitConverter.ToUInt64(savefile, offset + 8) != 0)
            {
                byte[] ekxdata = new byte[0xE8];
                Array.Copy(savefile, offset, ekxdata, 0, 0xE8);
                byte[] pkxdata = PKX.decryptArray(ekxdata);
                int species = BitConverter.ToInt16(pkxdata, 0x08); // Get Species
                if (species == 0)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                    return;
                }
                try
                {
                    Array.Copy(pkxdata, buff, 0xE8);
                    populateFields(buff);
                }
                catch // If it fails, try XORing encrypted zeroes
                {
                    try
                    {
                        byte[] blank = PKX.encryptArray(new byte[0xE8]);

                        for (int i = 0; i < 0xE8; i++)
                            blank[i] = (byte)(buff[i] ^ blank[i]);

                        populateFields(blank);
                    }
                    catch   // Still fails, just let the original errors occur.
                    { populateFields(buff); }
                }
                // Visual to display what slot is currently loaded.
                getSlotColor(slot, Properties.Resources.slotView);
            }
            else
                System.Media.SystemSounds.Exclamation.Play();
        }
        private void clickSet(object sender, EventArgs e)
        {
            if (!verifiedPKX()) { return; }
            int slot = getSlot(sender);
            if (slot == 30 && (CB_Species.SelectedIndex == 0 || CHK_IsEgg.Checked)) { Util.Alert("Can't have empty/egg first slot."); return; }
            int offset = getPKXOffset(slot);

            byte[] pkxdata = preparepkx();
            byte[] ekxdata = PKX.encryptArray(pkxdata);

            if (!savegame_oras)
            {
                // User Protection
                int move1 = BitConverter.ToInt16(pkxdata, 0x5A);
                int move2 = BitConverter.ToInt16(pkxdata, 0x5C);
                int move3 = BitConverter.ToInt16(pkxdata, 0x5E);
                int move4 = BitConverter.ToInt16(pkxdata, 0x60);
                int ability = pkxdata[0x14];
                int item = BitConverter.ToInt16(pkxdata, 0x0A);
                string err = "";

                if (move1 > 617 || move2 > 617 || move3 > 617 || move4 > 617)
                    err = "Move does not exist in X/Y.";
                else if (ability > 188)
                    err = "Ability does not exist in X/Y.";
                else if (item > 717)
                    err = "Item does not exist in X/Y.";

                if ((err != "") && Util.Prompt(MessageBoxButtons.YesNo, err, "Continue?") != DialogResult.Yes)
                    return;
            }
            if (slot >= 30 && slot < 36) // Party
                Array.Copy(ekxdata, 0, savefile, offset, 0x104);
            else if (slot < 30 || (slot >= 36 && slot < 42 && DEV_Ability.Enabled))
                Array.Copy(ekxdata, 0, savefile, offset, 0xE8);
            else return;

            setParty();
            setPokedex(pkxdata);
            getQuickFiller(getPictureBox(slot), pkxdata);
            savedited = true;

            getSlotColor(slot, Properties.Resources.slotSet);
        }
        private void clickDelete(object sender, EventArgs e)
        {
            int slot = getSlot(sender);
            if (slot == 30 && setParty() == 1 && !DEV_Ability.Enabled) { Util.Alert("Can't delete first slot."); return; }
            int offset = getPKXOffset(slot);

            byte[] pkxdata = new byte[0x104];
            byte[] ekxdata = PKX.encryptArray(pkxdata);

            savedited = true;

            if (slot >= 30 && slot < 36) // Party
            { Array.Copy(ekxdata, 0, savefile, offset, 0x104); setParty(); return; }
            if (slot < 30 || (slot >= 36 && slot < 42 && DEV_Ability.Enabled))
            { Array.Copy(ekxdata, 0, savefile, offset, 0xE8); }
            else return;

            getQuickFiller(getPictureBox(slot), pkxdata);
            getSlotColor(slot, Properties.Resources.slotDel);
        }
        private void clickClone(object sender, EventArgs e)
        {
            if (getSlot(sender) > 30) return; // only perform action if cloning to boxes
            if (!verifiedPKX()) { return; } // don't copy garbage to the box

            byte[] pkxdata;
            int box = C_BoxSelect.SelectedIndex + 1; // get box we're cloning to
            {
                if (Util.Prompt(MessageBoxButtons.YesNo, String.Format("Clone Pokemon from Editing Tabs to all slots in Box {0}?", box)) == DialogResult.Yes)
                {
                    pkxdata = preparepkx();
                    setPokedex(pkxdata);
                }
                else if (Util.Prompt(MessageBoxButtons.YesNo, String.Format("Delete Pokemon from all slots in Box {0}?", box)) == DialogResult.Yes)
                    pkxdata = new Byte[0xE8];
                else
                    return; // abort clone/delete
            }

            byte[] ekxdata = PKX.encryptArray(pkxdata);
            for (int i = 0; i < 30; i++) // write encrypted array to all box slots
                Array.Copy(ekxdata, 0, savefile, getPKXOffset(i), 0xE8);

            for (int i = 0; i < 30; i++)
                getQuickFiller(getPictureBox(i), pkxdata);

            savedited = true;
        }
        // Generic Subfunctions // 
        private void setPokedex(byte[] pkxdata)
        {
            if (savindex > 1) return;
            int species = BitConverter.ToUInt16(pkxdata, 0x8);  // Species
            int lang = pkxdata[0xE3] - 1; if (lang > 5) lang--; // 0-6 language vals
            int origin = pkxdata[0xDF];                         // Native / Non Native
            int gender = (pkxdata[0x1D] & 2) >> 1;              // Gender
            uint pid = BitConverter.ToUInt32(pkxdata, 0x18);
            ushort TID = BitConverter.ToUInt16(pkxdata, 0xC);
            ushort SID = BitConverter.ToUInt16(pkxdata, 0xE);
            int shiny = (PKX.getPSV(pid) ^ PKX.getTSV(TID, SID)) >> 4 == 0 ? 1 : 0;
            int dexoff = savindex * 0x7F000 + SaveGame.PokeDex; // Same offset for XY-ORAS
            int langoff = 0x3C8; if (savegame_oras) langoff = 0x400; // Not the same offset for language bools
            int shiftoff = (shiny * 0x60 * 2) + (gender * 0x60) + 0x60;

            // Set the [Species/Gender/Shiny] Owned Flag
            savefile[dexoff + shiftoff + (species - 1) / 8 + 0x8] |= (byte)(1 << ((species - 1) % 8));

            // Owned quality flag
            if (origin < 0x18 && species < 650 && !savegame_oras) // Pre 650 for X/Y, and not for ORAS; Set the Foreign Owned Flag
                savefile[0x1AA4C + 0x7F000 * savindex + (species - 1) / 8] |= (byte)(1 << ((species - 1) % 8));
            else if (origin >= 0x18 || savegame_oras) // Set Native Owned Flag (should always happen)
                savefile[dexoff + (species - 1) / 8 + 0x8] |= (byte)(1 << ((species - 1) % 8));

            // Set the Language
            if (lang < 0) lang = 1;
            savefile[dexoff + langoff + ((species - 1) * 7 + lang) / 8] |= (byte)(1 << ((((species - 1) * 7) + lang) % 8));
        }
        private byte setParty()
        {
            byte partymembers = 0; // start off with a ctr of 0
            int offset = SaveGame.Party + 0x7F000 * savindex;
            for (int i = 0; i < 6; i++)
            {
                // Gather all the species
                byte[] data = new byte[0x104];
                Array.Copy(savefile, offset + i * 0x104, data, 0, 0x104);
                byte[] decdata = PKX.decryptArray(data);
                int species = BitConverter.ToInt16(decdata, 8);
                if ((species != 0) && (species < 722))
                    Array.Copy(data, 0, savefile, offset + (partymembers++) * 0x104, 0x104);
            }

            // Write in the current party count
            savefile[offset + 6 * 0x104 + savindex * 0x7F000] = partymembers;
            // Zero out the party slots that are empty.
            for (int i = 0; i < 6; i++)
                if (i >= partymembers)
                    Array.Copy(PKX.encryptArray(new byte[0x104]), 0, savefile, offset + (i * 0x104), 0x104);

            // Repeat for Battle Box.
            byte battlemem = 0;
            int offset2 = SaveGame.BattleBox + 0x7F000 * savindex;
            for (int i = 0; i < 6; i++)
            {
                // Gather all the species
                byte[] data = new byte[0x104];
                Array.Copy(savefile, offset2 + i * 0xE8, data, 0, 0xE8);
                byte[] decdata = PKX.decryptArray(data);
                int species = BitConverter.ToInt16(decdata, 8);
                if ((species != 0) && (species < 722))
                    Array.Copy(data, 0, savefile, offset2 + (battlemem++) * 0xE8, 0xE8);
            }

            // Zero out the party slots that are empty.
            for (int i = 0; i < 6; i++)
                if (i >= battlemem)
                    Array.Copy(PKX.encryptArray(new byte[0x104]), 0, savefile, offset2 + (i * 0xE8), 0xE8);

            if (battlemem == 0)
                savefile[offset2 + 6 * 0xE8 + savindex * 0x7F000] = 0;

            // Refresh slots
            for (int i = 0; i < 6; i++)
            {
                getQuickFiller(getPictureBox(i + 30), PKX.decryptArray(savefile.Skip(SaveGame.Party + 0x7F000 * savindex + 260 * i).Take(232).ToArray()));
                getQuickFiller(getPictureBox(i + 36), PKX.decryptArray(savefile.Skip(SaveGame.BattleBox + 0x7F000 * savindex + 232 * i).Take(232).ToArray()));
            }

            return partymembers;
        }
        private int getPKXOffset(int slot)
        {
            int offset = SaveGame.Box + C_BoxSelect.SelectedIndex * (0xE8 * 30) + slot * 0xE8;

            if (slot > 29)          // Not a party
            {
                if (slot < 36)      // Party Slot
                    offset = SaveGame.Party + (slot - 30) * 0x104;
                else if (slot < 42) // Battle Box Slot
                    offset = SaveGame.BattleBox + (slot - 36) * 0xE8;
                else if (slot < 44) // Daycare
                    offset = SaveGame.Daycare + 8 + (slot - 42) * 0xF0;
                else if (slot < 45) // GTS
                    offset = SaveGame.GTS;
                else if (slot < 46) // Fused
                    offset = SaveGame.Fused;
                else                // SUBE
                    offset = SaveGame.SUBE + (slot - 46) * 0xEC;
            }
            offset += 0x7F000 * savindex;
            return offset;
        }
        private int getSlot(object sender)
        {
            string name = (sender is ToolStripItem) 
                ?
                ((sender as ToolStripItem).Owner as ContextMenuStrip).SourceControl.Name
                :
                (sender as PictureBox).Name;

            string[] pba = {
                                "bpkx1", "bpkx2", "bpkx3", "bpkx4", "bpkx5", "bpkx6",
                                "bpkx7", "bpkx8", "bpkx9", "bpkx10","bpkx11","bpkx12",
                                "bpkx13","bpkx14","bpkx15","bpkx16","bpkx17","bpkx18",
                                "bpkx19","bpkx20","bpkx21","bpkx22","bpkx23","bpkx24",
                                "bpkx25","bpkx26","bpkx27","bpkx28","bpkx29","bpkx30",

                                "ppkx1", "ppkx2", "ppkx3", "ppkx4", "ppkx5", "ppkx6",
                                "bbpkx1","bbpkx2","bbpkx3","bbpkx4","bbpkx5","bbpkx6",

                                "dcpkx1", "dcpkx2", "gtspkx", "fusedpkx","subepkx1","subepkx2","subepkx3",
                            };
            int slot = Array.IndexOf(pba, name);
            return slot;
        }
        private void setPKXBoxes()
        {
            int boxoffset = SaveGame.Box + 0x7F000 * savindex + C_BoxSelect.SelectedIndex * (0xE8 * 30);

            int boxbgofst = (0x7F000 * savindex) + 0x9C1E + C_BoxSelect.SelectedIndex;
            int boxbgval = 1 + savefile[boxbgofst];
            string imagename = "box_wp" + boxbgval.ToString("00"); if (savegame_oras && boxbgval > 16) imagename += "o";
            PAN_Box.BackgroundImage = (Image)Properties.Resources.ResourceManager.GetObject(imagename);

            PictureBox[] pba = {
                                    bpkx1, bpkx2, bpkx3, bpkx4, bpkx5, bpkx6,
                                    bpkx7, bpkx8, bpkx9, bpkx10,bpkx11,bpkx12,
                                    bpkx13,bpkx14,bpkx15,bpkx16,bpkx17,bpkx18,
                                    bpkx19,bpkx20,bpkx21,bpkx22,bpkx23,bpkx24,
                                    bpkx25,bpkx26,bpkx27,bpkx28,bpkx29,bpkx30,

                                    ppkx1, ppkx2, ppkx3, ppkx4, ppkx5, ppkx6,
                                    bbpkx1,bbpkx2,bbpkx3,bbpkx4,bbpkx5,bbpkx6,

                                    dcpkx1, dcpkx2, gtspkx, fusedpkx,subepkx1,subepkx2,subepkx3,
                                };
            for (int i = 0; i < 30; i++)
                getSlotFiller(boxoffset + 0xE8 * i, pba[i]);

            // Reload Party
            for (int i = 0; i < 6; i++)
                getSlotFiller(SaveGame.Party + (0x7F000 * savindex) + 0x104 * i, pba[i + 30]);

            // Reload Battle Box
            for (int i = 0; i < 6; i++)
                getSlotFiller(SaveGame.BattleBox + (0x7F000 * savindex) + 0xE8 * i, pba[i + 36]);

            // Reload Daycare
            Label[] dclabela = { L_DC1, L_DC2, };
            TextBox[] dctexta = { TB_Daycare1XP, TB_Daycare2XP };

            for (int i = 0; i < 2; i++)
            {
                getSlotFiller(SaveGame.Daycare + (0x7F000 * savindex) + 0xE8 * i + 8 * (i + 1), pba[i + 42]);
                dctexta[i].Text = BitConverter.ToUInt32(savefile, SaveGame.Daycare + (0x7F000 * savindex) + 0xF0 * i + 4).ToString();
                if (Convert.ToBoolean(savefile[SaveGame.Daycare + (0x7F000 * savindex) + 0xF0 * i]))   // If Occupied
                    dclabela[i].Text = (i + 1) + ": ✓";
                else
                {
                    dclabela[i].Text = (i + 1) + ": ✘";
                    pba[i + 42].Image = Util.ChangeOpacity(pba[i + 42].Image, 0.6);
                }
            }
            DayCare_HasEgg.Checked = Convert.ToBoolean(savefile[SaveGame.Daycare + (0x7F000 * savindex) + 0x1E0]);
            TB_RNGSeed.Text = BitConverter.ToUInt64(savefile, SaveGame.Daycare + (0x7F000 * savindex) + 0x1E8).ToString("X16");

            // GTS
            getSlotFiller(SaveGame.GTS + (0x7F000 * savindex), pba[44]);

            // Fused
            getSlotFiller(SaveGame.Fused + (0x7F000 * savindex), pba[45]);

            // SUBE
            for (int i = 0; i < 3; i++)
            {
                int offset = 0x22C90 + i * 0xEC + (0x7F000 * savindex);
                if (BitConverter.ToUInt64(savefile, offset) != 0)
                    getSlotFiller(offset, pba[46 + i]);
                else pba[46 + i].Image = null;
            }

            // Recoloring of a storage box slot (to not show for other storage boxes)
            if (colorizedslot < 32)
                pba[colorizedslot].BackgroundImage = (colorizedbox == C_BoxSelect.SelectedIndex) ? colorizedcolor : null;
        }
        private void setBoxNames()
        {
            int selectedbox = C_BoxSelect.SelectedIndex; // precache selected box
            // Build ComboBox Dropdown Items
            try
            {
                C_BoxSelect.Items.Clear();
                for (int i = 0; i < 31; i++)
                    C_BoxSelect.Items.Add(Encoding.Unicode.GetString(savefile, SaveGame.PCLayout + (0x7F000 * savindex) + 0x22 * i, 0x22));
            }
            catch
            {
                C_BoxSelect.Items.Clear();
                for (int i = 1; i < 32; i++)
                    C_BoxSelect.Items.Add("Box " + i);
            }
            C_BoxSelect.SelectedIndex = selectedbox;    // restore selected box
        }
        private void setSAVLabel()
        {
            L_SAVINDEX.Text = (savindex + 1).ToString();
            RTB_S.AppendText("Loaded Save File " + (savindex + 1) + Environment.NewLine);
        }
        private void getSAVOffsets(ref bool oras)
        {
            // Get the save file offsets for the input game
            bool enableInterface = false;
            if (BitConverter.ToUInt32(savefile, 0x6A810 + 0x7F000 * savindex) == 0x42454546)
            {
                enableInterface = true;
                SaveGame = new PKX.Structures.SaveGame("XY");
            }
            else if (BitConverter.ToUInt32(savefile, 0x7B210 + 0x7F000 * savindex) == 0x42454546)
            {
                enableInterface = true;
                SaveGame = new PKX.Structures.SaveGame("ORAS");
                oras = true;
            }
            else
            {
                Util.Error("Unrecognized Save File loaded.");
                SaveGame = new PKX.Structures.SaveGame("Error");
            }

            // Enable Buttons
            GB_SAVtools.Enabled = B_JPEG.Enabled = B_VerifyCHK.Enabled = B_VerifySHA.Enabled = B_SwitchSAV.Enabled
                = enableInterface;
        }
        private PictureBox getPictureBox(int slot)
        {
            PictureBox[] pba = {
                                    bpkx1, bpkx2, bpkx3, bpkx4, bpkx5, bpkx6,
                                    bpkx7, bpkx8, bpkx9, bpkx10,bpkx11,bpkx12,
                                    bpkx13,bpkx14,bpkx15,bpkx16,bpkx17,bpkx18,
                                    bpkx19,bpkx20,bpkx21,bpkx22,bpkx23,bpkx24,
                                    bpkx25,bpkx26,bpkx27,bpkx28,bpkx29,bpkx30,

                                    ppkx1, ppkx2, ppkx3, ppkx4, ppkx5, ppkx6,
                                    bbpkx1,bbpkx2,bbpkx3,bbpkx4,bbpkx5,bbpkx6,

                                    dcpkx1, dcpkx2, gtspkx, fusedpkx,subepkx1,subepkx2,subepkx3,
                                };
            return pba[slot];
        }
        private void getQuickFiller(PictureBox pb, byte[] dslotdata = null)
        {
            if (!init) return;
            dslotdata = dslotdata ?? preparepkx(false); // don't perform control loss click

            int species = BitConverter.ToInt16(dslotdata, 0x08); // Get Species
            uint isegg = (BitConverter.ToUInt32(dslotdata, 0x74) >> 30) & 1;

            int altforms = (dslotdata[0x1D] >> 3);
            int gender = (dslotdata[0x1D] >> 1) & 0x3;

            string file;

            if (pb == dragout) L_QR.Visible = (species != 0);
            if (species == 0)
            { pb.Image = (Image)Properties.Resources.ResourceManager.GetObject("_0"); return; }
            {
                file = "_" + species;
                if (altforms > 0) // Alt Form Handling
                    file = file + "_" + altforms;
                else if (gender == 1 && (species == 592 || species == 593)) // Frillish & Jellicent
                    file = file + "_" + gender;
                else if (gender == 1 && (species == 521 || species == 668)) // Unfezant & Pyroar
                    file = "_" + species + "f";
            }

            // Redrawing logic
            Image baseImage = (Image)Properties.Resources.ResourceManager.GetObject(file);
            if (baseImage == null)
            {
                if (species < 722)
                {
                    baseImage = Util.LayerImage(
                        (Image)Properties.Resources.ResourceManager.GetObject("_" + species),
                        Properties.Resources.unknown,
                        0, 0, .5);
                }
                else
                    baseImage = Properties.Resources.unknown;
            }
            if (isegg == 1)
            {
                // Start with a partially transparent species by layering the species with partial opacity onto a blank image.
                baseImage = Util.LayerImage((Image)Properties.Resources.ResourceManager.GetObject("_0"), baseImage, 0, 0, 0.33);
                // Add the egg layer over-top with full opacity.
                baseImage = Util.LayerImage(baseImage, (Image)Properties.Resources.ResourceManager.GetObject("egg"), 0, 0, 1);
            }
            if (PKX.getIsShiny(BitConverter.ToUInt32(dslotdata, 0x18), BitConverter.ToUInt16(dslotdata, 0x0C), BitConverter.ToUInt16(dslotdata, 0x0E)))
            {   // Is Shiny
                // Redraw our image
                baseImage = Util.LayerImage(baseImage, Properties.Resources.rare_icon, 0, 0, 0.7);
            }
            if (BitConverter.ToUInt16(dslotdata, 0xA) > 0)
            {
                // Has Item
                int item = BitConverter.ToUInt16(dslotdata, 0xA);
                Image itemimg = (Image)Properties.Resources.ResourceManager.GetObject("item_" + item) ?? Properties.Resources.helditem;
                // Redraw
                baseImage = Util.LayerImage(baseImage, itemimg, 22 + (15 - itemimg.Width) / 2, 15 + (15 - itemimg.Height), 1);
            }

            pb.Image = baseImage;
        }
        private void getSlotFiller(int offset, PictureBox pb)
        {
            byte[] slotdata = new byte[0xE8];
            Array.Copy(savefile, offset, slotdata, 0, 0xE8);    // Fill Our EKX Slot
            byte[] dslotdata = PKX.decryptArray(slotdata);

            ushort chk = 0;
            for (int i = 8; i < 232; i += 2) // Loop through the entire PKX
                chk += BitConverter.ToUInt16(dslotdata, i);

            if (chk != BitConverter.ToUInt16(dslotdata, 6) && (!savLoaded && !slotdata.SequenceEqual(new byte[0xE8]))) // Bad Egg
            {
                pb.Image = null;
                pb.BackColor = Color.Red;
                return;
            }
            pb.BackColor = Color.Transparent;
            int species = BitConverter.ToInt16(dslotdata, 0x08); // Get Species
            uint isegg = (BitConverter.ToUInt32(dslotdata, 0x74) >> 30) & 1;

            int altforms = (dslotdata[0x1D] >> 3);
            int gender = (dslotdata[0x1D] >> 1) & 0x3;

            if (species == 0)
            { pb.Image = (Image)Properties.Resources.ResourceManager.GetObject("_0"); return; }

            string file = "_" + species;
            if (altforms > 0) // Alt Form Handling
                file = file + "_" + altforms;
            else if (gender == 1 && (species == 592 || species == 593)) // Frillish & Jellicent
                file = file + "_" + gender;
            else if (gender == 1 && (species == 521 || species == 668)) // Unfezant & Pyroar
                file = "_" + species + "f";

            // Redrawing logic
            Image baseImage = (Image)Properties.Resources.ResourceManager.GetObject(file);
            if (baseImage == null)
            {
                if (species < 722)
                {
                    baseImage = Util.LayerImage(
                        (Image)Properties.Resources.ResourceManager.GetObject("_" + species),
                        Properties.Resources.unknown,
                        0, 0, .5);
                }
                else
                    baseImage = Properties.Resources.unknown;
            }
            if (isegg == 1)
            {
                // Start with a partially transparent species by layering the species with partial opacity onto a blank image.
                baseImage = Util.LayerImage((Image)Properties.Resources.ResourceManager.GetObject("_0"), baseImage, 0, 0, 0.33);
                // Add the egg layer over-top with full opacity.
                baseImage = Util.LayerImage(baseImage, (Image)Properties.Resources.ResourceManager.GetObject("egg"), 0, 0, 1);
            }
            if (PKX.getIsShiny(BitConverter.ToUInt32(dslotdata, 0x18), BitConverter.ToUInt16(dslotdata, 0x0C), BitConverter.ToUInt16(dslotdata, 0x0E)))
            {   // Is Shiny
                // Redraw our image
                baseImage = Util.LayerImage(baseImage, Properties.Resources.rare_icon, 0, 0, 0.7);
            }
            if (BitConverter.ToUInt16(dslotdata, 0xA) > 0)
            {
                // Has Item
                int item = BitConverter.ToUInt16(dslotdata, 0xA);
                Image itemimg = (Image)Properties.Resources.ResourceManager.GetObject("item_" + item) ?? Properties.Resources.helditem;
                // Redraw
                baseImage = Util.LayerImage(baseImage, itemimg, 22 + (15 - itemimg.Width) / 2, 15 + (15 - itemimg.Height), 1);
            }

            pb.Image = baseImage;
        }
        private void getSlotColor(int slot, Image color)
        {
            PictureBox[] pba = {
                                    bpkx1, bpkx2, bpkx3, bpkx4, bpkx5, bpkx6,
                                    bpkx7, bpkx8, bpkx9, bpkx10,bpkx11,bpkx12,
                                    bpkx13,bpkx14,bpkx15,bpkx16,bpkx17,bpkx18,
                                    bpkx19,bpkx20,bpkx21,bpkx22,bpkx23,bpkx24,
                                    bpkx25,bpkx26,bpkx27,bpkx28,bpkx29,bpkx30,

                                    ppkx1, ppkx2, ppkx3, ppkx4, ppkx5, ppkx6,
                                    bbpkx1,bbpkx2,bbpkx3,bbpkx4,bbpkx5,bbpkx6,

                                    dcpkx1, dcpkx2, gtspkx, fusedpkx,subepkx1,subepkx2,subepkx3,
                                };

            foreach (PictureBox t in pba)
                t.BackgroundImage = null;

            if (slot < 32)
                colorizedbox = C_BoxSelect.SelectedIndex;

            pba[slot].BackgroundImage = color;
            colorizedcolor = color;
            colorizedslot = slot;
        }
        private void getBox(object sender, EventArgs e)
        {
            setPKXBoxes();
        }
        private void getTSV(object sender, EventArgs e)
        {
            uint tsv = PKX.getTSV(Util.ToUInt32(TB_TID.Text), Util.ToUInt32(TB_SID.Text));
            Tip1.SetToolTip(TB_TID, "TSV: " + tsv.ToString("0000"));
            Tip2.SetToolTip(TB_SID, "TSV: " + tsv.ToString("0000"));

            uint psv = PKX.getPSV(Util.getHEXval(TB_PID));
            Tip3.SetToolTip(TB_PID, "PSV: " + psv.ToString("0000"));
        }

        private void getNatureModification(object sender, EventArgs e)
        {
            if (sender is ComboBox && (sender as ComboBox).Name != CB_Nature.Name) return;
            int nature = Util.getIndex(CB_Nature);
            int incr = nature / 5;
            int decr = nature % 5;

            Label[] labarray = { Label_ATK, Label_DEF, Label_SPE, Label_SPA, Label_SPD };
            // Reset Label Colors
            foreach (Label label in labarray)
                label.ForeColor = defaultControlText;

            // Set Colored StatLabels only if Nature isn't Neutral
            NatureTip.SetToolTip(CB_Nature,
                incr != decr
                    ? String.Format("+{0} / -{1}", labarray[incr].Text, labarray[decr].Text).Replace(":", "")
                    : "-/-");
        }
        private void switchDaycare(object sender, EventArgs e)
        {
            if (!savegame_oras) return;
            if (DialogResult.Yes == Util.Prompt(MessageBoxButtons.YesNo, "Would you like to switch the view to the other Daycare?", String.Format("Currently viewing daycare {0}.", SaveGame.Daycare / 0x211F0 + 1)))
                // If ORAS, alter the daycare offset via toggle.
                SaveGame.Daycare = (SaveGame.Daycare == 0x21000) ? 0x211F0 : 0x21000;

            // Refresh Boxes
            setPKXBoxes();
        }
        private void mainMenuBoxDumpLoad(object sender, EventArgs e)
        {
            DialogResult dr = Util.Prompt(MessageBoxButtons.YesNoCancel, "Press Yes to Import All from Folder." + Environment.NewLine + "Press No to Dump All to Folder.", "Press Cancel to Abort.");
            if (dr == DialogResult.Cancel) return;
            string exepath = Application.StartupPath;
            string path = "";
            {
                int offset = SaveGame.Box;
                const int size = 232;
                if (dr == DialogResult.Yes) // Import
                {
                    if (Directory.Exists(Path.Combine(exepath, "db")))
                    {
                        DialogResult ld = Util.Prompt(MessageBoxButtons.YesNo, "Load from PKHeX's database?");
                        if (ld == DialogResult.Yes)
                            path = Path.Combine(exepath, "db");
                        else if (ld == DialogResult.No)
                        {
                            // open folder dialog
                            FolderBrowserDialog fbd = new FolderBrowserDialog();
                            if (fbd.ShowDialog() == DialogResult.OK)
                                path = fbd.SelectedPath;
                        }
                        else return;
                    }
                    else
                    {
                        // open folder dialog
                        FolderBrowserDialog fbd = new FolderBrowserDialog();
                        if (fbd.ShowDialog() == DialogResult.OK)
                            path = fbd.SelectedPath;
                    }
                    loadBoxesFromDB(path);
                }
                else if (dr == DialogResult.No)
                {
                    // Dump all of box content to files.
                    {
                        DialogResult ld = Util.Prompt(MessageBoxButtons.YesNo, "Save to PKHeX's database?");
                        if (ld == DialogResult.Yes)
                        {
                            path = Path.Combine(exepath, "db");
                            if (!Directory.Exists(path))
                                Directory.CreateDirectory(path);
                        }
                        else if (ld == DialogResult.No)
                        {
                            // open folder dialog
                            FolderBrowserDialog fbd = new FolderBrowserDialog();
                            if (fbd.ShowDialog() == DialogResult.OK)
                                path = fbd.SelectedPath;
                        }
                        else return;
                    }
                    for (int i = 0; i < 31 * 30 * size; i += size)
                    {
                        byte[] ekxdata = new byte[size];
                        Array.Copy(savefile, offset + i, ekxdata, 0, size);
                        byte[] pkxdata = PKX.decryptArray(ekxdata);


                        int species = BitConverter.ToInt16(pkxdata, 0x08);
                        if (species == 0) continue;
                        uint chk = BitConverter.ToUInt16(pkxdata, 0x06);
                        uint EC = BitConverter.ToUInt32(pkxdata, 0);
                        uint IV32 = BitConverter.ToUInt32(pkxdata, 0x74);

                        string nick;
                        if (Convert.ToBoolean((IV32 >> 31) & 1))
                            nick = Util.TrimFromZero(Encoding.Unicode.GetString(pkxdata, 0x40, 24)) + " (" + specieslist[species] + ")";
                        else
                            nick = specieslist[species];
                        if (Convert.ToBoolean((IV32 >> 30) & 1))
                            nick += " (" + eggname + ")";

                        string isshiny = "";

                        // Is Shiny
                        if (PKX.getIsShiny(BitConverter.ToUInt32(pkxdata, 0x18), Util.ToUInt32(TB_TID.Text), Util.ToUInt32(TB_SID.Text)))
                            isshiny = " ★";

                        string savedname =
                            species.ToString("000") + isshiny + " - "
                            + nick + " - "
                            + chk.ToString("X4") + EC.ToString("X8")
                            + ".pk6";
                        Array.Resize(ref pkxdata, 232);
                        if (!File.Exists(Path.Combine(path, savedname)))
                            File.WriteAllBytes(Path.Combine(path, Util.CleanFileName(savedname)), pkxdata);
                    }
                }
            }
        }
        private void loadBoxesFromDB(string path)
        {
            if (path == "") return;
            int offset = SaveGame.Box;
            int ctr = C_BoxSelect.SelectedIndex * 30;
            int pastctr = 0;

            // Clear out the box data array.
            // Array.Clear(savefile, offset, size * 30 * 31);
            DialogResult dr = Util.Prompt(MessageBoxButtons.YesNoCancel, "Clear subsequent boxes when importing data?", "If you only want to overwrite for new data, press no.");
            if (dr == DialogResult.Cancel) return;
            if (dr == DialogResult.Yes)
            {
                byte[] ezeros = PKX.encryptArray(new byte[232]);
                for (int i = ctr; i < 30 * 31; i++)
                    Array.Copy(ezeros, 0, savefile, offset + i * 232, 232);
            }
            string[] filepaths = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            var Converter = new pk2pk();

            foreach (string t in filepaths)
            {
                long len = new FileInfo(t).Length;
                if (len > 260)
                    continue;
                if (
                    len != 232 && len != 260 // 6th Gen
                    && len != 136 && len != 220 && len != 236 // 5th Gen
                    && len != 100 && len != 80) // 4th Gen
                    continue;
                byte[] data = new byte[232];
                string ext = Path.GetExtension(t);
                switch (ext)
                {
                    case ".pk5":
                    case ".pk4":
                    case ".pk3":
                    case ".3gpkm":
                    case ".pkm":
                    {
                        // Verify PKM (decrypted)
                        byte[] input = File.ReadAllBytes(t);
                        if (!PKX.verifychk(input))
                            continue;
                        {
                            try // to convert g5pkm
                            { data = PKX.encryptArray(Converter.ConvertPKM(input, savefile, savindex)); pastctr++; }
                            catch
                            { continue; }
                        }
                    }
                        break;
                    case ".pk6":
                    case ".pkx":
                    {
                        byte[] input = File.ReadAllBytes(t);
                        if ((BitConverter.ToUInt16(input, 0xC8) == 0) && (BitConverter.ToUInt16(input, 0x58) == 0))
                        {
                            if (BitConverter.ToUInt16(input, 0x8) == 0) // if species = 0
                                continue;
                            Array.Resize(ref input, 232);

                            ushort chk = 0;
                            for (int z = 8; z < 232; z += 2) // Loop through the entire PKX
                                chk += BitConverter.ToUInt16(input, z);
                            if (chk != BitConverter.ToUInt16(input, 0x6)) continue;
                            data = PKX.encryptArray(input);
                        }
                    }
                        break;
                    case ".ek6":
                    case ".ekx":
                    {
                        byte[] input = File.ReadAllBytes(t);
                        Array.Resize(ref input, 232);
                        Array.Copy(input, data, 232);
                        // check if it is good data
                        byte[] decrypteddata = PKX.decryptArray(input);

                        if (BitConverter.ToUInt16(decrypteddata, 0xC8) != 0 && BitConverter.ToUInt16(decrypteddata, 0x58) != 0)
                            continue; // don't allow improperly encrypted files. they must be encrypted properly.
                        //else if (BitConverter.ToUInt16(decrypteddata, 0x8) == 0) // if species = 0
                        //    continue;

                        ushort chk = 0;
                        for (int z = 8; z < 232; z += 2) // Loop through the entire PKX
                            chk += BitConverter.ToUInt16(decrypteddata, z);
                        if (chk != BitConverter.ToUInt16(decrypteddata, 0x6)) continue;
                    }
                        break;
                    default:
                        continue;
                }
                Array.Copy(data, 0, savefile, offset + ctr * 232, 232);
                setPokedex(PKX.decryptArray(data)); // Set the Pokedex data
                ctr++;
                if (ctr == 30 * 31) break; // break out if we have written all 31 boxes
            }
            if (ctr <= 0) return; 
            // if we've written at least one pk6 in, go ahead and make sure the window is stretched.
            if (Width < Height) // expand if boxes aren't visible
            {
                Width = largeWidth;
                tabBoxMulti.SelectedIndex = 0;
            }
            setPKXBoxes();
            string result = String.Format("Loaded {0} files to boxes.", ctr);
            if (pastctr > 0)
                Util.Alert(result, String.Format("Conversion successful for {0} past generation files.", pastctr));
            else
                Util.Alert(result);
        }
        private void B_SaveBoxBin_Click(object sender, EventArgs e)
        {
            DialogResult dr = Util.Prompt(MessageBoxButtons.YesNoCancel, "Yes: Export All Boxes" + Environment.NewLine + String.Format("No: Export {1} (Box {0})", C_BoxSelect.SelectedIndex + 1, C_BoxSelect.Text) + Environment.NewLine + "Cancel: Abort");

            if (dr == DialogResult.Yes)
            {
                SaveFileDialog sfd = new SaveFileDialog {Filter = "Box Data|*.bin", FileName = "pcdata.bin"};
                if (sfd.ShowDialog() == DialogResult.OK) 
                    File.WriteAllBytes(sfd.FileName, savefile.Skip(SaveGame.Box).Take(0xE8 * 30 * 31).ToArray());
            }
            else if (dr == DialogResult.No)
            {
                SaveFileDialog sfd = new SaveFileDialog {Filter = "Box Data|*.bin", FileName = "boxdata.bin"};
                if (sfd.ShowDialog() == DialogResult.OK) 
                    File.WriteAllBytes(sfd.FileName, savefile.Skip(SaveGame.Box + 0xE8 * 30 * C_BoxSelect.SelectedIndex).Take(0xE8 * 30).ToArray());
            }
        }
        // Subfunction Save Buttons //
        private void B_OpenWondercards_Click(object sender, EventArgs e)
        {
            // Open Wondercard Menu
            new SAV_Wondercard(this).ShowDialog();
        }
        private void B_OpenBoxLayout_Click(object sender, EventArgs e)
        {
            // Open Box Layout Menu
            new SAV_BoxLayout(this).ShowDialog();
            setBoxNames(); // fix box names
            setPKXBoxes(); // refresh box background
        }
        private void B_OpenTrainerInfo_Click(object sender, EventArgs e)
        {
            new SAV_Trainer(this).ShowDialog();
        }
        private void B_OpenPokepuffs_Click(object sender, EventArgs e)
        {
            new SAV_Pokepuff(this).ShowDialog();
        }
        private void B_OpenItemPouch_Click(object sender, EventArgs e)
        {
            new SAV_Inventory(this).ShowDialog();
        }
        private void B_OpenBerryField_Click(object sender, EventArgs e)
        {
            if (savegame_oras)
            {
                DialogResult dr = Util.Prompt(MessageBoxButtons.YesNo, "No editing support for ORAS :(", "Repopulate all with random berries?");
                if (dr != DialogResult.Yes) return; // abort
                // Randomize the trees.
                int offset = 0x1C400 + 0x5400 + savindex * 0x7F000;
                byte[] ready = { 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x80, 0x40, 0x01, 0x00, 0x00, 0x00, };
                int[] berrylist =
                {
                    0,149,150,151,152,153,154,155,156,157,158,159,160,161,162,
                    163,164,165,166,167,168,169,170,171,172,173,174,175,176,177,
                    178,179,180,181,182,183,184,185,186,187,188,189,190,191,192,
                    193,194,195,196,197,198,199,200,201,202,203,204,205,206,207,
                    208,209,210,211,212,686,687,688,
                };
                for (int i = 0; i < 90; i++)
                {
                    Array.Copy(ready, 0, savefile, offset + 0x10 * i, 0x10); // prep the berry template tree (which we replace offset 0x6 for the Tree Item)
                    int randberry = (int)(Util.rnd32() % berrylist.Length); // generate a random berry that will go into the tree
                    int index = berrylist[randberry]; // get berry item ID from list
                    Array.Copy(BitConverter.GetBytes(index), 0, savefile, offset + 0x10 * i + 6, 2); // put berry into tree.
                }
            }
            else
                new SAV_BerryField(this, SaveGame.BerryField).ShowDialog();
        }
        private void B_OpenEventFlags_Click(object sender, EventArgs e)
        {
            // Open Flag Menu
            if (savegame_oras)
                new SAV_EventFlagsORAS(this).ShowDialog();
            else
                new SAV_EventFlagsXY(this).ShowDialog();
        }
        private void B_OpenSuperTraining_Click(object sender, EventArgs e)
        {
            // Open ST Menu
            new SAV_SuperTrain(this).ShowDialog();
        }
        private void B_OpenOPowers_Click(object sender, EventArgs e)
        {
            // Open O-Power Menu
            if (savegame_oras)
            {
                DialogResult dr = Util.Prompt(MessageBoxButtons.YesNo, "No editing support for ORAS :(", "Max O-Powers with a working code?");
                if (dr != DialogResult.Yes) return;
                byte[] maxoras =
                { 
                    0x00, 0x01, 0x01, 0x01,
                    0x01, 0x00, 0x01, 0x01,
                    0x01, 0x01, 0x00, 0x01,
                    0x01, 0x01, 0x01, 0x00,
                    0x01, 0x01, 0x01, 0x01,
                    0x00, 0x01, 0x01, 0x01,
                    0x01, 0x00, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x00, 0x01,
                    0x01, 0x01, 0x01, 0x00,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x00, 0x00, 0x00, 
                };
                Array.Copy(maxoras, 0, savefile, 0x17400 + 0x5400 + 0x7F000 * savindex, 0x44);
            }
            else
                new SAV_OPower(this).ShowDialog();
        }
        private void B_OpenPokedex_Click(object sender, EventArgs e)
        {
            // Open Pokedex Menu
            if (savegame_oras)
                new SAV_PokedexORAS(this).ShowDialog();
            else
                new SAV_PokedexXY(this).ShowDialog();
        }
        private void B_OUTPasserby_Click(object sender, EventArgs e)
        {
            string result = "";
            result += "PSS List" + Environment.NewLine;
            string[] headers = {
                                   "PSS Data - Friends",
                                   "PSS Data - Acquaintances",
                                   "PSS Data - Passerby",
                               };
            int offset = savindex * 0x7F000 + SaveGame.PSS;
            for (int g = 0; g < 3; g++)
            {
                result += Environment.NewLine
                    + "----" + Environment.NewLine
                    + headers[g] + Environment.NewLine
                    + "----" + Environment.NewLine;
                // uint count = BitConverter.ToUInt32(savefile, offset + 0x4E20);
                int r_offset = offset;

                for (int i = 0; i < 100; i++)
                {
                    ulong unkn = BitConverter.ToUInt64(savefile, r_offset);
                    if (unkn == 0) break; // No data present here
                    if (i > 0) result += Environment.NewLine + Environment.NewLine;

                    string otname = Util.TrimFromZero(Encoding.Unicode.GetString(savefile, r_offset + 8, 0x1A));
                    string message = Util.TrimFromZero(Encoding.Unicode.GetString(savefile, r_offset + 0x22, 0x22));

                    // Trim terminated

                    // uint unk1 = BitConverter.ToUInt32(savefile, r_offset + 0x44);
                    // ulong unk2 = BitConverter.ToUInt64(savefile, r_offset + 0x48);
                    // uint unk3 = BitConverter.ToUInt32(savefile, r_offset + 0x50);
                    // uint unk4 = BitConverter.ToUInt16(savefile, r_offset + 0x54);
                    byte region = savefile[r_offset + 0x56];
                    byte country = savefile[r_offset + 0x57];
                    byte game = savefile[r_offset + 0x5A];
                    // ulong outfit = BitConverter.ToUInt64(savefile, r_offset + 0x5C);
                    int favpkm = BitConverter.ToUInt16(savefile, r_offset + 0x9C) & 0x7FF;
                    string gamename;
                    try { gamename = gamelist[game]; }
                    catch { gamename = "UNKNOWN GAME"; }

                    string[] cr = PKX.getCountryRegionText(country, region, curlanguage);
                    result +=
                        "OT: " + otname + Environment.NewLine +
                        "Message: " + message + Environment.NewLine +
                        "Game: " + gamename + Environment.NewLine +
                        "Country: " + cr[0] + Environment.NewLine +
                        "Region: " + cr[1] + Environment.NewLine +
                        "Favorite: " + specieslist[favpkm];

                    r_offset += 0xC8; // Advance to next entry
                }
                offset += 0x5000; // Advance to next block
            }
            RTB_T.Text = result;
            RTB_T.Font = new Font("Courier New", 8);
            tabBoxMulti.SelectedIndex = 3;
        }
        private void B_OUTHallofFame_Click(object sender, EventArgs e)
        {
            // Open HoF Menu
            new SAV_HallOfFame(this).ShowDialog();
        }
        private void B_OpenSecretBase_Click(object sender, EventArgs e)
        {
            // Open Secret Base Menu
            new SAV_SecretBase(this).ShowDialog();
        }
        private void B_JPEG_Click(object sender, EventArgs e)
        {
            int offset = 0x7F000 * savindex + SaveGame.JPEG;

            string filename = Encoding.Unicode.GetString(savefile, offset + 0, 0x1A).Replace("\0", string.Empty);
            filename += "'s picture";
            offset += 0x54;
            if (savefile[offset] != 0xFF)
            {
                Util.Alert("No PGL picture data found in the save file!");
                return;
            }
            const int length = 0xE004;

            byte[] jpeg = new byte[length];
            Array.Copy(savefile, offset, jpeg, 0, length);
            SaveFileDialog savejpeg = new SaveFileDialog {FileName = filename, Filter = "JPEG|*.jpeg"};
            if (savejpeg.ShowDialog() != DialogResult.OK) return;
            string path = savejpeg.FileName;
            if (File.Exists(path))
            {
                // File already exists, save a .bak
                byte[] backupfile = File.ReadAllBytes(path);
                File.WriteAllBytes(path + ".bak", backupfile);
            }
            File.WriteAllBytes(path, jpeg);
        }
        // Save Folder Related
        private void clickSaveFileName(object sender, EventArgs e)
        {
            // Get latest SaveDataFiler save location
            pathSDF = Util.GetSDFLocation();
            string path = null;

            if (pathSDF != null && ModifierKeys != Keys.Control) // if we have a result
                path = Path.Combine(pathSDF, "main");
            else if (File.Exists(Util.NormalizePath(Path.Combine(Util.GetTempFolder(), "root" + Path.DirectorySeparatorChar + "main")))) // else if cgse exists
                path = Util.NormalizePath(Path.Combine(Util.GetTempFolder(), "root" + Path.DirectorySeparatorChar + "main"));

            if (path == null) return;
            if (Util.Prompt(MessageBoxButtons.YesNo, "Open save file from the following location?", path) == DialogResult.Yes)
                openQuick(path); // load save
        }
        private void clickOpenTempFolder(object sender, EventArgs e)
        {
            string path;
            switch (ModifierKeys)
            {
                case Keys.Control: // Cache
                    path = Util.GetCacheFolder();
                    if (Directory.Exists(path)) System.Diagnostics.Process.Start("explorer.exe", @path); else Util.Alert("Can't find the cache folder."); 
                    break;
                case Keys.Alt: // SaveDataFiler
                    path = Util.GetSDFLocation();
                    if (Directory.Exists(path)) System.Diagnostics.Process.Start("explorer.exe", @path); else Util.Alert("Can't find the SDF folder."); 
                    break;
                default: // Root
                    path = Util.GetTempFolder();
                    if (Directory.Exists(Path.Combine(path, "root"))) System.Diagnostics.Process.Start("explorer.exe", @Path.Combine(path, "root")); 
                    else if (Directory.Exists(path)) System.Diagnostics.Process.Start("explorer.exe", @path); 
                    else { Util.Error("Can't find the temporary file.", "Make sure the Cyber Gadget software is paused."); }
                    break;
            }
        }

        private void clickSwitchSAV(object sender, EventArgs e)
        {
            DialogResult switchsav = Util.Prompt(MessageBoxButtons.YesNo, String.Format("Current Savefile is Save {0}.", (savindex + 1)), String.Format("Would you like to switch to Save {0}?", ((savindex + 1) % 2 + 1)));
            if (switchsav != DialogResult.Yes) return;
            savindex = (savindex + 1) % 2;
            setBoxNames();
            setPKXBoxes();
            setSAVLabel();
        }

        // Drag & Drop within Box
        private void pbBoxSlot_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Alt || ModifierKeys == Keys.Shift || ModifierKeys == (Keys.Control | Keys.Alt))
            { clickSlot(sender, e); return; }
            PictureBox pb = (PictureBox)(sender);
            if (pb.Image == null)
                return;

            pkm_from_slot = getSlot(sender);
            int offset = getPKXOffset(pkm_from_slot);
            if (e.Button != MouseButtons.Left || e.Clicks != 1) return;
            // Create Temp File to Drag
            string basepath = Application.StartupPath;
            Cursor.Current = Cursors.Hand;

            // Prepare Data
            Array.Copy(savefile, offset, pkm_from, 0, 0xE8);
            pkm_from_offset = offset;

            // Make a new file name based off the PID
            byte[] dragdata = PKX.decryptArray(pkm_from);
            Array.Resize(ref dragdata, 0xE8);
            PKX pkx = new PKX(dragdata, "Boxes");
            string filename = pkx.Nickname;
            if (filename != pkx.Species)
                filename += " (" + pkx.Species + ")";
            filename += " - " + pkx.PID + ".pk6";

            // Make File
            string newfile = Path.Combine(basepath, Util.CleanFileName(filename));
            try
            {
                File.WriteAllBytes(newfile, dragdata);

                string[] filesToDrag = { newfile };
                DoDragDrop(new DataObject(DataFormats.FileDrop, filesToDrag), DragDropEffects.Move);
                File.Delete(newfile); // after drop, delete the temporary file
            }
            catch (ArgumentException x)
            { Util.Error("Drag & Drop Error:", x.ToString()); }
            File.Delete(newfile);
            pkm_from_offset = 0;
        }
        private void pbBoxSlot_DragDrop(object sender, DragEventArgs e)
        {
            int slot = getSlot(sender);
            int offset = getPKXOffset(slot);

            // Check for In-Dropped files (PKX,SAV,ETC)
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (Directory.Exists(files[0])) { loadBoxesFromDB(files[0]); return; }
            if (pkm_from_offset == 0)
            {
                if (files.Length > 0)
                {
                    FileInfo fi = new FileInfo(files[0]);

                    // Detect if PKM/PKX
                    if ((fi.Length == 136) || (fi.Length == 220) || (fi.Length == 236) || (fi.Length == 100) || (fi.Length == 80))
                    {
                        byte[] input = File.ReadAllBytes(files[0]);
                        var Converter = new pk2pk();
                        if (!PKX.verifychk(input)) Util.Alert("Invalid File Loaded.", "Checksum is not valid.");
                        try // to convert past gen pkm
                        {
                            byte[] data = Converter.ConvertPKM(input, savefile, savindex);
                            Array.Copy(PKX.encryptArray(data), 0, savefile, offset, 0xE8);
                        }
                        catch
                        { Util.Error("Attempted to load previous generation PKM.", "Conversion failed."); }
                    }
                    else if (fi.Length == 232 || fi.Length == 260)
                    {
                        byte[] data = File.ReadAllBytes(files[0]);
                        if (fi.Extension == ".pkx" || fi.Extension == ".pk6")
                            data = PKX.encryptArray(data);
                        else if (fi.Extension != ".ekx" && fi.Extension != ".ek6")
                        { openQuick(files[0]); return; } // lazy way of aborting 

                        byte[] decdata = PKX.decryptArray(data);
                        if (!PKX.verifychk(decdata))
                            Util.Alert("Attempted to load Invalid File.", "Checksum is not valid.");
                        else
                        {
                            Array.Copy(data, 0, savefile, offset, 0xE8);
                            setPokedex(decdata);
                            getQuickFiller(getPictureBox(slot), decdata);
                            getSlotColor(slot, Properties.Resources.slotSet);
                        }
                    }
                    else // not PKX/EKX, so load with the general function
                    { openQuick(files[0]); }
                }
            }
            else
            {
                if (ModifierKeys == Keys.Alt && slot > -1) // overwrite delete old slot
                {
                    byte[] cleardata = new Byte[0xE8];

                    // Clear from slot picture
                    getQuickFiller(getPictureBox(pkm_from_slot), cleardata);

                    // Clear from slot data
                    Array.Copy(PKX.encryptArray(cleardata), 0, savefile, pkm_from_offset, 0xE8);
                }
                else if (ModifierKeys != Keys.Control && slot > -1)
                {
                    // Load data from destination
                    byte[] swapdata = new Byte[0xE8];
                    Array.Copy(savefile, offset, swapdata, 0, 0xE8);

                    // Swap slot picture
                    getQuickFiller(getPictureBox(pkm_from_slot), PKX.decryptArray(swapdata));

                    // Swap slot data to source
                    Array.Copy(swapdata, 0, savefile, pkm_from_offset, 0xE8);
                }
                // Copy from temp slot to new.
                Array.Copy(pkm_from, 0, savefile, offset, 0xE8);
                getQuickFiller(getPictureBox(slot), PKX.decryptArray(pkm_from));

                pkm_from_offset = 0; // Clear offset value
            }

            savedited = true;
        }
        private void pbBoxSlot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null)
                e.Effect = DragDropEffects.Move;
        }
        private byte[] pkm_from = PKX.encryptArray(new byte[0xE8]);
        private int pkm_from_offset;
        private int pkm_from_slot = -1;
        #endregion
    }
}

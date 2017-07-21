﻿using System.IO;
using SacanaWrapper;
using System.Windows.Forms;
using System.Collections.Generic;
using System;

namespace VNXTLP {
    internal static partial class Engine {
        internal static string[] Open(string Path, bool TempMode = false) {
            string[] ReturnContent;
            if (TempMode) {
                Editor = BackupEditor;
                RemapBackup = StrMap;
                StringCountBackup = StringCount;
                PrefixBackup = Prefix;
                SufixBackup = Sufix;
            }else {
                ScriptPath = Path;
                SetConfig("VNXTLP", "LastScript", ScriptPath);
            }

            byte[] Script = File.ReadAllBytes(Path);
            Editor = new Wrapper();
            ReturnContent = Editor.Import(Script, System.IO.Path.GetExtension(Path), true);

            StringCount = 0;
            if (File.Exists(Path + ".map"))
                Remap(ref ReturnContent, Path + ".map");

            ClearStrings(ref ReturnContent);

            if (TempMode) {
                Editor = BackupEditor;
                StrMap = RemapBackup;
                StringCountBackup = StringCount;
                Prefix = PrefixBackup;
                Sufix = SufixBackup;
            }
            return ReturnContent;
        }

       

        internal static void Save(string Path, string[] Content) {
            if (Path != ScriptPath) {
                if (File.Exists(ScriptPath + ".map")) {
                    File.Copy(ScriptPath + ".map", Path + ".map", true);
                }
            }
            ScriptPath = Path;
            SetConfig("VNXTLP", "LastScript", ScriptPath);

            UndoRemap(ref Content);
            RestoreStrings(ref Content);

            if (File.Exists(ScriptPath))
                File.Delete(ScriptPath);

            byte[] Output = Editor.Export(Content);
            File.WriteAllBytes(ScriptPath, Output);
        }

        private static void ClearStrings(ref string[] Strings) {
            Engine.Prefix = new Dictionary<uint, string>();
            Engine.Sufix = new Dictionary<uint, string>();
            string Trim = GetConfig("Filter", "Trim", false);
            if (string.IsNullOrWhiteSpace(Trim))
                return;

            for (uint i = 0; i < Strings.LongLength; i++) {
                string Result = string.Empty, Prefix = string.Empty, Sufix = string.Empty;
                while (true) {
                    Result = ClearPrefix(ref Strings[i], Trim);
                    if (Result == null)
                        break;
                    Prefix += Result;
                }

                while (true) {
                    Result = ClearSufix(ref Strings[i], Trim);
                    if (Result == null)
                        break;
                    Sufix = Result + Sufix;
                }

                Engine.Prefix.Add(i, Prefix);
                Engine.Sufix.Add(i, Sufix);
            }
        }

        private static void RestoreStrings(ref string[] Strings) {
            for (uint i = 0; i < Strings.LongLength; i++) {
                if (Prefix.ContainsKey(i))
                    Strings[i] = Prefix[i] + Strings[i];

                if (Sufix.ContainsKey(i))
                    Strings[i] += Sufix[i];
            }
        }

        private static string ClearPrefix(ref string String, string Trim) {
            string[] Prefixs = Trim.Split(',');
            foreach (string Prefix in Prefixs) {
                if (String.StartsWith(Prefix)) {
                    String = String.Substring(Prefix.Length, String.Length - Prefix.Length);
                    return Prefix;
                }
            }
            return null;
        }

        private static string ClearSufix(ref string String, string Trim) {
            string[] Sufixs = Trim.Split(',');
            foreach (string Sufix in Sufixs) {
                if (String.EndsWith(Sufix)) {
                    String = String.Substring(0, String.Length - Sufix.Length);
                    return Sufix;
                }
            }
            return null;
        }

        private static void Remap(ref string[] Strings, string Remap) {
            StrMap = new Dictionary<uint, uint>();
            BinaryReader Reader = new BinaryReader(File.Open(Remap, FileMode.Open, FileAccess.Read));
            uint OriLen = StringCount = Reader.ReadUInt32();
            uint RstLen = Reader.ReadUInt32();
            if (Strings.Length != OriLen) {
                MessageBox.Show(LoadTranslation(97), "VNXTLP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string[] Result = new string[RstLen];

            for (uint oi = 0; oi < OriLen; oi++) {
                uint Repeats = Reader.ReadUInt32();
                for (uint ri = 0; ri < Repeats; ri++) {
                    uint Pos = Reader.ReadUInt32();
                    Result[Pos] = Strings[oi];
                    StrMap.Add(Pos, oi);
                }
            }
            Strings = Result;
        }

        internal static void Genmap(string OriPath, string RstPath) {
            string[] Ori = Open(OriPath, true);
            string[] Rst = Open(RstPath, true);


            BinaryWriter Writer = new BinaryWriter(File.Create(OriPath + ".map"));
            Writer.Write((uint)Ori.LongLength);
            Writer.Write((uint)Rst.LongLength);

            for (uint oi = 0; oi < Ori.LongLength; oi++) {
                List<uint> Map = new List<uint>();
                for (uint ri = 0; ri < Rst.LongLength; ri++) {
                    if (Rst[ri] == Ori[oi])
                        Map.Add(ri);
                }
                uint[] StrMap = Map.ToArray();

                if (StrMap.LongLength == 0) {
                    MessageBox.Show(LoadTranslation(98), "VNXTLP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Writer.Close();
                    File.Delete(OriPath + ".map");
                    return;
                }

                Writer.Write((uint)StrMap.LongLength);
                foreach (uint Pos in StrMap)
                    Writer.Write(Pos);
            }

            Writer.Close();
            MessageBox.Show(LoadTranslation(100), "VNXTLP", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private static void UndoRemap(ref string[] Strings) {
            if (StringCount == 0)
                return;

            string[] Result = new string[StringCount];
            for (uint i = 0; i < Strings.Length; i++) {
                Result[StrMap[i]] = Strings[i];
            }

            Strings = Result;
        }
    }
}

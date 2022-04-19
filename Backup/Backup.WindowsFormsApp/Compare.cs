using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ZetaLongPaths;

namespace Backup {
    class Compare {

        public static string log = "log.log";

        /// <summary>
        /// Compare the two folders
        /// </summary>
        public static void Folders(string pathA, string pathB) {
            List<string> lista = null;
            File.WriteAllText(Log.AttachmentFile, DateTime.Now.ToString("dd/MM HH:mm") + " Comparando pastas: " + Environment.NewLine);
            try {
                Console.WriteLine("Pegando Info dos diretórios " + pathA);
                ZlpDirectoryInfo dir1 = new ZlpDirectoryInfo(pathA);
                ZlpDirectoryInfo dir2 = new ZlpDirectoryInfo(pathB);
                Console.WriteLine("Evento GetFiles:");
                IEnumerable<ZlpFileInfo> list1 = dir1.GetFiles("*.*", SearchOption.AllDirectories);
                IEnumerable<ZlpFileInfo> list2 = dir2.GetFiles("*.*", SearchOption.AllDirectories);

                lista = new List<string>();
                FileCompare myFileCompare = new FileCompare();
                var queryList1Only = (from file in list1 select file).Except(list2, myFileCompare);

                Console.WriteLine("Arquivos diferentes:");
                foreach (var v in queryList1Only) {
                    if (!v.NameWithoutExtension.Contains("~")) {
                        lista.Add(v.FullName);
                        Console.WriteLine(v.FullName);
                    }
                }
            } catch (Exception ex) {
                File.WriteAllText(Log.AttachmentFile, "Ocorreu um erro:" + Environment.NewLine + ex.Message);
                Console.WriteLine("Ocorreu um erro:");
                Console.WriteLine(ex.Message);
            }
            try {
                File.WriteAllLines(log, lista);
            } catch (Exception ex) {
                Console.WriteLine("Ocorreu um erro ao escrever arquivo " + log + Environment.NewLine + ex.Message);
            }
            Console.WriteLine("Fim");
            Console.WriteLine("");
        }

        public static void Ignore(string arquivoTxt) {
            List<string> textoFinal = new List<string>();
            string[] linhasIgnorar = File.ReadAllLines(arquivoTxt);
            if (linhasIgnorar.Length > 0) {
                string[] linhasLog = File.ReadAllLines(log);
                foreach (string linhaIgnorar in linhasIgnorar)
                    foreach (string linhaLog in linhasLog)
                        if (!linhasLog.Contains(linhaIgnorar))
                            textoFinal.Add(linhaLog);
                File.WriteAllLines(log, textoFinal);
            }
        }
    }

    class FileCompare : IEqualityComparer<ZlpFileInfo> {
        public FileCompare() { }
        public bool Equals(ZlpFileInfo f1, ZlpFileInfo f2) {
            return (f1.Name == f2.Name &&
                    f1.Length == f2.Length);
        }
        public int GetHashCode(ZlpFileInfo fi) {
            string s = $"{fi.Name}{fi.Length}";
            return s.GetHashCode();
        }
    }
}

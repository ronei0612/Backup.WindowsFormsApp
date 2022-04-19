using System;
using System.Collections.Generic;
using System.IO;
using ZetaLongPaths;
using System.Linq;

namespace Backup {
    static class Copy {
        static string temppath, nam, file;
        static string[] arquivos;
        static List<string> log = new List<string>();

        /// <summary>
        /// Execute the task
        /// </summary>
        public static void ToCopy(string sourceDirName, string destDirName) {
            if (sourceDirName[sourceDirName.Length - 1] != '\\')
                sourceDirName = sourceDirName + "\\";
            if (destDirName[destDirName.Length - 1] != '\\')
                destDirName = destDirName + "\\";
            arquivos = File.ReadAllLines(Compare.log);
            bool erro = false;
            int n = 1;
            log.Add("");
            log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Total de arquivos copiados: " + arquivos.Count());
            Log._message = Log._message + Environment.NewLine + sourceDirName + " --> " + destDirName + Environment.NewLine + "  Iniciado em: " + DateTime.Now + Environment.NewLine + "Total de arquivos copiados: " + arquivos.Count() + Environment.NewLine;
            Log._attachment = Log._attachment + Environment.NewLine + sourceDirName + " --> " + destDirName + Environment.NewLine + "  Iniciado em: " + DateTime.Now + Environment.NewLine;
            log.Add("");
            Console.WriteLine("Total de arquivos: " + arquivos.Count());
            Console.WriteLine("Copiando...");
            foreach (string arquivo in arquivos) {
                Console.Write("\r" + arquivo + " - " + n);
                file = arquivo.Replace(sourceDirName, destDirName);
                temppath = ZlpPathHelper.GetDirectoryPathNameFromFilePath(file);

                // If directory does not exist
                if (!ZlpIOHelper.DirectoryExists(temppath))
                    try {
                        ZlpIOHelper.CreateDirectory(temppath);
                        log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Diretório criado: " + temppath);
                    } catch (Exception ex) {
                            erro = true;
                            log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Erro ao criar diretório:" + Environment.NewLine + file + ex.Message + Environment.NewLine);
                    }

                // If the file does not exist
                if (!ZlpIOHelper.FileExists(file))
                    try {
                        File.Copy(arquivo, file, false);
                        log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Arquivo criado: " + file);
                    } catch (Exception ex) {
                        if (ex.Message.Contains("260"))
                            try {
                                ZlpIOHelper.CopyFile(arquivo, file, false);
                            } catch (Exception e) {
                                erro = true;
                                log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Erro ao copiar:" + Environment.NewLine + arquivo + " --> " + file + Environment.NewLine + e.Message + Environment.NewLine);
                            }
                        else {
                            erro = true;
                            log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Erro ao copiar:" + Environment.NewLine + arquivo + " --> " + file + Environment.NewLine + ex.Message + Environment.NewLine);
                        }
                    }

                // File exists, then it is different
                else {
                    nam = ZlpIOHelper.GetFileLastWriteTime(arquivo).ToString("~dd-MM-yyyy_HH-mm-ss");
                    try {
                        File.Move(file, Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + nam + Path.GetExtension(file));
                        log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Arquivo movido: " + file + nam);
                    } catch (Exception ex) {
                        if (ex.Message.Contains("260")) {
                            try {
                                ZlpIOHelper.MoveFile(file, Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + nam + Path.GetExtension(file), true);
                                log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Arquivo movido: " + file + nam);
                            } catch (Exception e) {
                                erro = true;
                                log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Erro ao mover arquivo:" + Environment.NewLine + file + Environment.NewLine + file + nam + e.Message + Environment.NewLine);
                            }
                        } else {
                            erro = true;
                            log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Erro ao mover arquivo:" + Environment.NewLine + file + Environment.NewLine + file + nam + ex.Message + Environment.NewLine);
                        }
                    }
                    try {
                        File.Copy(arquivo, file, true);
                        log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Arquivo copiado: " + file);
                    } catch (Exception ex) {
                        if (WindowsPathTooLong(ex.Message)) {
                            try {
                                ZlpIOHelper.CopyFile(arquivo, file, true);
                                log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Arquivo copiado: " + file);
                            } catch (Exception e) {
                                erro = true;
                                log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Erro ao criar arquivo:" + Environment.NewLine + file + Environment.NewLine + file + nam + e.Message + Environment.NewLine);
                            }
                        } else {
                            erro = true;
                            log.Add(DateTime.Now.ToString("dd/MM HH:mm") + " Erro ao criar arquivo:" + Environment.NewLine + file + Environment.NewLine + file + nam + ex.Message + Environment.NewLine);
                        }
                    }
                }
                n++;
            }
            log.Add("");
            Log._attachment = Log._attachment + string.Join(Environment.NewLine, log.ToArray());
            if (erro) {
                Log._subject = "Backup - " + DateTime.Now + " Com erros";
                Log._message = Log._message + "Backup - " + DateTime.Now + " com erros" + Environment.NewLine + @"Vide anexo em " + Log.saveLog;
            } else
                Log._subject = "Backup - " + DateTime.Now + " Sucesso";
            Log._message = Log._message + Environment.NewLine + @"Vide LOG em " + Log.saveLog;
        }

        /// <summary>
        /// Windows has a limit of the number of characters in path of the File Explorer, then WindowsPathTooLong fix it
        /// </summary>
        static private bool WindowsPathTooLong(string message) {
            if (message.Contains("260"))
                return true;
            else
                return false;
        }

        public static void SendEmail() {
            // Here finish the copy
            if (Log.EmailTo != "") {
                if (Log.Cc == null)
                    Log.SendEmail(Log.EmailTo, Log.AttachmentFile);
                else
                    Log.SendEmail(Log.EmailTo, Log.AttachmentFile, Log.Cc);
            }
        }
    }
}
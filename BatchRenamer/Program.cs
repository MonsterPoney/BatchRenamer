using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;

namespace BatchRenamer {
    class Program {
        static readonly ConfigFile ini = new ConfigFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/config.ini");
        public static string logPath = ini.ReadKey("main", "logPath")??"\\.";
        public static Logger log;
        public static bool isWarning = false;
        public static XmlDocument res;
        static readonly string lang = (System.Globalization.CultureInfo.CurrentUICulture.ToString().Substring(0, 2)=="fr") ? "fr" : "en";
        static string path = "";
        static string input, line, command;
        static int modification = 0, type = 0, choice;
        static string extension = "";
        static string[] commands;
        static HashSet<string> fileSet;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static void Main(string[] args) {
            // Init
            var handle = GetConsoleWindow();
            try {
                fileSet=new HashSet<string>();

                res=new XmlDocument();
                res.Load($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\res\String-{lang}.xml");

                SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

                if (logPath[logPath.Length-1]!='\\')
                    logPath+="\\";
                log=new Logger(logPath, "");
            } catch (Exception e) {
                log.WriteLog("[E] : Exception during startup :", e.Message+e.StackTrace);
                Environment.Exit(1);
            }

            try {
                // TODO : manage arguments when executed from windows explorer path
                if (args.Length>0) {
                    ShowWindow(handle, SW_HIDE);
                    string[] arguments = string.Join("", args).Split('/');
                    path=arguments[0];
                    type=int.Parse(arguments[1]);
                    extension=arguments[2];

                    fileSet=(type==1) ? Directory.GetFiles(path, $"*{extension}").ToHashSet() : Directory.GetDirectories(path).ToHashSet();
                    if (fileSet.Count==0)
                        Environment.Exit(18);

                    commands=arguments.Skip(3).ToArray();

                    foreach (string cmd in commands) {
                        log.WriteLog("[I]: Command "+cmd);
                        _=new Modification(fileSet, extension, path, type, cmd);
                    }
                    Environment.Exit(0);
                }
            } catch (Exception e) {
                log.WriteLog("[E] : Exception with arguments commands : ", e.Message+e.StackTrace);
            }

            try {
tagStart:
                try {
                    // Case when executed from the windows explorer path
                    if (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!=Environment.CurrentDirectory)
                        path=Environment.CurrentDirectory;
                    else {
                        Console.WriteLine(res.Get("pathExemple"));
                        path=Console.ReadLine();
                    }

                    if (!Directory.Exists(path)) {
                        Console.WriteLine(res.Get("pathError"), path);
                        goto tagStart;
                    }
                } catch (Exception e) {
                    log.WriteLog("[E] : Exception path : ", e.Message+e.StackTrace);
                }

                try {
tagType:
                    Console.WriteLine(res.Get("type"));
                    type=int.Parse(Console.ReadLine());

                    if (type<1||type>2) {
                        Console.WriteLine(res.Get("incorValue"));
                        goto tagType;
                    }
                } catch (Exception e) {
                    log.WriteLog("[E] : Exception type : ", e.Message+e.StackTrace);
                }

                try {
                    if (type==1) {
                        Console.WriteLine(res.Get("docExtension"));
                        extension=Console.ReadLine();

                        if (!extension.StartsWith("."))
                            extension="."+extension;

                        fileSet=Directory.GetFiles(path, $"*{extension}").ToHashSet();
                    } else
                        fileSet=Directory.GetDirectories(path).ToHashSet();
                    if (fileSet.Count==0) {
                        Console.WriteLine(res.Get("noMatch"));
                    }
                    Console.WriteLine(res.Get("numberOfMatch"), ((type==1) ? res.Get("files") : res.Get("folders")), fileSet.Count);
                } catch (Exception e) {
                    log.WriteLog("[E] : Exception extension : ", e.Message+e.StackTrace);
                }

// : entre param / entre cmd
tagMode:
                Console.WriteLine(res.Get("mode"));
                choice=int.Parse(Console.ReadLine());
                if (choice<1||choice>2) {
                    Console.WriteLine(res.Get("incorValue"));
                    goto tagMode;
                }
                if (choice==2) {
                    Console.WriteLine(res.Get("reminder"));
                    command=Console.ReadLine();
                    commands=command.Split('/');
                    Console.WriteLine($"{commands.Length} commands : ");
                    foreach (string cmd in commands)
                        Console.WriteLine($"\n{cmd}");
tagValidationCMD:
                    Console.WriteLine(res.Get("validation"));
                    string valid = Console.ReadLine().ToUpper();
                    if (valid=="N")
                        goto tagStart;
                    else if (valid=="Y") {
                        foreach (string cmd in commands) {
                            _=new Modification(fileSet, extension, path, type, cmd);
                        }
                        CloseCons();
                    } else {
                        Console.WriteLine(res.Get("incorValue"));
                        goto tagValidationCMD;
                    }
                }

                try {
tagModif:
                    Console.WriteLine(res.Get("modifications"));
                    modification=int.Parse(Console.ReadLine());

                    if (modification<1||modification>7) {
                        Console.WriteLine(res.Get("incorValue"));
                        goto tagModif;
                    }
                } catch (Exception e) {
                    log.WriteLog("[E] : Exception modifications : ", e.Message+e.StackTrace);
                }

                switch (modification) {
                    // Addition
                    case 1:
                        try {
tagCase1:
                            Console.WriteLine(res.Get("case1.1"));
                            line=Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase1;
                            Console.WriteLine(res.Get("case1.2"));
                            choice=int.Parse(Console.ReadLine());

                            if (choice!=1&&choice!=2) {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagCase1;
                            }

tagValidation1:
                            GeneValidation(path, String.Format((choice==1) ? res.Get("addBegin") : res.Get("addEnd"), line), type, extension, fileSet.Count);
                            input=Console.ReadLine().ToUpper();
                            if (input=="N")
                                goto tagStart;
                            else if (input=="Y")
                                Modification.Addition(fileSet, type, choice, line, extension);
                            else {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagValidation1;
                            }
                        } catch (Exception e) {
                            log.WriteLog("[E] : Exception case1(Addition) : ", e.Message+e.StackTrace);
                        }
                        break;

                    // Supression
                    case 2:
                        try {
tagCase2:
                            Console.WriteLine(res.Get("case2.1"));
                            line=Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase2;
                            fileSet.RemoveWhere(f => !f.Split('\\').Last().Contains(line));
tagValidation2:
                            GeneValidation(path, String.Format(res.Get("removeFromDoc"), line), type, extension, fileSet.Count);
                            input=Console.ReadLine().ToUpper();
                            if (input=="N")
                                goto tagStart;
                            else if (input=="Y")
                                Modification.Supression(fileSet, type, line, extension);
                            else {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagValidation2;
                            }
                        } catch (Exception e) {
                            log.WriteLog("[E] : Exception case2(Supression) : ", e.Message+e.StackTrace);
                        }
                        break;

                    // Delete numbers
                    case 3:
                        try {
                            fileSet.RemoveWhere(f => !f.Split('\\').Last().Any(char.IsDigit));
tagValidation3:
                            GeneValidation(path, res.Get("removeNum"), type, extension, fileSet.Count);
                            input=Console.ReadLine().ToUpper();
                            if (input=="N")
                                goto tagStart;
                            else if (input=="Y")
                                Modification.DeleteNumbers(fileSet, type, extension);
                            else {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagValidation3;
                            }
                        } catch (Exception e) {
                            log.WriteLog("[E] : Exception case3(Number delete) : ", e.Message+e.StackTrace);
                        }
                        break;

                    // Replace
                    case 4:
                        try {
tagCase4:
                            Console.WriteLine(res.Get("case4.1"));
                            line=Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase4;
                            if (line.Split(';').Count()!=2) {
                                Console.WriteLine(res.Get("argError"));
                                goto tagCase4;
                            }
                            string oldChar = line.Split(';')[0];
                            string newChar = line.Split(';')[1];
                            fileSet.RemoveWhere(f => !f.Split('\\').Last().Contains(oldChar));
tagValidation4:
                            GeneValidation(path, String.Format(res.Get("replaceBy"), oldChar, newChar), type, extension, fileSet.Count);
                            input=Console.ReadLine().ToUpper();
                            if (input=="N")
                                goto tagStart;
                            else if (input=="Y")
                                Modification.Replace(fileSet, type, line, extension);
                            else {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagValidation4;
                            }
                        } catch (Exception e) {
                            log.WriteLog("[E] : Exception case4(Replace) : ", e.Message+e.StackTrace);
                        }
                        break;

                    // Rename
                    case 5:
                        try {
tagCase5:
                            Console.WriteLine(res.Get("case5.1"));
                            line=Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase5;
                            Console.WriteLine(res.Get("case5.2"));
                            choice=int.Parse(Console.ReadLine());

                            if (choice!=1&&choice!=2) {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagCase5;
                            }

tagValidation5:
                            GeneValidation(path, String.Format(res.Get("newName"), line), type, extension, fileSet.Count);
                            input=Console.ReadLine().ToUpper();
                            if (input=="N")
                                goto tagStart;
                            else if (input=="Y")
                                Modification.Rename(fileSet, type, choice, line, extension);
                            else {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagValidation5;
                            }
                        } catch (Exception e) {
                            log.WriteLog("[E] : Exception case5(Rename) : ", e.Message+e.StackTrace);
                        }
                        break;

                    // Upper/lower case
                    case 6:
                        try {
tagCase6:
                            Console.WriteLine(res.Get("case6.1"));
                            choice=int.Parse(Console.ReadLine());

                            if (choice<1||choice>3) {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagCase6;
                            } else if (choice==3) {
// First letter upper
tagValidation6b:
                                GeneValidation(path, res.Get("case6.2"), type, extension, fileSet.Count);
                                input=Console.ReadLine().ToUpper();
                                if (input=="N")
                                    goto tagStart;
                                else if (input=="Y")
                                    Modification.CaseChange(fileSet, type, choice, extension);
                                else {
                                    Console.WriteLine(res.Get("incorValue"));
                                    goto tagValidation6b;
                                }
                            } else {
tagValidation6:
                                GeneValidation(path, res.Get("caseChange"), type, extension, fileSet.Count);
                                input=Console.ReadLine().ToUpper();
                                if (input=="N")
                                    goto tagStart;
                                else if (input=="Y")
                                    Modification.CaseChange(fileSet, type, choice, extension);
                                else {
                                    Console.WriteLine(res.Get("incorValue"));
                                    goto tagValidation6;
                                }
                            }

                        } catch (Exception e) {
                            log.WriteLog("[E] : Exception case6(Upper/lower case) : ", e.Message+e.StackTrace);
                        }
                        break;

                    // Change extension
                    case 7:
                        try {
                            if (type!=1) {
                                Console.WriteLine(res.Get("case7.1"));
                                goto tagStart;
                            }
tagCase7:
                            Console.WriteLine(res.Get("newExten"), "");
                            line=Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase7;
                            if (!line.StartsWith("."))
                                line="."+line;

tagValidation7:
                            GeneValidation(path, String.Format(res.Get("newExten"), line), type, extension, fileSet.Count);
                            input=Console.ReadLine().ToUpper();
                            if (input=="N")
                                goto tagStart;
                            else if (input=="Y")
                                Modification.ExtensionChange(fileSet, line);
                            else {
                                Console.WriteLine(res.Get("incorValue"));
                                goto tagValidation7;
                            }

                        } catch (Exception e) {
                            log.WriteLog("[E] : Exception case7(extension) : ", e.Message+e.StackTrace);
                        }
                        break;
                }
                CloseCons();
            } catch (Exception e) {
                log.WriteLog("[E] : General exception : ", e.GetType()+e.Message+e.StackTrace+e.InnerException);
            }


            // Search for forbidden char
            bool ValidLine(string _line) {
                var regex = new Regex("^[/\\:*?\"<>|]$");

                string lower = _line.ToLower();
                if (lower=="con"||lower=="aux"||lower=="prn"||lower=="lst"||lower=="nul") {
                    Console.WriteLine(res.Get("forbiddenChar"));
                    return false;
                }
                for (int i = 0; i<=9; i++) {
                    if (lower==$"com{i}"||lower==$"lpt{i}") {
                        Console.WriteLine(res.Get("forbiddenChar"));
                        return false;
                    }
                }

                if (regex.IsMatch(_line)) {
                    Console.WriteLine(res.Get("forbiddenChar"));
                    return false;
                } else
                    return true;
            }

            // Generate text for verification
            void GeneValidation(string _path, string _custom, int _type, string _extension, int _count) {
                if (_type==1)
                    Console.WriteLine(res.Get("validationFile"), _path, _custom, _extension, _count);
                else
                    Console.WriteLine(res.Get("validationFolder"), _path, _custom, _count);
            }

        }

        public static void CloseCons() {
            if (isWarning) {
                Console.BackgroundColor=ConsoleColor.Yellow;
                Console.ForegroundColor=ConsoleColor.Black;
                Console.WriteLine(res.Get("endWarn"));
            } else if (!log.isWritten) {
                File.Delete(log.logName);
                Console.BackgroundColor=ConsoleColor.Green;
                Console.WriteLine(res.Get("end"));
            } else {
                Console.BackgroundColor=ConsoleColor.Red;
                Console.WriteLine(res.Get("endErr"), logPath);
            }
            Console.ResetColor();
            Console.WriteLine(res.Get("exitRestart"));
            char line = Console.ReadKey().KeyChar;
            if (line=='1')
                Main(new string[] { });
            else
                Environment.Exit(0);
        }


        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        public static bool ConsoleCtrlCheck(CtrlTypes ctrlType) {
            if (!log.isWritten)
                File.Delete(log.logName);
            return true;
        }
    }

    /// <summary>
    /// Return the inner text of a child node of xml tag
    /// </summary>
    public static class XmlDocumentExtention {
        public static string Get(this XmlDocument doc, string nodeName) {
            return doc.SelectSingleNode($"xml/{nodeName}").InnerText;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BatchRenamer {
    class Program {
        static ConfigFile ini = new ConfigFile("./config.ini");
        public static string logPath = ini.ReadKey("main", "logPath");
        static Logger log = new Logger(logPath, "");
        static bool isWarning = false;
        static void Main() {
            string lang = ini.ReadKey("main", "language");

            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            if (logPath[logPath.Length - 1] != '\\') {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("log File cannot be properly named, logPath must finish by '\'");
                Console.ResetColor();
            }

            if (lang != "fr" || lang != "en")
                lang = "en";

            // https://docs.microsoft.com/en-us/dotnet/framework/resources/creating-resource-files-for-desktop-apps#resources-in-resx-files
            //ResourceManager rm = new ResourceManager("String_fr", typeof(Program).Assembly);

            try {
                tagStart:
                string path = "", input, line;
                int modification = 0, type = 0, choice;
                string extension = "";

                HashSet<string> fileSet = new HashSet<string>();

                try {
                    tagPath:
                    Console.WriteLine(@"Chemin (Ex : M:\Documents\Renomage) : ");
                    path = Console.ReadLine();

                    if (!Directory.Exists(path)) {
                        Console.WriteLine($"Le chemin suivant n'existe pas : {path}");
                        goto tagPath;
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception path : ", e.Message);
                }

                try {
                    tagType:
                    Console.WriteLine("\nType de document a renommer :\n" +
                        "1 - Fichier\n" +
                        "2 - Dossier");
                    type = int.Parse(Console.ReadLine());

                    if (type < 1 || type > 2) {
                        Console.WriteLine("valeur incorrecte");
                        goto tagType;
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception type : ", e.Message);
                }

                try {
                    if (type == 1) {
                        Console.WriteLine("\nExtension des documents :\n" +
                            "Ex : .png || .txt || .pdf || etc...");
                        extension = Console.ReadLine();

                        if (!extension.StartsWith("."))
                            extension = "." + extension;

                        fileSet = Directory.GetFiles(path, $"*{extension}").ToHashSet();
                    } else
                        fileSet = Directory.GetDirectories(path).ToHashSet();
                    if (fileSet.Count == 0) {
                        Console.WriteLine("Aucun Documents correspondants trouver");
                    }
                    Console.WriteLine($"\nNombre de {((type == 1) ? "fichiers" : "Documents")} correspondants : {fileSet.Count}");
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception extension : ", e.Message);
                }

                try {
                    tagModif:
                    Console.WriteLine("\nModifications :\n" +
                        "1 - Ajout de caracteres\n" +
                        "2 - Suppression de caracteres\n" +
                        "3 - Suppression des chiffres\n" +
                        "4 - Remplacement de caracteres\n" +
                        "5 - Rennomage complet\n" +
                        "6 - MAJ / min\n" +
                        "7 - Changement extension");
                    modification = int.Parse(Console.ReadLine());

                    if (modification < 1 || modification > 7) {
                        Console.WriteLine("valeur incorrecte");
                        goto tagModif;
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception modifications : ", e.Message);
                }

                switch (modification) {
                    // Addition
                    case 1:
                        try {
                            tagCase1:
                            Console.WriteLine("Caractères a ajouter : ");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase1;
                            Console.WriteLine("\n1 - En début de chaine\n" +
                                "2 - En fin de chaine");
                            choice = int.Parse(Console.ReadLine());

                            if (choice != 1 && choice != 2) {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagCase1;
                            }

                            tagValidation1:
                            GeneValidation(path, $"Ajouter {line} {((choice == 1) ? "en début de chaine\n" : "en fin de chaine\n")}", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y") {
                                foreach (var file in fileSet) {
                                    if (type == 1) {
                                        string name = Directory.GetParent(file) + @"\" + ((choice == 1) ? line + Path.GetFileNameWithoutExtension(file) : Path.GetFileNameWithoutExtension(file) + line) + extension;
                                        MoveIfNotExists(true, file, name);
                                    } else {
                                        string name = Directory.GetParent(file) + @"\" + ((choice == 1) ? line + file.Split('\\').Last() : file.Split('\\').Last() + line);
                                        MoveIfNotExists(false, file, name);
                                    }
                                }
                            } else {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagValidation1;
                            }
                        }
                        catch (Exception e) {
                            log.WriteLog("E : Exception case1(Addition) : ", e.Message);
                        }
                        break;

                    // Supression
                    case 2:
                        try {
                            tagCase2:
                            Console.WriteLine("Caractères a supprimer : ");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase2;
                            fileSet.RemoveWhere(f => !f.Split('\\').Last().Contains(line));
                            tagValidation2:
                            GeneValidation(path, $"Supprimer {line} des noms de fichiers\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y") {
                                foreach (var file in fileSet) {
                                    if (type == 1) {
                                        string name = Directory.GetParent(file) + @"\" + Path.GetFileNameWithoutExtension(file).Replace(line, "") + extension;
                                        MoveIfNotExists(true, file, name);
                                    } else {
                                        string name = Directory.GetParent(file) + @"\" + file.Split('\\').Last().Replace(line, "");
                                        MoveIfNotExists(false, file, name);
                                    }
                                }
                            } else {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagValidation2;
                            }
                        }
                        catch (Exception e) {
                            log.WriteLog("E : Exception case2(Supression) : ", e.Message);
                        }
                        break;

                    // Delete numbers
                    case 3:
                        try {
                            fileSet.RemoveWhere(f => !f.Split('\\').Last().Any(char.IsDigit));
                            tagValidation3:
                            GeneValidation(path, $"Supprimer les chiffres\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y") {
                                Regex regexNumber = new Regex(@"[\d-]");
                                if (type == 1) {
                                    foreach (var file in fileSet) {
                                        string name = Directory.GetParent(file) + @"\" + Regex.Replace(Path.GetFileNameWithoutExtension(file), @"[\d]", "") + extension;
                                        MoveIfNotExists(true, file, name);
                                    }
                                } else {
                                    foreach (var file in fileSet) {
                                        string name = Directory.GetParent(file) + @"\" + Regex.Replace(file.Split('\\').Last(), @"[\d]", "");
                                        MoveIfNotExists(false, file, name);
                                    }
                                }
                            } else {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagValidation3;
                            }
                        }
                        catch (Exception e) {
                            log.WriteLog("E : Exception case3(Number delete) : ", e.Message);
                        }
                        break;

                    // Replace
                    case 4:
                        try {
                            tagCase4:
                            Console.WriteLine("\nSyntaxe : [avant];[apres] || Ex : _;-");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase4;
                            string oldChar = line.Split(';')[0];
                            string newChar = line.Split(';')[1];
                            fileSet.RemoveWhere(f => !f.Split('\\').Last().Contains(oldChar));
                            tagValidation4:
                            GeneValidation(path, $"Remplacer {oldChar} par {newChar}\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y") {
                                foreach (var file in fileSet) {
                                    if (type == 1) {
                                        string name = Directory.GetParent(file) + @"\" + Path.GetFileNameWithoutExtension(file).Replace(oldChar, newChar) + extension;
                                        MoveIfNotExists(true, file, name);
                                    } else {
                                        string name = Directory.GetParent(file) + @"\" + file.Split('\\').Last().Replace(oldChar, newChar);
                                        MoveIfNotExists(false, file, name);
                                    }
                                }
                            } else {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagValidation4;
                            }
                        }
                        catch (Exception e) {
                            log.WriteLog("E : Exception case4(Replace) : ", e.Message);
                        }
                        break;

                    // Rename
                    case 5:
                        try {
                            tagCase5:
                            Console.WriteLine("Nouveaux nom :");
                            string newName = Console.ReadLine();
                            if (!ValidLine(newName))
                                goto tagCase5;
                            Console.WriteLine("\n\nDifférencier les noms par :\n" +
                                "1 - Nombre en début de chaine\n" +
                                "2 - Nombre en fin de chaine");
                            choice = int.Parse(Console.ReadLine());

                            if (choice != 1 && choice != 2) {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagCase5;
                            }

                            tagValidation5:
                            GeneValidation(path, $"Nouveau nom : {newName}\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y") {
                                int i = 1;
                                foreach (var file in fileSet) {
                                    string name = file;
                                    if (type == 1) {
                                        // Rename file
                                        name = name.Replace(Path.GetFileNameWithoutExtension(file), (choice == 1) ? i.ToString() + newName : newName + i.ToString());
                                        MoveIfNotExists(true, file, name);
                                    } else {
                                        // Rename folder
                                        name = Directory.GetParent(file) + @"\" + ((choice == 1) ? i.ToString() + newName : newName + i.ToString());
                                        MoveIfNotExists(false, file, name);
                                    }

                                    i++;
                                }
                            } else {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagValidation5;
                            }
                        }
                        catch (Exception e) {
                            log.WriteLog("E : Exception case5(Rename) : ", e.Message + e.StackTrace);
                        }
                        break;

                    // TODO : Upper/lower case
                    case 6:
                        try {
                            tagCase6:
                            Console.WriteLine("\n1 - Tout majuscule\n" +
                                "2 - Tout minuscule\n" +
                                "3 - 1e Lettre majuscule");
                            choice = int.Parse(Console.ReadLine());

                            if (choice < 1 || choice > 3) {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagCase6;
                            } else if (choice == 3) {
                                tagValidation6b:
                                GeneValidation(path, "1e lettre majuscule \n", type, extension, fileSet.Count);
                                input = Console.ReadLine().ToUpper();
                                if (input == "N")
                                    goto tagStart;
                                else if (input == "Y") {
                                    if (type == 1) {
                                        foreach (string file in fileSet) {
                                            string name = Path.GetFileNameWithoutExtension(file);
                                            string tempName = Directory.GetParent(file) + @"\" + char.ToUpper(name[0]) + name.Substring(1) + "_Temp_" + extension;
                                            MoveIfNotExists(true, file, tempName);
                                            name = Directory.GetParent(file) + @"\" + char.ToUpper(name[0]) + name.Substring(1) + extension;
                                            MoveIfNotExists(true, tempName, name);
                                        }
                                    } else {
                                        foreach (string file in fileSet) {
                                            string name = file.Split('\\').Last();
                                            string tempName = Directory.GetParent(file) + @"\" + char.ToUpper(name[0]) + name.Substring(1) + "_Temp_";
                                            MoveIfNotExists(false, file, tempName);
                                            name = Directory.GetParent(file) + @"\" + char.ToUpper(name[0]) + name.Substring(1);
                                            MoveIfNotExists(false, tempName, name);
                                        }

                                    }
                                } else {
                                    Console.WriteLine("Valeur incorrecte");
                                    goto tagValidation6b;
                                }
                            } else {
                                tagValidation6:
                                GeneValidation(path, "Changement MAJ / min\n", type, extension, fileSet.Count);
                                input = Console.ReadLine().ToUpper();
                                if (input == "N")
                                    goto tagStart;
                                else if (input == "Y") {
                                    if (type == 1) {
                                        foreach (var file in fileSet) {
                                            string name = file;
                                            string tempName = Directory.GetParent(file) + @"\" + ((choice == 1) ? Path.GetFileNameWithoutExtension(file).ToUpper() : Path.GetFileNameWithoutExtension(file).ToLower()) + "_Temp_" + extension;
                                            MoveIfNotExists(true, file, tempName);
                                            name = Directory.GetParent(file) + @"\" + ((choice == 1) ? Path.GetFileNameWithoutExtension(file).ToUpper() : Path.GetFileNameWithoutExtension(file).ToLower()) + extension;
                                            MoveIfNotExists(true, tempName, name);
                                        }
                                    } else {
                                        foreach (var file in fileSet) {
                                            string name = file;
                                            string tempName = Directory.GetParent(file) + @"\" + ((choice == 1) ? file.Split('\\').Last().ToUpper() : file.Split('\\').Last().ToLower()) + "_Temp_";
                                            MoveIfNotExists(false, file, tempName);
                                            name = Directory.GetParent(file) + @"\" + ((choice == 1) ? file.Split('\\').Last().ToUpper() : file.Split('\\').Last().ToLower());
                                            MoveIfNotExists(false, tempName, name);
                                        }
                                    }
                                } else {
                                    Console.WriteLine("Valeur incorrecte");
                                    goto tagValidation6;
                                }
                            }

                        }
                        catch (Exception e) {
                            log.WriteLog("E : Exception case6(Upper/lower case) : ", e.Message + e.StackTrace);
                        }
                        break;

                    // Change extension
                    case 7:
                        try {
                            if (type != 1) {
                                Console.WriteLine("Pas de changement d'extension sur un dossier");
                                goto tagStart;
                            }
                            tagCase7:
                            Console.WriteLine("Nouvelle extension : ");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase7;
                            if (!line.StartsWith("."))
                                line = "." + line;

                            tagValidation7:
                            GeneValidation(path, $"Nouvelle extension : {line}\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y") {
                                foreach (var file in fileSet) {
                                    {
                                        string name = Directory.GetParent(file) + @"\" + Path.GetFileNameWithoutExtension(file) + line;
                                        MoveIfNotExists(true, file, name);
                                    }
                                }
                            } else {
                                Console.WriteLine("Valeur incorrecte");
                                goto tagValidation7;
                            }

                        }
                        catch (Exception e) {
                            log.WriteLog("E : Exception case7(extension) : ", e.Message + e.StackTrace);
                        }
                        break;
                }
                CloseCons();
            }
            catch (Exception e) {
                log.WriteLog("E : General exception : ", e.Message + e.StackTrace);
            }

            void MoveIfNotExists(bool _isFile, string _file, string _name) {
                if (_isFile) {
                    if (File.Exists(_name))
                        WarningExist(_file.Split('\\').Last(), _name.Split('\\').Last());
                    else
                        File.Move(_file, _name);
                } else {
                    if (Directory.Exists(_name))
                        WarningExist(_file.Split('\\').Last(), _name.Split('\\').Last());
                    else
                        Directory.Move(_file, _name);
                }
            }

            // Search for forbidden char
            bool ValidLine(string _line) {
                var regex = new Regex("^[/\\:*?\"<>|]$");

                if (regex.IsMatch(_line)) {
                    Console.WriteLine("Présence de caractère interdit ( / \\ : * ? \" < > | )");
                    return false;
                } else
                    return true;
            }

            // Generate text for verification
            void GeneValidation(string _path, string _custom, int _type, string _extension, int _count) {
                Console.WriteLine("\n---------------------\n" +
                    "Validation : \n" +
                    $"Chemin : {_path}\n" +
                    $"{_custom}" +
                    ((_type == 1) ? $"Extension de fichier {_extension}\n" : "Type : dossier\n") +
                    $"Nombre de documents affectés : {_count}\n" +
                    "(Y) Valider, (N) Recommencer");
            }

            // Write warning --> existing file
            void WarningExist(string _fileName, string _finalName) {
                isWarning = true;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                log.WriteLog($"W: Impossible de créer un fichier déjà existant: {_fileName} en {_finalName} ");
                Console.WriteLine($"Impossible de créer un fichier déjà existant : {_fileName} en {_finalName}");
                Console.ResetColor();
            }
        }

        public static void CloseCons() {
            if (isWarning) {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("\nFin d'exécution du programme, voir warnings dans la console");
            } else if (!log.isWritten) {
                File.Delete(log.logName);
                Console.BackgroundColor = ConsoleColor.Green;
                Console.WriteLine("\nFin d'exécution du programme.");
            } else {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFin d'exécution du programme, voir logs pour erreurs dans le dossier : {logPath}");
            }
            //Console.Read();
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
            CloseCons();
            return true;
        }


    }
}

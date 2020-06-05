using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BatchRenamer {
    class Program {
        static readonly ConfigFile ini = new ConfigFile($"{Directory.GetParent(Directory.GetCurrentDirectory()).ToString()}/config.ini");
        public static string logPath = ini.ReadKey("main", "logPath") ?? "\\.";
        static readonly Logger log = new Logger(logPath, "");
        static bool isWarning = false;
        const string incorValue = "Incorrect value";
        static void Main() {
            start:
            Console.WriteLine("New name :");
            string linetst = Console.ReadLine();
            bool validd = ValidLine(linetst);
            Console.WriteLine(validd);
            goto start;

                SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            if (logPath[logPath.Length - 1] != '\\') {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("log File cannot be properly named, logPath must finish by '\' in config.ini");
                Console.ResetColor();
            }

            try {
                tagStart:
                string path = "", input, line, command;
                int modification = 0, type = 0, choice;
                string extension = "";
                string[] commands;

                HashSet<string> fileSet = new HashSet<string>();

                try {
                    tagPath:
                    Console.WriteLine(@"Path (Ex : M:\Documents\Renomage) : ");
                    path = Console.ReadLine();

                    if (!Directory.Exists(path)) {
                        Console.WriteLine($"The following path does not exists : {path}");
                        goto tagPath;
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception path : ", e.Message);
                }

                try {
                    tagType:
                    Console.WriteLine("\nType of document to rename :\n" +
                        "1 - File\n" +
                        "2 - Folder");
                    type = int.Parse(Console.ReadLine());

                    if (type < 1 || type > 2) {
                        Console.WriteLine(incorValue);
                        goto tagType;
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception type : ", e.Message);
                }

                try {
                    if (type == 1) {
                        Console.WriteLine("\nDocuments extension :\n" +
                            "Ex : .png || .txt || .pdf || etc...");
                        extension = Console.ReadLine();

                        if (!extension.StartsWith("."))
                            extension = "." + extension;

                        fileSet = Directory.GetFiles(path, $"*{extension}").ToHashSet();
                    } else
                        fileSet = Directory.GetDirectories(path).ToHashSet();
                    if (fileSet.Count == 0) {
                        Console.WriteLine("No matching documents found");
                    }
                    Console.WriteLine($"\nNumber of {((type == 1) ? "files" : "folder")} matching : {fileSet.Count}");
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception extension : ", e.Message);
                }

                // : entre param |entre cmd
                tagMode:
                Console.WriteLine("\n1 - Wizard (simple)\n" +
                    "2 - One line command (!Warning)");
                choice = int.Parse(Console.ReadLine());
                if (choice < 1 || choice > 2) {
                    Console.WriteLine(incorValue);
                    goto tagMode;
                }
                if (choice == 2) {
                    Console.WriteLine("Reminder :\n" +
@"1- Adding characters
    Parameter: One or more characters
        Option 1 : At the beginning of the chain
        Option 2 : At the end of the chain

 2- Deletion of characters
    Parameter: One or more characters

 3- Deletion of numbers

 4- Characters replacement
    Parameter: old char(s) and new char(s)

 5- Full renaming
    Parameter: New name
        Option 1 : Differentiate names by number at the beginning of the chain
        Option 2 :Differentiate names by number at the end of the chain

 6- Upper/lower case
    Option 1 : All upper case
    Option 2 : All lower case
    Option 3 : First letter upper case

 7- Extension replacement
	/ !\ Obviously not available for folders / !\
    Parameter : new extension
 " +
 "\nEx :  3|1:exa:2|4:-;__   Delete numbers then add 'exa' at the end of the chain then replace '-' by '__'" +
 "\nSyntax : ':' between variables and '|' between each command" +
 "\n!!! Warning : Be sure about your command line, the process will continue until the end without asking validation !!!");
                    command = Console.ReadLine();
                    commands = command.Split('|');
                    Console.WriteLine($"{commands.Length} commands : ");
                    foreach (string cmd in commands)
                        Console.WriteLine($"\n{cmd}");
                    tagValidationCMD:
                    Console.WriteLine("Last validation, are you sure to continue ? (Y/N)");
                    string valid = Console.ReadLine().ToUpper();
                    if (valid == "N")
                        goto tagStart;
                    else if (valid == "Y") {
                        foreach (string cmd in commands) {
                            Modification(fileSet, extension, path, type, cmd);
                        }
                        CloseCons();
                    } else {
                        Console.WriteLine(incorValue);
                        goto tagValidationCMD;
                    }
                }

                try {
                    tagModif:
                    Console.WriteLine("\nModifications :\n" +
                        "1 - Adding characters\n" +
                        "2 - Deletion of characters\n" +
                        "3 - Deletion of numbers\n" +
                        "4 - Characters replacement\n" +
                        "5 - Full renaming\n" +
                        "6 - Upper/Lower case\n" +
                        "7 - Extension replacement");
                    modification = int.Parse(Console.ReadLine());

                    if (modification < 1 || modification > 7) {
                        Console.WriteLine(incorValue);
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
                            Console.WriteLine("Characters to add : ");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase1;
                            Console.WriteLine("\n1 - At the beginning of the chain\n" +
                                "2 - At the end of the chain");
                            choice = int.Parse(Console.ReadLine());

                            if (choice != 1 && choice != 2) {
                                Console.WriteLine(incorValue);
                                goto tagCase1;
                            }

                            tagValidation1:
                            GeneValidation(path, $"Add {line} {((choice == 1) ? "at the beginning of the chain\n" : "at the end of the chain\n")}", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y")
                                Addition(fileSet, type, choice, line, extension);
                            else {
                                Console.WriteLine(incorValue);
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
                            Console.WriteLine("Characters to delete : ");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase2;
                            fileSet.RemoveWhere(f => !f.Split('\\').Last().Contains(line));
                            tagValidation2:
                            GeneValidation(path, $"Remove {line} from documents name\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y")
                                Supression(fileSet, type, line, extension);
                            else {
                                Console.WriteLine(incorValue);
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
                            GeneValidation(path, $"Remove numbers\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y")
                                DeleteNumbers(fileSet, type, extension);
                            else {
                                Console.WriteLine(incorValue);
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
                            Console.WriteLine("\nSyntax : [avant];[apres] || Ex : _;-");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase4;
                            string oldChar = line.Split(';')[0];
                            string newChar = line.Split(';')[1];
                            fileSet.RemoveWhere(f => !f.Split('\\').Last().Contains(oldChar));
                            tagValidation4:
                            GeneValidation(path, $"Replace {oldChar} by {newChar}\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y")
                                Replace(fileSet, type, line, extension);
                            else {
                                Console.WriteLine(incorValue);
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
                            Console.WriteLine("New name :");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase5;
                            Console.WriteLine("\n\nDifférentiates names by :\n" +
                                "1 - Number at the beginning of the chain\n" +
                                "2 - Number at the end of the chain");
                            choice = int.Parse(Console.ReadLine());

                            if (choice != 1 && choice != 2) {
                                Console.WriteLine(incorValue);
                                goto tagCase5;
                            }

                            tagValidation5:
                            GeneValidation(path, $"New name : {line}\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y")
                                Rename(fileSet, type, choice, line, extension);
                            else {
                                Console.WriteLine(incorValue);
                                goto tagValidation5;
                            }
                        }
                        catch (Exception e) {
                            log.WriteLog("E : Exception case5(Rename) : ", e.Message + e.StackTrace);
                        }
                        break;

                    // Upper/lower case
                    case 6:
                        try {
                            tagCase6:
                            Console.WriteLine("\n1 - All upper case\n" +
                                "2 - All lower case\n" +
                                "3 - First letter upper case");
                            choice = int.Parse(Console.ReadLine());

                            if (choice < 1 || choice > 3) {
                                Console.WriteLine(incorValue);
                                goto tagCase6;
                            } else if (choice == 3) {
                                tagValidation6b:
                                GeneValidation(path, "First letter upper case \n", type, extension, fileSet.Count);
                                input = Console.ReadLine().ToUpper();
                                if (input == "N")
                                    goto tagStart;
                                else if (input == "Y")
                                    CaseChange(fileSet, type, choice, extension);
                                else {
                                    Console.WriteLine(incorValue);
                                    goto tagValidation6b;
                                }
                            } else {
                                tagValidation6:
                                GeneValidation(path, "case change\n", type, extension, fileSet.Count);
                                input = Console.ReadLine().ToUpper();
                                if (input == "N")
                                    goto tagStart;
                                else if (input == "Y")
                                    CaseChange(fileSet, type, choice, extension);
                                else {
                                    Console.WriteLine(incorValue);
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
                                Console.WriteLine("No extension change on a folder");
                                goto tagStart;
                            }
                            tagCase7:
                            Console.WriteLine("New extension : ");
                            line = Console.ReadLine();
                            if (!ValidLine(line))
                                goto tagCase7;
                            if (!line.StartsWith("."))
                                line = "." + line;

                            tagValidation7:
                            GeneValidation(path, $"New extension : {line}\n", type, extension, fileSet.Count);
                            input = Console.ReadLine().ToUpper();
                            if (input == "N")
                                goto tagStart;
                            else if (input == "Y")
                                ExtensionChange(fileSet, line);
                            else {
                                Console.WriteLine(incorValue);
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


            void Modification(HashSet<string> _fileSet, string _extension, string _path, int _type, string _cmd) {
                int _modif = 0;
                string _line = "";
                int _choice = 0;
                try {
                    _modif = int.Parse(_cmd.Split(':')[0]);
                    _line = _cmd.Split(':')[1];
                    _choice = int.Parse(_cmd.Split(':')[2]);
                }
                catch (IndexOutOfRangeException outRange) {

                }

                if (_type == 1) {
                    _fileSet = Directory.GetFiles(_path, $"*{_extension}").ToHashSet();
                } else
                    _fileSet = Directory.GetDirectories(_path).ToHashSet();
                if (_fileSet.Count == 0) {
                    CloseCons();
                }

                switch (_modif) {
                    case 1:
                        Addition(_fileSet, _type, _choice, _line, _extension);
                        break;

                    case 2:
                        Supression(_fileSet, _type, _line, _extension);
                        break;

                    case 3:
                        DeleteNumbers(_fileSet, _type, _extension);
                        break;

                    case 4:
                        Replace(_fileSet, _type, _line, _extension);
                        break;

                    case 5:
                        Rename(_fileSet, _type, _choice, _line, _extension);
                        break;

                    case 6:
                        CaseChange(_fileSet, _type, _choice, _extension);
                        break;

                    case 7:
                        ExtensionChange(_fileSet, _line);
                        break;
                }
            }

            void Addition(HashSet<string> _fileSet, int _type, int _choice, string _line, string _extension) {
                try {
                    foreach (var file in _fileSet) {
                        if (_type == 1) {
                            string name = Directory.GetParent(file) + @"\" + ((_choice == 1) ? _line + Path.GetFileNameWithoutExtension(file) : Path.GetFileNameWithoutExtension(file) + _line) + _extension;
                            MoveIfNotExists(true, file, name);
                        } else {
                            string name = Directory.GetParent(file) + @"\" + ((_choice == 1) ? _line + file.Split('\\').Last() : file.Split('\\').Last() + _line);
                            MoveIfNotExists(false, file, name);
                        }
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception Addition() : ", e.Message);
                }
            }

            void Supression(HashSet<string> _fileSet, int _type, string _line, string _extension) {
                try {
                    _fileSet.RemoveWhere(f => !f.Split('\\').Last().Contains(_line));
                    foreach (var file in _fileSet) {
                        if (_type == 1) {
                            string name = Directory.GetParent(file) + @"\" + Path.GetFileNameWithoutExtension(file).Replace(_line, "") + _extension;
                            MoveIfNotExists(true, file, name);
                        } else {
                            string name = Directory.GetParent(file) + @"\" + file.Split('\\').Last().Replace(_line, "");
                            MoveIfNotExists(false, file, name);
                        }
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception Supression() : ", e.Message);
                }
            }

            void DeleteNumbers(HashSet<string> _fileSet, int _type, string _extension) {
                try {
                    _fileSet.RemoveWhere(f => !f.Split('\\').Last().Any(char.IsDigit));
                    Regex regexNumber = new Regex(@"[\d-]");
                    if (_type == 1) {
                        foreach (var file in _fileSet) {
                            string name = Directory.GetParent(file) + @"\" + Regex.Replace(Path.GetFileNameWithoutExtension(file), @"[\d]", "") + _extension;
                            MoveIfNotExists(true, file, name);
                        }
                    } else {
                        foreach (var file in _fileSet) {
                            string name = Directory.GetParent(file) + @"\" + Regex.Replace(file.Split('\\').Last(), @"[\d]", "");
                            MoveIfNotExists(false, file, name);
                        }
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception DeleteNumbers() : ", e.Message);
                }
            }

            void Replace(HashSet<string> _fileSet, int _type, string _line, string _extension) {
                try {
                    string oldChar = _line.Split(';')[0];
                    string newChar = _line.Split(';')[1];
                    _fileSet.RemoveWhere(f => !f.Split('\\').Last().Contains(oldChar));
                    foreach (var file in _fileSet) {
                        if (_type == 1) {
                            string name = Directory.GetParent(file) + @"\" + Path.GetFileNameWithoutExtension(file).Replace(oldChar, newChar) + _extension;
                            MoveIfNotExists(true, file, name);
                        } else {
                            string name = Directory.GetParent(file) + @"\" + file.Split('\\').Last().Replace(oldChar, newChar);
                            MoveIfNotExists(false, file, name);
                        }
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception Replace() : ", e.Message);
                }
            }

            void Rename(HashSet<string> _fileSet, int _type, int _choice, string _line, string _extension) {
                try {
                    int i = 1;
                    foreach (var file in _fileSet) {
                        string name = file;
                        if (_type == 1) {
                            // Rename file
                            name = name.Replace(Path.GetFileNameWithoutExtension(file), (_choice == 1) ? i.ToString() + _line : _line + i.ToString());
                            MoveIfNotExists(true, file, name);
                        } else {
                            // Rename folder
                            name = Directory.GetParent(file) + @"\" + ((_choice == 1) ? i.ToString() + _line : _line + i.ToString());
                            MoveIfNotExists(false, file, name);
                        }

                        i++;
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception Rename() : ", e.Message);
                }
            }

            void CaseChange(HashSet<string> _fileSet, int _type, int _choice, string _extension) {
                try {
                    if (_choice == 3) {

                        if (_type == 1) {
                            foreach (string file in _fileSet) {
                                string name = Path.GetFileNameWithoutExtension(file);
                                string tempName = Directory.GetParent(file) + @"\" + char.ToUpper(name[0]) + name.Substring(1) + "_Temp_" + _extension;
                                MoveIfNotExists(true, file, tempName);
                                name = Directory.GetParent(file) + @"\" + char.ToUpper(name[0]) + name.Substring(1) + _extension;
                                MoveIfNotExists(true, tempName, name);
                            }
                        } else {
                            foreach (string file in _fileSet) {
                                string name = file.Split('\\').Last();
                                string tempName = Directory.GetParent(file) + @"\" + char.ToUpper(name[0]) + name.Substring(1) + "_Temp_";
                                MoveIfNotExists(false, file, tempName);
                                name = Directory.GetParent(file) + @"\" + char.ToUpper(name[0]) + name.Substring(1);
                                MoveIfNotExists(false, tempName, name);
                            }

                        }
                    } else {

                        if (_type == 1) {
                            foreach (var file in _fileSet) {
                                string name = file;
                                string tempName = Directory.GetParent(file) + @"\" + ((_choice == 1) ? Path.GetFileNameWithoutExtension(file).ToUpper() : Path.GetFileNameWithoutExtension(file).ToLower()) + "_Temp_" + _extension;
                                MoveIfNotExists(true, file, tempName);
                                name = Directory.GetParent(file) + @"\" + ((_choice == 1) ? Path.GetFileNameWithoutExtension(file).ToUpper() : Path.GetFileNameWithoutExtension(file).ToLower()) + _extension;
                                MoveIfNotExists(true, tempName, name);
                            }
                        } else {
                            foreach (var file in _fileSet) {
                                string name = file;
                                string tempName = Directory.GetParent(file) + @"\" + ((_choice == 1) ? file.Split('\\').Last().ToUpper() : file.Split('\\').Last().ToLower()) + "_Temp_";
                                MoveIfNotExists(false, file, tempName);
                                name = Directory.GetParent(file) + @"\" + ((_choice == 1) ? file.Split('\\').Last().ToUpper() : file.Split('\\').Last().ToLower());
                                MoveIfNotExists(false, tempName, name);
                            }
                        }

                    }

                }
                catch (Exception e) {
                    log.WriteLog("E : Exception CaseChange() : ", e.Message);
                }
            }

            void ExtensionChange(HashSet<string> _fileSet, string _line) {
                try {
                    foreach (var file in _fileSet) {
                        {
                            string name = Directory.GetParent(file) + @"\" + Path.GetFileNameWithoutExtension(file) + _line;
                            MoveIfNotExists(true, file, name);
                        }
                    }
                }
                catch (Exception e) {
                    log.WriteLog("E : Exception ExtensionChange() : ", e.Message);
                }
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
                // TODO : Add "con","aux","prn","lst","com0"->"com9","lpt0"->"lpt9","nul"
                var regexName = new Regex("con|aux|prn|lst|nul|com[0-9]|lpt[0-9]");
                if (regexName.IsMatch(_line.ToLower())) {
                    Console.WriteLine("Forbidden names (con aux prn lst nul com0->com9 lpt0->lpt9)");
                    return false;
                }

                if (regex.IsMatch(_line)) {
                    Console.WriteLine("Forbidden characters ( / \\ : * ? \" < > | )"); // \
                    return false;
                } else
                    return true;
            }

            // Generate text for verification
            void GeneValidation(string _path, string _custom, int _type, string _extension, int _count) {
                Console.WriteLine("\n---------------------\n" +
                    "Validation : \n" +
                    $"Path : {_path}\n" +
                    $"{_custom}" +
                    ((_type == 1) ? $"File extension : {_extension}\n" : "Type : folder\n") +
                    $"Number of documents affected : {_count}\n" +
                    "(Y) Validate, (N) Restart");
            }

            // Write warning --> existing file
            void WarningExist(string _fileName, string _finalName) {
                isWarning = true;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                log.WriteLog($"W: Impossible to create an existing file : {_fileName} en {_finalName} ");
                Console.WriteLine($"Impossible to create an existing file : {_fileName} en {_finalName}");
                Console.ResetColor();
            }
        }

        public static void CloseCons() {
            if (isWarning) {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("\nEnd of program, see warnings in the console");
            } else if (!log.isWritten) {
                File.Delete(log.logName);
                Console.BackgroundColor = ConsoleColor.Green;
                Console.WriteLine("\nEnd of program.");
            } else {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nEnd of program, see logs for errors in the folder : {logPath}");
            }
            Console.ResetColor();
            Console.WriteLine("Press enter to exit, or '1' to restart");
            string line = Console.ReadLine();
            if (line == "1")
                Main();
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
            CloseCons();
            return true;
        }
    }
}

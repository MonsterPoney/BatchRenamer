using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BatchRenamer {
    class Modification {

        public Modification(HashSet<string> _fileSet, string _extension, string _path, int _type, string _cmd) {
            int _modif = 0;
            string _line = "";
            int _choice = 0;
            try {
                _modif = int.Parse(_cmd.Split(':')[0]);
                _line = _cmd.Split(':')[1];
                _choice = int.Parse(_cmd.Split(':')[2]);
            }
            catch (IndexOutOfRangeException _) { }

            if (_type == 1) {
                _fileSet = Directory.GetFiles(_path, $"*{_extension}").ToHashSet();
            } else
                _fileSet = Directory.GetDirectories(_path).ToHashSet();
            if (_fileSet.Count == 0) {
                Program.CloseCons();
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
                    CaseChange(_fileSet, _type, int.Parse(_line), _extension);
                    break;

                case 7:
                    ExtensionChange(_fileSet, _line);
                    break;
            }
        }

        public static void Addition(HashSet<string> _fileSet, int _type, int _choice, string _line, string _extension) {
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
                Program.log.WriteLog("E : Exception Addition() : ", e.Message);
            }
        }

        public static void Supression(HashSet<string> _fileSet, int _type, string _line, string _extension) {
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
                Program.log.WriteLog("E : Exception Supression() : ", e.Message);
            }
        }

        public static void DeleteNumbers(HashSet<string> _fileSet, int _type, string _extension) {
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
                Program.log.WriteLog("E : Exception DeleteNumbers() : ", e.Message);
            }
        }

        public static void Replace(HashSet<string> _fileSet, int _type, string _line, string _extension) {
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
                Program.log.WriteLog("E : Exception Replace() : ", e.Message);
            }
        }

        public static void Rename(HashSet<string> _fileSet, int _type, int _choice, string _line, string _extension) {
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
                Program.log.WriteLog("E : Exception Rename() : ", e.Message);
            }
        }

        public static void CaseChange(HashSet<string> _fileSet, int _type, int _choice, string _extension) {
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
                Program.log.WriteLog("E : Exception CaseChange() : ", e.Message);
            }
        }

        public static void ExtensionChange(HashSet<string> _fileSet, string _line) {
            try {
                if (!_line.Contains('.'))
                    _line = "." + _line;
                foreach (var file in _fileSet) {
                    {
                        string name = Directory.GetParent(file) + @"\" + Path.GetFileNameWithoutExtension(file) + _line;
                        MoveIfNotExists(true, file, name);
                    }
                }
            }
            catch (Exception e) {
                Program.log.WriteLog("E : Exception ExtensionChange() : ", e.Message);
            }
        }

        public static void MoveIfNotExists(bool _isFile, string _file, string _name) {
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

        // Write warning --> existing file
        public static void WarningExist(string _fileName, string _finalName) {
            Program.isWarning = true;
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Program.log.WriteLog($"W: {String.Format(Program.res.Get("existingFileError"), _fileName, _finalName)}");
            Console.WriteLine(Program.res.Get("existingFileError"), _fileName, _finalName);
            Console.ResetColor();
        }
    }
}

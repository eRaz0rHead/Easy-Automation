//====================================================================== 
//       Easy Automation V2.013 (Stable and Dev Build Compatible) 
//====================================================================== 
 
//This work is under the MIT License. 
//You can find it on https://opensource.org/licenses/MIT or on the GitHub 
//repository https://github.com/TheLinkingTinker/Easy-Automation/blob/master/LICENSE. 
 
//========================= User Configuration ========================= 
//Name of the timer block that is used to cycle with this script. 
string linkedTBName = "Timer Block - Easy Automation"; 
 
//The max number of instructions that a LCD can contain. 
int maxInstructions = 500; 
 
//Set to true and have an LCD named "Debug LCD" to see debug info 
static bool debug = false; 
 
//======== End User Configuration (do not edit below this line) ======== 
 
TimeSpan delayMilliseconds; 
bool resetElapsedTime = true; 
int delayTime; 
 
Dictionary<string, Dictionary<int, List<string>>> code; 
List<string> vars = new List<string>{}; 
List<string> crntStorage; 
List<IMyTerminalBlock> crntBlockList = new List<IMyTerminalBlock>(); 
string currentBlock; 
string oldArgument; 
string oldPrivateText; 
 
string codeLCDName; 
string codeBlockArg; 
List<string> arguVars = new List<string>{}; 
string codeBlockName; 
 
int currentLine; 
int tokenCount; 
int arguLoc; 
string lastToken; 
 
static char[] spaceSplit = new char[]{' ', '\t'}; // Turns out newlines are important :D
static char[] quoteSplit = new char[]{'"'}; 
 
IMyTimerBlock easyAutoTB = null; 
IMyTextPanel codeLCD; 
IMyTextPanel debugLCD; 
 
string errorText = ""; 
 
 
public Program() { 
 
    // The constructor, called only once every session and 
    // always before any other method is called. Use it to 
    // initialize your script. 
 
    if(Storage.LastIndexOf('¶') != -1 && Storage.LastIndexOf('¶') != Storage.Length - 1) 
    { 
        delayMilliseconds = TimeSpan.Parse(Storage.Substring(Storage.LastIndexOf('¶') + 1)); 
        resetElapsedTime = false; 
 
        if(easyAutoTB == null) {easyAutoTB = (IMyTimerBlock)GridTerminalSystem.GetBlockWithName(linkedTBName); } 
        if(easyAutoTB == null) 
        { 
            ShowError(linkedTBName + " Not Found.\nBe sure to enter the name of the Timer Block you " + 
                "wish to use as a cycler in the code editor and hit the Remember & Exit button"); 
            return; 
        } 
        easyAutoTB.ApplyAction("TriggerNow"); 
    } 
} 
 
 
 
public void Save() { 
    // Called when the program needs to save its state. Use 
    // this method to save your state to the Storage field 
    // or some other means. 
 
    Storage = Storage + "§¶"; 
    if(!resetElapsedTime) 
    { 
        Storage = Storage + Convert.ToString(delayMilliseconds); 
    } 
    if(Storage[1] == '¶') Storage = ""; 
} 
 
 
void Main(string argument) 
{ 
//--If reset is passed as the argument then Storage is wiped and program is stoped 
//      (good for recovering from runaways) 
    //___________ 
    if (string.Equals(argument, "RESET", StringComparison.OrdinalIgnoreCase)) 
        { ClearDebugTextPanel(); Storage = ""; return; } 
    if (string.Equals(argument, "CHECK STORAGE", StringComparison.OrdinalIgnoreCase)) 
        { Echo("Storage currently contains\n\"" + Storage + "\""); return; } 
    //========== 
 
//--Sets "easyAutoTB" as the cycling timer block 
    //___________ 
    if(easyAutoTB == null) {easyAutoTB = (IMyTimerBlock)GridTerminalSystem.GetBlockWithName(linkedTBName); } 
    if(easyAutoTB == null) 
    { 
        ShowError(linkedTBName + " Not Found.\nBe sure to enter the name of the Timer Block you " + 
            "wish to use as a cycler in the code editor and hit the Remember & Exit button"); 
        return; 
    } 
    //========== 
 
//--Erases the stored time at the end of storage after loading from save as well as 
// takeing care of a special problem with Detailed info loading after the program runs on a fresh load 
//      (basicly Detailed info for things like Current Position = 0 no mater what on the first run, so we skip that run) 
    //___________ 
    if(Storage.LastIndexOf('¶') != -1) 
    { 
        Storage = Storage.Substring(0, Storage.LastIndexOf('§')); 
        easyAutoTB.ApplyAction("TriggerNow"); 
        return; 
    } 
    //========== 
 
//--if the current run is getting heavy then it will stop and continue on in the next frame 
    //__________ 
    if(Runtime.CurrentInstructionCount > maxInstructions) 
    { 
        easyAutoTB.ApplyAction("TriggerNow"); 
        return; 
    } 
    //========== 
 
    if(argument == "restart" && Storage.Length == 0) return; 
 
 
//--sets "debugLCD" as any LCD named "Debug LCD" and displays what is in Storage 
    //___________ 
    if (debug && debugLCD == null) 
    { 
        debugLCD = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Debug LCD"); 
        if(debugLCD == null) 
        { 
            ShowError("LCD named Debug LCD Not Found.\nBe sure to name an LCD \"Debug LCD\" " + 
                               "or set debug to false"); 
            return; 
        }else if(debug) DebugEcho(Storage + "\n"); 
    } 
    //========== 
 
//--If this is the first run (Storage = ""), Initializes and stores the line tracker in Storage at line 1 
//      The line tracker has two parts seperated by a "§" 
//      The first part is the argument and the second part is the line number. eg.    LCDName(CodeBlock)§1 
//--Then it stores the LCD name in the globle string variable "codeLCDName" 
//--Then it stores what is in the round brackets in the globle string variable "codeBlockArg" 
//--Then if codeBlockArg contains round brackets it stores the 
//      contence in the globle string array variable "arguVars" 
//      whether round brackets are found or not, it stores what isn't in brackets in 
//      the globle string variable "codeBlockName" 
    //___________ 
    if (!ProcessArgument(argument)) { return; } 
    //========== 
 
//Sets "codeLCD" as the LCD with code on it 
    //___________ 
    codeLCD = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(codeLCDName); 
    if (codeLCD == null) 
    { 
        ShowError("LCD " + codeLCDName + " not found.\nCheck spelling and case"); 
        return; 
    } 
    //========== 
 
//--Stores the private text of the code LCD in the "code" Dictionary. 
//      The first keys are the names of the code blocks. 
//      The first values are new dictionaries. 
//      These secondary dictionary's keys are the line numbers in the code blocks starting at 1. 
//      These secondary dictionary's values are lists containing the tokens in each line. 
//Tokens are: 
//      -The statement types (if, else, when, delay, write, writeline, writenew, clear, run, 
//                                           end, stop, and (actions and slider types)) 
//      -Detailed info types such as "Current position" 
//      -Names of blocks 
//      -Oporators such as ">", "<", and "=" 
//      -Seporators such as "of" and "to" 
//      -Values such as a number or a true, false, on, off 
//--If the code dictionary has been initiated in a previous run and nothing has changed in the private text 
//      of the code LCD then this is skipped. 
//___________ 
    string currentPrivateText = codeLCD.GetPrivateText(); 
 
    if(!string.Equals(oldPrivateText, currentPrivateText)) 
    { 
        if (BracketProblem('{', '}', currentPrivateText)) {ShowError("Compile Error\n" + errorText); errorText = ""; return;} 
        if (BracketProblem('(', ')', currentPrivateText)) {ShowError("Compile Error\n" + errorText); errorText = ""; return;} 
        oldPrivateText = currentPrivateText; 
 
        InitiateCodeDictionary(currentPrivateText, ref code); 
 
        if(code.ContainsKey("Variables")) 
        { 
            vars = new List<string>{}; 
            for(int i = 0; i < code["Variables"].Count; i++) vars.Add(code["Variables"][i][0]); 
        } 
    } 
//========== 
 
//--Check if the requested code block is pressent 
//___________ 
    if(!code.ContainsKey(codeBlockName)) 
    { 
        ShowError("The LCD \"" + codeLCDName + "\" does not contain a code block named \"" + codeBlockName +"\"\n"); 
        return; 
    } 
//========== 
 
//puts a list of the elements in Storage in the global List<string> variable "crntStorage" 
//___________ 
    crntStorage = new List<string>(Storage.Split('§')); 
//========== 
 
//puts the current line indication from Storage into the global int variable "currentLine" 
//___________ 
    currentLine = int.Parse(crntStorage[1]); 
    currentBlock = GetCodeBlockName(crntStorage[0]); 
//========== 
 
//If the currentLine is blank then go to the next line 
//___________ 
    if(code[currentBlock][currentLine - 1][0] == "") 
    { 
        Next(currentLine + 1); 
        Main(""); 
    } 
//========== 
 
//storing usefull info about the current line 
//___________ 
    tokenCount = code[codeBlockName][currentLine - 1].Count;                //number of tokens in current line 
    lastToken = code[codeBlockName][currentLine - 1][tokenCount - 1];   //contence of last token in line 
    arguLoc = ArguVarLoc(lastToken);                 //Location of argument variable (*1, *2, etcetera), -1 if not pressent 
//========== 
 
//Handleing the current instruction line 
//___________ 
    string action = code[codeBlockName][currentLine - 1][0]; 
 
    if (string.Equals(action, "DELAY", StringComparison.OrdinalIgnoreCase)) { HandleDelay(); return;} 
    else if (string.Equals(action, "WHEN", StringComparison.OrdinalIgnoreCase)) { HandleIfWhen("when"); return;} 
    else if (action[0] == '@') { HandleBlockRefference(); return;} 
    else if (string.Equals(action, "END", StringComparison.OrdinalIgnoreCase)) { Storage = ""; return; } 
    else if (string.Equals(action, "STOP", StringComparison.OrdinalIgnoreCase)) { Storage = ""; return; } 
    else if (string.Equals(action, "IF", StringComparison.OrdinalIgnoreCase)) { HandleIfWhen("if"); return; } 
    else if (string.Equals(action, "ELSE", StringComparison.OrdinalIgnoreCase)) 
    { 
        int endOfElse = FindBracketPartnerInCode("{"); 
        Next(endOfElse + 1); 
        return; 
    } 
    else if (string.Equals(action, "ROTATE", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(action, "SHORTROTATE", StringComparison.OrdinalIgnoreCase)) { HandleRotate(); return; } 
    else if (string.Equals(action, "CLEAR", StringComparison.OrdinalIgnoreCase)) { HandleWrite("clear"); return; } 
    else if (string.Equals(action, "WRITE", StringComparison.OrdinalIgnoreCase)) { HandleWrite("add"); return; } 
    else if (string.Equals(action, "WRITELINE", StringComparison.OrdinalIgnoreCase)) { HandleWrite("addLn"); return;} 
    else if (string.Equals(action, "WRITENEW", StringComparison.OrdinalIgnoreCase)) { HandleWrite("new"); return;} 
    else if (string.Equals(action, "SHOW", StringComparison.OrdinalIgnoreCase)) { HandleShow(); return;} 
    else if(code[currentBlock][currentLine - 1].Count > 4 && 
    string.Equals(code[currentBlock][currentLine - 1][1], "OF", StringComparison.OrdinalIgnoreCase) && 
    code[currentBlock][currentLine - 1][3] == "=") 
        { HandlePropertySetter(); return;} 
    else if(code[currentBlock][currentLine - 1].Count == 2) 
        { HandleActionSetter(); return;} 
    else if (string.Equals(action, "RUN", StringComparison.OrdinalIgnoreCase) && 
                code[currentBlock][currentLine - 1][2] == "=") { HandleRunWithArgument(); return;} 
    else if (string.Equals(action, "RENAME", StringComparison.OrdinalIgnoreCase)) { HandleRename(); return; } 
    else 
    { 
        Next(currentLine + 1); 
        return; 
    } 
//========== 
} 
 
                //=========================================================== 
                //                                                        METHODS 
                //=========================================================== 
 
 
                                                                //================== 
                                                                //Initialization Methods 
                                                                //================== 
 
 
public void InitiateCodeDictionary(string _privateText, ref Dictionary<string, Dictionary<int, List<string>>> _code) 
{ 
    _code = new Dictionary<string, Dictionary<int, List<string>>>{}; 
    string codeBlockName = ""; 
 
    int a = 0; 
    while((a = _privateText.IndexOf('{', a)) != -1) 
    { 
        var lineDictionary = new Dictionary<int, List<string>>{}; 
        codeBlockName = _privateText.Substring(_privateText.LastIndexOf('@', a) + 1, a - _privateText.LastIndexOf('@', a) - 1).Trim(); 
        string codeBlock = _privateText.Substring(a + 1, FindBracketPartner('{', _privateText, a) - a - 1).Trim() + '\n'; 
 
        int i = 0; 
        int dictKey = 0; 
        while(i < codeBlock.Length - 1) 
        { 
            int startLine = i; 
            int endLine = codeBlock.IndexOf('\n', i); 
                    //pull the current single line out of the code block and into one string 
            var currentLine = codeBlock.Substring(startLine, endLine - startLine).Trim(); 
                    //split the current line into its individual words and operators 
            List<string> writeLine = new List<string>(currentLine.Split(spaceSplit, StringSplitOptions.RemoveEmptyEntries)); 
 
//_________________for empty lines 
            if(writeLine.Count == 0) 
            { 
                        //Set the start of the next line after the end of the current line 
                i = codeBlock.IndexOf('\n', i) + 1; 
 
                writeLine.Add(""); 
 
//for(int k = 0;k<writeLine.Count;k++)Echo("!"+writeLine[k]+"!"); 
                lineDictionary.Add(dictKey, writeLine); 
            } 
//_________________for Block Refferencing 
            else if(currentLine[0] == '@') 
            { 
                        //Set the start of the next line after the end of the current line 
                i = codeBlock.IndexOf('\n', i) + 1; 
 
                        //Parse text into tokens 
                if(writeLine[writeLine.Count - 1].Trim()[0] == '*') 
                    RecompileList("¤", writeLine[writeLine.Count - 1], ref writeLine); 
                else 
                    RecompileList("¤", "¤", ref writeLine); 
//for(int k = 0;k<writeLine.Count;k++)Echo("!"+writeLine[k]+"!"); 
                lineDictionary.Add(dictKey, writeLine); 
            } 
//_________________for LCD Write instructions 
            else if(currentLine.IndexOf('"') != -1) 
            { 
                int startQuote = codeBlock.IndexOf('"', startLine); 
                if(codeBlock.Contains ("\\\"")) 
                { 
                    codeBlock = codeBlock.Replace("\\\"", "¶"); 
                    endLine = codeBlock.IndexOf('\n', i); 
                } 
                int endQuote = codeBlock.IndexOf('"', startQuote + 1); 
 
//_________________for LCD Write Instructions that span muliple lines 
                if(startQuote < endLine && endQuote > endLine) 
                { 
                            //pull the multiple lines out of the code block and into one string 
                    currentLine = codeBlock.Substring(startLine, codeBlock.IndexOf('\n', endQuote) - startLine).Trim(); 
 
                            //Set the start of the next line after the end of the last line in the multi line write Instruction 
                    i = codeBlock.IndexOf('\n', endQuote) + 1; 
 
                    //parse the text onto Tokens 
                    string[] writePars = currentLine.Split(quoteSplit, StringSplitOptions.RemoveEmptyEntries); 
                    writePars[0] = writePars[0].Trim(); 
                    writeLine = new List<string>(writePars[0].Split(spaceSplit, StringSplitOptions.RemoveEmptyEntries)); 
                    if(writePars[1].IndexOf('¶') != -1) 
                        writePars[1] = writePars[1].Replace('¶', '"'); 
                    writeLine.Add(writePars[1]); 
 
                    //if not a variables code block 
                    if(codeBlockName != "Variables") 
                    { 
                        if(writePars.Length == 3 && writePars[2].Trim()[0] == '*') writeLine.Add(writePars[2].Trim()); 
                        RecompileList(writeLine[1], "=", ref writeLine); 
                    } 
 
//for(int k = 0;k<writeLine.Count;k++)Echo("!"+writeLine[k]+"!"); 
                            //store the list of tokens in a dictionary with the line number in the code block as a key 
                                //(starts at 0) 
                    lineDictionary.Add(dictKey, writeLine); 
                } 
//________________for LCD Write Instructions that are on one line 
                else 
                { 
                            //pull the single line out of the code block and into one string 
                    currentLine = codeBlock.Substring(i, endLine - i).Trim(); 
 
                            //Set the start of the next line after the end of the current line 
                    i = codeBlock.IndexOf('\n', i) + 1; 
 
                    //parse the text onto Tokens 
                    string[] writePars = currentLine.Split(quoteSplit, StringSplitOptions.RemoveEmptyEntries); 
                    writePars[0] = writePars[0].Trim(); 
                    writeLine = new List<string>(writePars[0].Split(spaceSplit, StringSplitOptions.RemoveEmptyEntries)); 
                    if(writePars[1].IndexOf('¶') != -1) 
                        writePars[1] = writePars[1].Replace('¶', '"'); 
                    writeLine.Add(writePars[1]); 
 
                    //if not a Variables code block 
                    if(codeBlockName != "Variables") 
                    { 
                        if(writePars.Length == 3 && writePars[2].Trim()[0] == '*') writeLine.Add(writePars[2].Trim()); 
                        if(String.Equals(writeLine[0], "RENAME", StringComparison.OrdinalIgnoreCase)) 
                        { 
                            if(writeLine.IndexOf("=") != -1) RecompileList(writeLine[0], "=", ref writeLine); 
                            if(writeLine.IndexOf("+") != -1) RecompileList(writeLine[0], "+", ref writeLine); 
                            if(writeLine.IndexOf("-") != -1) RecompileList(writeLine[0], "-", ref writeLine); 
                        }else RecompileList(writeLine[1], "=", ref writeLine); 
                    } 
 
//for(int k = 0;k<writeLine.Count;k++)Echo("!"+writeLine[k]+"!"); 
                            //store the list of tokens in a dictionary with the line number in the code block as a key 
                            //(starts at 0) 
                    lineDictionary.Add(dictKey, writeLine); 
                } 
            } 
//________________for Instructions that have no quotation marks 
            else 
            { 
 
                bool isMath = false; 
 
                        //Set the start of the next statement after the end of the current statement 
                        //and Tokenization of math type variables 
                if(codeBlockName == "Variables" && 
                writeLine.Count >= 3 && 
                writeLine[2].Length >= 4 && 
                string.Equals(writeLine[2].Substring(0, 4), "MATH", StringComparison.OrdinalIgnoreCase)) 
                { 
                    isMath = true; 
                    i = codeBlock.IndexOf('\n', FindBracketPartner('(', codeBlock, codeBlock.IndexOf('(', i))) + 1; 
                    endLine = i - 1; 
                    currentLine = codeBlock.Substring(startLine, endLine - startLine).Trim(); 
                    StringBuilder builder = new StringBuilder(currentLine); 
                    builder.Replace("\n", " "); 
                    builder.Replace("(", " ( "); 
                    builder.Replace(")", " ) "); 
                    builder.Replace("+", " + "); 
                    builder.Replace("-", " - "); 
                    builder.Replace("/", " / "); 
                    builder.Replace("*", " * "); 
                    currentLine = builder.ToString().Trim(); 
                    writeLine = new List<string>(currentLine.Split(spaceSplit, StringSplitOptions.RemoveEmptyEntries)); 
                } 
                else 
                { 
                    i = codeBlock.IndexOf('\n', i) + 1; 
                } 
 
                        //parse the text into Tokens 
                string spliter = ""; 
                if(writeLine.Count > 1 && string.Equals(writeLine[1], "TO", StringComparison.OrdinalIgnoreCase)) 
                { 
                    writeLine[1] = writeLine[1].ToLower(); 
                    spliter = "to"; 
                } 
                else spliter = "of"; 
 
                if(writeLine.Contains("=") || writeLine.Contains("<") || writeLine.Contains(">") || writeLine.Contains("!=") || 
                   writeLine.Contains("<<") || writeLine.Contains(">>")) 
                { 
                    writeLine = new List<string>(currentLine.Split(spaceSplit, StringSplitOptions.RemoveEmptyEntries)); 
 
                            //if not a variable 
                    if(writeLine[1] != "=") 
                    { 
                        if(writeLine.Contains("=")) 
                        { 
                            if(string.Equals(writeLine[0], "RUN", StringComparison.OrdinalIgnoreCase)) 
                                RecompileList(writeLine[0], "=", ref writeLine); 
                            else 
                                RecompileList(spliter, "=", ref writeLine); 
 
                            if(writeLine[writeLine.Count - 1].Trim()[0] == '*') 
                                RecompileList("=", writeLine[writeLine.Count - 1], ref writeLine); 
                            else 
                                RecompileList("=", "¤", ref writeLine); 
                        } 
                        if(writeLine.Contains("!=")) 
                        { 
                            if(writeLine[writeLine.Count - 1].Trim()[0] == '*') 
                                RecompileList("!=", writeLine[writeLine.Count - 1], ref writeLine); 
                            else 
                                RecompileList("!=", "¤", ref writeLine); 
                        } 
                        if(writeLine.Contains("<")) RecompileList(spliter, "<", ref writeLine); 
                        if(writeLine.Contains("<<")) RecompileList(spliter, "<<", ref writeLine); 
                        if(writeLine.Contains(">")) RecompileList(spliter, ">", ref writeLine); 
                        if(writeLine.Contains(">>")) RecompileList(spliter, ">>", ref writeLine); 
                        if(string.Equals(writeLine[0], "IF", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(writeLine[0], "WHEN", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(writeLine[0], "ELSE", StringComparison.OrdinalIgnoreCase)) 
                        { 
                            writeLine[0] = writeLine[0].ToLower(); 
                            if(writeLine[0] == "if") 
                                RecompileList("if", "of", ref writeLine); 
                            else if(writeLine.Count > 0 && writeLine [0] == "else" && 
                            string.Equals(writeLine[1], "IF", StringComparison.OrdinalIgnoreCase)) 
                            { 
                                writeLine[1] = writeLine[1].ToLower(); 
                                RecompileList("if", "of", ref writeLine); 
                            } 
                            else if(writeLine[0] == "when") 
                                RecompileList("when", "of", ref writeLine); 
                        } 
                        else if(writeLine.Contains(spliter)) RecompileList("¤", spliter,ref writeLine);     //2.002 fix multi word properties 
                    }else 
                    { 
                            //this is for variables that pull values when called 
                        if(writeLine.Contains(spliter)) 
                        { 
                            if(isMath) 
                            { 
                                int m = 0; 
                                List<string> startOporators = new List<string>(){"(", "+", "-", "*", "/"}; 
                                List<string> endOporators = new List<string>(){")", "+", "-", "*", "/"}; 
 
                                while(m != -1) 
                                { 
                                    m = writeLine.IndexOf("of", m + 1); 
                                    int startLength = writeLine.Count; 
                                    if(m != -1) 
                                    { 
                                        LastRecompileListFrom(m, startOporators, ref writeLine); 
                                        int endLength = writeLine.Count; 
                                        m = m - (startLength - endLength); 
                                        RecompileListFrom(m, endOporators, ref writeLine); 
                                    } 
                                } 
                            } 
                            else 
                            { 
                                RecompileList("=", spliter, ref writeLine); 
                                RecompileList(spliter, "¤", ref writeLine); 
                            } 
                        } 
                    } 
                }else 
                { 
//string.Equals(writeLine[0], "FLOATING", StringComparison.OrdinalIgnoreCase) 
                            //-------------------------------------------------------------------------------------------------- 
                            //Parsing special case of action that contains spaces such as "Detect Small Ships" 
                    if(string.Equals(writeLine[0], "Detect") || 
                       string.Equals(writeLine[0], "Force") || 
                       string.Equals(writeLine[0], "IncreaseBlink") ||    //2.002 fix for new milti word actions 
                       string.Equals(writeLine[0], "DecreaseBlink"))    //2.002 fix for new milti word actions 
                    { 
                        if(string.Equals(writeLine[1], "Small") || 
                           string.Equals(writeLine[1], "Large") || 
                           string.Equals(writeLine[1], "Floating")) 
                        { 
                            writeLine[0] = writeLine[0] + " " + writeLine[1] + " " + writeLine[2]; 
                            writeLine.RemoveRange(1, 2); 
                        } 
                        else 
                        { 
                            writeLine[0] = writeLine[0] + " " + writeLine[1]; 
                            writeLine.RemoveAt(1); 
                        } 
                    } 
                                //Parse text into tokens 
                    if(string.Equals(writeLine[0], "SHOW", StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(writeLine[2], "OF", StringComparison.OrdinalIgnoreCase)) 
                    { 
                        if(writeLine[writeLine.Count - 1].Trim()[0] == '*') 
                            RecompileList(writeLine[2], writeLine[writeLine.Count - 1], ref writeLine); 
                        else 
                            RecompileList(writeLine[2], "¤", ref writeLine); 
                    } 
                    else if((string.Equals(writeLine[0], "ROTATE", StringComparison.OrdinalIgnoreCase) || 
                               string.Equals(writeLine[0], "SHORTROTATE", StringComparison.OrdinalIgnoreCase)) && 
                               writeLine.IndexOf("to") != -1) 
                    { 
                        RecompileList(writeLine[0], "to", ref writeLine); 
                    } 
                    else if(writeLine[writeLine.Count - 1].Trim()[0] == '*') 
                        RecompileList(writeLine[0], writeLine[writeLine.Count - 1], ref writeLine); 
                    else 
                        RecompileList(writeLine[0], "¤", ref writeLine); 
                } 
 
//for(int k = 0;k<writeLine.Count;k++)Echo("!"+writeLine[k]+"!"); 
                lineDictionary.Add(dictKey, writeLine); 
            } 
            dictKey++; 
        } 
        _code.Add(codeBlockName, lineDictionary); 
 
//this is changed from a++; 
        a = FindBracketPartner('{', _privateText, a); 
    } 
} 
 
Boolean ProcessArgument(string _arg) 
{ 
    //Storage is used to keep track of the original argument and which line of code is being worked with 
    if (Storage.Length == 0) Storage = (_arg + "§1"); 
 
    //if no argument has been passed to the pb instructs the user how to do so 
    if (Storage == "§1") { 
        ShowError("Pass argument like this: LCD Name(Code Block Name)"); 
        return false; 
    } 
 
    _arg = Storage.Substring(0, Storage.IndexOf('§')); 
    if(_arg.Length != 0 && oldArgument != _arg) 
    { 
        int start = _arg.IndexOf('('); 
        int end = _arg.LastIndexOf(')'); 
        if(start != -1) codeLCDName = _arg.Substring(0, start).Trim(); 
        else 
        { 
            Storage = ""; 
            ShowError("Argument is missing an opening round bracket \"(\""); 
            return false; 
        } 
 
        if(end != -1) codeBlockArg = _arg.Substring(start + 1, end - start -1).Trim(); 
        else 
        { 
            Storage = ""; 
            ShowError("Argument is missing a closing round bracket \")\""); 
            return false; 
        } 
 
        if(codeBlockArg.IndexOf('(') != -1 && codeBlockArg.IndexOf(')') != -1) 
        { 
 
            start = codeBlockArg.IndexOf('('); 
            end = codeBlockArg.LastIndexOf(')'); 
            codeBlockName = codeBlockArg.Substring(0, start).Trim(); 
            arguVars = new List<string>(codeBlockArg.Substring(start + 1, end - start -1).Split(',')); 
            for(int i=0;i<arguVars.Count;i++) arguVars[i] = arguVars[i].Trim(); 
        }else 
        { 
            arguVars = new List<string>(); 
            codeBlockName = codeBlockArg; 
        } 
 
        oldArgument = _arg; 
    } 
 
    return true; 
} 
 
string GetCodeBlockName(string _arg) 
{ 
    int start = _arg.IndexOf('('); 
    int end = _arg.LastIndexOf(')'); 
 
    string cbArg = _arg.Substring(start + 1, end - start -1).Trim(); 
    string cbName = ""; 
 
    if(cbArg.IndexOf('(') != -1 && cbArg.IndexOf(')') != -1) 
    { 
        start = cbArg.IndexOf('('); 
        end = cbArg.LastIndexOf(')'); 
        cbName = cbArg.Substring(0, start).Trim(); 
    }else cbName = cbArg; 
 
    return cbName; 
} 
 
void GetRequiredBlocksByName(string name, ref List<IMyTerminalBlock> selected) 
{ 
    if(name[0] == '(' && name[name.Length - 1] == ')') 
    { 
        IMyBlockGroup foundGroup = GridTerminalSystem.GetBlockGroupWithName (name.TrimStart('(').TrimEnd(')')); 
        if (foundGroup == null) 
                ShowError("group \"" + name + "\" not found. Check case, spelling, and ownership"); 
 
        foundGroup.GetBlocks(selected); 
    } 
    else if (name[0] == '#') 
    { 
                        IMyTerminalBlock foundBlock = GridTerminalSystem.GetBlockWithName(name.Substring(1)); 
        if(foundBlock == null) 
            ShowError("block \"" + name + "\" not found, Check case, spelling, and ownership"); 
 
        selected.Add(foundBlock); 
                    } 
    else 
        GridTerminalSystem.SearchBlocksOfName(name, selected); 
 
    if (selected.Count == 0) 
        ShowError(name + " not found, Check case, spelling, and ownership"); 
} 
 
List<string> HandleDynamicVariables() 
{ 
    List<string> values = new List<string>{}; 
    string value = code[currentBlock][currentLine - 1][tokenCount - 2]; 
 
    if (arguVars.Count >= arguLoc && arguVars[arguLoc - 1] != "*") value = arguVars[arguLoc - 1]; 
    if(vars.Contains(value)) values = HandleVariables(value); 
    else values.Add(value); 
 
    return values; 
} 
 
List<string> HandleVariables(string varName) 
{ 
    List<string> values = new List<string>{}; 
    int varLoc = vars.IndexOf(varName); 
    if(code["Variables"][varLoc].Count == 5) 
        values = GetSignals(code["Variables"][varLoc][4], code["Variables"][varLoc][2]); 
    else if( code["Variables"][varLoc].Count >= 3 && 
                code["Variables"][varLoc][2].Length >= 4 && 
                string.Equals(code["Variables"][varLoc][2].Substring(0, 4), "MATH", StringComparison.OrdinalIgnoreCase)) 
    { 
        values.Add(GetSolution(code["Variables"][varLoc])); 
    } 
    else 
        values.Add(code["Variables"][varLoc][2]); 
    return values; 
} 
 
List<string> GetSignals(string blockName, string property) 
{ 
    List<string> signals = new List<string>{}; 
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>{}; 
 
    GetRequiredBlocksByName(blockName, ref blocks); 
 
    for(int i = 0; i < blocks.Count; i++) 
    { 
        string signal = GetValue(blocks[i], property); 
        if(signal == "") signal = GetDetailedInfoValue(blocks[i], property); 
        if(signal == "") Echo(blocks[i].DefinitionDisplayNameText + " does not have a\n\"" + property + "\" value\n"); 
        signals.Add(signal); 
    } 
    return signals; 
} 
 
string GetSolution(List<string> _varStatement) 
{ 
    List<string> equasion = new List<string>(); 
 
    int a = 4; 
    while(a < _varStatement.Count - 1) 
    { 
        equasion.Add(_varStatement[a]); 
        a++; 
    } 
    return MathFunc(equasion)[0]; 
} 
 
List<string> MathFunc(List<string> _equasion) 
{ 
    if (_equasion.Contains("¶DbyZ")) return _equasion; 
    int bPartner = 0; 
    List<string> newEquasion = new List<string>(); 
 
        //if the equasion does not contain a "(" then do the math and return a list with the answer in position 0 
    if( !_equasion.Contains("(") ) 
    { 
//string check = ""; 
//foreach(string i in _equasion) check += " " + i + " "; 
//Echo(check + " = "); 
        _equasion = SolveAlgibra(_equasion); 
//Echo(_equasion[0]); 
        return _equasion; 
    } 
 
        //if the equasion does contain at least one "(" 
    while (_equasion.IndexOf("(") > -1) 
    { 
        List<string> result = new List<string>(); 
        List<string> subEquasion = new List<string>(); 
 
            //get the indexes of the start and end of the first bracket pair encountered from left to right 
        int bStart = _equasion.IndexOf("("); 
        bPartner = FindBracketPartner("(", _equasion, bStart); 
 
            //assign the contence of that bracket pair to a new list 
        for(int i = bStart + 1; i < bPartner; i++) subEquasion.Add(_equasion[i]); 
        string solution = MathFunc(subEquasion)[0]; 
 
            //INCEPTION!!! BWAAAAO 
        for(int i = 0; i < bStart; i++) result.Add(_equasion[i]); 
        result.Add(solution); 
        for(int i = bPartner + 1; i < _equasion.Count; i++) result.Add(_equasion[i]); 
 
        _equasion = new List<string>(result); 
    } 
    _equasion = MathFunc(_equasion); 
    return _equasion; 
} 
 
List<string> SolveAlgibra(List<string> _equasion) 
{ 
    int b = 0; 
        while(b < _equasion.Count && (b = _equasion.IndexOf("of", b)) > -1) 
        { 
            _equasion[b] = GetSignals(_equasion[b + 1], _equasion[b - 1])[0]; 
            _equasion.RemoveAt(b + 1); 
            _equasion.RemoveAt(b - 1); 
        } 
 
        b = 0; 
        while(b < _equasion.Count && (b = _equasion.IndexOf("*", b)) > -1) 
        { 
            _equasion[b] = (FloatParseValue(_equasion[b - 1]) * FloatParseValue(_equasion[b + 1])).ToString(); 
            _equasion.RemoveAt(b + 1); 
            _equasion.RemoveAt(b - 1); 
        } 
        b = 0; 
        while(b < _equasion.Count && (b = _equasion.IndexOf("/", b)) > -1) 
        { 
            if(_equasion[b + 1] == "0") 
            { 
                Storage = "Devide By Zero Error"; 
                Echo("This Automation has stoped due to an attempt to devide by Zero"); 
                _equasion[b] = "¶DbyZ"; 
            } 
            else 
                _equasion[b] = (FloatParseValue(_equasion[b - 1]) / FloatParseValue(_equasion[b + 1])).ToString(); 
 
            _equasion.RemoveAt(b + 1); 
            _equasion.RemoveAt(b - 1); 
        } 
        b = 0; 
        while(b < _equasion.Count && (b = _equasion.IndexOf("+", b)) > -1) 
        { 
            _equasion[b] = (FloatParseValue(_equasion[b - 1]) + FloatParseValue(_equasion[b + 1])).ToString(); 
            _equasion.RemoveAt(b + 1); 
            _equasion.RemoveAt(b - 1); 
        } 
        b = 0; 
        while(b < _equasion.Count && (b = _equasion.IndexOf("-", b)) > -1) 
        { 
            _equasion[b] = (FloatParseValue(_equasion[b - 1]) - FloatParseValue(_equasion[b + 1])).ToString(); 
            _equasion.RemoveAt(b + 1); 
            _equasion.RemoveAt(b - 1); 
        } 
            //_equasion now holds the answer in position 0 
        return _equasion; 
} 
 
 
 
                                                        //==================== 
                                                        //Action Handler Methods 
                                                        //==================== 
 
void HandleDelay() 
{ 
    List<string> values = new List<string>{}; 
 
    if(lastToken.IndexOf('*') == 0) values = HandleDynamicVariables(); 
    else if(vars.Contains(lastToken)) values = HandleVariables(lastToken); 
    else values.Add(lastToken); 
 
    delayTime = IntParseValue(values[0]); 
    if (delayTime < 1) delayTime = 1; 
 
    if (resetElapsedTime == true) 
    { 
        resetElapsedTime = false; 
        delayMilliseconds = new System.TimeSpan(0); 
        //ElapsedTime = new System.TimeSpan(0); 
    } else delayMilliseconds += Runtime.TimeSinceLastRun; 
 
    //delayMilliseconds += ElapsedTime; 
    if (delayMilliseconds < TimeSpan.FromMilliseconds (delayTime)) 
    { 
        //if (debug) DebugEcho(Convert.ToString(delayMilliseconds) + "\n"+" of " + value + " milliseconds delayed\n"); 
        if (debug) Echo(Convert.ToString(delayMilliseconds)+ "\n"+" of " + values[0] + " milliseconds delayed"); 
        easyAutoTB.ApplyAction("TriggerNow"); 
        return; 
    } 
    //if (debug) DebugEcho(Convert.ToString(Convert.ToString(delayMilliseconds) + "\n"+" of " + value + " milliseconds delayed\n")); 
    if (debug) Echo(Convert.ToString(delayMilliseconds)+ "\n"+" of " + values[0] + " milliseconds delayed"); 
    delayMilliseconds = new TimeSpan(); 
    resetElapsedTime = true; 
 
    Next(currentLine + 1); 
    return; 
} 
 
void HandleBlockRefference() 
{ 
    List<string> blockRefs = new List<string>{}; 
 
    if(lastToken.IndexOf('*') == 0) blockRefs = HandleDynamicVariables(); 
    else blockRefs.Add(lastToken); 
 
    string blockRef = blockRefs[0]; 
    blockRef = blockRef.Trim('@'); 
    if(blockRef.IndexOf('(') != -1 && blockRef.IndexOf(')') != -1) 
    { 
        var nextLCD = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(blockRef.Substring(0,blockRef.IndexOf('(')).Trim()); 
        if(nextLCD == null) 
        { 
            ShowError("The LCD" + blockRef.Substring(0,blockRefs[0].IndexOf('(')).Trim() + " does not exist.\nYour attempt to refference another LCD on line " + currentLine + " has failed"); 
            return; 
        } 
    }else blockRef = codeLCDName + "(" + blockRef + ")"; 
 
    StringBuilder storageBuilder = new StringBuilder(); 
    int crntBlkLngth = code[currentBlock].Count; 
    if(currentLine < crntBlkLngth) 
    { 
        if(blockRef != crntStorage[0]) 
        { 
            storageBuilder.Append(blockRef).Append('§').Append('1').Append('§') 
                                    .Append(crntStorage[0]).Append('§').Append(Convert.ToString(currentLine + 1)); 
            for(int i = 2; i < crntStorage.Count; i++) storageBuilder.Append('§').Append(crntStorage[i]); 
        } 
        else 
        { 
            storageBuilder.Append(crntStorage[0]).Append('§').Append('1'); 
            for(int i = 2; i < crntStorage.Count; i++) storageBuilder.Append('§').Append(crntStorage[i]); 
        } 
 
        Storage = Convert.ToString(storageBuilder); 
        Main(""); 
        //easyAutoTB.ApplyAction("TriggerNow"); 
        //return; 
    }else 
    { 
        storageBuilder.Append(blockRef).Append('§').Append('1'); 
        for(int i = 2; i < crntStorage.Count; i++) storageBuilder.Append('§').Append(crntStorage[i]); 
        Storage = Convert.ToString(storageBuilder); 
        Main(""); 
        //easyAutoTB.ApplyAction("TriggerNow"); 
        //return; 
    } 
} 
 
void HandleIfWhen(string ifWhen) 
{ 
    List<string> reqVals = new List<string>{}; 
 
    if(lastToken.IndexOf('*') == 0) reqVals = HandleDynamicVariables(); 
    else if(vars.Contains(lastToken)) reqVals = HandleVariables(lastToken); 
    else reqVals.Add(lastToken); 
 
    int isElseIf = 0; 
    if(string.Equals(code[currentBlock][currentLine - 1][0], "ELSE", StringComparison.OrdinalIgnoreCase)) 
    { 
        isElseIf = 1; 
    } 
 
    var actValList = new List<string>{}; 
    string itemName = code[codeBlockName][currentLine - 1][isElseIf + 3]; 
 
    actValList = GetSignals(itemName, code[codeBlockName][currentLine - 1][isElseIf + 1]); 
 
    string oper = code[codeBlockName][currentLine - 1][isElseIf + 4]; 
    if(oper == "=" || oper == "!=") 
    { 
        reqVals[0] = MaybeBool(reqVals[0]); 
         
        for(int i = 0; i < actValList.Count; i++) 
        { 
            if((actValList[i][0] - 48) > 9 || (actValList[i][0] - 48) < 0) //if the first char of actVal is not a number 0-9 
            { 
                if(string.Equals(actValList[i], reqVals[0], StringComparison.OrdinalIgnoreCase) && oper == "=") 
                { 
                    Next(currentLine + 1); 
                    return; 
                }else if(!string.Equals(actValList[i], reqVals[0], StringComparison.OrdinalIgnoreCase) && oper == "!=") 
                { 
                    Next(currentLine + 1); 
                    return; 
                }else if(ifWhen == "when" && i == actValList.Count - 1) 
                { 
                    easyAutoTB.ApplyAction("TriggerNow"); 
                    return; 
                }else if(ifWhen == "if" && i == actValList.Count - 1) 
                { 
                    IfCascade(); 
                    return; 
                } 
            }else 
            { 
                if(FloatParseValue(reqVals[0]) == FloatParseValue(actValList[i]) && oper == "=") 
                { 
                    Next(currentLine + 1); 
                    return; 
                }else if (FloatParseValue(reqVals[0]) != FloatParseValue(actValList[i]) && oper == "!=") 
                { 
                    Next(currentLine + 1); 
                    return; 
                }else if (ifWhen == "when" && i == actValList.Count - 1) 
                { 
                    easyAutoTB.ApplyAction("TriggerNow"); 
                    return; 
                }else if(ifWhen == "if" && i == actValList.Count - 1) 
                { 
                    IfCascade(); 
                    return; 
                } 
            } 
        } 
    } 
    else if(oper == ">" || oper == ">>") 
    { 
        float reqVal = FloatParseValue(reqVals[0]); 
        for(int i = 0; i < reqVals.Count; i++) 
        { 
            float fVal = FloatParseValue(reqVals[i]); 
            if (oper == ">>" && fVal > reqVal) reqVal = fVal; 
            else if (oper == ">" && fVal < reqVal) reqVal = fVal; 
        } 
 
        for(int i = 0; i < actValList.Count; i++) 
        { 
            if(FloatParseValue(actValList[i]) > reqVal) 
            { 
                Next(currentLine + 1); 
                return; 
            }else if(ifWhen == "when" && i == actValList.Count - 1) 
            { 
                easyAutoTB.ApplyAction("TriggerNow"); 
                return; 
            }else if(ifWhen == "if" && i == actValList.Count - 1) 
            { 
                IfCascade(); 
                return; 
            } 
        } 
    } 
    else if(oper == "<" || oper == "<<") 
    { 
        float reqVal = FloatParseValue(reqVals[0]); 
        for(int i = 0; i < reqVals.Count; i++) 
        { 
            float fVal = FloatParseValue(reqVals[i]); 
            if (oper == "<<" && fVal < reqVal) reqVal = fVal; 
            else if (oper == "<" && fVal > reqVal) reqVal = fVal; 
        } 
 
        for(int i = 0; i < actValList.Count; i++) 
        { 
            if(FloatParseValue(actValList[i]) < reqVal) 
            { 
                Next(currentLine + 1); 
                return; 
            }else if(ifWhen == "when" && i == actValList.Count - 1) 
            { 
                easyAutoTB.ApplyAction("TriggerNow"); 
                return; 
            }else if(ifWhen == "if" && i == actValList.Count - 1) 
            { 
                IfCascade(); 
                return; 
            } 
        } 
    } 
} 
 
void IfCascade() 
{ 
    int endOfIf = FindBracketPartnerInCode("{"); 
 
    if(code[currentBlock].Count == endOfIf) 
    { 
        Next(endOfIf); 
    } 
    else if(string.Equals(code[currentBlock][endOfIf][0], "ELSE", StringComparison.OrdinalIgnoreCase)) 
    { 
        if(code[currentBlock][endOfIf].Count > 1 && 
        string.Equals(code[currentBlock][endOfIf][1], "IF", StringComparison.OrdinalIgnoreCase)) 
        { 
            currentLine = endOfIf + 1; 
            lastToken = code[currentBlock][currentLine - 1][code[currentBlock][currentLine - 1].Count - 1]; 
            HandleIfWhen("if"); 
        }else 
        { 
            Next(endOfIf + 3); 
        } 
    }else{ 
         Next(endOfIf + 1);} 
} 
 
void HandleRunWithArgument() 
{ 
    List<string> runArgs = new List<string>{}; 
 
    if(lastToken.IndexOf('*') == 0) runArgs = HandleDynamicVariables(); 
    else if(vars.Contains(lastToken)) runArgs = HandleVariables(lastToken); 
    else runArgs.Add(lastToken); 
 
    var progBlock = (IMyProgrammableBlock)GridTerminalSystem.GetBlockWithName(code[currentBlock][currentLine - 1][1]); 
    if(progBlock == null) 
    { 
        ShowError("The Programming Block " + code[currentBlock][currentLine - 1][1] + " does not exist.\nYour attempt to run another programming block with an argument on line " + currentLine + " has failed."); 
        return; 
    } 
 
    bool ret = progBlock.TryRun(runArgs[0]); 
    if(!ret) 
        Echo("Failed to run " + code[currentBlock][currentLine - 1][1] + " With the argument: " + runArgs[0]); 
 
    Next(currentLine + 1); 
    return; 
 
} 
 
void HandleRotate() 
{ 
    List<string> toAngles = new List<string>{}; 
    if(code[codeBlockName][currentLine - 1].Count > 4 && 
       code[codeBlockName][currentLine - 1][4].IndexOf('*') == 0) 
    { 
        arguLoc = ArguVarLoc(code[codeBlockName][currentLine - 1][4]); 
        if(arguVars.Count >= arguLoc) toAngles = HandleDynamicVariables(); 
    }else if(vars.Contains(code[codeBlockName][currentLine - 1][3])) toAngles = HandleVariables(code[codeBlockName][currentLine - 1][3]); 
    else toAngles.Add(code[codeBlockName][currentLine - 1][3]); 
 
    string atSpeed = "30"; 
    if(code[codeBlockName][currentLine - 1].Count > 5) 
    { 
        atSpeed = lastToken; 
 
        if(lastToken.IndexOf('*') == 0) 
        { 
            arguLoc = ArguVarLoc(lastToken); 
            atSpeed = HandleDynamicVariables()[0]; 
        }else if(vars.Contains(lastToken)) atSpeed = HandleVariables(lastToken)[0]; 
    } 
 
    var blockList = new List<IMyTerminalBlock>{}; 
    string itemName = code[currentBlock][currentLine - 1][1]; 
 
    GetRequiredBlocksByName(itemName, ref blockList); 
 
    int toAng = IntParseValue(toAngles[0]); 
    float atSpd = FloatParseValue(atSpeed); 
    for(int i = 0; i < blockList.Count; i++) 
    { 
        if(blockList[i] is IMyMotorStator) 
        { 
            blockList[i].SetValueFloat("UpperLimit", toAng); 
            blockList[i].SetValueFloat("LowerLimit", toAng); 
            blockList[i].SetValueFloat("UpperLimit", toAng); 
            if(string.Equals(code[currentBlock][currentLine - 1][0], "SHORTROTATE", StringComparison.OrdinalIgnoreCase)) 
            { 
                int curAng = IntParseValue(GetDetailedInfoValue(blockList[i], "Current angle")); 
                int dif = toAng - curAng; 
                if ((dif >= 0 && dif < 180) || dif <= -180) atSpd = System.Math.Abs(atSpd); 
                else atSpd = System.Math.Abs(atSpd) * -1; 
            } 
            blockList[i].SetValueFloat("Velocity", atSpd); 
        } 
    } 
    Next(currentLine + 1); 
    return; 
} 
 
void HandleWrite(string writeType) 
{ 
    string text = ""; 
 
    if(lastToken.IndexOf('*') == 0) text = HandleDynamicVariables()[0]; 
    else if(vars.Contains(lastToken)) text = HandleVariables(lastToken)[0]; 
    else text = lastToken; 
 
    if(text.IndexOf('\\') != -1) 
    { 
        text = text + ' '; 
        int a = 0; 
        while((a = text.IndexOf('\\', a)) != -1) 
        { 
            string v = text.Substring(a + 1, text.IndexOfAny(new char[]{' ','\n'}, a) - a); 
 
            for (int i = 0; i < vars.Count; i++) 
            { 
                if(v.Trim() == vars[i]) {text = text.Replace('\\' + v, HandleVariables(vars[i])[0] + v[v.Length - 1]); break;} 
            } 
 
            a++; 
        } 
    } 
 
    string panelName; 
    if(writeType == "clear") 
        panelName = code[currentBlock][currentLine - 1][1]; 
    else 
        panelName = code[currentBlock][currentLine - 1][2]; 
 
    List<IMyTextPanel> panels = new List<IMyTextPanel>{}; 
    List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>{}; 
 
    GetRequiredBlocksByName(panelName, ref blockList); 
 
    if(blockList.Count == 0) 
    { 
        ShowError("No LCD's of name \"" + panelName + "\" exist.\nYour attempt to write to a screen on line " + currentLine + " has failed."); 
        return; 
    } 
 
    for(int i = 0; i < blockList.Count; i++) panels.Add((IMyTextPanel)blockList[i]); 
 
    for(int i = 0; i < panels.Count; i++) 
    { 
        if(writeType == "add") 
            panels[i].WritePublicText(text, true); 
        else if(writeType == "addLn") 
            panels[i].WritePublicText('\n' + text, true); 
        else if(writeType == "new") 
            panels[i].WritePublicText(text); 
        else if(writeType == "clear") 
            panels[i].WritePublicText(""); 
 
        panels[i].ShowPrivateTextOnScreen(); 
        panels[i].ShowPublicTextOnScreen(); 
        panels[i].UpdateVisual(); 
    } 
 
    Next(currentLine + 1); 
    return; 
} 
 
void HandlePropertySetter() 
{ 
    List<string> setTos = new List<string>{}; 
 
    if(lastToken.IndexOf('*') == 0) setTos = HandleDynamicVariables(); 
    else if(vars.Contains(lastToken)) setTos = HandleVariables(lastToken); 
    else setTos.Add(lastToken); 
 
    string setTo = setTos[0]; 
    var blockList = new List<IMyTerminalBlock>{}; 
    string itemName = code[currentBlock][currentLine - 1][2]; 
 
    GetRequiredBlocksByName(itemName, ref blockList); 
 
    for(int i = 0; i < blockList.Count; i++) 
    { 
        string property = code[currentBlock][currentLine - 1][0]; 
        if(string.Equals(property, "IMAGE", StringComparison.OrdinalIgnoreCase)) 
        { 
            var pnl =  blockList[i] as IMyTextPanel; 
            pnl.ClearImagesFromSelection(); 
            pnl.AddImageToSelection(setTo); 
            pnl.ShowPrivateTextOnScreen(); 
            pnl.ShowTextureOnScreen(); 
            pnl.UpdateVisual(); 
        }else if(property == "Color" || property == "FontColor" || property == "BackgroundColor") 
        { 
            var color = ParseColor(setTo); 
 
            if (blockList[i] is IMyTextPanel) 
            { 
                var pnl = blockList[i] as IMyTextPanel; 
                pnl.SetValue(property, color); 
            } 
            else if (blockList[i] is IMyLightingBlock) 
            { 
                var spt = blockList[i] as IMyLightingBlock; 
                spt.SetValue(property, color); 
            } 
        }else if(GetValue(blockList[i], property) != "") 
        { 
            setTo = MaybeBool(setTo); 
 
            if(setTo == "True" || setTo == "False") 
            { 
                bool setToFloat = bool.Parse(setTo); 
                blockList[i].SetValueBool(property, setToFloat); 
            } 
            else 
            { 
                float setToFloat = FloatParseValue(setTo); 
                blockList[i].SetValueFloat(property, setToFloat); 
            } 
 
        }else 
        { 
            ShowError("the property \"" + property + "\" was not found in the block \"" + code[currentBlock][currentLine - 1][2] + "\""); 
            return; 
        } 
    } 
    Next(currentLine + 1); 
    return; 
} 
 
void HandleActionSetter() 
{ 
    string action = code[currentBlock][currentLine - 1][0]; 
 
    var blockList = new List<IMyTerminalBlock>{}; 
    string itemName = code[currentBlock][currentLine - 1][1]; 
 
    GetRequiredBlocksByName(itemName, ref blockList); 
 
    for(int i = 0; i < blockList.Count; i++) 
    { 
        var actions = new List<ITerminalAction>{}; 
        blockList[i].GetActions(actions); 
        bool isAction = false; 
 
        int ii = 0; 
        while (ii < actions.Count) 
        { 
            if (actions[ii].Id == action) 
            { 
                isAction = true; 
                break; 
            }else ii++; 
        } 
 
        if(isAction == true) blockList[i].ApplyAction(action); 
        else 
        { 
            ShowError("the Action \"" + action + "\" was not found in the block \"" + code[currentBlock][currentLine - 1][1] + "\""); 
            return; 
        } 
    } 
 
    Next(currentLine + 1); 
    return; 
} 
 
void HandleShow() 
{ 
    string blockName = ""; 
 
    if(lastToken.IndexOf('*') == 0) blockName = HandleDynamicVariables()[0]; 
    else if(vars.Contains(lastToken)) blockName = HandleVariables(lastToken)[0]; 
    else blockName = lastToken; 
 
    string propOrAct = code[currentBlock][currentLine - 1][1]; 
    var block = GridTerminalSystem.GetBlockWithName(blockName); 
    if(block == null) 
    { 
        ShowError("No block with name \"" + blockName + "\" exists.\nYour attempt to show its values has failed on line " + currentLine); 
        return; 
    } 
 
    if (string.Equals(propOrAct, "PROPERTIES", StringComparison.OrdinalIgnoreCase)) 
    { 
        var properties = new List<ITerminalProperty>(); 
        block.GetProperties(properties); 
 
        codeLCD.WritePublicText("Properties of \"" + blockName + "\":\n\n"); 
        for(int i = 0; i < properties.Count; i++) 
        { 
            string kind = ""; 
            if(properties[i].TypeName == "Boolean") kind = "True/False"; 
            else if(properties[i].TypeName == "Single") kind = "Number"; 
            else if(properties[i].TypeName == "Color") kind = "Color"; 
 
            if (kind != "") codeLCD.WritePublicText(properties[i].Id + ": " + kind + '\n', true); 
        } 
 
        codeLCD.ShowPrivateTextOnScreen(); 
        codeLCD.ShowPublicTextOnScreen(); 
        codeLCD.UpdateVisual(); 
    } 
    else if (string.Equals(propOrAct, "ACTIONS", StringComparison.OrdinalIgnoreCase)) 
    { 
        var actions = new List<ITerminalAction>(); 
        block.GetActions(actions); 
 
        codeLCD.WritePublicText("Actions of \"" + blockName + "\":\n\n"); 
        for(int i = 0; i < actions.Count; i++) 
        { 
            codeLCD.WritePublicText(actions[i].Id + '\n', true); 
        } 
 
        codeLCD.ShowPrivateTextOnScreen(); 
        codeLCD.ShowPublicTextOnScreen(); 
        codeLCD.UpdateVisual(); 
    } 
    else 
    { 
        ShowError("\"" + propOrAct + "\" is not valid.\nPlease use \"Properties\" or \"Actions\" with the Show statement"); 
        return; 
    } 
 
    Next(currentLine + 1); 
    return; 
} 
 
void HandleRename() 
{ 
    string nameMod = ""; 
 
    if(lastToken.IndexOf('*') == 0) nameMod = HandleDynamicVariables()[0]; 
    else if(vars.Contains(lastToken)) nameMod = HandleVariables(lastToken)[0]; 
    else nameMod = lastToken; 
 
    var blockList = new List<IMyTerminalBlock>{}; 
    string itemName = code[currentBlock][currentLine - 1][1]; 
 
    GetRequiredBlocksByName(itemName, ref blockList); 
 
    if(code[currentBlock][currentLine - 1][2] == "=") 
    { 
        for(int i = 0; i < blockList.Count; i++) blockList[i].SetCustomName(nameMod); 
        Next(currentLine + 1); 
    } 
    else if(code[currentBlock][currentLine - 1][2] == "+") 
    { 
        for(int i = 0; i < blockList.Count; i++) blockList[i].SetCustomName(blockList[i].CustomName + nameMod); 
        Next(currentLine + 1); 
    } 
    else if(code[currentBlock][currentLine - 1][2] == "-") 
    { 
        for(int i = 0; i < blockList.Count; i++) blockList[i].SetCustomName(blockList[i].CustomName.Replace(nameMod, "")); 
        Next(currentLine + 1); 
    } 
    else 
    { 
        ShowError("\"" + code[currentBlock][currentLine - 1][2] + "\" isn't a recognized oporator for the \"rename\" statement"); 
        return; 
    } 
} 
                                                        //============= 
                                                        //Helper Methods 
                                                        //============= 
 
                        //-------------------------------------------------------------------------------- 
                        //for pulling multiword objects into a single location in the string list 
                        //1st argument = start after this string location, can check for more then one string 
                            //by seporating each item with a comma (no spaces), "¤" includes the first position in the list 
                        //2nd argument = end after this string location, can check for more then one string 
                            //by seporating each item with a comma (no spaces), "¤" includes the first position in the list 
                        //3rd argument = List being recompiled 
void RecompileList(string _startAfter, string _endBefor, ref List<string> _reqSentList) 
{ 
 
    int startLoc = 0; 
    int endLoc = _reqSentList.Count - 1; 
    if (endLoc == 0) return; 
    List<string> startAfterList = new List<string>(_startAfter.Split(',')); 
    List<string> endBeforList = new List<string>(_endBefor.Split(',')); 
 
        //Use "¤" to set the start or end locations to inlude the first or last word 
    if(startAfterList.Contains("¤") == false) 
    { 
        int i = 0; 
        while(i < startAfterList.Count && _reqSentList.IndexOf(startAfterList[i]) == -1) i++; 
        startLoc = _reqSentList.IndexOf(startAfterList[i]) + 1; 
    } 
    if(endBeforList.Contains("¤") == false) 
    { 
        int i = 0; 
        while(i < endBeforList.Count && _reqSentList.IndexOf(endBeforList[i]) == -1) i++; 
        endLoc = _reqSentList.IndexOf(endBeforList[i]) - 1; 
    } 
 
    int numOfWords = (endLoc - startLoc) + 1; 
 
    if(numOfWords != 1) 
    { 
        for (int i = 1; i < numOfWords; i++) 
        { 
            _reqSentList[startLoc] = _reqSentList[startLoc] + " " + _reqSentList[startLoc + i]; 
        } 
 
        _reqSentList.RemoveRange(startLoc + 1, numOfWords - 1); 
    } 
} 
 
void RecompileListFrom(int _from, List<string> _endBefor, ref List<string> _reqSentList) 
{ 
 
    int startLoc = 0; 
    int endLoc = _reqSentList.Count - 1; 
    if (endLoc == 0) return; 
 
    startLoc = _from + 1; 
 
        //Use "¤" to set the start location to inlude the first word 
    if(_endBefor.Contains("¤") == false) 
        endLoc = IndexOfAny(_reqSentList, _endBefor, _from) - 1; 
 
    int numOfWords = (endLoc - startLoc) + 1; 
 
    if(numOfWords != 1) 
    { 
        for (int i = 1; i < numOfWords; i++) 
            _reqSentList[startLoc] = _reqSentList[startLoc] + " " + _reqSentList[startLoc + i]; 
 
        _reqSentList.RemoveRange(startLoc + 1, numOfWords - 1); 
    } 
} 
 
void LastRecompileListFrom(int _from, List<string> _startAfter, ref List<string> _reqSentList) 
{ 
 
    int startLoc = 0; 
    int endLoc = _reqSentList.Count - 1; 
    if (endLoc == 0) return; 
 
        //Use "¤" to set the start location to inlude the first word 
    if(_startAfter.Contains("¤") == false) 
        startLoc = LastIndexOfAny(_reqSentList, _startAfter, _from) + 1; 
    endLoc = _from - 1; 
    int numOfWords = (endLoc - startLoc) + 1; 
 
    if(numOfWords != 1) 
    { 
        for (int i = 1; i < numOfWords; i++) 
            _reqSentList[startLoc] = _reqSentList[startLoc] + " " + _reqSentList[startLoc + i]; 
 
        _reqSentList.RemoveRange(startLoc + 1, numOfWords - 1); 
    } 
} 
 
void WriteTextToPanel(IMyTextPanel panel, String text) 
{ 
    panel.WritePublicText(text, true); 
    panel.ShowPrivateTextOnScreen(); 
    panel.ShowPublicTextOnScreen(); 
    panel.UpdateVisual(); 
} 
 
void ClearTextPanel(IMyTextPanel panel) 
{ 
    panel.WritePublicText("", false); 
    panel.ShowPrivateTextOnScreen(); 
    panel.ShowPublicTextOnScreen(); 
    panel.UpdateVisual(); 
} 
 
void DebugEcho(string s) 
{ 
    if (debugLCD == null) 
        Echo(s); 
    else { 
        WriteTextToPanel(debugLCD, s); 
        Echo(s); 
    } 
 
} 
 
void ShowError(String errormsg) 
{ 
    if(Storage == null || Storage == "")DebugEcho(errormsg + "\n"); 
    else DebugEcho("(" + Storage + ")\n" + errormsg + "\n"); 
    Storage = ""; 
    return; 
} 
 
void ClearDebugTextPanel() { 
    if (debugLCD != null) { 
        debugLCD.WritePublicText("", false); 
        debugLCD.ShowPrivateTextOnScreen(); 
        debugLCD.ShowPublicTextOnScreen(); 
        debugLCD.UpdateVisual(); 
    } 
} 
 
int ArguVarLoc(string _lastToken) 
{ 
    if(_lastToken.IndexOf('*') == 0) 
    { 
        return int.Parse(_lastToken.Trim('*')); 
    }else return -1; 
} 
 
string GetHiddenValue(IMyTerminalBlock _block, string _requestedValue) { 
    // Note: Keen will not allow MemberInfo inside scripts, so we can't use reflection. 
    if (_block is IMyShipController) { 
        switch (_requestedValue) { 
            case "IsUnderControl" : return Convert.ToString((_block as IMyShipController).IsUnderControl); 
            // case "ControlWheels" : return Convert.ToString(_block.ControlWheels); 
            // case "ControlThrusters" : return Convert.ToString(_block.ControlThrusters); 
            // case "HandBrake" : return Convert.ToString(_block.HandBrake); 
            // case "DampenersOverride" : return Convert.ToString(_block.DampenersOverride); 
        } 
    } 
     
    if (_block is IMyTextPanel) { 
        IMyTextPanel panel = _block as IMyTextPanel;
        switch (_requestedValue) { 
            case "PublicText" : return panel.GetPublicText(); 
            case "PublicTitle" : return panel.GetPublicTitle(); 
            case "PrivateText" : return panel.GetPrivateText(); 
            case "PrivateTitle" : return panel.GetPrivateTitle(); 
            // case "CurrentlyShownImage" : return _block.CurrentlyShownImage;  
            case "ShowText" : return Convert.ToString(panel.ShowText); 
        } 
    } 
     
    if (_block is IMyShipConnector) { 
        switch (_requestedValue) { 
            // case "ThrowOut" : return Convert.ToString(_block.ThrowOut); 
            // case "CollectAll" : return Convert.ToString(_block.CollectAll); 
            // case "IsLocked" : return Convert.ToString(_block.IsLocked); 
            case "IsConnected" : return Convert.ToString((_block as IMyShipConnector).IsConnected); 
        } 
    } 
         
    return null; 
} 
 
string GetValue(IMyTerminalBlock _block, string _requestedValue) 
{ 
    string value = GetHiddenValue(_block, _requestedValue); 
     
    if (value != null) return value; 
     
    value = ""; 
     
    var properties = new List<ITerminalProperty>(); 
    _block.GetProperties(properties); 
     
    var found = properties.Find( 
        delegate(ITerminalProperty prop) 
        { 
            return prop.Id == _requestedValue; 
        } 
    ); 
     
    if (found != null) { 
        switch (found.TypeName)  
        { 
            case "Single" : 
                value = Convert.ToString(_block.GetValue<float>(_requestedValue)); 
                break; 
            case "String" : 
                value = Convert.ToString(_block.GetValue<String>(_requestedValue)); 
                break; 
            case "Boolean" :
                value = Convert.ToString(_block.GetValue<Boolean>(_requestedValue)); 
                break; 
            case "Color" :
                value = FormatColor(Convert.ToString(_block.GetValue<Color>(_requestedValue))); 
                break; 
        } 
    } 
    return value; 
} 
 
string GetDetailedInfoValue(IMyTerminalBlock _block, string _reqVal) 
{ 
    string value = ""; 
 
    string detInf = _block.DetailedInfo; 
    if(detInf.IndexOf(_reqVal) != -1) 
    { 
        int start = detInf.IndexOf(':', detInf.IndexOf(_reqVal)) + 1; 
        int end = detInf.IndexOf('\n', start) - 1; 
        if(end == -2) end = detInf.Length; 
        value = detInf.Substring(start, end - start).Trim(); 
    } 
    return value; 
} 
 
void Next(int toLine) 
{ 
    int crntBlkLngth = code[currentBlock].Count; 
 
    if(toLine > crntBlkLngth) currentLine = crntBlkLngth; 
 
    StringBuilder storageBuilder = new StringBuilder(); 
    if(currentLine < crntBlkLngth) 
    { 
        storageBuilder.Append(crntStorage[0]).Append('§').Append(Convert.ToString(toLine)); 
        for(int i = 2; i < crntStorage.Count; i++) storageBuilder.Append('§').Append(crntStorage[i]); 
        Storage = Convert.ToString(storageBuilder); 
        Main(""); 
    }else 
    { 
        if(crntStorage.Count > 2) 
        { 
            storageBuilder.Append(crntStorage[2]); 
            for(int i = 3; i < crntStorage.Count; i++) storageBuilder.Append('§').Append(crntStorage[i]); 
            Storage = Convert.ToString(storageBuilder); 
            Main(""); 
        }else 
        { 
            Storage = ""; 
        } 
    } 
} 
 
int IntParseValue(string value) 
{ 
        int result = 0; 
    if(value == "Not pressurized" || value == "-") return result; 
    if(value.IndexOf(' ') != -1) 
    { 
        string[] valUnit = value.Split(' '); 
        if (valUnit[1][0] == 'M') result = ((int)(float.Parse(valUnit[0]) * 1000000 + 0.4999f)); 
        else if (valUnit[1][0] == 'k') result = ((int)(float.Parse(valUnit[0]) * 1000 + 0.4999f)); 
        else if (valUnit[1] == "min") result = ((int)(float.Parse(valUnit[0])  * 60 + 0.4999f)); 
        else if (valUnit[1] == "hours") result = ((int)(float.Parse(valUnit[0])  * 60 * 60 + 0.4999f)); 
        else if (valUnit[1] == "days") result = ((int)(float.Parse(valUnit[0])  * 60 * 60 * 24 + 0.4999f)); 
        else result = (int)(float.Parse(valUnit[0]) + 0.4999f); 
    }else 
    { 
        for (int i = value.Length - 1; i >= 0; i--) 
        { 
            if((value[i] - 48) <= 9 && (value[i] - 48) >= 0) //if current char is a number 0-9 
            { 
                result = (int)(float.Parse(value.Substring(0, i + 1)) + 0.4999f); 
                i = -1; 
            } 
        } 
    } 
        return result; 
} 
 
float FloatParseValue(string value) 
{ 
    float result = 0; 
    if(value == "Not pressurized" || value == "-") return result; 
    if(value.IndexOf(' ') != -1) 
    { 
        string[] valUnit = value.Split(' '); 
        if (valUnit[1][0] == 'M') result = float.Parse(valUnit[0]) * 1000000; 
        else if (valUnit[1][0] == 'k') result = float.Parse(valUnit[0]) * 1000; 
        else if (valUnit[1] == "min") result = float.Parse(valUnit[0])  * 60; 
        else if (valUnit[1] == "hours") result = float.Parse(valUnit[0])  * 60 * 60; 
        else if (valUnit[1] == "days") result = float.Parse(valUnit[0])  * 60 * 60 * 24; 
        else result = float.Parse(valUnit[0]); 
    }else 
    { 
        for (int i = value.Length - 1; i >= 0; i--) 
        { 
            if((value[i] - 48) <= 9 && (value[i] - 48) >= 0) //if current char is a number 0-9 
            { 
                result = float.Parse(value.Substring(0, i + 1)); 
                i = -1; 
            } 
        } 
    } 
 
    return result; 
} 
 
bool AreListsTheSame(List<string> _list1, List<string> _list2) 
{ 
    return _list1.Count == _list2.Count // assumes unique values in each list 
        && new HashSet<string>(_list1).SetEquals(_list2); 
} 
 
int FindBracketPartner(char opener, string text, int start) 
{ 
    int closerLoc = -1; 
    char closer = '-'; 
    if(opener == '{') closer = '}'; 
    else if(opener == '(') closer = ')'; 
    else if(opener == '[') closer = ']'; 
 
    int counter = 1; 
    while(counter > 0 && start < text.Length) 
    { 
        if(text.IndexOf(opener, start + 1) < text.IndexOf(closer, start + 1) && text.IndexOf(opener, start + 1) != -1) 
        { 
            counter++; 
            start = text.IndexOf(opener, start + 1); 
        } 
        else 
        { 
            counter--; 
            start = text.IndexOf(closer, start + 1); 
        } 
        if (counter == 0) closerLoc = start; 
    } 
 
    return closerLoc; 
} 
 
int FindBracketPartner(string opener, List<string> text, int start) 
{ 
    int closerLoc = -1; 
    string closer = "-"; 
    if(opener == "{") closer = "}"; 
    else if(opener == "(") closer = ")"; 
    else if(opener == "[") closer = "]"; 
 
    int counter = 1; 
    while(counter > 0 && start < text.Count) 
    { 
        if(text.IndexOf(opener, start + 1) < text.IndexOf(closer, start + 1) && text.IndexOf(opener, start + 1) != -1) 
        { 
            counter++; 
            start = text.IndexOf(opener, start + 1); 
        } 
        else 
        { 
            counter--; 
            start = text.IndexOf(closer, start + 1); 
        } 
        if (counter == 0) closerLoc = start; 
    } 
 
    return closerLoc; 
} 
 
int FindBracketPartnerInCode(string opener) 
{ 
    int closerLoc = -1; 
    string closer = "-"; 
    if(opener == "{") closer = "}"; 
    else if(opener == "(") closer = ")"; 
    else if(opener == "[") closer = "]"; 
 
    int lineCount = code[currentBlock].Count; 
    int i = currentLine; 
 
    while(i < lineCount) 
    { 
        if (i - 1 == lineCount) ShowError("if statment is missing an opening curly bracket"); 
        else if (code[currentBlock][i][0] == opener) {i++; break;} 
        else if (code[currentBlock][i].Count != 0) ShowError("A \"{\" is required directly after an if statement"); 
        i++; 
    } 
 
    int counter = 1; 
    while(i < lineCount) 
    { 
        if(code[currentBlock][i].Count > 0) 
        { 
            if(code[currentBlock][i][0] == opener) 
                counter++; 
            else if(code[currentBlock][i][0] == closer) 
                counter--; 
        } 
 
        if(counter == 0) 
        { 
            closerLoc = i; 
            i = lineCount; 
        } 
        i++; 
    } 
    return closerLoc + 1; 
} 
 
int LastIndexOfAny(List<string> _lookIn, List<string> _lookFor, int _from) 
{ 
    int lastIndex = -1; 
    int a = _from; 
 
    while((a = a - 1) >= 0) 
    { 
        foreach(string b in _lookFor) 
            if(b == _lookIn[a]) 
                return a; 
    } 
    return lastIndex; 
} 
 
int IndexOfAny(List<string> _lookIn, List<string> _lookFor, int _from) 
{ 
    int lastIndex = -1; 
    int a = _from; 
    while((a = a + 1) <= _lookIn.Count - 1) 
    { 
        foreach(string b in _lookFor) 
            if(b == _lookIn[a]) 
                return a; 
    } 
    return lastIndex; 
} 
 
bool BracketProblem(char opener, char closer, string text) 
{ 
    bool isProb = false; 
    int countOpeners = 0; 
    int countClosers = 0; 
 
    foreach (char c in text) { 
        if (c == opener) countOpeners++; 
        if (c == closer) countClosers++; 
    } 
 
    if (countOpeners != countClosers) 
    { 
        isProb = true; 
        errorText =  "something is wrong with your containment \"" + opener + " " + closer + "\""; 
    } 
 
    return isProb; 
} 
 
string MaybeBool (string text)  
{ 
    string[] truthy = { "ON", "OPEN", "TRUE" }; 
    string[] falsy =  { "OFF", "CLOSE", "FALSE" }; 
     
    if ( Array.Exists(truthy, element => element == text.ToUpper())) return "True"; 
    if ( Array.Exists(falsy, element => element == text.ToUpper())) return "False"; 
     
    return text; 
} 
 
Color ParseColor (string text) 
{ 
    string[] rgb = text.Split(':'); 
    return new Color(int.Parse(rgb[0]),int.Parse(rgb[1]), int.Parse(rgb[1])); 
} 
 
string FormatColor (string colorProperty) 
{ 
    string value = ""; 
    int a = 0; 
    int b = 0; 
    while((a = colorProperty.IndexOf(':', a)) != -1 && b < 3) 
    { 
        value = value + colorProperty.Substring(a, colorProperty.IndexOf(' ', a) - a); 
        b++; 
        a++; 
    } 
    value = value.Trim(':'); 
    return value; 
} 

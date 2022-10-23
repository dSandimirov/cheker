using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Resources;
using System.Text;
using ZennoLab.CommandCenter;
using ZennoLab.Emulation;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.InterfacesLibrary.ProjectModel.Enums;
using System.Threading;
using System.Text.RegularExpressions;

namespace cheker
{
    public class Program : IZennoExternalCode
    {	
        public int Execute(Instance instance, IZennoPosterProjectModel project)
        {
            // read parametrs
            string pathListInput = project.Variables["pathListInput"].Value;
            string pathOutput = project.Variables["pathOutput"].Value;
            string howCheck = project.Variables["howCheck"].Value;
            // account data
            string login = string.Empty;
            string password = string.Empty;
            string idAcc = string.Empty;
            // group data
            string idGroup = string.Empty;
            // names file
            string nameFileBadAccs = @"\bag_accs.txt";
            string nameFileGoodAccs = @"\good_accs.txt";
            string nameFileBadGroup = @"\bag_group.txt";
            string nameFileGoodGroup = @"\good_group.txt";
            // lists
            List<string> inputList = new List<string>();
            List<string> searchTextList = new List<string>();

            // settings instance
            Program.InstanceSettings(instance, project, "", "");

            try
            {
                // check input list
                if (pathListInput == string.Empty)
                {
                    pathListInput = project.Directory + @"\input.txt";
                    project.SendWarningToLog("Path list input empty. Default: \"{project.Directory}\\input.txt\"");
                }
                if (File.Exists(pathListInput))
                {
                    inputList.Clear();
                    inputList = File.ReadAllLines(pathListInput).ToList();
                    project.SendInfoToLog("Read input list: " + pathListInput);
                }
                else
                {
                    throw new Exception("No input file");
                }
                // check output path
                if (pathOutput == string.Empty)
                {
                    pathOutput = project.Directory + @"\output\";
                    project.SendWarningToLog("Path output empty. Default: \"{project.Directory}\\output\"");
                }
                if (!Directory.Exists(pathOutput))
                {
                    Directory.CreateDirectory(pathOutput);
                    project.SendInfoToLog("Path output create: " + pathOutput);
                }
                // check accounts
                if (howCheck == "Accounts")
                {
                    searchTextList.Clear(); searchTextList.Add(@"Этой страницы нет в OK");
                    if (File.Exists(pathOutput + nameFileBadAccs)) File.Delete(pathOutput + nameFileBadAccs);
                    if (File.Exists(pathOutput + nameFileGoodAccs)) File.Delete(pathOutput + nameFileGoodAccs);
                    for (int i = 0; i < inputList.Count; i++)
                    {
                        bool status = false;
                        Program.DisassembleUser(project, inputList.ElementAt(i), out login, out password, out idAcc);
                        idAcc = idAcc.TrimEnd('|');
                        idAcc = idAcc.TrimEnd(';');
                        Program.GoUrl(instance, "https://ok.ru/profile/" + idAcc, 2);
                        status = Program.SearchTextOnPage(instance, searchTextList);

                        if (status)
                        {
                            File.AppendAllText (pathOutput + nameFileBadAccs, inputList.ElementAt(i) + Environment.NewLine);
                            project.SendWarningToLog(idAcc + " -> bad account");
                        }
                        else
                        {
                            File.AppendAllText(pathOutput + nameFileGoodAccs, inputList.ElementAt(i) + Environment.NewLine);
                            project.SendInfoToLog(idAcc + " -> good account");
                        }
                    }
                }
                // check group
                if (howCheck == "Group")
                {
                    searchTextList.Clear(); searchTextList.Add(@"Этой страницы нет в OK");
                    if (File.Exists(pathOutput + nameFileBadGroup)) File.Delete(pathOutput + nameFileBadGroup);
                    if (File.Exists(pathOutput + nameFileGoodGroup)) File.Delete(pathOutput + nameFileGoodGroup);
                    for (int i = 0; i < inputList.Count; i++)
                    {
                        bool status = false;
                        idGroup = inputList.ElementAt(i);
                        Program.GoUrl(instance, "https://ok.ru/group/" + idGroup, 2);
                        status = Program.SearchTextOnPage(instance, searchTextList);

                        if (status)
                        {
                            File.AppendAllText(pathOutput + nameFileBadGroup, inputList.ElementAt(i) + Environment.NewLine);
                            project.SendWarningToLog(idGroup + " -> bad group");
                        }
                        else
                        {
                            File.AppendAllText(pathOutput + nameFileGoodGroup, inputList.ElementAt(i) + Environment.NewLine);
                            project.SendInfoToLog(idGroup + " -> good group");
                        }
                    }
                }
            } catch(Exception ex)
            {
                project.SendErrorToLog(ex.ToString());
            }

            project.SendInfoToLog("Finish!");

            return 0;
        }

        // go url
        public static void GoUrl(Instance instance, string url, short delay)
        {
            Tab tab = instance.ActiveTab;
            if ((tab.IsVoid) || (tab.IsNull)) throw new Exception("error load page");
            if (tab.IsBusy) tab.WaitDownloading();
            tab.Navigate(url, "google.ru");
            if (tab.IsBusy) tab.WaitDownloading();
        }
        // disassembled user
        public static void DisassembleUser(IZennoPosterProjectModel project, string user, out string login, out string password, out string id)
        {
            var splitters = ":".ToCharArray();
            string[] temp_arr = user.Split(splitters);

            if (temp_arr[0].Length != 0) login = temp_arr[0];
            else throw new Exception("error login");
            if (temp_arr[1].Length != 0) password = temp_arr[1];
            else throw new Exception("error password");
            if (temp_arr[2].Length != 0) id = temp_arr[2];
            else throw new Exception("error id");

            // project.SendInfoToLog(user + " disassemble user", true);
        }
        // seqrch text on page
        public static bool SearchTextOnPage(Instance instance, List<string> searchTextList)
        {
            bool status = false;
            Tab tab = instance.ActiveTab;

            for (int i = 0; i < searchTextList.Count; i++)
            {
                string pageText = tab.PageText;
                var pattern = new Regex(searchTextList.ElementAt(i));
                var match = pattern.Match(pageText);
                if ((match.Value) == searchTextList.ElementAt(i))
                {
                    status = true;
                    break;
                }
                else continue;
            }
            return status;
        }
        // instance settings
        public static void InstanceSettings(Instance instance, IZennoPosterProjectModel project, string nameInstance, string proxy)
        {
            instance.ClearCookie();
            instance.ClearCache();

            instance.DownloadActiveX = false;
            instance.DownloadFrame = true;
            instance.DownloadVideos = false;

            instance.IgnoreAdditionalRequests = true;
            instance.IgnoreAjaxRequests = true;
            instance.IgnoreFlashRequests = true;
            instance.IgnoreFrameRequests = true;

            instance.UseJavaApplets = true;
            instance.UseJavaScripts = true;
            instance.UsePlugins = false;
            instance.UseCSS = true;
            instance.UseMedia = false;
            instance.UseAdds = false;
            instance.UsePluginsForceWmode = false;

            instance.LoadPictures = false;
            instance.AllowPopUp = false;
            instance.RunActiveX = false;
            instance.BackGroundSoundsPlay = false;

            instance.MinimizeMemory();

            // Random rnd = new Random();
            // instance.SymbolEmulationDelay = rnd.Next(250, 500);
            // instance.FieldEmulationDelay = rnd.Next(1000, 3000);
            // instance.UseFullMouseEmulation = true;
            // instance.EmulationLevel = "SuperEmulation";

            // instance.SetWindowSize(1000, 800);

            // instance.SetProxy(proxy);

            // instance.AddToTitle(nameInstance);

            // project.Profile.AcceptLanguage = "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3";

            project.SendInfoToLog("set instance settings", true);
        }
    }
}
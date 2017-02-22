using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Winter
{
    internal sealed class Plex : MediaPlayer
    {
        //the black right-pointing triangle aka a play button
        private const char PlexRecogPattern = '\u25b6';
        private Process[] Processes;

        public Plex()
        {
            this.SetProcesses();
        }

        private void SetProcesses()
        {
            Process[] Chrome = Process.GetProcessesByName("chrome");
            Process[] Firefox = Process.GetProcessesByName("firefox");
            this.Processes = Chrome.Union(Firefox).ToArray();
        }

        private void SetTabs(Process p)
        {
            AutomationElement window = AutomationElement.FromHandle(p.MainWindowHandle);

            if (this.tabs == null)
            {
                this.tabs = window.FindAll(TreeScope.Subtree,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
            }
        }

        private AutomationElementCollection tabs = null;

        public override void Update()
        {
            if (this.Processes.Length > 0)
            {
                foreach (Process proc in this.Processes)
                {
                    if (proc.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        this.SetTabs(proc);

                        if (this.tabs != null)
                        {
                            foreach (AutomationElement tab in this.tabs)
                            {
                                try
                                {
                                    string tabTitle = tab.GetCurrentPropertyValue(AutomationElement.NameProperty).ToString();

                                    AutomationElement next = tab.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

                                    if (tabTitle.LastIndexOf(PlexRecogPattern) == 0)
                                    {
                                        if (next != null)
                                        {
                                            string nextContent = next.GetCurrentPropertyValue(AutomationElement.NameProperty).ToString();
                                            if (nextContent != "")
                                            {
                                                // In case a tab is playing any audio its element next to the text is an empty button (actually the speakers icon)
                                                // If this button is "close" (or anything else than "") it's likely that there's no music from this tab and we
                                                // can simply skip the current process.
                                                break;
                                            }
                                        }

                                        if (tabTitle != this.LastTitle)
                                        {
                                            string songTitle = tabTitle.Substring(2, tabTitle.Length - 2);

                                            if (songTitle.LastIndexOf("-", StringComparison.OrdinalIgnoreCase) > 0)
                                            {
                                                // Assuming the video title is "Artist - Song"
                                                string artist = songTitle.Split('-').First().Trim();
                                                string song = songTitle.Split('-').Last().Trim();

                                                TextHandler.UpdateText(song, artist);
                                            }
                                            else
                                            {
                                                TextHandler.UpdateText(songTitle);
                                            }

                                            this.LastTitle = tabTitle;
                                        }

                                        // Since we've found a music playing tab there's no need to look for additional tabs.
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            // No tabs found
                            TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("PlexIsNotRunning"));
                        }
                    }
                    catch
                    {
                        // No chrome window found
                        TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("PlexIsNotRunning"));
                    }
                }
            }
        }
    }
}

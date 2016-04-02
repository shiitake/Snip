using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Winter
{
    using System;
    using System.Diagnostics;

    internal sealed class YouTube : MediaPlayer
    {
        private const string YoutubeRecogPattern = "- YouTube";
        private Process[] Processes;

        public YouTube()
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
            if(this.Processes.Length > 0)
            {
                foreach(Process proc in this.Processes)
                {
                    if (proc.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    try {
                        this.SetTabs(proc);

                        if (this.tabs != null)
                        {
                            foreach (AutomationElement tab in this.tabs)
                            {
                                try {
                                    string tabTitle = tab.GetCurrentPropertyValue(AutomationElement.NameProperty).ToString();

                                    AutomationElement next = tab.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                                    
                                    if (tabTitle.LastIndexOf(YoutubeRecogPattern, StringComparison.OrdinalIgnoreCase) > 0)
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
                                            string videoTitle = tabTitle.Substring(0, tabTitle.Length - YoutubeRecogPattern.Length);

                                            if (videoTitle.LastIndexOf("-", StringComparison.OrdinalIgnoreCase) > 0)
                                            {
                                                // Assuming the video title is "Artist - Song"
                                                string artist = videoTitle.Split('-').First().Trim();
                                                string song = videoTitle.Split('-').Last().Trim();

                                                TextHandler.UpdateText(song, artist);
                                            }
                                            else
                                            {
                                                TextHandler.UpdateText(videoTitle);
                                            }

                                            this.LastTitle = tabTitle;
                                        }

                                        // Since we've found a music playing tab there's no need to look for additional tabs.
                                        break;
                                    }
                                } catch { }
                            }
                        }
                        else
                        {
                            // No tabs found
                            TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("YouTubeIsNotRunning"));
                        }
                    }
                    catch
                    {
                        // No chrome window found
                        TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("YouTubeIsNotRunning"));
                    }
                }
            }
        }
    }

}

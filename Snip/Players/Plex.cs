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
        AutomationPropertyChangedEventHandler propChangeHandler;
        private AutomationElementCollection tabs = null;
        private AutomationElement musicTab = null;

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

        public void SubscribePropertyChange(AutomationElement element)
        {
            Automation.AddAutomationPropertyChangedEventHandler(element,
                TreeScope.Element,
                propChangeHandler = new AutomationPropertyChangedEventHandler(OnPropertyChange),
                AutomationElement.NameProperty);

        }

        private void OnPropertyChange(object src, AutomationPropertyChangedEventArgs e)
        {
            AutomationElement sourceElement = src as AutomationElement;
            if (e.Property == AutomationElement.NameProperty)
            {
                GetSongInfo(sourceElement);
            }
        }

        public void UnsubscribePropertyChange(AutomationElement element)
        {
            if (propChangeHandler != null)
            {
                Automation.RemoveAutomationPropertyChangedEventHandler(element, propChangeHandler);
            }
        }

        public void GetMusicTab()
        {
            if (this.Processes.Length <= 0) return;
            foreach (Process proc in Processes.Where(p => p.MainWindowHandle != IntPtr.Zero))
            {
                try
                {
                    SetTabs(proc);
                    if (tabs != null)
                    {
                        foreach (AutomationElement tab in tabs)
                        {
                            string tabTitle = tab.GetCurrentPropertyValue(AutomationElement.NameProperty).ToString();
                            var next = tab.FindFirst(TreeScope.Children,
                                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                            string nextContent =
                                next.GetCurrentPropertyValue(AutomationElement.NameProperty).ToString();
                            if (tabTitle.LastIndexOf(PlexRecogPattern) == 0)
                            {
                                if (next != null && nextContent != "")
                                {
                                    break;
                                }
                                musicTab = tab;
                                GetSongInfo(musicTab);
                                break;
                            }
                        }
                    }
                    else
                    {
                        TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("PlexIsNotRunning"));
                    }
                }
                catch
                {
                    TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("PlexIsNotRunning"));
                }
            }
        }

        private void GetSongInfo(AutomationElement tab)
        {
            string tabTitle = tab.GetCurrentPropertyValue(AutomationElement.NameProperty).ToString();
            if (tabTitle == LastTitle) return;
            string songTitle = tabTitle.Substring(2, tabTitle.Length - 2);

            if (songTitle.LastIndexOf("-", StringComparison.OrdinalIgnoreCase) > 0)
            {
                // Assuming the title is "Artist - Song"
                string artist = songTitle.Split('-').First().Trim();
                string song = songTitle.Split('-').Last().Trim();

                TextHandler.UpdateText(song, artist);
            }
            else
            {
                TextHandler.UpdateText(songTitle);
            }
            LastTitle = tabTitle;
        }

        public override void Update()
        {
            if (musicTab != null && propChangeHandler != null) return;
            if (musicTab == null) GetMusicTab();
            if (propChangeHandler == null && musicTab != null ) SubscribePropertyChange(musicTab);
            if (musicTab == null || propChangeHandler == null)
            {
                TextHandler.UpdateTextAndEmptyFilesMaybe(Globals.ResourceManager.GetString("PlexIsNotRunning"));
            }
        }

        public override void Unload()
        {
            base.Unload();
            if (musicTab != null)
            {
                UnsubscribePropertyChange(musicTab);
            }
        }
    }
}
